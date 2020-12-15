namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.IO;
	using BlueDotBrigade.IO;
	using Data;

	[DebuggerDisplay("ActiveIndex={_pinNavigator.ActiveIndex}")]
	internal class NavigationManager : INavigate
	{
		private readonly string _sourceFilePath;
		private readonly ICoreExtension _coreCoreExtension;
		private readonly ImmutableArray<IRecord> _allRecords;
		private readonly PinNavigator _pinNavigator;

		private TableOfContents _tableOfContents;

		public NavigationManager(string sourceFilePath, ICoreExtension coreExtension, ImmutableArray<IRecord> allRecords, TableOfContents tableOfContents)
		{
			_sourceFilePath = sourceFilePath;
			_coreCoreExtension = coreExtension;
			_allRecords = allRecords;
			_pinNavigator = new PinNavigator(allRecords);
			_tableOfContents = tableOfContents;
		}

		public IPinNavigator Pinned => _pinNavigator;

		public TableOfContents TableOfContents => _tableOfContents;

		ITableOfContents INavigate.TableOfContents => _tableOfContents;

		internal void SetActiveRecord(int lineNumber)
		{
			_pinNavigator.SetActiveRecord(lineNumber);
		}

		internal void UpdateDataSource(ImmutableArray<IRecord> records)
		{
			_pinNavigator.UpdateDataSource(records);
		}

		public INavigate RebuildTableOfContents()
		{
			if (File.Exists(_sourceFilePath))
			{
				using (FileStream sourceFileStream = FileHelper.Open(_sourceFilePath))
				{
					using (var sourceReader = new StreamReader(sourceFileStream))
					{
						_tableOfContents = _coreCoreExtension.BuildTableOfContents(sourceReader);
					}
				}
			}

			return this;
		}
	}
}
