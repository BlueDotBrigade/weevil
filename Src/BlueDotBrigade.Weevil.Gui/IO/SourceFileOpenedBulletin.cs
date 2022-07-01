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
	}
}
