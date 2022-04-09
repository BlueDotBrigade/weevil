namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;

	internal class SourceFileOpenedBulletin
	{
		public SourceFileOpenedBulletin(string sourceFilePath, TimeSpan sourceFileLoadingPeriod, ContextDictionary context, int totalRecordCount)
		{
			this.SourceFilePath = sourceFilePath;
			this.SourceFileLoadingPeriod = sourceFileLoadingPeriod;
			this.Context = context;
			this.TotalRecordCount = totalRecordCount;
		}

		public string SourceFilePath { get; }

		public ContextDictionary Context { get; }

		public int TotalRecordCount { get; }

		public TimeSpan SourceFileLoadingPeriod { get; }
	}
}
