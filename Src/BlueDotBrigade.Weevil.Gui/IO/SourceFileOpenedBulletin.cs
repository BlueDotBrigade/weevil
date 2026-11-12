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

		public string SourceFilePath { get; init; }

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
	}
}
