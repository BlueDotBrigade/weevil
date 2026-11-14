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
			private static readonly Version LatestVersion = new Version(512, 512, 512, 512);
			public const int FirstRecordLineNumber = 1;

#if DEBUG
			/// <summary>
			/// The storage mode of a builder given none: RAM, or disk when the environment variable <c>WEEVIL_TEST_STORAGE</c> is <c>Disk</c>,
			/// so CI runs the Core tests on disk too, through <c>Tst/Disk.runsettings</c> (#1025). A mode given to the builder, or carried by Reload, wins.
			/// </summary>
			/// <remarks>
			/// A Debug build of the GUI, CLI or PowerShell module started with the variable set opens on disk too.
			/// </remarks>
			internal static readonly StorageMode DefaultStorageMode = string.Equals(Environment.GetEnvironmentVariable("WEEVIL_TEST_STORAGE"), nameof(StorageMode.Disk), StringComparison.OrdinalIgnoreCase)
				? StorageMode.Disk
				: StorageMode.Ram;
#else
			internal static readonly StorageMode DefaultStorageMode = StorageMode.Ram;
#endif

			private readonly string _sourceFilePath;
			private readonly int _startAtLineNumber;

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

			private StorageMode _storageMode = DefaultStorageMode;

			internal CoreEngineBuilder(string sourceFilePath) : this(sourceFilePath, FirstRecordLineNumber)
			{
				// nothing to do
			}

			internal CoreEngineBuilder(string sourceFilePath, int startAtLineNumber)
			{
				_sourceFilePath = sourceFilePath;

				_startAtLineNumber = startAtLineNumber;

				_userDefinedContext = new ContextDictionary();

				var message = string.Format(
					$"Core engine construction will read records from the provided file. {nameof(sourceFilePath)}={0}, {nameof(startAtLineNumber)}={1}",
					sourceFilePath,
					startAtLineNumber);

				Log.Default.Write(LogSeverityType.Debug, message);
			}

			public CoreEngineBuilder UsingTelemetry(ITelemetryMetricRecorder recorder)
			{
				_telemetryRecorder = recorder ?? NullTelemetryMetricRecorder.Instance;
				return this;
			}

			/// <summary>
			/// Keeps each record's content in memory, or reads it back from the log whenever it is needed (#1025).
			/// </summary>
			public CoreEngineBuilder UsingStorage(StorageMode storageMode)
			{
				_storageMode = Enum.IsDefined(storageMode)
					? storageMode
					: throw new ArgumentOutOfRangeException(nameof(storageMode), storageMode, "Value is expected to be a storage mode.");

				Log.Default.Write(
					LogSeverityType.Debug,
					$"Core engine construction has been given a storage mode. Value={storageMode}");

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
			/// Keeps the session, the flags and the telemetry recorder of an engine this one replaces, as Reload does (#1013, #1107, #1185).
			/// The session's state, such as comments, pins, bookmarks, regions and remarks, is then not loaded from the sidecar again.
			/// </summary>
			public CoreEngineBuilder UsingStateOf(CoreEngine previous)
			{
				if (previous == null)
				{
					throw new ArgumentNullException(nameof(previous));
				}

				_session = previous.Session;
				_recordState = previous.RecordState;
				_telemetryRecorder = previous._telemetryRecorder;

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

			/// <summary>
			/// Reopens the log from the record table saved with its index, parsing only what it grew by since (#1023).
			/// </summary>
			/// <remarks>
			/// A method of its own, so the saved table that a grown log's longer table replaces is not kept by <see cref="Build"/>'s frame
			/// when the engine is created, whose collection would otherwise keep it: about 22 bytes per record.
			/// </remarks>
			/// <param name="source">On disk, the source the records read their content back through: otherwise <see langword="null"/>.</param>
			/// <returns><see langword="false"/> when the log must be parsed.</returns>
			private bool TryReopen(FileStream dataSource, LineIndex lineIndex, IPlugin plugin, ICoreExtension coreExtension, out RecordList records, out LogFileSource source)
			{
				records = null;

				if (!LineIndexFile.TryLoadTable(_sourceFilePath, dataSource, lineIndex, plugin, _storageMode, out RecordTable savedTable, out source))
				{
					return false;
				}

				SavedParse savedParse = lineIndex.Parse;

				if (savedParse.EndOffset == dataSource.Length)
				{
					records = new TableRecordList(savedTable);
					return true;
				}

				if (!GrownLogParser.TryParse(dataSource, _sourceFilePath, savedTable, savedParse, coreExtension.GetRecordParser(), _storageMode, lineIndex.ContentEncoding, out RecordTable grownTable, out LogFileSource parsedSource))
				{
					source = null;
					return false;
				}

				// The log grew since it was indexed: only its new part was parsed, and the longer table is saved.
				records = new TableRecordList(grownTable);
				source = _storageMode == StorageMode.Disk ? parsedSource : null;
				LineIndexFile.SaveParse(_sourceFilePath, dataSource, lineIndex, SavedParse.Create(plugin, _storageMode, parsedSource), grownTable);

				return true;
			}

			/// <summary>
			/// Creates an instance of <see cref="CoreEngine"/> based on the provided parameters.
			/// </summary>
			public CoreEngine Build()
			{
				Log.Default.Write(
					LogSeverityType.Debug,
					"Core engine is being constructed.");

				var openingStopwatch = Stopwatch.StartNew();
				var sourceFileRemarks = string.Empty;

				string sourceFilePath;
				long sourceFileLength;
				Encoding sourceFileEncoding;
				Stopwatch loadingStopwatch = new Stopwatch();

				SidecarManager sidecarManager = null;
				ICoreExtension coreExtension = null;
				LineIndex lineIndex;
				LogFileSource source;

				var maxRecords = _maxRecords;
				RecordList records;

				var knownContext = new ContextDictionary();

				Session session;
				RecordState recordState;

				var inclusiveFilterHistory = new List<string>();
				var exclusiveFilterHistory = new List<string>();
				var tableOfContents = new List<Section>();
				var regions = new List<Region>();
				var bookmarks = new List<Bookmark>();

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

					// Sized from the line index, before the records are parsed.
					var isKeepingSession = _session != null;
					session = _session ?? new Session(lineIndex.LineCount);
					recordState = _recordState ?? new RecordState(session.Annotations, new FlagSet(lineIndex.LineCount));

					var startAtLineNumber = _startAtLineNumber >= 1 ? _startAtLineNumber : FirstRecordLineNumber;
					Range lineNumberRange = GetLineNumberRange();

					// A full open, from line 1 with no range and no limit, can reuse the record table saved with the index (#1023).
					var isFullOpen = startAtLineNumber == FirstRecordLineNumber && lineNumberRange.Equals(Range.All) && maxRecords == int.MaxValue;
					records = null;
					source = null;

					if (isFullOpen)
					{
						_ = TryReopen(dataSource, lineIndex, plugin, coreExtension, out records, out source);
					}

					if (records == null)
					{
						// Reading starts at the start line's byte offset, taken from the line index (#977).
						// A start line beyond the end of the log reads from the top, which finds no records to keep.
						// In disk mode, the records read their content back from the log, through a source complete before Get returns (#1025).
						var repository = startAtLineNumber > FirstRecordLineNumber && startAtLineNumber <= lineIndex.LineCount
							? new SerializedRecordRepository(
								 dataSource,
								 coreExtension.GetRecordParser(),
								 lineIndex.GetByteOffset(dataSource, startAtLineNumber),
								 startAtLineNumber,
								 true,
								 storageMode: _storageMode)
							: new SerializedRecordRepository(
								 dataSource,
								 coreExtension.GetRecordParser(),
								 GetMaximumLinesToSearch(lineNumberRange),
								 _storageMode);

						records = repository.Get(lineNumberRange, maxRecords);
						source = repository.Source;

						// After a full parse, the saved index keeps what parsed the log, the checksums of the bytes read and the record table,
						// so the log can be reopened without parsing (#1023). A parse from a start line, of a range or up to a limit keeps only part of it.
						if (isFullOpen && repository.ParsedSource != null && records is TableRecordList { WholeTable: RecordTable parsedTable })
						{
							LineIndexFile.SaveParse(_sourceFilePath, dataSource, lineIndex, SavedParse.Create(plugin, _storageMode, repository.ParsedSource), parsedTable);
						}
					}

					loadingStopwatch.Stop();

					sidecarManager.Load(
						records,
						isKeepingSession ? null : session.Annotations,
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

					// Reload keeps the session's state in memory instead of reading it back from the sidecar (#1107).
					if (!isKeepingSession)
					{
						session.Load(
							sourceFileRemarks,
							new TableOfContents(tableOfContents),
							regions.ToImmutableArray(),
							bookmarks.ToImmutableArray(),
							inclusiveFilterHistory,
							exclusiveFilterHistory);
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
					sidecarManager,
					lineIndex,
					records,
					session,
					recordState,
					_telemetryRecorder,
					_storageMode,
					source,
					hasReadLog: true);

				LogFileLoaded(coreEngine, openingStopwatch.Elapsed);

				return coreEngine;
			}

			/// <summary>
			/// Records what the performance requirements measure once a log has opened (#1084, #1086),
			/// so a shared application log is enough to compare them (#1105).
			/// </summary>
			/// <remarks>
			/// The engine has already collected the memory used while parsing. The memory is the whole process's:
			/// it includes the application, and any engine the caller still holds, such as the one a second open replaces.
			/// Reload lets go of the engine it replaces before parsing (#1126).
			/// </remarks>
			private static void LogFileLoaded(CoreEngine coreEngine, TimeSpan openingDuration)
			{
				LogFileMetrics metrics = coreEngine.Metrics;

				Log.Default.Write(
					LogSeverityType.Information,
					"File loading complete.",
					new Dictionary<string, object>
					{
						{ "OpeningDuration", openingDuration.TotalSeconds },
						{ "SourceFileLoadingPeriod", metrics.SourceFileLoadingPeriod.TotalSeconds },
						{ "RecordAndMetadataLoadDuration", metrics.RecordAndMetadataLoadDuration.TotalSeconds },
						{ "RecordCount", metrics.RecordCount },
						{ "FileSize", metrics.FileSize },
						{ "WorkingSet", Environment.WorkingSet },
						{ "ManagedMemory", GC.GetTotalMemory(forceFullCollection: false) },
						{ "SourceFilePath", coreEngine.SourceFilePath },
						{ "StorageMode", coreEngine.StorageMode },
					});
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

			private ContextDictionary GetContext(ICoreExtension extension, ContextDictionary knownContext, RecordList records)
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