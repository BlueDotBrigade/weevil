namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
	using BlueDotBrigade.Weevil.Data;
	using Diagnostics;

	/// <summary>
	/// Extends the record table saved for a log that has grown since, by parsing only its new part (#1023).
	/// </summary>
	/// <remarks>
	/// Logs grow by appending, so the saved table still holds the log's old part, once its blocks are checked against the saved checksums.
	/// - The rows from the last chunk's first record on are dropped: the lines added may continue the last record, and parsing from the chunk's start
	///   lets that chunk grow, as a full parse would.
	/// - The log is parsed from that record to its end, with its line number, and the checksums of the new part follow those saved for the old part.
	/// - Where that record starts comes from the table, which the checksums vouch for, never from the line index, which can be stale.
	///   A table with no chunk, whose rows all keep a string, as UTF-16 in the other byte order and UTF-32 in RAM, cannot say, so its log is parsed in full.
	/// </remarks>
	internal static class GrownLogParser
	{
		internal const string NewPartParsedMessage = "The log grew since it was indexed, so only its new part was parsed.";

		/// <param name="log">The log, now longer than <see cref="SavedParse.EndOffset"/>.</param>
		/// <param name="logPath">The log's path, which chunks on file read back from.</param>
		/// <param name="saved">The table saved for the log's old part, already checked against it.</param>
		/// <param name="parse">The parse that made <paramref name="saved"/>.</param>
		/// <param name="parser">The parser, the same as made <paramref name="saved"/>.</param>
		/// <param name="table">The saved table's rows that are kept, then those parsed.</param>
		/// <param name="parsed">The bytes of the whole log parsed, old part and new, with their checksums.</param>
		/// <returns><see langword="false"/> when the log must be parsed in full.</returns>
		public static bool TryParse(
			Stream log,
			string logPath,
			RecordTable saved,
			SavedParse parse,
			IRecordParser parser,
			StorageMode storageMode,
			Encoding encoding,
			out RecordTable table,
			out LogFileSource parsed)
		{
			table = null;
			parsed = null;

			if (saved.ChunkStarts.Count == 0)
			{
				return false;
			}

			// A chunk starts at a row that holds the log's bytes, so where it starts in the log is known.
			var stopwatch = Stopwatch.StartNew();
			var resumeAt = saved.ChunkStarts[saved.ChunkStarts.Count - 1];
			var lineNumber = saved.GetLineNumber(resumeAt);

			if (!saved.TryGetFileOffset(resumeAt, out var resumeOffset) || resumeOffset < parse.StartOffset || resumeOffset >= parse.EndOffset)
			{
				return false;
			}

			// The checksums start at the block that holds the first byte parsed again, so they follow those saved for the blocks before it.
			var checksumStart = System.Math.Max(parse.StartOffset, resumeOffset / LogFileSource.BlockSize * LogFileSource.BlockSize);
			var keptChecksums = (int)((checksumStart / LogFileSource.BlockSize) - (parse.StartOffset / LogFileSource.BlockSize));

			var originalPosition = log.Position;

			try
			{
				var repository = new SerializedRecordRepository(log, parser, resumeOffset, lineNumber, loggingEnabled: true, storageMode: storageMode, checksumStartOffset: checksumStart);

				if (repository.Get(Range.All, int.MaxValue) is not TableRecordList { WholeTable: RecordTable newPart } || repository.ParsedSource is null)
				{
					return false;
				}

				ReadOnlySpan<ulong> newChecksums = repository.ParsedSource.GetChecksums();
				var checksums = new ulong[keptChecksums + newChecksums.Length];

				for (var index = 0; index < keptChecksums; index++)
				{
					checksums[index] = parse.Checksums[index];
				}

				newChecksums.CopyTo(checksums.AsSpan(keptChecksums));

				parsed = LogFileSource.Create(logPath, parse.StartOffset, repository.ParsedSource.EndOffset, log.Length, checksums);

				// On disk, every chunk reads back through the one source of the whole log. In RAM, the chunks hold their bytes.
				LogFileSource source = parsed;
				table = storageMode == StorageMode.Disk
					? saved.Append(resumeAt, newPart, chunk => new ContentChunk(source, chunk.FileOffset, chunk.Length, encoding))
					: saved.Append(resumeAt, newPart, chunk => chunk);
			}
			finally
			{
				log.Position = originalPosition;
			}

			Log.Default.Write(
				LogSeverityType.Information,
				NewPartParsedMessage,
				new Dictionary<string, object>
				{
					{ "RecordsKept", resumeAt },
					{ "RecordsParsed", table.Count - resumeAt },
					{ "BytesParsed", parsed.EndOffset - resumeOffset },
					{ "Duration", stopwatch.Elapsed.TotalSeconds },
				});

			return true;
		}
	}
}
