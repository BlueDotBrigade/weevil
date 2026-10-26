namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Text;
	using BlueDotBrigade.Weevil.Collections.Generic;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Test;
	using Configuration.Sidecar;
	using Data;
	using Diagnostics;
	using Navigation;

	internal partial class CoreEngine
	{
		/// <summary>
		/// Facilitates the creation of a new <see cref="CoreEngine"/> object through a configurable Fluent API.
		/// </summary>
		/// <remarks>
		/// The builder design pattern greatly simplifies the <see cref="CoreEngine"/> constructors,
		/// while centralizing object instantiation in one location.
		/// </remarks>
		/// <seealso href="https://refactoring.guru/design-patterns/builder">Refactoring Guru: Builder Design Pattern</seealso>
		/// <seealso href="https://code-maze.com/builder-design-pattern/">Builder Design Pattern and Fluent Builder</seealso>
		/// <seealso href="https://dev.to/alialp/builder-design-pattern-for-objects-with-nested-properties-5g3c">Builder design pattern for objects with nested properties</seealso>
		/// <seealso href="https://stackoverflow.com/questions/38480410/builder-pattern-with-nested-objects">StackOverflow: Builder pattern with nested objects</seealso>
		internal class CoreEngineBuilder
		{
			private const int NotSet = -1;

			private static readonly Version LatestVersion = new Version(512, 512, 512, 512);
			public const int FirstRecordLineNumber = 1;

			private readonly string _sourceFilePath;
			private readonly CoreEngine _sourceInstance;
			private readonly int _startAtLineNumber;

			private readonly ClearOperation _clearOperation;
			private readonly bool _hasBeenCleared;

			private bool _isUsingUserDefinedContext;
			private ContextDictionary _userDefinedContext;

			/// <summary>
			/// Represents the maximum number of records that will be loaded.
			/// </summary>
			private int _maxRecords = int.MaxValue;

			/// <summary>
			/// Represents the line numbers that will be loaded.
			/// </summary>
			private Range _range = Range.All;

			private Session _session;

			private RecordState _recordState;

			/// <summary>
			/// Records semantic usage metrics. Defaults to a no-op recorder when the host does not supply one.
			/// </summary>
			private ITelemetryMetricRecorder _telemetryRecorder = NullTelemetryMetricRecorder.Instance;

			internal CoreEngineBuilder(string sourceFilePath) : this(sourceFilePath, FirstRecordLineNumber)
			{
				// nothing to do
			}

			internal CoreEngineBuilder(string sourceFilePath, int startAtLineNumber)
			{
				_sourceFilePath = sourceFilePath;

				_startAtLineNumber = startAtLineNumber;
				_hasBeenCleared = false;

				_userDefinedContext = new ContextDictionary();

				var message = string.Format(
					$"Core engine construction will read records from the provided file. {nameof(sourceFilePath)}={0}, {nameof(startAtLineNumber)}={1}",
					sourceFilePath,
					startAtLineNumber);

				Log.Default.Write(LogSeverityType.Debug, message);
			}

			internal CoreEngineBuilder(CoreEngine source, ClearOperation clearOperation)
			{
				_sourceInstance = source;
				_clearOperation = clearOperation;
				_hasBeenCleared = true;

				// A cleared/derived engine keeps recording to the same telemetry session as its source.
				_telemetryRecorder = source._telemetryRecorder;

				_startAtLineNumber = NotSet;

				Log.Default.Write(
					LogSeverityType.Debug,
					$"Core engine construction will read records from an existing instance. OriginalFilename={Path.GetFileName(source.SourceFilePath)}, Operation={clearOperation}");
			}

			public CoreEngineBuilder UsingTelemetry(ITelemetryMetricRecorder recorder)
			{
				_telemetryRecorder = recorder ?? NullTelemetryMetricRecorder.Instance;
				return this;
			}

			public CoreEngineBuilder UsingContext(ContextDictionary context)
			{
				_isUsingUserDefinedContext = true;

				_userDefinedContext = context;

				Log.Default.Write(
					LogSeverityType.Debug,
					$"Core engine construction has has been given a specific context. Parameters={context.Count}");

				return this;
			}

			/// <summary>
			/// During the loading process the total number of records will be limited to the specified value.
			/// </summary>
			/// <param name="maxRecords">Represents the maximum number of records that can be returned.</param>
			public CoreEngineBuilder UsingLimit(int maxRecords)
			{
				_maxRecords = maxRecords > 0
					? maxRecords
					: throw new ArgumentOutOfRangeException(nameof(maxRecords),
						"Value is expected to be greater than or equal to zero.");

				Log.Default.Write(
					LogSeverityType.Debug,
					$"Core engine construction has a maximum record limit. Value={maxRecords}");

				return this;
			}

			/// <summary>
			/// Keeps the session and the flags of an engine this one replaces, as Reload does (#1013).
			/// The sidecar's comments and pins are then not loaded again.
			/// </summary>
			public CoreEngineBuilder UsingStateOf(CoreEngine previous)
			{
				if (previous == null)
				{
					throw new ArgumentNullException(nameof(previous));
				}

				_session = previous.Session;
				_recordState = previous.RecordState;

				return this;
			}

			/// <summary>
			/// During the loading process the records whose line number are within range will be loaded.
			/// </summary>
			/// <param name="range">Represents the line numbers that will be loaded.</param>
			public CoreEngineBuilder UsingRange(Range range)
			{
				_range = range;

				Log.Default.Write(
					LogSeverityType.Debug,
					$"Core engine construction has a maximum record limit. Range={range}");

				return this;
			}

			private bool UseExistingInstance => _sourceInstance != null;

			/// <summary>
			/// Creates an instance of <see cref="CoreEngine"/> based on the provided parameters.
			/// </summary>
			public CoreEngine Build()
			{
				Log.Default.Write(
					LogSeverityType.Debug,
					"Core engine is being constructed.");

				var sourceFileRemarks = string.Empty;

				string sourceFilePath;
				long sourceFileLength;
				Encoding sourceFileEncoding;
				Stopwatch loadingStopwatch = new Stopwatch();

				SidecarManager sidecarManager = null;
				ICoreExtension coreExtension = null;
				LineIndex lineIndex;

				var maxRecords = _maxRecords;
				ImmutableArray<IRecord> records;
				ImmutableArray<IRecord> selectedRecords = ImmutableArray<IRecord>.Empty;

				var knownContext = new ContextDictionary();

				Session session;
				RecordState recordState;

				var inclusiveFilterHistory = new List<string>();
				var exclusiveFilterHistory = new List<string>();
				var tableOfContents = new List<Section>();
				var regions = new List<Region>();
				var bookmarks = new List<Bookmark>();

				if (this.UseExistingInstance)
				{
					sourceFilePath = _sourceInstance.SourceFilePath;
					sourceFileLength = _sourceInstance._logFileMetrics.FileSize;
					sourceFileEncoding = _sourceInstance._sourceFileEncoding;

					knownContext = _sourceInstance._context;
					sourceFileRemarks = _sourceInstance._sourceFileRemarks;

					tableOfContents = _sourceInstance.Navigate.TableOfContents.Sections.ToList();
					regions.AddRange(_sourceInstance._regionManager.Regions);
					bookmarks.AddRange(_sourceInstance._bookmarkManager.Bookmarks);

					inclusiveFilterHistory.AddRange(_sourceInstance.Filter.IncludeHistory);
					exclusiveFilterHistory.AddRange(_sourceInstance.Filter.ExcludeHistory);

					coreExtension = _sourceInstance._coreExtension;
					sidecarManager = _sourceInstance._sidecarManager;
					lineIndex = _sourceInstance._lineIndex;

					// The source's session and view state carry over, with its records.
					session = _sourceInstance._session;
					recordState = _sourceInstance._recordState;

					selectedRecords = _sourceInstance._selectionManager.GetSelected();

					var repository = new InMemoryRecordRepository(
						_sourceInstance._allRecords,
						_sourceInstance._filterManager.Results,
						selectedRecords,
						_clearOperation,
						regions.ToImmutableArray());

					records = repository.Get(maxRecords);

					selectedRecords = GetVisibleRecordSelection(records, selectedRecords);
				}
				else
				{
					sourceFileEncoding = EncodingHelper.GetEncoding(FileHelper.Open(_sourceFilePath));

					using (FileStream dataSource = FileHelper.Open(_sourceFilePath))
					{
						sourceFilePath = _sourceFilePath;
						sourceFileLength = dataSource.Length;

						IPlugin plugin = new PluginFactory().Create(sourceFilePath);
						coreExtension = plugin.GetExtension(sourceFilePath);

						sidecarManager = new SidecarManager(_sourceFilePath);

						loadingStopwatch.Restart();

						lineIndex = LineIndexFile.Load(_sourceFilePath, dataSource);

						// Created before parsing, so they age with the records: attaching then stores no young reference
						// in every record, which would slow the next garbage collections (#1013).
						var isKeepingAnnotations = _session != null;
						session = _session ?? new Session(new AnnotationStore(lineIndex.LineCount));
						recordState = _recordState ?? new RecordState(session.Annotations, new FlagSet(lineIndex.LineCount));

						var startAtLineNumber = _startAtLineNumber >= 1 ? _startAtLineNumber : FirstRecordLineNumber;
						Range lineNumberRange = GetLineNumberRange();

						// Reading starts at the start line's byte offset, taken from the line index (#977).
						// A start line beyond the end of the log reads from the top, which finds no records to keep.
						var repository = startAtLineNumber > FirstRecordLineNumber && startAtLineNumber <= lineIndex.LineCount
							? new SerializedRecordRepository(
								 dataSource,
								 coreExtension.GetRecordParser(),
								 lineIndex.GetByteOffset(dataSource, startAtLineNumber),
								 startAtLineNumber,
								 true)
							: new SerializedRecordRepository(
								 dataSource,
								 coreExtension.GetRecordParser(),
								 GetMaximumLinesToSearch(lineNumberRange));

						records = repository.Get(lineNumberRange, maxRecords);

						loadingStopwatch.Stop();

						sidecarManager.Load(
							records,
							isKeepingAnnotations ? null : session.Annotations,
							out knownContext,
							out sourceFileRemarks,
							out inclusiveFilterHistory,
							out exclusiveFilterHistory,
							out tableOfContents,
							out regions,
							out bookmarks);

						if (inclusiveFilterHistory.Count == 0)
						{
							inclusiveFilterHistory.AddRange(plugin.GetDefaultInclusiveHistory());
						}

						if (exclusiveFilterHistory.Count == 0)
						{
							exclusiveFilterHistory.AddRange(plugin.GetDefaultExclusiveHistory());
						}
					}
				}

				ContextDictionary context = GetContext(coreExtension, knownContext, records);

				var coreEngine = new CoreEngine(
					sourceFilePath,
					sourceFileLength,
					sourceFileEncoding,
					loadingStopwatch.Elapsed,
					coreExtension,
					context,
					sourceFileRemarks,
					sidecarManager,
					lineIndex,
					records,
					session,
					recordState,
					_hasBeenCleared,
					new TableOfContents(tableOfContents),
					regions.ToImmutableArray(),
					bookmarks.ToImmutableArray(),
					_telemetryRecorder);

				if (inclusiveFilterHistory.Count > 0)
				{
					coreEngine.Filter.IncludeHistory.Clear();
					coreEngine.Filter.IncludeHistory.AddRange(inclusiveFilterHistory);
				}

				if (exclusiveFilterHistory.Count > 0)
				{
					coreEngine.Filter.ExcludeHistory.Clear();
					coreEngine.Filter.ExcludeHistory.AddRange(exclusiveFilterHistory);
				}

				coreEngine.Selector.Select(selectedRecords);

				return coreEngine;
			}

			/// <summary>
			/// Returns the line numbers to load, combining the start line with the requested range.
			/// </summary>
			private Range GetLineNumberRange()
			{
				if (_startAtLineNumber <= FirstRecordLineNumber)
				{
					return _range;
				}

				if (_range.Equals(Range.All))
				{
					return new Range(_startAtLineNumber, int.MaxValue);
				}

				return new Range(System.Math.Max(_range.Start.Value, _startAtLineNumber), _range.End);
			}

			/// <summary>
			/// Returns the number of lines that can be read while searching for the first record.
			/// </summary>
			/// <remarks>
			/// Reading starts at the top of the file, so the search must be able to reach the start line,
			/// plus the usual allowance for lines that do not start a record.
			/// </remarks>
			private static int GetMaximumLinesToSearch(Range lineNumberRange)
			{
				long linesBeforeStart = System.Math.Max(lineNumberRange.Start.Value, FirstRecordLineNumber) - 1;

				return (int)System.Math.Min(linesBeforeStart + MultilineRecordParser.MaximumLinesToSearch, int.MaxValue);
			}

			private ImmutableArray<IRecord> GetVisibleRecordSelection(ImmutableArray<IRecord> visibleRecords, ImmutableArray<IRecord>  selectedRecords)
			{
				var result = new List<IRecord>();

				foreach (IRecord record in selectedRecords)
				{
					if (visibleRecords.TryRecordOfLineNumber(record.LineNumber, out _ ))
					{
						result.Add(record);
					}
				}

				return result.ToImmutableArray();
			}

			private ContextDictionary GetContext(ICoreExtension extension, ContextDictionary knownContext, ImmutableArray<IRecord> records)
			{
				ContextDictionary result = ContextDictionary.Empty;

				if (_isUsingUserDefinedContext)
				{
					result = _userDefinedContext;
				}
				else
				{
					if (knownContext.Count > 0)
					{
						result = knownContext;
					}
					else
					{
						result = extension.DetermineContext(records);
					}
				}

				return result;
			}
		}
	}
}