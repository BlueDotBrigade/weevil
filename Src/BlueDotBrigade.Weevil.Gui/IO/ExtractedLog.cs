namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.IO;
	using BlueDotBrigade.Weevil.Diagnostics;
	using LogInUseException = BlueDotBrigade.Weevil.IO.LogInUseException;

	/// <summary>
	/// A log Weevil extracted from a zip, kept while it is open (#1162), to a folder of its own that is kept between opens (#1253).
	/// </summary>
	/// <remarks>
	/// Only the extracted log is deleted. Its sidecar (<c>.xml</c>) and saved index (<c>.idx</c>) stay beside it, so the comments, pins and bookmarks
	/// added to it are back, and it reopens without being parsed, when the same file is opened from the same zip again.
	/// Exports, reports and splits saved beside the log stay too.
	/// </remarks>
	internal sealed class ExtractedLog
	{
		/// <summary>
		/// What a companion's name adds to the log's: its sidecar and saved index travel with it out of a zip, unless kept ones are already there.
		/// </summary>
		public static readonly string[] CompanionSuffixes = { ".xml", ".idx" };

		/// <summary>
		/// Where a log opened from a zip is extracted, to a folder of its own named by the zip, the log's entry, and its CRC-32 and size (#1253).
		/// </summary>
		/// <remarks>
		/// Not the temporary folder, where Weevil saves neither a sidecar nor an index. Nothing cleans it up, and its paths stay short.
		/// </remarks>
		public static readonly string DefaultCopiesFolder = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"BlueDotBrigade",
			"Weevil",
			"Zips");

		private readonly (long Length, DateTime LastWriteUtc) _stamp;

		public ExtractedLog(string logPath)
		{
			this.LogPath = Path.GetFullPath(logPath ?? throw new ArgumentNullException(nameof(logPath)));
			_stamp = GetStamp(this.LogPath);
		}

		/// <summary>
		/// The extracted log.
		/// </summary>
		public string LogPath { get; }

		/// <summary>
		/// Returns <see langword="true"/> when <paramref name="path"/> is the log Weevil extracted, so deleting this copy would delete it.
		/// </summary>
		public bool Contains(string path)
		{
			return string.Equals(Path.GetFullPath(path), this.LogPath, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Deletes the extracted log, and nothing else.
		/// </summary>
		/// <remarks>
		/// A log written since it was extracted, for example one an export replaced, is the user's and stays.
		/// So is a copy another Weevil has open, for example after an open here was refused (#1254): two instances share the copy of the same file of the same zip.
		/// </remarks>
		public void Delete()
		{
			try
			{
				if (File.Exists(this.LogPath) && GetStamp(this.LogPath) == _stamp)
				{
					Engine.ThrowIfInUse(this.LogPath);
					File.Delete(this.LogPath);
				}
			}
			catch (LogInUseException)
			{
				Log.Default.Write(LogSeverityType.Information, $"A log extracted from a zip is kept: another Weevil has it open. Path={this.LogPath}");
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				// A leftover copy does no harm, so it must not stop Weevil. The next open of the same file extracts it again over this one.
				Log.Default.Write(LogSeverityType.Warning, e, $"Unable to delete a log extracted from a zip. Path={this.LogPath}");
			}
		}

		private static (long Length, DateTime LastWriteUtc) GetStamp(string file)
		{
			var info = new FileInfo(file);

			// A file gone since extraction, for example one antivirus removed, matches no file that appears later.
			return info.Exists ? (info.Length, info.LastWriteTimeUtc) : (-1, DateTime.MinValue);
		}
	}
}
