namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
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

		public NavigationManager(string sourceFilePath, ICoreExtension coreExtension, ImmutableArray<IRecord> allRecords, TableOfContents tableOfContents, ITelemetryMetricRecorder telemetryRecorder = null)
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
				new PinNavigator(_activeRecord),
				new CommentNavigator(_activeRecord),
				new FlagNavigator(_activeRecord),
				new ElapsedTimeNavigator(_activeRecord),
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

		/// <summary>
		/// Runs a search and records how long it took, so a shared application log shows whether a search blocks the UI (#1072, #1105).
		/// </summary>
		/// <remarks>
		/// The text searched for is not recorded, because it may be sensitive.
		/// A search that finds nothing, whether it throws <see cref="RecordNotFoundException"/> or returns <see cref="Record.Dummy"/>, is recorded as not found.
		/// </remarks>
		private IRecord Search(string search, bool useRegex, Func<IRecord> find)
		{
			var startedAt = Stopwatch.GetTimestamp();
			IRecord record = Record.Dummy;

			try
			{
				record = find();
				return record;
			}
			finally
			{
				Log.Default.Write(
					LogSeverityType.Information,
					"Search complete.",
					new Dictionary<string, object>
					{
						{ "Search", search },
						{ "UseRegex", useRegex },
						{ "Duration", Stopwatch.GetElapsedTime(startedAt).TotalSeconds },
						{ "WasFound", Record.IsGenuine(record) },
						{ "Records", _activeRecord.DataSource.Length },
					});
			}
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

			return Search(nameof(PreviousContent), useRegex, () => this
				.Using<IContentNavigator>()
				.FindPrevious(text, isCaseSensitive, useRegex));
		}

		public IRecord NextContent(string text, bool isCaseSensitive, bool useRegex = false)
		{
			if (text == null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			_telemetryRecorder.Increment(TelemetryMetrics.NavigationFindNextContent);

			return Search(nameof(NextContent), useRegex, () => this
				.Using<IContentNavigator>()
				.FindNext(text, isCaseSensitive, useRegex));
		}

		public IRecord PreviousPin()
		{
			return Search(nameof(PreviousPin), false, () => this
				.Using<IPinNavigator>()
				.FindPrevious());
		}

		public IRecord NextPin()
		{
			return Search(nameof(NextPin), false, () => this
				.Using<IPinNavigator>()
				.FindNext());
		}

		public IRecord PreviousComment()
		{
			return Search(nameof(PreviousComment), false, () => this
				.Using<ICommentNavigator>()
				.FindPrevious());
		}

		public IRecord NextComment()
		{
			return Search(nameof(NextComment), false, () => this
				.Using<ICommentNavigator>()
				.FindNext());
		}

		public IRecord PreviousCommentWithText(string text, bool isCaseSensitive, bool useRegex = false)
		{
			if (text == null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			return Search(nameof(PreviousCommentWithText), useRegex, () => this
				.Using<ICommentNavigator>()
				.FindPrevious(text, isCaseSensitive, useRegex));
		}

		public IRecord NextCommentWithText(string text, bool isCaseSensitive, bool useRegex = false)
		{
			if (text == null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			return Search(nameof(NextCommentWithText), useRegex, () => this
				.Using<ICommentNavigator>()
				.FindNext(text, isCaseSensitive, useRegex));
		}

		public IRecord PreviousFlag()
		{
			return Search(nameof(PreviousFlag), false, () => this
				.Using<IFlagNavigator>()
				.FindPrevious());
		}

		public IRecord NextFlag()
		{
			return Search(nameof(NextFlag), false, () => this
				.Using<IFlagNavigator>()
				.FindNext());
		}

		public IRecord PreviousElapsedTime(int? minMilliseconds, int? maxMilliseconds)
		{
			return Search(nameof(PreviousElapsedTime), false, () => this
				.Using<IElapsedTimeNavigator>()
				.FindPrevious(minMilliseconds, maxMilliseconds));
		}

		public IRecord NextElapsedTime(int? minMilliseconds, int? maxMilliseconds)
		{
			return Search(nameof(NextElapsedTime), false, () => this
				.Using<IElapsedTimeNavigator>()
				.FindNext(minMilliseconds, maxMilliseconds));
		}
	}
}
