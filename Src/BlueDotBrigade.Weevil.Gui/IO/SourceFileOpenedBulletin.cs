namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;

	internal class SourceFileOpenedBulletin
	{
		public SourceFileOpenedBulletin()
		{
			this.SourceFilePath = string.Empty;
			this.SourceFileLoadingPeriod = TimeSpan.Zero;
			this.Context = ContextDictionary.Empty;
			this.TotalRecordCount = 0;
		}

		/// <summary>
		/// The log as the analyst knows it: for a log opened from a zip, the zip read like a folder, which is not a file (#1312).
		/// </summary>
		public string SourceFilePath { get; init; }

		/// <summary>
		/// The size of the file read, in bytes: <see cref="SourceFilePath"/> may not be a file to measure.
		/// </summary>
		public long SourceFileSize { get; init; }

		public ContextDictionary Context { get; init; }

		public int TotalRecordCount { get; init; }

		public TimeSpan SourceFileLoadingPeriod { get; init; }

		/// <summary>
		/// Whether the log's text is kept in RAM or read back from the log on disk (#1028).
		/// </summary>
		public BlueDotBrigade.Weevil.IO.StorageMode StorageMode { get; init; }

		/// <summary>
		/// "RAM" or "Disk", as the status bar shows it, or nothing before a log opens.
		/// </summary>
		public string StorageModeName => string.IsNullOrEmpty(this.SourceFilePath)
			? string.Empty
			: this.StorageMode == BlueDotBrigade.Weevil.IO.StorageMode.Disk ? "Disk" : "RAM";

		/// <summary>
		/// The part of the log that is open (#1219).
		/// </summary>
		public BlueDotBrigade.Weevil.IO.LogPart Part { get; init; } = BlueDotBrigade.Weevil.IO.LogPart.Whole;

		/// <summary>
		/// The part of the log that is open, as the status bar shows it, so the analyst doesn't mistake it for the whole log:
		/// nothing for the whole log, or before a log opens.
		/// </summary>
		public string PartName
		{
			get
			{
				BlueDotBrigade.Weevil.IO.LogPart part = this.Part ?? BlueDotBrigade.Weevil.IO.LogPart.Whole;

				return string.IsNullOrEmpty(this.SourceFilePath) || part.IsWhole ? string.Empty
					: part.SectionName is not null ? $"Section: {part.SectionName}"
					: part.RegionName is not null ? $"Region: {part.RegionName}"
					: part.IsToEndOfLog ? $"From line {part.Lines.Start.Value:N0}"
					: $"Lines {part.Lines.Start.Value:N0} – {part.Lines.End.Value:N0}";
			}
		}
	}
}
