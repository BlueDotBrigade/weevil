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

		private readonly ActiveRecord _activeRecord;
		private ImmutableArray<INavigator> _navigators;

		private TableOfContents _tableOfContents;

		public NavigationManager(string sourceFilePath, ICoreExtension coreExtension, ImmutableArray<IRecord> allRecords, TableOfContents tableOfContents)
		{
			_sourceFilePath = sourceFilePath;
			_coreCoreExtension = coreExtension;
			_tableOfContents = tableOfContents;

			_activeRecord = new ActiveRecord(allRecords);

			_navigators = new List<INavigator>
			{
				new LineNumberNavigator(_activeRecord),
				new TimestampNavigator(_activeRecord),
				new TextNavigator(_activeRecord),
				new PinNavigator(_activeRecord),
				new FlagNavigator(_activeRecord),
			}.ToImmutableArray();
		}

		public T Using<T>() where T : INavigator
		{
			return _navigators.OfType<T>().First();
		}

		public TableOfContents TableOfContents => _tableOfContents;

		ITableOfContents INavigate.TableOfContents => _tableOfContents;

		internal void SetActiveLineNumber(int lineNumber)
		{
			var index = _activeRecord.DataSource.IndexOfLineNumber(lineNumber);
			_activeRecord.SetActiveIndex(index);
		}

		internal void UpdateDataSource(ImmutableArray<IRecord> filterResults)
		{
			_activeRecord.UpdateDataSource(filterResults);
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
