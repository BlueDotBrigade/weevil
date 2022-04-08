namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;

	internal class FileChangedBulletin
	{
		public FileChangedBulletin(string sourceFilePath, ContextDictionary context, int totalRecordCount, bool totalRecordCountChanged, TimeSpan sourceFileLoadingPeriod)
		{
			this.SourceFilePath = sourceFilePath;
			this.Context = context;
			this.TotalRecordCount = totalRecordCount;
			this.TotalRecordCountChanged = totalRecordCountChanged;
			this.SourceFileLoadingPeriod = sourceFileLoadingPeriod;
		}

		public string SourceFilePath { get; }

		public ContextDictionary Context { get; }

		public int TotalRecordCount { get; }

		public bool TotalRecordCountChanged { get; }

		public TimeSpan SourceFileLoadingPeriod { get; }
	}
}
