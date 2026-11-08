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
		public ExtractedLog(string folder, IReadOnlyList<string> files)
		{
			this.Folder = folder ?? throw new ArgumentNullException(nameof(folder));
			this.Files = files ?? throw new ArgumentNullException(nameof(files));
		}

		public string Folder { get; }

		public IReadOnlyList<string> Files { get; }

		/// <summary>
		/// Deletes the extracted files, then the folders they leave empty.
		/// </summary>
		public void Delete()
		{
			try
			{
				if (!Directory.Exists(this.Folder))
				{
					return;
				}

				foreach (var file in this.Files.Where(IsInFolder))
				{
					File.Delete(file);
				}

				// The zip's own folders, deepest first, then the folder Weevil extracted to.
				IEnumerable<string> folders = Directory
					.GetDirectories(this.Folder, "*", SearchOption.AllDirectories)
					.OrderByDescending(folder => folder.Length)
					.Append(this.Folder);

				foreach (var folder in folders)
				{
					if (!Directory.EnumerateFileSystemEntries(folder).Any())
					{
						Directory.Delete(folder);
					}
				}
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				// A leftover temporary file or folder does no harm, so it must not stop Weevil.
				Log.Default.Write(LogSeverityType.Warning, e, $"Unable to delete the files extracted from a zip. Path={this.Folder}");
			}
		}

		/// <summary>
		/// Never anything outside the folder Weevil extracted to (#1156).
		/// </summary>
		private bool IsInFolder(string path)
		{
			return Path.GetFullPath(path).StartsWith(Path.GetFullPath(this.Folder) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
		}
	}
}
