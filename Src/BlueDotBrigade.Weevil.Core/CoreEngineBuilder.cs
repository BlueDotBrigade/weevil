namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Collections.Generic;
	using BlueDotBrigade.Weevil.IO;
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

			private readonly ClearRecordsOperation _clearOperation;
			private readonly bool _hasBeenCleared;

			private bool _isUsingUserDefinedContext;
			private ContextDictionary _userDefinedContext;

			private int _maxRecords = int.MaxValue;
			private Range _range = Range.Complete;

			internal CoreEngineBuilder(string sourceFilePath) : this(sourceFilePath, FirstRecordLineNumber)
			{
				// nothing to do
			}

			internal CoreEngineBuilder(string sourceFilePath, int lineNumber)
			{
				_sourceFilePath = sourceFilePath;
				_startAtLineNumber = lineNumber;
				_hasBeenCleared = false;

				_userDefinedContext = new ContextDictionary();

				var message = string.Format(
					$"Core engine construction will read records from the provided file. {nameof(sourceFilePath)}={0}, {nameof(lineNumber)}={1}",
					sourceFilePath,
					lineNumber);

				Log.Default.Write(LogSeverityType.Debug, message);
			}

			internal CoreEngineBuilder(CoreEngine source, ClearRecordsOperation clearOperation)
			{
				_sourceInstance = source;
				_clearOperation = clearOperation;
				_hasBeenCleared = true;

				_startAtLineNumber = NotSet;

				Log.Default.Write(
					LogSeverityType.Debug,
					$"Core engine construction will read records from an existing instance. OriginalFilename={Path.GetFileName(source.SourceFilePath)}, Operation={clearOperation}");
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

			public CoreEngineBuilder UsingRange(Range range)
			{
				_range = range ?? throw new ArgumentNullException(nameof(range));

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

				string sourceFilePath;
				long sourceFileLength;
				Stopwatch sourceFileLoadingPeriod = new Stopwatch();

				SidecarManager sidecarManager = null;
				ICoreExtension coreExtension = null;

				var maxRecords = _maxRecords;
				ImmutableArray<IRecord> records;
				ImmutableArray<IRecord> selectedRecords = ImmutableArray<IRecord>.Empty;

				Range range = _range;
				if (_range.IsCompleteRange)
				{
					range = new Range(1, _range.Maximum);
				}

				var knownContext = new ContextDictionary();

				var inclusiveFilterHistory = new List<string>();
				var exclusiveFilterHistory = new List<string>();
				var tableOfContents = new List<Section>();

				if (this.UseExistingInstance)
				{
					sourceFilePath = _sourceInstance.SourceFilePath;
					sourceFileLength = _sourceInstance._logFileMetrics.FileSize;

					knownContext = _sourceInstance._context;

					tableOfContents = _sourceInstance.Navigate.TableOfContents.Sections.ToList();

					inclusiveFilterHistory.AddRange(_sourceInstance.Filter.IncludeHistory);
					exclusiveFilterHistory.AddRange(_sourceInstance.Filter.ExcludeHistory);

					coreExtension = _sourceInstance._coreExtension;
					sidecarManager = _sourceInstance._sidecarManager;

					selectedRecords = _sourceInstance
						._selectionManager
						.GetSelected();

					var repository = new InMemoryRecordRepository(
						_sourceInstance._allRecords,
						_sourceInstance._filterManager.Results,
						selectedRecords,
						_clearOperation);

					records = repository.Get(maxRecords);

					selectedRecords = GetVisibleRecordSelection(records, selectedRecords);
				}
				else
				{
					using (FileStream dataSource = FileHelper.Open(_sourceFilePath))
					{
						sourceFilePath = _sourceFilePath;
						sourceFileLength = dataSource.Length;

						IPlugin plugin = new PluginFactory().Create(sourceFilePath);
						coreExtension = plugin.GetExtension(sourceFilePath);

						sidecarManager = new SidecarManager(_sourceFilePath);

						var startAtLineNumber = _startAtLineNumber >= 1 ? _startAtLineNumber : 1;

						sourceFileLoadingPeriod.Restart();

						var repository = new SerializedRecordRepository(
							 dataSource,
							 coreExtension.GetRecordParser(),
							 sidecarManager.GetByteOffsetOrDefault(startAtLineNumber),
							 startAtLineNumber,
							 true);

						records = repository.Get(range, maxRecords);

						sourceFileLoadingPeriod.Stop();

						sidecarManager.Load(
							records,
							out knownContext,
							out inclusiveFilterHistory,
							out exclusiveFilterHistory,
							out tableOfContents);

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
					sourceFileLoadingPeriod.Elapsed,
					coreExtension,
					context,
					sidecarManager,
					records,
					_hasBeenCleared,
					new TableOfContents(tableOfContents));

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
