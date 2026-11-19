namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Runtime.CompilerServices;
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

			/// <summary>
			/// The name of the region whose line numbers will be loaded, in place of <see cref="_range"/>: otherwise <see langword="null"/>.
			/// </summary>
			private string _regionName;

			/// <summary>
			/// The name of the level-1 section whose line numbers will be loaded, in place of <see cref="_range"/>: otherwise <see langword="null"/>.
			/// </summary>
			private string _sectionName;

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
						"Value is expected to be greater than zero.");

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
					$"Core engine construction reads only a range of lines. Range={range}");

				return this;
			}

			/// <summary>
			/// During the loading process only the records of the region with this name will be loaded, from the regions the log's sidecar lists (#1213).
			/// </summary>
			/// <remarks>
			/// The region replaces any range. <see cref="Build"/> throws a <see cref="KeyNotFoundException"/> when the sidecar lists no region with this name,
			/// and an <see cref="InvalidOperationException"/> when a section was also named.
			/// </remarks>
			/// <param name="regionName">The region's name, ignoring case.</param>
			public CoreEngineBuilder UsingRegion(string regionName)
			{
				_regionName = string.IsNullOrWhiteSpace(regionName)
					? throw new ArgumentException("A region's name is expected.", nameof(regionName))
					: regionName;

				Log.Default.Write(
					LogSeverityType.Debug,
					$"Core engine construction will load one region. Region=`{regionName}`");

				return this;
			}

			/// <summary>
			/// During the loading process only the records of the level-1 section with this name will be loaded, from the log's sidecar (#1212):
			/// from its line to the line before the next level-1 section, or to the end of the log, so the sections nested in it are loaded too.
			/// </summary>
			/// <remarks>
			/// The section replaces any range. <see cref="Build"/> throws a <see cref="KeyNotFoundException"/> when the sidecar lists no level-1 section with this name,
			/// and an <see cref="InvalidOperationException"/> when a region was also named.
			/// </remarks>
			/// <param name="sectionName">The section's name, ignoring case.</param>
			public CoreEngineBuilder UsingSection(string sectionName)
			{
				_sectionName = string.IsNullOrWhiteSpace(sectionName)
					? throw new ArgumentException("A section's name is expected.", nameof(sectionName))
					: sectionName;

				Log.Default.Write(
					LogSeverityType.Debug,
					$"Core engine construction will load one section. Section=`{sectionName}`");

				return this;
			}

			/// <summary>
			/// Reopens the log from the record table saved with its index, parsing only what it grew by since (#1023).
			/// </summary>
			/// <remarks>
			/// A method of its own, never inlined, so the saved table that a grown log's longer table replaces is not kept by <see cref="Build"/>'s frame
			/// when the engine is created, whose collection would otherwise keep it: about 22 bytes per record. Clearing the variable in <see cref="Build"/> did not free it.
			/// </remarks>
			/// <param name="source">The source made from the saved checksums, in both modes: on disk, the records read their content back through it.</param>
			/// <returns><see langword="false"/> when the log must be parsed.</returns>
			[MethodImpl(MethodImplOptions.NoInlining)]
			private bool TryReopen(FileStream log, LineIndex lineIndex, IPlugin plugin, ICoreExtension coreExtension, out RecordList records, out LogFileSource source)
			{
				records = null;

				if (!LineIndexFile.TryLoadTable(_sourceFilePath, log, lineIndex, plugin, _storageMode, out RecordTable savedTable, out source))
				{
					return false;
				}

				SavedParse savedParse = lineIndex.Parse;

				if (savedParse.EndOffset == log.Length)
				{
					records = new TableRecordList(savedTable);
					return true;
				}

				if (!GrownLogParser.TryParse(log, _sourceFilePath, savedTable, savedParse, coreExtension.GetRecordParser(), _storageMode, lineIndex.ContentEncoding, out RecordTable grownTable, out LogFileSource parsedSource))
				{
					source = null;
					return false;
				}

				// The log grew since it was indexed: only its new part was parsed, and the longer table is saved.
				records = new TableRecordList(grownTable);
				source = parsedSource;
				LineIndexFile.SaveParse(_sourceFilePath, log, lineIndex, SavedParse.Create(plugin, _storageMode, parsedSource), grownTable);

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

				CoreEngine coreEngine = BuildEngine();

				// Opening's only forced collection. It runs once BuildEngine has returned, so nothing still refers to what parsing
				// and the first filter left behind (#1240). Aggressive mode returns that memory to the operating system (#969).
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);

				LogFileLoaded(coreEngine, openingStopwatch.Elapsed);

				return coreEngine;
			}

			/// <remarks>
			/// Not inlined: <see cref="Build"/>'s collection can free this method's temporaries only once it has returned (#1240).
			/// </remarks>
			[MethodImpl(MethodImplOptions.NoInlining)]
			private CoreEngine BuildEngine()
			{
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

				using (FileStream log = FileHelper.Open(_sourceFilePath))
				{
					sourceFilePath = _sourceFilePath;
					sourceFileLength = log.Length;

					IPlugin plugin = new PluginFactory().Create(sourceFilePath);
					coreExtension = plugin.GetExtension(sourceFilePath);

					sidecarManager = new SidecarManager(_sourceFilePath);

					loadingStopwatch.Restart();

					lineIndex = LineIndexFile.Load(_sourceFilePath, log);

					// Sized from the line index, before the records are parsed.
					var isKeepingSession = _session != null;
					session = _session ?? new Session(lineIndex.LineCount);
					recordState = _recordState ?? new RecordState(session.Annotations, new FlagSet(lineIndex.LineCount));

					var startAtLineNumber = _startAtLineNumber >= 1 ? _startAtLineNumber : FirstRecordLineNumber;
					Range lineNumberRange = GetLineNumberRange(GetRequestedRange(sidecarManager));

					// A full open, from line 1 with no range and no limit, can reuse the record table saved with the index (#1023).
					var isFullOpen = startAtLineNumber == FirstRecordLineNumber && lineNumberRange.Equals(Range.All) && maxRecords == int.MaxValue;
					records = null;
					source = null;

					if (isFullOpen)
					{
						_ = TryReopen(log, lineIndex, plugin, coreExtension, out records, out source);
					}

					if (records == null)
					{
						// Reading starts at the start line's byte offset, taken from the line index (#977).
						// A start line beyond the end of the log reads from the top, which finds no records to keep.
						// In disk mode, the records read their content back from the log, through a source complete before Get returns (#1025).
						SerializedRecordRepository repository = startAtLineNumber > FirstRecordLineNumber && startAtLineNumber <= lineIndex.LineCount
							? new SerializedRecordRepository(
								 log,
								 coreExtension.GetRecordParser(),
								 lineIndex.GetByteOffset(log, startAtLineNumber),
								 startAtLineNumber,
								 true,
								 GetMaximumLinesToSearch(startAtLineNumber, lineIndex.LineCount),
								 _storageMode)
							: new SerializedRecordRepository(
								 log,
								 coreExtension.GetRecordParser(),
								 GetMaximumLinesToSearch(lineNumberRange),
								 _storageMode);

						records = repository.Get(lineNumberRange, maxRecords);
						source = repository.Source;

						// After a full parse, the saved index keeps what parsed the log, the checksums of the bytes read and the record table,
						// so the log can be reopened without parsing (#1023). A parse from a start line, of a range or up to a limit keeps only part of it.
						if (isFullOpen && repository.Source != null && records is TableRecordList { WholeTable: RecordTable parsedTable })
						{
							LineIndexFile.SaveParse(_sourceFilePath, log, lineIndex, SavedParse.Create(plugin, _storageMode, repository.Source), parsedTable);
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

				return new CoreEngine(
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
			}

			/// <summary>
			/// Records what the performance requirements measure once a log has opened (#1084, #1086),
			/// so a shared application log is enough to compare them (#1105).
			/// </summary>
			/// <remarks>
			/// Opening has already collected the memory that parsing and the first filter left behind (#1240). The memory is the whole process's:
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
			private Range GetLineNumberRange(Range range)
			{
				if (_startAtLineNumber <= FirstRecordLineNumber)
				{
					return range;
				}

				if (range.Equals(Range.All))
				{
					return new Range(_startAtLineNumber, int.MaxValue);
				}

				return new Range(System.Math.Max(range.Start.Value, _startAtLineNumber), range.End);
			}

			/// <summary>
			/// Returns the line numbers asked for: those of the region or section named, or else the range.
			/// </summary>
			/// <exception cref="InvalidOperationException">Both a region and a section were named.</exception>
			private Range GetRequestedRange(SidecarManager sidecarManager)
			{
				if (_regionName is not null && _sectionName is not null)
				{
					throw new InvalidOperationException($"Only one part of a log can be opened: the region `{_regionName}` or the section `{_sectionName}`, not both.");
				}

				return _regionName is not null ? GetRegionRange(sidecarManager)
					: _sectionName is not null ? GetSectionRange(sidecarManager)
					: _range;
			}

			/// <summary>
			/// Returns the line numbers of the level-1 section named by <see cref="UsingSection"/>,
			/// from its line to the line before the next level-1 section, or to the end of the log.
			/// </summary>
			/// <exception cref="KeyNotFoundException">The sidecar lists no level-1 section with that name.</exception>
			private Range GetSectionRange(SidecarManager sidecarManager)
			{
				List<Section> sections = new TableOfContents(sidecarManager.LoadSections()).Sections
					.Where(s => s.Level == 1)
					.ToList();
				var index = sections.FindIndex(s => string.Equals(s.Name, _sectionName, StringComparison.OrdinalIgnoreCase));

				if (index < 0)
				{
					var names = sections.Count == 0 ? "none" : string.Join(", ", sections.Select(s => $"`{s.Name}`"));
					throw new KeyNotFoundException($"The log's sidecar lists no level-1 section named `{_sectionName}`. Its level-1 sections: {names}.");
				}

				var lastLine = index + 1 < sections.Count ? sections[index + 1].LineNumber - 1 : int.MaxValue;
				return new Range(sections[index].LineNumber, lastLine);
			}

			/// <summary>
			/// Returns the line numbers of the region named by <see cref="UsingRegion"/>, from its first line to its last.
			/// </summary>
			/// <exception cref="KeyNotFoundException">The sidecar lists no region with that name.</exception>
			private Range GetRegionRange(SidecarManager sidecarManager)
			{
				List<Region> regions = sidecarManager.LoadRegions();
				Region region = regions.FirstOrDefault(r => string.Equals(r.Name, _regionName, StringComparison.OrdinalIgnoreCase));

				if (region is null)
				{
					var names = regions.Count == 0 ? "none" : string.Join(", ", regions.Select(r => $"`{r.Name}`"));
					throw new KeyNotFoundException($"The log's sidecar lists no region named `{_regionName}`. Its regions: {names}.");
				}

				return new Range(region.Minimum.LineNumber, region.Maximum.LineNumber);
			}

			/// <summary>
			/// Returns the number of lines that can be read from a start line while searching for the first record (#1237).
			/// </summary>
			/// <remarks>
			/// The start line can be inside a record of many lines, such as a long stack trace, so the search can reach the end of the log,
			/// plus the usual allowance in case the log grew since it was indexed.
			/// </remarks>
			private static int GetMaximumLinesToSearch(int startAtLineNumber, int lineCount)
			{
				long linesFromStart = System.Math.Max((long)lineCount - startAtLineNumber + 1, 0);

				return (int)System.Math.Min(linesFromStart + MultilineRecordParser.MaximumLinesToSearch, int.MaxValue);
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