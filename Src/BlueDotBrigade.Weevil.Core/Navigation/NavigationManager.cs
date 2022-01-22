namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
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
				new ContentNavigator(_activeRecord),
				new PinNavigator(_activeRecord),
				new CommentNavigator(_activeRecord),
				new FlagNavigator(_activeRecord),
			}.ToImmutableArray();
		}

		public TableOfContents TableOfContents => _tableOfContents;

		ITableOfContents INavigate.TableOfContents => _tableOfContents;

		/// <summary>
		/// Sets the focus of the navigation manager to the specified <paramref name="lineNumber"/>.
		/// </summary>
		/// <exception cref="RecordNotFoundException"/>
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

		private T Using<T>() where T : INavigator
		{
			return _navigators.OfType<T>().First();
		}

		public IRecord GoTo(int lineNumber, RecordSearchType recordSearchType)
		{
			return this
				.Using<ILineNumberNavigator>()
				.Find(lineNumber, recordSearchType);
		}

		public IRecord GoTo(string timestamp, RecordSearchType recordSearchType)
		{
			return this
				.Using<ITimestampNavigator>()
				.Find(timestamp, recordSearchType);
		}

		public IRecord PreviousContent(string text, bool isCaseSensitive)
		{
			if (text == null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			return this
				.Using<IContentNavigator>()
				.FindPrevious(text, isCaseSensitive);
		}

		public IRecord NextContent(string text, bool isCaseSensitive)
		{
			if (text == null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			return this
				.Using<IContentNavigator>()
				.FindNext(text, isCaseSensitive);
		}

		public IRecord PreviousPin()
		{
			return this
				.Using<IPinNavigator>()
				.FindPrevious();
		}

		public IRecord NextPin()
		{
			return this
				.Using<IPinNavigator>()
				.FindNext();
		}

		public IRecord PreviousComment()
		{
			return this
				.Using<ICommentNavigator>()
				.FindPrevious();
		}

		public IRecord NextComment()
		{
			return this
				.Using<ICommentNavigator>()
				.FindNext();
		}

		public IRecord PreviousFlag()
		{
			return this
				.Using<IFlagNavigator>()
				.FindPrevious();
		}

		public IRecord NextFlag()
		{
			return this
				.Using<IFlagNavigator>()
				.FindNext();
		}
	}
}
