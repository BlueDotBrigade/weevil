namespace BlueDotBrigade.Windows.Dialogs.Storage
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Compression;
	using System.Linq;

	/// <summary>
	/// What a folder or zip holds, or why it could not be read.
	/// </summary>
	public sealed class ListingResult
	{
		public ListingResult(Location location, IReadOnlyList<BrowserItem> items)
		{
			this.Location = location;
			this.Items = items;
		}

		public ListingResult(Location location, string error)
		{
			this.Location = location;
			this.Items = Array.Empty<BrowserItem>();
			this.Error = error;
		}

		public Location Location { get; }

		public IReadOnlyList<BrowserItem> Items { get; }

		/// <summary>
		/// Why the place could not be listed, for the analyst: <see langword="null"/> when it was.
		/// </summary>
		public string Error { get; }
	}

	/// <summary>
	/// Lists and reads the drives, folders on disk, and zips to any depth, without extracting a zip to list it.
	/// </summary>
	/// <remarks>
	/// Thread-safe: the browser lists from a background thread, and a newer listing may start before an older one ends.
	/// A zip inside a zip is copied to a temporary folder the first time it is read, because a zip can only be read from a stream that can seek.
	/// <see cref="Dispose"/> deletes those copies; files given by <see cref="Extract"/> are the caller's to delete.
	/// </remarks>
	public sealed class ContainerReader : IDisposable
	{
		// What a cloud file such as OneDrive's sets when its contents are not on this computer.
		private const FileAttributes RecallOnOpen = (FileAttributes)0x40000;
		private const FileAttributes RecallOnDataAccess = (FileAttributes)0x400000;
		private const FileAttributes OnlineOnly = RecallOnOpen | RecallOnDataAccess | FileAttributes.Offline;

		private readonly HashSet<string> _containerExtensions;
		private readonly object _sync = new object();
		private readonly Dictionary<string, string> _innerZipCopies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private string _copyFolder;

		/// <param name="containerExtensions">The extensions of the files the browser enters like folders: <c>.zip</c> by default.</param>
		public ContainerReader(IEnumerable<string> containerExtensions = null)
		{
			_containerExtensions = new HashSet<string>(
				(containerExtensions ?? new[] { ".zip" }).Select(extension => extension.StartsWith('.') ? extension : $".{extension}"),
				StringComparer.OrdinalIgnoreCase);
		}

		public bool IsContainerName(string name) => _containerExtensions.Contains(Path.GetExtension(name));

		public ListingResult List(Location location)
		{
			ArgumentNullException.ThrowIfNull(location);

			try
			{
				if (location.IsComputer)
				{
					return new ListingResult(location, ListDrives());
				}

				return location.IsInArchive
					? ListArchiveFolder(location)
					: new ListingResult(location, ListDirectory(location.FileSystemPath));
			}
			catch (Exception exception) when (IsReadFailure(exception))
			{
				return new ListingResult(location, Describe(exception));
			}
		}

		/// <summary>
		/// Says what a typed path names, or <see langword="null"/> when it names nothing.
		/// </summary>
		public ItemKind? GetKind(Location location)
		{
			ArgumentNullException.ThrowIfNull(location);

			try
			{
				if (location.IsComputer)
				{
					return ItemKind.Folder;
				}

				if (!location.IsInArchive)
				{
					return Directory.Exists(location.FileSystemPath) ? ItemKind.Folder
						: File.Exists(location.FileSystemPath) ? (IsContainerName(location.FileSystemPath) ? ItemKind.Container : ItemKind.File)
						: null;
				}

				var last = location.ArchivePath[^1];

				using ZipArchive archive = OpenArchive(location, location.ArchivePath.Count - 1);

				if (last.Length == 0)
				{
					return ItemKind.Container;
				}

				if (FindEntry(archive, last) is not null)
				{
					return IsContainerName(last) ? ItemKind.Container : ItemKind.File;
				}

				var folderPrefix = $"{last}/";
				return archive.Entries.Any(entry => Location.Normalize(entry.FullName).Equals(last, StringComparison.OrdinalIgnoreCase) ||
				                                    Location.Normalize(entry.FullName).StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase))
					? ItemKind.Folder
					: null;
			}
			catch (Exception exception) when (IsReadFailure(exception))
			{
				return null;
			}
		}

		/// <summary>
		/// Says whether a file exists, on disk or in a zip, for example a log's sidecar beside it.
		/// </summary>
		public bool Exists(Location file)
		{
			ArgumentNullException.ThrowIfNull(file);

			if (file.IsComputer)
			{
				return false;
			}

			if (!file.IsInArchive)
			{
				return File.Exists(file.FileSystemPath);
			}

			if (file.ArchivePath[^1].Length == 0)
			{
				return false;
			}

			try
			{
				using ZipArchive archive = OpenArchive(file, file.ArchivePath.Count - 1);
				return FindEntry(archive, file.ArchivePath[^1]) is not null;
			}
			catch (Exception exception) when (IsReadFailure(exception))
			{
				return false;
			}
		}

		/// <summary>
		/// The file called <paramref name="name"/> in the same folder or zip folder as <paramref name="file"/>.
		/// </summary>
		public static Location GetSibling(Location file, string name) => file.GetParent().Combine(name, entersContainer: false);

		/// <summary>
		/// Opens a file for reading, on disk or in a zip. The caller disposes the stream.
		/// </summary>
		/// <remarks>
		/// A file on disk is shared for reading, writing and deletion, so the application writing a log is never blocked.
		/// </remarks>
		public Stream OpenRead(Location file)
		{
			ArgumentNullException.ThrowIfNull(file);

			if (!file.IsInArchive)
			{
				return OpenFile(file.FileSystemPath);
			}

			ZipArchive archive = OpenArchive(file, file.ArchivePath.Count - 1);

			try
			{
				ZipArchiveEntry entry = FindEntry(archive, file.ArchivePath[^1]) ?? throw new FileNotFoundException($"The zip holds no file called `{file.Name}`.");
				return new EntryStream(entry.Open(), archive);
			}
			catch
			{
				archive.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Gives the path of a file on disk: a file on disk as it is, and a file in a zip copied out to <paramref name="destinationFolder"/>,
		/// with the companions beside it in the zip that exist, such as <c>App.log.xml</c> for <c>App.log</c>.
		/// </summary>
		/// <param name="companionSuffixes">What a companion's name adds to the file's, such as <c>.xml</c>.</param>
		public string Extract(Location file, IEnumerable<string> companionSuffixes, string destinationFolder)
		{
			ArgumentNullException.ThrowIfNull(file);

			if (!file.IsInArchive)
			{
				return file.FileSystemPath;
			}

			Directory.CreateDirectory(destinationFolder);

			var extracted = Path.Combine(destinationFolder, file.Name);
			Copy(file, extracted);

			foreach (var suffix in companionSuffixes ?? Enumerable.Empty<string>())
			{
				Location companion = GetSibling(file, file.Name + suffix);

				if (Exists(companion))
				{
					Copy(companion, extracted + suffix);
				}
			}

			return extracted;
		}

		public void Dispose()
		{
			lock (_sync)
			{
				_innerZipCopies.Clear();

				if (_copyFolder is not null && Directory.Exists(_copyFolder))
				{
					try
					{
						Directory.Delete(_copyFolder, recursive: true);
					}
					catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
					{
						// A copy still open elsewhere stays in the temporary folder.
					}
				}

				_copyFolder = null;
			}
		}

		private static List<BrowserItem> ListDrives()
		{
			var items = new List<BrowserItem>();

			foreach (DriveInfo drive in DriveInfo.GetDrives())
			{
				var root = drive.RootDirectory.FullName;
				var letter = root.TrimEnd('\\', '/');
				string label = null;

				try
				{
					// A drive that is not ready, such as an empty card reader, has no label.
					if (drive.IsReady)
					{
						label = drive.VolumeLabel;
					}
				}
				catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
				{
					// Listed without a label.
				}

				var kind = drive.DriveType == DriveType.Network ? "Network drive" : "Local disk";
				items.Add(new BrowserItem(Location.ForFileSystem(root), $"{(string.IsNullOrEmpty(label) ? kind : label)} ({letter})", ItemKind.Drive));
			}

			return items;
		}

		/// <remarks>
		/// .NET would list a folder it cannot read as empty, and skip hidden items, which hides ProgramData and AppData, where many logs live.
		/// So reading the folder fails loudly, and only items both hidden and system are skipped, as Explorer does.
		/// </remarks>
		private List<BrowserItem> ListDirectory(string path)
		{
			var options = new EnumerationOptions
			{
				IgnoreInaccessible = false,
				AttributesToSkip = 0,
				RecurseSubdirectories = false,
				ReturnSpecialDirectories = false,
			};

			var items = new List<BrowserItem>();

			foreach (FileSystemInfo info in new DirectoryInfo(path).EnumerateFileSystemInfos("*", options))
			{
				FileAttributes attributes = info.Attributes;

				if ((attributes & (FileAttributes.Hidden | FileAttributes.System)) == (FileAttributes.Hidden | FileAttributes.System))
				{
					continue;
				}

				Location location = Location.ForFileSystem(info.FullName);

				items.Add(info is FileInfo file
					? new BrowserItem(location, file.Name, IsContainerName(file.Name) ? ItemKind.Container : ItemKind.File, file.Length, file.LastWriteTime, (attributes & OnlineOnly) != 0)
					: new BrowserItem(location, info.Name, ItemKind.Folder, modified: info.LastWriteTime));
			}

			return items;
		}

		/// <remarks>
		/// A zip need not list its folders, so a folder is also known from the paths of the files inside it.
		/// </remarks>
		private ListingResult ListArchiveFolder(Location location)
		{
			using ZipArchive archive = OpenArchive(location, location.ArchivePath.Count - 1);

			var folder = location.ArchivePath[^1];
			var prefix = folder.Length == 0 ? string.Empty : $"{folder}/";
			var isFound = folder.Length == 0;
			var folders = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);
			var files = new List<BrowserItem>();

			foreach (ZipArchiveEntry entry in archive.Entries)
			{
				var name = Location.Normalize(entry.FullName);
				var isFolderEntry = entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\');

				if (name.Length == 0)
				{
					continue;
				}

				if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				{
					isFound |= name.Equals(folder, StringComparison.OrdinalIgnoreCase);
					continue;
				}

				isFound = true;
				var rest = name.Substring(prefix.Length);
				var slash = rest.IndexOf('/');

				if (slash >= 0)
				{
					folders.TryAdd(rest.Substring(0, slash), null);
				}
				else if (isFolderEntry)
				{
					folders[rest] = entry.LastWriteTime.LocalDateTime;
				}
				else
				{
					files.Add(new BrowserItem(location.Combine(rest, entersContainer: false), rest, IsContainerName(rest) ? ItemKind.Container : ItemKind.File, entry.Length, entry.LastWriteTime.LocalDateTime));
				}
			}

			if (!isFound)
			{
				return new ListingResult(location, "This folder is not in the zip.");
			}

			var items = folders
				.Select(pair => new BrowserItem(location.Combine(pair.Key, entersContainer: false), pair.Key, ItemKind.Folder, modified: pair.Value))
				.Concat(files)
				.ToList();

			return new ListingResult(location, items);
		}

		/// <summary>
		/// Opens the zip at <paramref name="level"/> of the location's path: 0 is the zip on disk, 1 the zip inside it, and so on.
		/// </summary>
		private ZipArchive OpenArchive(Location location, int level)
		{
			var path = level == 0 ? location.FileSystemPath : GetInnerZipCopy(location, level);
			return new ZipArchive(OpenFile(path), ZipArchiveMode.Read, leaveOpen: false);
		}

		private string GetInnerZipCopy(Location location, int level)
		{
			// The zip on disk's time is part of the key, so a zip replaced while the browser is open is read again.
			var key = $"{location.FileSystemPath}|{File.GetLastWriteTimeUtc(location.FileSystemPath).Ticks}|{string.Join("|", location.ArchivePath.Take(level))}";

			lock (_sync)
			{
				if (_innerZipCopies.TryGetValue(key, out var existing) && File.Exists(existing))
				{
					return existing;
				}
			}

			using ZipArchive outer = OpenArchive(location, level - 1);
			ZipArchiveEntry entry = FindEntry(outer, location.ArchivePath[level - 1])
				?? throw new FileNotFoundException($"The zip holds no zip called `{location.ArchivePath[level - 1]}`.");

			string copy;

			lock (_sync)
			{
				_copyFolder ??= Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "BlueDotBrigade.OpenAs", Guid.NewGuid().ToString("N"))).FullName;
				copy = Path.Combine(_copyFolder, $"{Guid.NewGuid():N}.zip");
			}

			using (Stream source = entry.Open())
			using (FileStream target = File.Create(copy))
			{
				source.CopyTo(target);
			}

			lock (_sync)
			{
				_innerZipCopies[key] = copy;
			}

			return copy;
		}

		private void Copy(Location file, string destinationPath)
		{
			using Stream source = OpenRead(file);
			using FileStream target = File.Create(destinationPath);
			source.CopyTo(target);
		}

		/// <remarks>
		/// Ignores case and accepts either slash, unlike <see cref="ZipArchive.GetEntry"/>, since zips made on Windows use both.
		/// </remarks>
		private static ZipArchiveEntry FindEntry(ZipArchive archive, string entryPath)
		{
			return archive.Entries.FirstOrDefault(entry =>
				!entry.FullName.EndsWith('/') && !entry.FullName.EndsWith('\\') &&
				Location.Normalize(entry.FullName).Equals(entryPath, StringComparison.OrdinalIgnoreCase));
		}

		private static FileStream OpenFile(string path)
		{
			return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		}

		private static bool IsReadFailure(Exception exception)
		{
			return exception is IOException or UnauthorizedAccessException or InvalidDataException or System.Security.SecurityException;
		}

		private static string Describe(Exception exception) => exception switch
		{
			UnauthorizedAccessException or System.Security.SecurityException => "You do not have permission to open this folder.",
			DirectoryNotFoundException => "This folder does not exist.",
			FileNotFoundException => "This file does not exist.",
			InvalidDataException => "This is not a zip, or it is damaged.",
			_ => exception.Message,
		};

		/// <summary>
		/// A file read from a zip, which closes the zip when it is closed.
		/// </summary>
		private sealed class EntryStream : Stream
		{
			private readonly Stream _entry;
			private readonly ZipArchive _archive;

			public EntryStream(Stream entry, ZipArchive archive)
			{
				_entry = entry;
				_archive = archive;
			}

			public override bool CanRead => true;

			public override bool CanSeek => false;

			public override bool CanWrite => false;

			public override long Length => throw new NotSupportedException();

			public override long Position
			{
				get => throw new NotSupportedException();
				set => throw new NotSupportedException();
			}

			public override int Read(byte[] buffer, int offset, int count) => _entry.Read(buffer, offset, count);

			public override int Read(Span<byte> buffer) => _entry.Read(buffer);

			public override void Flush()
			{
				// Nothing is written.
			}

			public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

			public override void SetLength(long value) => throw new NotSupportedException();

			public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					_entry.Dispose();
					_archive.Dispose();
				}

				base.Dispose(disposing);
			}
		}
	}
}
