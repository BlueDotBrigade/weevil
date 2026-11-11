namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;

	/// <summary>
	/// Estimates the memory a log needs when it is opened in RAM, so that Weevil can suggest RAM or Disk before opening it (#1028).
	/// </summary>
	/// <remarks>
	/// The estimate is the log's size plus <see cref="BytesPerRecord"/> for each record (the #986 plan).
	/// The record count is the line count, which is never lower:
	/// <list type="bullet">
	///   <item>from the saved line index's header, scaled up when the log has grown past the length the index covers;</item>
	///   <item>otherwise from the line breaks in the log's first <see cref="SampleLength"/> bytes, scaled to the log's size. That average can err either way.</item>
	/// </list>
	/// The estimate covers the log's content and records, not Weevil's window, nor the log that is open while another one opens.
	/// </remarks>
	internal static class MemoryEstimator
	{
		/// <summary>
		/// About 30 bytes for the record table, and 4 for a filter's temporary results (the #986 plan).
		/// </summary>
		public const int BytesPerRecord = 34;

		public const int SampleLength = 1024 * 1024;

		/// <exception cref="FileNotFoundException">The log does not exist.</exception>
		/// <exception cref="IOException">The log cannot be read.</exception>
		/// <exception cref="UnauthorizedAccessException">The log cannot be read.</exception>
		public static StorageUnit Estimate(string logFilePath)
		{
			if (string.IsNullOrWhiteSpace(logFilePath))
			{
				throw new ArgumentNullException(nameof(logFilePath));
			}

			// Shared for reading, writing and deletion, as every read of a log is, so the application writing it is never blocked (#986).
			using var log = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1);

			var length = log.Length;

			if (length == 0)
			{
				return StorageUnit.Zero;
			}

			var lineCount = LineIndexFile.TryReadLineCount(logFilePath, out var indexedLineCount, out var indexedLength) &&
			                indexedLength > 0 &&
			                indexedLength <= length
				? Scale(indexedLineCount, indexedLength, length)
				: CountLines(log, length);

			return new StorageUnit((ulong)length + (ulong)lineCount * BytesPerRecord);
		}

		/// <summary>
		/// Counts the lines in the first <see cref="SampleLength"/> bytes, and scales the count to the whole log.
		/// </summary>
		/// <remarks>
		/// Counts the <c>\n</c> bytes, or the <c>\r</c> bytes in a log that has none, whatever the encoding: in UTF-16 and UTF-32 each line break has one such byte.
		/// A few other characters contain the byte too, which only makes the estimate higher.
		/// </remarks>
		private static long CountLines(Stream log, long length)
		{
			var sample = new byte[(int)Math.Min(length, SampleLength)];
			log.ReadExactly(sample);

			var span = sample.AsSpan();
			var lineBreaks = span.Count((byte)'\n');

			if (lineBreaks == 0)
			{
				lineBreaks = span.Count((byte)'\r');
			}

			// The last line of the log needs no line break.
			var lastByte = sample[^1];
			var lines = sample.Length == length && lastByte != '\n' && lastByte != '\r'
				? lineBreaks + 1
				: lineBreaks;

			return Math.Max(1, Scale(lines, sample.Length, length));
		}

		private static long Scale(long count, long countedLength, long length)
		{
			return countedLength == length
				? count
				: (long)Math.Ceiling(count * ((double)length / countedLength));
		}
	}
}
