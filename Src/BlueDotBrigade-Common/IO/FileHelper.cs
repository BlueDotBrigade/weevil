namespace BlueDotBrigade.IO
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Threading;

	public sealed class FileHelper
	{
		private static readonly TimeSpan DelayBetweenAttempts = TimeSpan.FromMilliseconds(100);
		private static readonly TimeSpan TryOpenPeriod = TimeSpan.FromSeconds(2);

		/// <summary>Opens a read-only <see cref="T:System.IO.FileStream" /> on the specified path.</summary>
		/// <param name="path">The file to open.</param>
		/// <returns>An unshared <see cref="T:System.IO.FileStream" /> that provides access to the specified file, with the specified mode and access.</returns>
		public static FileStream Open(string path)
		{
			// FileShare.Read is not good enough, you need to use `FileShare.ReadWrite` as indicated by:
			// ... https://stackoverflow.com/a/12942773/94968
			return Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		}

		/// <summary>Opens a <see cref="T:System.IO.FileStream" /> on the specified path, with the specified mode and access with no sharing.</summary>
		/// <param name="path">The file to open.</param>
		/// <param name="mode">A <see cref="T:System.IO.FileMode" /> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
		/// <param name="access">A <see cref="T:System.IO.FileAccess" /> value that specifies the operations that can be performed on the file.</param>
		/// <returns>An unshared <see cref="T:System.IO.FileStream" /> that provides access to the specified file, with the specified mode and access.</returns>
		/// <exception cref="T:System.ArgumentException">
		/// <paramref name="path" /> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="F:System.IO.Path.InvalidPathChars" />.
		/// -or-
		/// <paramref name="access" /> specified <see langword="Read" /> and <paramref name="mode" /> specified <see langword="Create" />, <see langword="CreateNew" />, <see langword="Truncate" />, or <see langword="Append" />.</exception>
		/// <exception cref="T:System.ArgumentNullException">
		/// <paramref name="path" /> is <see langword="null" />.</exception>
		/// <exception cref="T:System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
		/// <exception cref="T:System.IO.DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive).</exception>
		/// <exception cref="T:System.IO.IOException">An I/O error occurred while opening the file.</exception>
		/// <exception cref="T:System.UnauthorizedAccessException">
		/// <paramref name="path" /> specified a file that is read-only and <paramref name="access" /> is not <see langword="Read" />.
		/// -or-
		/// <paramref name="path" /> specified a directory.
		/// -or-
		/// The caller does not have the required permission.
		/// -or-
		/// <paramref name="mode" /> is <see cref="F:System.IO.FileMode.Create" /> and the specified file is a hidden file.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// <paramref name="mode" /> or <paramref name="access" /> specified an invalid value.</exception>
		/// <exception cref="T:System.IO.FileNotFoundException">The file specified in <paramref name="path" /> was not found.</exception>
		/// <exception cref="T:System.NotSupportedException">
		/// <paramref name="path" /> is in an invalid format.</exception>
		public static FileStream Open(string path, FileMode mode, FileAccess access)
		{
			return Open(path, mode, access, FileShare.None);
		}

		/// <summary>Initializes a new instance of the <see cref="T:System.IO.FileStream" /> class with the specified path, creation mode, read/write permission, and sharing permission.</summary>
		/// <param name="path">A relative or absolute path for the file that the current <see langword="FileStream" /> object will encapsulate.</param>
		/// <param name="mode">A constant that determines how to open or create the file.</param>
		/// <param name="access">A constant that determines how the file can be accessed by the <see langword="FileStream" /> object. This also determines the values returned by the <see cref="P:System.IO.FileStream.CanRead" /> and <see cref="P:System.IO.FileStream.CanWrite" /> properties of the <see langword="FileStream" /> object. <see cref="P:System.IO.FileStream.CanSeek" /> is <see langword="true" /> if <paramref name="path" /> specifies a disk file.</param>
		/// <param name="share">A constant that determines how the file will be shared by processes.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// <paramref name="path" /> is <see langword="null" />.</exception>
		/// <exception cref="T:System.ArgumentException">
		///         <paramref name="path" /> is an empty string (""), contains only white space, or contains one or more invalid characters.
		/// -or-
		/// <paramref name="path" /> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc. in an NTFS environment.</exception>
		/// <exception cref="T:System.NotSupportedException">
		/// <paramref name="path" /> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc. in a non-NTFS environment.</exception>
		/// <exception cref="T:System.IO.FileNotFoundException">The file cannot be found, such as when <paramref name="mode" /> is <see langword="FileMode.Truncate" /> or <see langword="FileMode.Open" />, and the file specified by <paramref name="path" /> does not exist. The file must already exist in these modes.</exception>
		/// <exception cref="T:System.IO.IOException">An I/O error, such as specifying <see langword="FileMode.CreateNew" /> when the file specified by <paramref name="path" /> already exists, occurred.
		/// -or-
		/// The system is running Windows 98 or Windows 98 Second Edition and <paramref name="share" /> is set to <see langword="FileShare.Delete" />.
		/// -or-
		/// The stream has been closed.</exception>
		/// <exception cref="T:System.Security.SecurityException">The caller does not have the required permission.</exception>
		/// <exception cref="T:System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
		/// <exception cref="T:System.UnauthorizedAccessException">The <paramref name="access" /> requested is not permitted by the operating system for the specified <paramref name="path" />, such as when <paramref name="access" /> is <see langword="Write" /> or <see langword="ReadWrite" /> and the file or directory is set for read-only access.</exception>
		/// <exception cref="T:System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// <paramref name="mode" /> contains an invalid value.</exception>
		public static FileStream Open(string path, FileMode mode, FileAccess access, FileShare share)
		{
			FileStream stream = null;
			Exception capturedException = new IOException($"An unknown problem has occurred while trying to open the requested file. Path={path}");

			DateTime startedAt = DateTime.Now;

			do
			{
				try
				{
					stream = File.Open(path, mode, access, share);
				}
				catch (IOException e)
				{
					Debug.WriteLine(e.Message);
					capturedException = e;
				}

				if (stream == null)
				{
					Thread.Sleep(DelayBetweenAttempts);
				}
			} while (stream == null && DateTime.Now - startedAt < TryOpenPeriod);

			if (stream == null)
			{
				throw capturedException;
			}

			return stream;
		}
	}
}
