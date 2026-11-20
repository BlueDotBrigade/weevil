namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Runtime.CompilerServices;
	using BlueDotBrigade.Weevil.Data;
	using Diagnostics;

	/// <summary>
	/// The index saved beside a log, named like the sidecar: <c>Application.log.idx</c>. It decides what an open reads from it, reuses and writes (#1023).
	/// </summary>
	/// <remarks>
	/// The file is a cache: it is rebuilt when it is missing, corrupt, or does not match the log, so deleting it is always safe. Its format is <see cref="IndexFile"/>.
	/// Like the sidecar, it is not saved for a log in the temporary folder. A log opened from a zip is extracted to a folder kept between opens, where both are saved (#1253).
	/// An open loads the line index first (<see cref="Load"/>). A full open then reads every record through <see cref="ReadAllRecords"/>:
	/// - from the record table saved with the index, when the same parser would make it and every block of the log parsed still matches its checksum;
	/// - extended by a parse of only what the log grew by, then saved again, longer;
	/// - or else by a parse of the whole log, whose parse and table are saved, unless the file holds them already.
	/// An open that is not full parses what it needs itself, and saves no table. What the file held when it was loaded never changes, so an instance serves one open.
	/// </remarks>
	internal sealed class SavedIndex
	{
		public const string FileExtension = "idx";

		/// <summary>
		/// Logged when a log is opened from the record table saved with its index, without being parsed (#1023, #1085).
		/// </summary>
		internal const string RecordsReadFromIndexMessage = "The records were read from the saved index, without parsing the log.";

		internal const string TableRejectedMessage = "The saved record table does not match the log, so the log will be parsed.";

		internal const string LogChangedMessage = "The log changed since it was last parsed, so its line index is rebuilt.";

		internal const string OldRecordsReadFromIndexMessage = "The records of the log's old part were read from the saved index: the log grew since.";

		/// <summary>
		/// Logged when the log, not the saved index, could not be read while the saved record table was checked against it.
		/// </summary>
		internal const string LogUnreadableMessage = "The log could not be read while its saved record table was checked, so the log will be parsed. The saved table is kept.";

		private readonly string _logFilePath;
		private readonly string _indexFilePath;

		// What the file held when it was loaded: what parsed the log, and whether its table follows.
		private readonly IndexFile _loaded;

		private SavedIndex(string logFilePath, string indexFilePath, LineIndex lineIndex, IndexFile loaded)
		{
			_logFilePath = logFilePath;
			_indexFilePath = indexFilePath;
			_loaded = loaded;
			this.LineIndex = lineIndex;
		}

		/// <summary>
		/// What became of the record table saved with the index, when a full open tried to reuse it.
		/// </summary>
		private enum TableLoad
		{
			/// <summary>
			/// The records were read from it.
			/// </summary>
			Loaded,

			/// <summary>
			/// It was not used, but may still be the log's, so the parse that follows keeps it when it makes the same table.
			/// For example, there is none, another parser or build made it, or the log could not be read.
			/// </summary>
			NotLoaded,

			/// <summary>
			/// It is damaged, it is not the log's, or the saved index could not be read, so the parse that follows saves its own,
			/// rather than leave one that every open rejects.
			/// </summary>
			Rejected,
		}

		/// <summary>
		/// The log's line index, which matches the log: it is updated or rebuilt in place when the log changed.
		/// </summary>
		public LineIndex LineIndex { get; }

		/// <summary>
		/// Returns the saved index when it matches the log, after scanning any content added since. Otherwise scans the whole log.
		/// </summary>
		/// <remarks>
		/// The index is saved whenever it changes, unless it cannot be: Weevil then continues without saving it.
		/// An index that holds a record table is not saved when the log grew, as that would drop the table: a full open extends the table,
		/// and saves both (#1023). After any other open, the next one scans the new content again.
		/// </remarks>
		public static SavedIndex Load(string logFilePath, Stream log)
		{
			var indexFilePath = GetIndexFilePath(logFilePath);
			var stopwatch = Stopwatch.StartNew();
			var hasChanged = true;

			if (TryRead(indexFilePath, log, out LineIndex lineIndex, out IndexFile loaded))
			{
				hasChanged = lineIndex.IndexedLength < log.Length;

				if (hasChanged)
				{
					lineIndex.Update(log);
				}
			}
			else
			{
				lineIndex = LineIndex.Build(log);
				loaded = IndexFile.Empty;
			}

			if (hasChanged && !loaded.HasTable)
			{
				TrySave(logFilePath, indexFilePath, lineIndex, loaded.Parse);
			}

			Log.Default.Write(
				LogSeverityType.Information,
				"The line index is ready.",
				new Dictionary<string, object>
				{
					{ "LineCount", lineIndex.LineCount },
					{ "IndexedLength", lineIndex.IndexedLength },
					{ "HasChanged", hasChanged },
					{ "Duration", stopwatch.Elapsed.TotalSeconds },
					{ "IndexFilePath", indexFilePath },
				});

			return new SavedIndex(logFilePath, indexFilePath, lineIndex, loaded);
		}

		/// <summary>
		/// Reads every record of the log, for a full open: from the record table saved with the index, parsing only what the log grew by since,
		/// or else by parsing the whole log. Then saves what parsed it, the checksums of the bytes read and the table, unless the file holds them already.
		/// </summary>
		/// <remarks>
		/// Call it once, after <see cref="Load"/>: it decides from what the file held then.
		/// </remarks>
		/// <param name="source">
		/// The checksummed source of the bytes parsed, or made from the saved checksums, in both modes: on disk, the records read their content back through it.
		/// <see langword="null"/> for a log read from a stream that is not a file, which only RAM mode accepts.
		/// </param>
		/// <param name="displayPath">The log as the analyst knows it, when it is a copy: see <see cref="LogFileSource.Path"/> (#1312).</param>
		public RecordList ReadAllRecords(Stream log, IPlugin plugin, ICoreExtension coreExtension, StorageMode storageMode, string displayPath, out LogFileSource source)
		{
			TableLoad savedTable = TryReopen(log, plugin, coreExtension, storageMode, displayPath, out RecordTable table, out source);

			if (savedTable == TableLoad.Loaded)
			{
				return new TableRecordList(table);
			}

			// In disk mode, the records read their content back from the log, through a source complete before Get returns (#1025).
			var repository = new SerializedRecordRepository(log, coreExtension.GetRecordParser(), MultilineRecordParser.MaximumLinesToSearch, storageMode, displayPath);
			RecordList records = repository.Get(Range.All, int.MaxValue);
			source = repository.Source;

			if (source != null && records is TableRecordList { WholeTable: RecordTable parsed })
			{
				SaveParse(log, SavedParse.Create(plugin, storageMode, source), parsed, savedTable);
			}

			return records;
		}

		/// <summary>
		/// Reads the line count, and the length of the log it covers, from the saved index's header, without checking that the index matches the log.
		/// </summary>
		/// <remarks>
		/// For an estimate only (#1028). Returns <see langword="false"/> when there is no index, or it cannot be read.
		/// </remarks>
		public static bool TryReadLineCount(string logFilePath, out int lineCount, out long indexedLength)
		{
			lineCount = 0;
			indexedLength = 0;

			var indexFilePath = GetIndexFilePath(logFilePath);

			if (!System.IO.File.Exists(indexFilePath))
			{
				return false;
			}

			try
			{
				using var indexFile = new System.IO.FileStream(indexFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

				return IndexFile.TryReadLineCount(indexFile, out lineCount, out indexedLength);
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
			{
				return false;
			}
		}

		/// <summary>
		/// Deletes the saved index, so the next open rebuilds it: it checks only the start and end of the log,
		/// so it can still match a log changed in the middle (#1025).
		/// </summary>
		public static void Delete(string logFilePath)
		{
			TryDelete(GetIndexFilePath(logFilePath));
		}

		/// <summary>
		/// Reads the records from the record table saved with the index, extended by a parse of what the log grew by since, which is then saved.
		/// </summary>
		/// <remarks>
		/// A method of its own, never inlined, so the saved table that a grown log's longer table replaces is not kept by the opening method's frame
		/// when the engine is created, whose collection would otherwise keep it: about 22 bytes per record.
		/// </remarks>
		/// <param name="table">The table to read the records from, when <see cref="TableLoad.Loaded"/>.</param>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private TableLoad TryReopen(Stream log, IPlugin plugin, ICoreExtension coreExtension, StorageMode storageMode, string displayPath, out RecordTable table, out LogFileSource source)
		{
			TableLoad savedTable = TryLoadTable(log, plugin, storageMode, displayPath, out RecordTable saved, out source);

			if (savedTable != TableLoad.Loaded || _loaded.Parse.EndOffset == log.Length)
			{
				table = saved;
				return savedTable;
			}

			if (!GrownLogParser.TryParse(log, _logFilePath, saved, _loaded.Parse, coreExtension.GetRecordParser(), storageMode, this.LineIndex.Encoding.ToEncoding(), displayPath, out table, out LogFileSource parsed))
			{
				source = null;
				return TableLoad.NotLoaded;
			}

			// The log grew since it was indexed: only its new part was parsed, and the longer table is saved.
			source = parsed;
			SaveParse(log, SavedParse.Create(plugin, storageMode, parsed), table, savedTable);

			return TableLoad.Loaded;
		}

		/// <summary>
		/// Builds the record table saved with the index, instead of parsing the log, when the same parser would make it and the log has not changed since (#1023).
		/// </summary>
		/// <remarks>
		/// Every block of the log that was parsed is checked against the checksums saved with the table: one that changed means the log must be parsed.
		/// <see cref="SaveParse"/> then rebuilds the line index, which cannot see such a change.
		/// For a log that grew since, the table holds only its old part, up to <see cref="SavedParse.EndOffset"/>: see <see cref="GrownLogParser"/>.
		/// </remarks>
		/// <param name="source">The source made from the saved checksums, in both modes: on disk, the records read their content back through it.</param>
		private TableLoad TryLoadTable(Stream log, IPlugin plugin, StorageMode storageMode, string displayPath, out RecordTable table, out LogFileSource source)
		{
			table = null;
			source = null;

			SavedParse parse = _loaded.Parse;
			var logLength = log.Length;

			if (parse is null || !_loaded.HasTable || !parse.IsSameParser(plugin, storageMode, this.LineIndex.Encoding.IsQuickToDecode) || parse.EndOffset > logLength)
			{
				return TableLoad.NotLoaded;
			}

			var stopwatch = Stopwatch.StartNew();
			var originalPosition = log.Position;

			try
			{
				// The log is checked while the index is open, so another instance of Weevil may still replace the index meanwhile.
				using var indexFile = new System.IO.FileStream(_indexFilePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

				// Read again, from the file now open: another instance of Weevil may have replaced the index since it was loaded.
				if (!IndexFile.TryRead(indexFile, logLength, out LineIndex savedLines, out IndexFile saved) ||
				    !saved.HasTable ||
				    !parse.IsSameAs(saved.Parse) ||
				    !Matches(savedLines, log))
				{
					Log.Default.Write(
						LogSeverityType.Information,
						$"The saved index changed after it was loaded, so the log will be parsed. IndexFilePath={_indexFilePath}");
					return TableLoad.NotLoaded;
				}

				indexFile.Position = saved.TableOffset;

				if (!SavedTableReader.TryRead(indexFile, log, System.IO.Path.GetFullPath(_logFilePath), parse, this.LineIndex.Encoding.ToEncoding(), storageMode, displayPath, out table, out source))
				{
					Log.Default.Write(
						LogSeverityType.Information,
						$"{TableRejectedMessage} IndexFilePath={_indexFilePath}");
					return TableLoad.Rejected;
				}
			}
			catch (LogReadException exception)
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					exception.InnerException,
					$"{LogUnreadableMessage} LogFilePath={_logFilePath}");
				table = null;
				source = null;
				return TableLoad.NotLoaded;
			}
			// ArgumentException too: a defect in reading the table should cost a parse, not the open.
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is FormatException || exception is ArgumentException)
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					exception,
					$"The saved record table could not be read, so the log will be parsed. IndexFilePath={_indexFilePath}");
				table = null;
				source = null;
				return TableLoad.Rejected;
			}
			finally
			{
				log.Position = originalPosition;
			}

			Log.Default.Write(
				LogSeverityType.Information,
				parse.EndOffset == log.Length ? RecordsReadFromIndexMessage : OldRecordsReadFromIndexMessage,
				new Dictionary<string, object>
				{
					{ "RecordCount", table.Count },
					{ "Duration", stopwatch.Elapsed.TotalSeconds },
					{ "IndexFilePath", _indexFilePath },
				});

			return TableLoad.Loaded;
		}

		/// <summary>
		/// After a full parse, saves with the index what parsed the log, the checksums of the bytes it read, and the record table it made,
		/// unless the saved index holds them already (#1023).
		/// </summary>
		/// <remarks>
		/// The line index is saved before the log is parsed, by <see cref="Load"/>, and again here once the parse is known.
		/// When the bytes parsed differ from those of the index's earlier parse, the log changed in the middle, where the line index cannot see it,
		/// so it is rebuilt before it is saved.
		/// </remarks>
		/// <param name="log">The log, read again if the line index must be rebuilt.</param>
		/// <param name="savedTable">What became of the table saved with the index: one that was rejected is saved again, even by the same parse.</param>
		private void SaveParse(Stream log, SavedParse parse, RecordTable table, TableLoad savedTable)
		{
			if (savedTable != TableLoad.Rejected && _loaded.HasTable && parse.IsSameAs(_loaded.Parse))
			{
				return;
			}

			if (_loaded.Parse != null && !_loaded.Parse.HasSameBytesAs(parse))
			{
				Log.Default.Write(
					LogSeverityType.Information,
					$"{LogChangedMessage} IndexFilePath={_indexFilePath}");
				this.LineIndex.Rebuild(log);
			}

			TrySave(_logFilePath, _indexFilePath, this.LineIndex, parse, table);
		}

		/// <summary>
		/// Reads the saved index, when it is intact and matches the log.
		/// </summary>
		private static bool TryRead(string indexFilePath, Stream log, out LineIndex lineIndex, out IndexFile file)
		{
			lineIndex = null;
			file = null;

			if (!System.IO.File.Exists(indexFilePath))
			{
				return false;
			}

			try
			{
				using var indexFile = new System.IO.FileStream(indexFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

				if (IndexFile.TryRead(indexFile, log.Length, out lineIndex, out file) && lineIndex.Matches(log))
				{
					return true;
				}

				Log.Default.Write(
					LogSeverityType.Information,
					$"The saved line index does not match the log, so it will be rebuilt. IndexFilePath={indexFilePath}");
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					exception,
					$"The saved line index could not be read, so it will be rebuilt. IndexFilePath={indexFilePath}");
			}

			lineIndex = null;
			file = null;
			return false;
		}

		/// <summary>
		/// Checks that the log still has the content a line index scanned.
		/// </summary>
		/// <exception cref="LogReadException">The log could not be read.</exception>
		private static bool Matches(LineIndex lineIndex, Stream log)
		{
			try
			{
				return lineIndex.Matches(log);
			}
			catch (IOException exception)
			{
				throw new LogReadException(exception);
			}
		}

		private static void TrySave(string logFilePath, string indexFilePath, LineIndex lineIndex, SavedParse parse, RecordTable table = null)
		{
			if (FileHelper.IsInTemporaryDirectory(logFilePath))
			{
				Log.Default.Write(
					LogSeverityType.Information,
					$"Skipping line index save for file from compressed archive. IndexFilePath={indexFilePath}");
				return;
			}

			// Written to a temporary file first, so a failure never leaves a partial index.
			var temporaryFilePath = $"{indexFilePath}.tmp";

			try
			{
				using (var destination = new System.IO.FileStream(temporaryFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					IndexFile.Write(destination, lineIndex, parse, table);
				}

				System.IO.File.Move(temporaryFilePath, indexFilePath, overwrite: true);
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					exception,
					$"The line index could not be saved, so it will be rebuilt the next time the log is opened. IndexFilePath={indexFilePath}");

				TryDelete(temporaryFilePath);
			}
		}

		private static string GetIndexFilePath(string logFilePath)
		{
			return $"{logFilePath}.{FileExtension}";
		}

		private static void TryDelete(string filePath)
		{
			try
			{
				if (System.IO.File.Exists(filePath))
				{
					System.IO.File.Delete(filePath);
				}
			}
			catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
			{
				Log.Default.Write(LogSeverityType.Warning, exception, $"A file could not be deleted. FilePath={filePath}");
			}
		}
	}
}
