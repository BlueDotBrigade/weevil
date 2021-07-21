namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using File = System.IO.File;

	internal class NavigationManager : INavigate
	{
		private readonly string _sourceFilePath;
		private readonly ICoreExtension _coreCoreExtension;

		private readonly RecordNavigator _recordNavigator;
		private ImmutableArray<INavigator> _navigators;

		private TableOfContents _tableOfContents;

		public NavigationManager(string sourceFilePath, ICoreExtension coreExtension, ImmutableArray<IRecord> allRecords, TableOfContents tableOfContents)
		{
			_sourceFilePath = sourceFilePath;
			_coreCoreExtension = coreExtension;
			_tableOfContents = tableOfContents;

			_recordNavigator = new RecordNavigator(allRecords);

			_navigators = new List<INavigator>
			{
				new LineNumberNavigator(_recordNavigator),
				new TimestampNavigator(_recordNavigator),
				new TextNavigator(_recordNavigator),
				new PinNavigator(_recordNavigator),
			}.ToImmutableArray();
		}

		public T By<T>() where T : INavigator
		{
			return _navigators.OfType<T>().First();
		}

		public TableOfContents TableOfContents => _tableOfContents;

		ITableOfContents INavigate.TableOfContents => _tableOfContents;

		internal void SetActiveRecord(int lineNumber)
		{
			_recordNavigator.SetActiveRecord(lineNumber);
		}

		internal void UpdateDataSource(ImmutableArray<IRecord> filterResults)
		{
			_recordNavigator.UpdateDataSource(filterResults);
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
