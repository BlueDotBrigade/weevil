namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Diagnostics;

	/// <summary>
	/// The files Weevil extracted from a zip to a folder of its own, kept while the log opened from them is open (#1162).
	/// </summary>
	/// <remarks>
	/// Only what Weevil extracted is deleted: exports, reports and splits saved beside the open log stay.
	/// </remarks>
	internal sealed class ExtractedLog
	{
		/// <summary>
		/// Each extracted file's size and last write when it was extracted.
		/// </summary>
		private readonly Dictionary<string, (long Length, DateTime LastWriteUtc)> _files;

		public ExtractedLog(string folder, IEnumerable<string> files)
		{
			this.Folder = folder ?? throw new ArgumentNullException(nameof(folder));

			_files = (files ?? throw new ArgumentNullException(nameof(files)))
				.Select(Path.GetFullPath)
				.ToDictionary(file => file, GetStamp, StringComparer.OrdinalIgnoreCase);
		}

		public string Folder { get; }

		/// <summary>
		/// Returns <see langword="true"/> when Weevil extracted <paramref name="path"/>, so deleting this copy would delete it.
		/// </summary>
		public bool Contains(string path)
		{
			return _files.ContainsKey(Path.GetFullPath(path));
		}

		/// <summary>
		/// Deletes the extracted files, then the folders they leave empty.
		/// </summary>
		/// <remarks>
		/// A file written since it was extracted, for example an export that replaced it, is the user's and stays.
		/// </remarks>
		public void Delete()
		{
			if (!Directory.Exists(this.Folder))
			{
				return;
			}

			foreach (var (file, stamp) in _files)
			{
				if (IsInFolder(file))
				{
					TryDelete(file, () =>
					{
						if (File.Exists(file) && GetStamp(file) == stamp)
						{
							File.Delete(file);
						}
					});
				}
			}

			TryDelete(this.Folder, () =>
			{
				// The zip's own folders, deepest first, then the folder Weevil extracted to.
				IEnumerable<string> folders = Directory
					.GetDirectories(this.Folder, "*", SearchOption.AllDirectories)
					.OrderByDescending(folder => folder.Length)
					.Append(this.Folder);

				foreach (var folder in folders)
				{
					TryDelete(folder, () =>
					{
						if (!Directory.EnumerateFileSystemEntries(folder).Any())
						{
							Directory.Delete(folder);
						}
					});
				}
			});
		}

		private static (long Length, DateTime LastWriteUtc) GetStamp(string file)
		{
			var info = new FileInfo(file);
			return (info.Length, info.LastWriteTimeUtc);
		}

		/// <summary>
		/// Never anything outside the folder Weevil extracted to (#1156).
		/// </summary>
		private bool IsInFolder(string path)
		{
			return path.StartsWith(Path.GetFullPath(this.Folder) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
		}

		private static void TryDelete(string path, Action delete)
		{
			try
			{
				delete();
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				// A leftover temporary file or folder does no harm, so it must not stop Weevil or the other deletions.
				Log.Default.Write(LogSeverityType.Warning, e, $"Unable to delete a file extracted from a zip. Path={path}");
			}
		}
	}
}
