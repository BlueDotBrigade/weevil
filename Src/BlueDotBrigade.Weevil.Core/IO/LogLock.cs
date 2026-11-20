namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Text;
	using Diagnostics;
	using Microsoft.Win32.SafeHandles;

	/// <summary>
	/// The lock beside an open log, <c>Application.log.lock</c>, that keeps another instance of Weevil from opening the log,
	/// so the two don't overwrite each other's sidecar and saved index (#1254). It records who has the log: the user and the computer.
	/// </summary>
	/// <remarks>
	/// It is held as long as the file is open, and released when the handle closes, even when the process dies, so a file left behind blocks no one.
	/// Disposing it deletes the file. Only one handle holds it, whether the other is in another process or in this one:
	/// - Windows enforces share modes on every handle, and over SMB: the file is held with write access, sharing only reading, so another Weevil can read who holds it,
	///   but can't take it or delete it.
	/// - Elsewhere, Weevil takes an advisory <c>flock</c> on the handle itself, exclusive so no one else holds it, then shared, so another Weevil can read who holds it
	///   while its own exclusive lock still fails. Only Weevil honours it. It is not left to .NET, whose <see cref="FileShare"/> lock
	///   <c>DOTNET_SYSTEM_IO_DISABLEFILELOCKING</c> turns off.
	/// </remarks>
	internal sealed class LogLock : IDisposable
	{
		public const string FileExtension = "lock";

		private const string UserKey = "User=";
		private const string ComputerKey = "Computer=";

		// ERROR_SHARING_VIOLATION and ERROR_LOCK_VIOLATION.
		private const int SharingViolation = unchecked((int)0x80070020);
		private const int LockViolation = unchecked((int)0x80070021);

		// flock(2)
		private const int LockShared = 1;
		private const int LockExclusive = 2;
		private const int LockNonBlocking = 4;

		private readonly FileStream _file;
		private readonly string _lockPath;

		private LogLock(string logPath, string lockPath, FileStream file)
		{
			this.LogPath = logPath;
			_lockPath = lockPath;
			_file = file;
		}

		/// <summary>
		/// The log locked, as a full path.
		/// </summary>
		public string LogPath { get; }

		/// <summary>
		/// Locks the log, so no other instance of Weevil opens it until this lock is disposed.
		/// </summary>
		/// <returns>
		/// The lock, or <see langword="null"/> when none can be created, for example in a folder Weevil can't write to: the log still opens, as Clear still clears there (#1246),
		/// and the application log says why it isn't locked. A log that doesn't exist isn't locked either: opening it fails anyway.
		/// </returns>
		/// <exception cref="LogInUseException">Another instance of Weevil, or another engine in this one, has the log open.</exception>
		public static LogLock Acquire(string logPath)
		{
			if (!System.IO.File.Exists(logPath))
			{
				return null;
			}

			var lockPath = GetLockPath(logPath);
			FileStream file;

			try
			{
				file = Open(lockPath);
			}
			catch (IOException exception) when (IsHeldByAnother(exception))
			{
				throw Refuse(logPath, lockPath, exception);
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DllNotFoundException or EntryPointNotFoundException)
			{
				// Also a C library without flock, as on some Linux distributions.
				Log.Default.Write(
					LogSeverityType.Warning,
					exception,
					$"The log opens without a lock, so another Weevil could open it too: its lock can't be created. Path={lockPath}");
				return null;
			}

			try
			{
				WriteOwner(file);
			}
			catch (IOException exception)
			{
				// Still held: only who holds it can't be read, for example on a full disk.
				Log.Default.Write(LogSeverityType.Warning, exception, $"The log is locked, but who has it open could not be written. Path={lockPath}");
			}

			Log.Default.Write(LogSeverityType.Debug, $"The log is locked. Path={lockPath}");

			return new LogLock(Path.GetFullPath(logPath), lockPath, file);
		}

		/// <summary>
		/// Throws when another instance of Weevil, or another engine in this one, has the log open: for example before a log is extracted from a zip,
		/// so its copy is not extracted for nothing. It creates no lock.
		/// </summary>
		/// <remarks>
		/// A lock that can't be read, for example in a folder Weevil can't read, is not a refusal: the open decides.
		/// </remarks>
		/// <exception cref="LogInUseException">The log is open.</exception>
		public static void ThrowIfHeld(string logPath)
		{
			var lockPath = GetLockPath(logPath);

			if (!System.IO.File.Exists(lockPath))
			{
				return;
			}

			try
			{
				// A lock left by a Weevil that crashed: no one holds it, and releasing it deletes it.
				Release(Open(lockPath), lockPath);
			}
			catch (IOException exception) when (IsHeldByAnother(exception))
			{
				throw Refuse(logPath, lockPath, exception);
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DllNotFoundException or EntryPointNotFoundException)
			{
				Log.Default.Write(LogSeverityType.Debug, $"Whether the log is open elsewhere can't be checked. Path={lockPath}, Reason=`{exception.Message}`");
			}
		}

		/// <summary>
		/// Returns <see langword="true"/> when this is the lock of the log at <paramref name="logPath"/>.
		/// </summary>
		public bool IsFor(string logPath)
		{
			// Windows and macOS ignore case in file names by default; Linux does not.
			StringComparison comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal;

			return string.Equals(Path.GetFullPath(logPath), this.LogPath, comparison);
		}

		/// <summary>
		/// Releases the lock, and deletes its file.
		/// </summary>
		public void Dispose()
		{
			Release(_file, _lockPath);
			Log.Default.Write(LogSeverityType.Debug, $"The log is no longer locked. Path={this.LogPath}");
		}

		private static string GetLockPath(string logPath)
		{
			return $"{logPath}.{FileExtension}";
		}

		/// <summary>
		/// Opens the lock, holding it, and creates it when it isn't there. Release it with <see cref="Release"/>.
		/// </summary>
		/// <remarks>
		/// On Windows, closing the handle deletes the file, also when the process dies. Elsewhere it isn't opened to be deleted on closing:
		/// an open that finds the file held would then delete the holder's file.
		/// </remarks>
		/// <exception cref="IOException">Someone holds it (<see cref="IsHeldByAnother"/>), or it can't be opened or locked.</exception>
		/// <exception cref="DllNotFoundException">The C library can't be loaded.</exception>
		/// <exception cref="EntryPointNotFoundException">The C library has no <c>flock</c>.</exception>
		private static FileStream Open(string lockPath)
		{
			if (OperatingSystem.IsWindows())
			{
				return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, bufferSize: 0, FileOptions.DeleteOnClose);
			}

			// .NET also takes an exclusive flock for FileShare.None, unless DOTNET_SYSTEM_IO_DISABLEFILELOCKING turns it off; taking it again on the same handle changes nothing.
			var file = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, bufferSize: 0);

			try
			{
				var error = Flock(file.SafeFileHandle, LockExclusive | LockNonBlocking);

				if (error != 0)
				{
					// EWOULDBLOCK, when another holds it, is recognised as a refusal by IsHeldByAnother.
					throw new IOException($"The log's lock can't be taken. Path={lockPath}, Error={error}", error);
				}

				// With LOCK_NB, Linux converts the lock without letting another in between.
				error = Flock(file.SafeFileHandle, LockShared | LockNonBlocking);

				if (error != 0)
				{
					// Still held exclusively, which keeps other instances out too: only who holds it can't be read.
					Log.Default.Write(LogSeverityType.Warning, $"The log's lock could not be shared, so who has the log open can't be read. Path={lockPath}, Error={error}");
				}

				return file;
			}
			catch
			{
				file.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Deletes the lock and releases it.
		/// </summary>
		/// <remarks>
		/// Elsewhere than on Windows, the file is deleted while it is still held, so no other Weevil takes it in between. One that opened it in the instant before
		/// could still lock the deleted file once it is released, while a third locks a new one: a race of microseconds between three instances, not handled.
		/// </remarks>
		private static void Release(FileStream file, string lockPath)
		{
			if (!OperatingSystem.IsWindows())
			{
				try
				{
					System.IO.File.Delete(lockPath);
				}
				catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
				{
					// A file left behind blocks no one: the next open takes it over.
					Log.Default.Write(LogSeverityType.Debug, $"The log's lock could not be deleted. Path={lockPath}, Reason=`{exception.Message}`");
				}
			}

			file.Dispose();
		}

		/// <summary>
		/// Returns <see langword="true"/> when the lock could not be taken because someone holds it.
		/// </summary>
		private static bool IsHeldByAnother(IOException exception)
		{
			if (OperatingSystem.IsWindows())
			{
				return exception.HResult is SharingViolation or LockViolation;
			}

			// The errno, from .NET or from Open.
			return exception.GetType() == typeof(IOException) && exception.HResult == WouldBlock;
		}

		private static LogInUseException Refuse(string logPath, string lockPath, IOException reason)
		{
			(string User, string Computer) owner = ReadOwner(lockPath);
			var refusal = new LogInUseException(logPath, owner.User, owner.Computer, reason);

			Log.Default.Write(LogSeverityType.Warning, $"{refusal.Message} Path={logPath}");

			return refusal;
		}

		private static void WriteOwner(FileStream file)
		{
			var owner = $"{UserKey}{Environment.UserName}\n{ComputerKey}{Environment.MachineName}\n";

			file.SetLength(0);
			file.Write(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(owner));
			file.Flush();
		}

		/// <returns>
		/// Who holds the lock, or <see langword="null"/> for what can't be read, for example while the other Weevil is still writing it.
		/// </returns>
		private static (string User, string Computer) ReadOwner(string lockPath)
		{
			string user = null;
			string computer = null;

			try
			{
				using var file = new FileStream(lockPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
				using var reader = new StreamReader(file, Encoding.UTF8);

				while (reader.ReadLine() is { } line)
				{
					if (line.StartsWith(UserKey, StringComparison.Ordinal))
					{
						user = line.Substring(UserKey.Length);
					}
					else if (line.StartsWith(ComputerKey, StringComparison.Ordinal))
					{
						computer = line.Substring(ComputerKey.Length);
					}
				}
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
			{
				Log.Default.Write(LogSeverityType.Debug, $"Who holds the log's lock can't be read. Path={lockPath}, Reason=`{exception.Message}`");
			}

			return (user, computer);
		}

		/// <summary>
		/// EWOULDBLOCK: 35 on macOS and FreeBSD, 11 on Linux.
		/// </summary>
		private static int WouldBlock => OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD() ? 35 : 11;

		/// <returns>0, or the errno.</returns>
		private static int Flock(SafeFileHandle handle, int operation)
		{
			var isReferenced = false;

			try
			{
				// Kept open while flock uses its descriptor.
				handle.DangerousAddRef(ref isReferenced);

				return FlockNative(handle.DangerousGetHandle().ToInt32(), operation) == 0 ? 0 : Marshal.GetLastPInvokeError();
			}
			finally
			{
				if (isReferenced)
				{
					handle.DangerousRelease();
				}
			}
		}

		[DllImport("libc", EntryPoint = "flock", SetLastError = true)]
		private static extern int FlockNative(int fileDescriptor, int operation);
	}
}
