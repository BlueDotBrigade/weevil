namespace BlueDotBrigade.Weevil.Gui.Filter
{
	internal class FileChangedBulletin
	{
		public FileChangedBulletin(string sourceFilePath, ContextDictionary context, int totalRecordCount, bool totalRecordCountChanged)
		{
			this.SourceFilePath = sourceFilePath;
			this.Context = context;
			this.TotalRecordCount = totalRecordCount;
			this.TotalRecordCountChanged = totalRecordCountChanged;
		}

		public string SourceFilePath { get; }

		public ContextDictionary Context { get; }

		public int TotalRecordCount { get; }

		public bool TotalRecordCountChanged { get; }
	}
}
