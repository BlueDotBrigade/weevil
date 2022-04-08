namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;

	internal class FileChangedBulletin
	{
		public FileChangedBulletin(string sourceFilePath, ContextDictionary context, int totalRecordCount, bool totalRecordCountChanged, TimeSpan recordLoadingPeriod)
		{
			this.SourceFilePath = sourceFilePath;
			this.Context = context;
			this.TotalRecordCount = totalRecordCount;
			this.TotalRecordCountChanged = totalRecordCountChanged;
			this.RecordLoadingPeriod = recordLoadingPeriod;
		}

		public string SourceFilePath { get; }

		public ContextDictionary Context { get; }

		public int TotalRecordCount { get; }

		public bool TotalRecordCountChanged { get; }

		public TimeSpan RecordLoadingPeriod { get; }
	}
}
