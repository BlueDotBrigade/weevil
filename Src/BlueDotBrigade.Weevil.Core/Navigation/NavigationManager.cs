namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Diagnostics;
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

		private readonly ITelemetryMetricRecorder _telemetryRecorder;

		public NavigationManager(
			string sourceFilePath,
			ICoreExtension coreExtension,
			ImmutableArray<IRecord> allRecords,
			TableOfContents tableOfContents,
			IAnnotations annotations,
			IFlags flags,
			Func<ElapsedTimes> getElapsedTimes,
			ITelemetryMetricRecorder telemetryRecorder = null)
		{
			_sourceFilePath = sourceFilePath;
			_coreCoreExtension = coreExtension;
			_tableOfContents = tableOfContents;
			_telemetryRecorder = telemetryRecorder ?? NullTelemetryMetricRecorder.Instance;

			_activeRecord = new ActiveRecord(allRecords);

			_navigators = new List<INavigator>
			{
				new LineNumberNavigator(_activeRecord),
				new TimestampNavigator(_activeRecord),
				new ContentNavigator(_activeRecord),
				new PinNavigator(_activeRecord, annotations),
				new CommentNavigator(_activeRecord, annotations),
				new FlagNavigator(_activeRecord, flags),
				new ElapsedTimeNavigator(_activeRecord, getElapsedTimes),
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

		/// <summary>
		/// The line number of the active record, or -1 when there is none.
		/// </summary>
		internal int ActiveLineNumber
		{
			get
			{
				ImmutableArray<IRecord> dataSource = _activeRecord.DataSource;
				var index = _activeRecord.Index;

				return index >= 0 && index < dataSource.Length ? dataSource[index].LineNumber : -1;
			}
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
			_telemetryRecorder.Increment(TelemetryMetrics.NavigationGoToLine);

			return this
				.Using<ILineNumberNavigator>()
				.Find(lineNumber, recordSearchType);
		}

		public IRecord GoTo(string timestamp, RecordSearchType recordSearchType)
		{
			_telemetryRecorder.Increment(TelemetryMetrics.NavigationGoToTimestamp);

			return this
				.Using<ITimestampNavigator>()
				.Find(timestamp, recordSearchType);
		}

		public IRecord PreviousContent(string text, bool isCaseSensitive, bool useRegex = false)
		{
			if (text == null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			_telemetryRecorder.Increment(TelemetryMetrics.NavigationFindPreviousContent);

			return this
				.Using<IContentNavigator>()
				.FindPrevious(text, isCaseSensitive, useRegex);
		}

		public IRecord NextContent(string text, bool isCaseSensitive, bool useRegex = false)
		{
			if (text == null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			_telemetryRecorder.Increment(TelemetryMetrics.NavigationFindNextContent);

			return this
				.Using<IContentNavigator>()
				.FindNext(text, isCaseSensitive, useRegex);
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

		public IRecord PreviousCommentWithText(string text, bool isCaseSensitive, bool useRegex = false)
		{
			if (text == null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			return this
				.Using<ICommentNavigator>()
				.FindPrevious(text, isCaseSensitive, useRegex);
		}

		public IRecord NextCommentWithText(string text, bool isCaseSensitive, bool useRegex = false)
		{
			if (text == null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			return this
				.Using<ICommentNavigator>()
				.FindNext(text, isCaseSensitive, useRegex);
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

		public IRecord PreviousElapsedTime(int? minMilliseconds, int? maxMilliseconds)
		{
			return this
				.Using<IElapsedTimeNavigator>()
				.FindPrevious(minMilliseconds, maxMilliseconds);
		}

		public IRecord NextElapsedTime(int? minMilliseconds, int? maxMilliseconds)
		{
			return this
				.Using<IElapsedTimeNavigator>()
				.FindNext(minMilliseconds, maxMilliseconds);
		}
	}
}
