namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;

	internal class SourceFileOpendBulletin
	{
		public SourceFileOpendBulletin(string sourceFilePath, ContextDictionary context, int totalRecordCount, TimeSpan sourceFileLoadingPeriod)
		{
			this.SourceFilePath = sourceFilePath;
			this.Context = context;
			this.TotalRecordCount = totalRecordCount;
			this.SourceFileLoadingPeriod = sourceFileLoadingPeriod;
		}

		public string SourceFilePath { get; }

		public ContextDictionary Context { get; }

		public int TotalRecordCount { get; }

		public TimeSpan SourceFileLoadingPeriod { get; }
	}
}
