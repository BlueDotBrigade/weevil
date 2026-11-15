namespace BlueDotBrigade.Weevil.Windows.Dialogs.Storage
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	/// <summary>
	/// A place the browser shows, or a file it selects: on disk, or inside a zip, inside any number of zips.
	/// </summary>
	/// <remarks>
	/// <see cref="ArchivePath"/> is empty for a place on disk. Inside a zip, <see cref="FileSystemPath"/> is the outermost zip,
	/// and each element of <see cref="ArchivePath"/> is a path inside the zip before it, with <c>/</c> between folders:
	/// every element but the last names a zip, and the last is a folder or a file, or empty for the zip's top.
	/// For example, <c>C:\Logs.zip\2026\Old.zip\App.log</c> is <c>C:\Logs.zip</c> then <c>2026/Old.zip</c> then <c>App.log</c>.
	/// </remarks>
	public sealed class Location : IEquatable<Location>
	{
		/// <summary>
		/// The computer itself, which lists the drives.
		/// </summary>
		public static readonly Location Computer = new Location(null, Array.Empty<string>());

		private Location(string fileSystemPath, IReadOnlyList<string> archivePath)
		{
			this.FileSystemPath = fileSystemPath;
			this.ArchivePath = archivePath;
		}

		public string FileSystemPath { get; }

		public IReadOnlyList<string> ArchivePath { get; }

		public bool IsComputer => this.FileSystemPath is null;

		public bool IsInArchive => this.ArchivePath.Count > 0;

		/// <summary>
		/// The last part of the path: a file, folder, zip or drive name.
		/// </summary>
		public string Name
		{
			get
			{
				if (this.IsComputer)
				{
					return "This PC";
				}

				// The last part that names something: for the top of a zip, the zip itself.
				var last = this.ArchivePath.LastOrDefault(segment => segment.Length > 0);

				if (last is null)
				{
					var name = Path.GetFileName(this.FileSystemPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
					return string.IsNullOrEmpty(name) ? this.FileSystemPath : name;
				}

				return last.Substring(last.LastIndexOf('/') + 1);
			}
		}

		public static Location ForFileSystem(string path)
		{
			ArgumentException.ThrowIfNullOrEmpty(path);
			return new Location(path, Array.Empty<string>());
		}

		/// <summary>
		/// The top of the zip at <paramref name="zipPath"/>, on disk.
		/// </summary>
		public static Location ForArchive(string zipPath, params string[] archivePath)
		{
			ArgumentException.ThrowIfNullOrEmpty(zipPath);
			return new Location(zipPath, archivePath.Length == 0 ? new[] { string.Empty } : archivePath.Select(Normalize).ToArray());
		}

		/// <summary>
		/// The item called <paramref name="name"/> in this folder or zip.
		/// </summary>
		/// <param name="entersContainer">The item is a zip, and the location is its top, not the zip as a file.</param>
		public Location Combine(string name, bool entersContainer)
		{
			ArgumentException.ThrowIfNullOrEmpty(name);

			if (this.IsComputer)
			{
				return ForFileSystem(name);
			}

			if (!this.IsInArchive)
			{
				var path = Path.Combine(this.FileSystemPath, name);
				return entersContainer ? new Location(path, new[] { string.Empty }) : ForFileSystem(path);
			}

			var last = this.ArchivePath[^1];
			var entry = last.Length == 0 ? name : $"{last}/{name}";
			var segments = this.ArchivePath.Take(this.ArchivePath.Count - 1).Append(entry);

			return new Location(this.FileSystemPath, (entersContainer ? segments.Append(string.Empty) : segments).ToArray());
		}

		/// <summary>
		/// The top of this zip, which is a file on disk or in another zip.
		/// </summary>
		public Location EnterContainer()
		{
			return new Location(this.FileSystemPath, this.ArchivePath.Append(string.Empty).ToArray());
		}

		/// <summary>
		/// The folder or zip that holds this place: <see cref="Computer"/> above a drive, and <see langword="null"/> above the computer.
		/// </summary>
		public Location GetParent()
		{
			if (this.IsComputer)
			{
				return null;
			}

			if (!this.IsInArchive)
			{
				var parent = Path.GetDirectoryName(this.FileSystemPath);
				return string.IsNullOrEmpty(parent) ? Computer : ForFileSystem(parent);
			}

			var last = this.ArchivePath[^1];
			var outer = this.ArchivePath.Take(this.ArchivePath.Count - 1).ToArray();

			if (last.Length > 0)
			{
				// A folder or file inside the innermost zip: its folder, or the zip's top.
				return new Location(this.FileSystemPath, outer.Append(GetEntryFolder(last)).ToArray());
			}

			if (outer.Length == 0)
			{
				// The top of the outermost zip: the folder on disk that holds the zip.
				var folder = Path.GetDirectoryName(this.FileSystemPath);
				return string.IsNullOrEmpty(folder) ? Computer : ForFileSystem(folder);
			}

			// The top of a zip inside a zip: the folder of the outer zip that holds it.
			var innerZip = outer[^1];
			return new Location(this.FileSystemPath, outer.Take(outer.Length - 1).Append(GetEntryFolder(innerZip)).ToArray());
		}

		/// <summary>
		/// Reads a path typed or pasted by the analyst. Each part that is a zip, on disk or inside a zip, is entered.
		/// </summary>
		/// <param name="isContainerName">Says whether a file name is a zip's, from its extension.</param>
		/// <returns><see langword="null"/> when the text is empty, or no part of it exists on disk.</returns>
		public static Location Parse(string text, Func<string, bool> isContainerName)
		{
			text = text?.Trim().Trim('"');

			if (string.IsNullOrEmpty(text))
			{
				return null;
			}

			if (Directory.Exists(text) || File.Exists(text))
			{
				return File.Exists(text) && isContainerName(text) ? ForArchive(text) : ForFileSystem(text);
			}

			// The longest part on disk must be a zip; the rest is inside it.
			var parts = text.Split(new[] { '\\', '/' }, StringSplitOptions.None);

			for (var count = parts.Length - 1; count > 0; count--)
			{
				var onDisk = string.Join(Path.DirectorySeparatorChar, parts.Take(count));

				if (onDisk.EndsWith(':'))
				{
					onDisk += Path.DirectorySeparatorChar;
				}

				if (!File.Exists(onDisk))
				{
					continue;
				}

				if (!isContainerName(onDisk))
				{
					return null;
				}

				var segments = new List<string>();
				var entry = string.Empty;

				foreach (var part in parts.Skip(count).Where(part => part.Length > 0))
				{
					entry = entry.Length == 0 ? part : $"{entry}/{part}";

					if (isContainerName(part))
					{
						segments.Add(entry);
						entry = string.Empty;
					}
				}

				segments.Add(entry);
				return new Location(onDisk, segments.ToArray());
			}

			return null;
		}

		public bool Equals(Location other)
		{
			return other is not null &&
			       string.Equals(this.FileSystemPath, other.FileSystemPath, StringComparison.OrdinalIgnoreCase) &&
			       this.ArchivePath.SequenceEqual(other.ArchivePath, StringComparer.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj) => Equals(obj as Location);

		public override int GetHashCode()
		{
			var hash = new HashCode();
			hash.Add(this.FileSystemPath, StringComparer.OrdinalIgnoreCase);

			foreach (var segment in this.ArchivePath)
			{
				hash.Add(segment, StringComparer.OrdinalIgnoreCase);
			}

			return hash.ToHashCode();
		}

		/// <summary>
		/// The path as Explorer shows it: a zip reads like a folder.
		/// </summary>
		public override string ToString()
		{
			if (this.IsComputer)
			{
				return this.Name;
			}

			var parts = this.ArchivePath
				.Where(segment => segment.Length > 0)
				.Select(segment => segment.Replace('/', Path.DirectorySeparatorChar));

			return string.Join(Path.DirectorySeparatorChar, parts.Prepend(this.FileSystemPath.TrimEnd(Path.DirectorySeparatorChar)));
		}

		internal static string Normalize(string entryPath) => entryPath.Replace('\\', '/').Trim('/');

		private static string GetEntryFolder(string entryPath)
		{
			var slash = entryPath.LastIndexOf('/');
			return slash < 0 ? string.Empty : entryPath.Substring(0, slash);
		}
	}
}
