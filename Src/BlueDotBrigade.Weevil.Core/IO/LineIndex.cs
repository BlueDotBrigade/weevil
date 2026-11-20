namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Security.Cryptography;

	/// <summary>
	/// The byte offset of every line in a log file, found by scanning the file's bytes for line breaks.
	/// </summary>
	/// <remarks>
	/// Lines are counted the way <see cref="StreamReader.ReadLine"/> counts them when the encoding is detected from a byte order mark:
	/// <c>\n</c>, <c>\r</c> and <c>\r\n</c> end a line, and the byte order mark is skipped. <see cref="LogEncoding"/> finds each line break.
	/// To keep the index small, only the offset of every <see cref="CheckpointInterval"/>th line is stored.
	/// Other offsets are found by scanning forward from the nearest stored one.
	/// Methods that read the log restore the position of its stream.
	/// <see cref="IndexFile"/> saves the index beside the log, and reads it back.
	/// </remarks>
	internal sealed class LineIndex
	{
		public const int DefaultCheckpointInterval = 64;
		public const int DefaultBufferSize = 1024 * 1024;
		public const int DefaultLookupBufferSize = 16 * 1024;

		private const int FingerprintLength = 4096;
		private const int MinimumBufferSize = 4;

		private readonly int _checkpointInterval;
		private readonly List<long> _checkpoints;

		private ScanState _endState;
		private byte[] _headHash;
		private byte[] _tailHash;

		private LineIndex(int checkpointInterval, List<long> checkpoints)
		{
			_checkpointInterval = checkpointInterval;
			_checkpoints = checkpoints;
		}

		public int CheckpointInterval => _checkpointInterval;

		/// <summary>
		/// The number of lines, which matches the number of lines returned by <see cref="StreamReader.ReadLine"/>.
		/// </summary>
		public int LineCount { get; private set; }

		/// <summary>
		/// The number of bytes of the log that have been scanned.
		/// </summary>
		public long IndexedLength { get; private set; }

		/// <summary>
		/// The log's encoding, found from its byte order mark.
		/// </summary>
		public LogEncoding Encoding { get; private set; }

		/// <summary>
		/// Scans the whole log.
		/// </summary>
		public static LineIndex Build(Stream log, int checkpointInterval = DefaultCheckpointInterval, int bufferSize = DefaultBufferSize)
		{
			if (checkpointInterval < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(checkpointInterval), checkpointInterval, "At least one line per checkpoint was expected.");
			}

			var index = new LineIndex(checkpointInterval, new List<long>());
			index.Update(log, bufferSize);

			return index;
		}

		/// <summary>
		/// Scans the content added to the log since it was last scanned.
		/// </summary>
		/// <remarks>
		/// Logs only grow by appending, so the content already scanned is assumed to be unchanged.
		/// Use <see cref="Matches"/> to check this for a saved index.
		/// </remarks>
		public void Update(Stream log, int bufferSize = DefaultBufferSize)
		{
			var originalPosition = log.Position;

			try
			{
				// When the log was last scanned, its byte order mark may not have been written yet, or only in part.
				var encoding = LogEncoding.Detect(log);

				if (encoding != this.Encoding)
				{
					Reset(encoding);
				}

				var lineCount = this.LineCount;
				ScanState state = _endState;

				this.IndexedLength = Scan(log, this.IndexedLength, ref state, bufferSize, lineStart =>
				{
					lineCount = CountLine(lineCount);

					if ((lineCount - 1) % _checkpointInterval == 0)
					{
						_checkpoints.Add(lineStart);
					}

					return true;
				});

				this.LineCount = lineCount;
				_endState = state;
				(_headHash, _tailHash) = ComputeFingerprint(log, this.IndexedLength);
			}
			finally
			{
				log.Position = originalPosition;
			}
		}

		/// <summary>
		/// The most lines a log can have for Weevil to open it (#1252).
		/// </summary>
		/// <remarks>
		/// Weevil numbers lines with an <see cref="int"/>, and the session keeps a byte for each line and one more, in one array.
		/// That is about 200 GB of 100-byte lines, ten times disk mode's 20 GB target.
		/// </remarks>
		internal static readonly int MaximumLineCount = Array.MaxLength - 1;

		/// <summary>
		/// Returns <paramref name="lineCount"/> plus the line just found.
		/// </summary>
		/// <exception cref="NotSupportedException">The log has more than <see cref="MaximumLineCount"/> lines.</exception>
		internal static int CountLine(int lineCount)
		{
			return lineCount < MaximumLineCount
				? lineCount + 1
				: throw new NotSupportedException(string.Format(
					CultureInfo.InvariantCulture,
					"Weevil can open logs of up to {0:N0} lines. This log has more.",
					MaximumLineCount));
		}

		/// <summary>
		/// Forgets everything scanned, and scans the whole log again.
		/// </summary>
		/// <remarks>
		/// For a log found changed in the middle, which <see cref="Matches"/> cannot see, as it checks only the start and end of what was scanned (#1023).
		/// </remarks>
		public void Rebuild(Stream log, int bufferSize = DefaultBufferSize)
		{
			var originalPosition = log.Position;

			try
			{
				Reset(LogEncoding.Detect(log));
			}
			finally
			{
				log.Position = originalPosition;
			}

			Update(log, bufferSize);
		}

		/// <summary>
		/// Forgets everything scanned, so that scanning restarts after the byte order mark of the given encoding.
		/// </summary>
		private void Reset(LogEncoding encoding)
		{
			this.Encoding = encoding;
			_checkpoints.Clear();
			this.LineCount = 0;
			this.IndexedLength = encoding.PreambleLength;
			_endState = ScanState.AtLineStart;
		}

		/// <summary>
		/// Returns the byte offset where the specified line begins.
		/// </summary>
		public long GetByteOffset(Stream log, int lineNumber, int bufferSize = DefaultLookupBufferSize)
		{
			if (lineNumber < 1 || lineNumber > this.LineCount)
			{
				throw new ArgumentOutOfRangeException(nameof(lineNumber), lineNumber, $"A line number from 1 to {this.LineCount} was expected.");
			}

			var checkpointOffset = _checkpoints[(lineNumber - 1) / _checkpointInterval];
			var linesAfterCheckpoint = (lineNumber - 1) % _checkpointInterval;

			if (linesAfterCheckpoint == 0)
			{
				return checkpointOffset;
			}

			var originalPosition = log.Position;

			try
			{
				// The checkpoint is a line start, so it is reported first.
				var linesFound = -1;
				var byteOffset = -1L;
				ScanState state = ScanState.AtLineStart;

				Scan(log, checkpointOffset, ref state, bufferSize, lineStart =>
				{
					linesFound++;

					if (linesFound == linesAfterCheckpoint)
					{
						byteOffset = lineStart;
						return false;
					}

					return true;
				});

				return byteOffset;
			}
			finally
			{
				log.Position = originalPosition;
			}
		}

		/// <summary>
		/// The content scanned is unchanged, as far as can be told without scanning it again: the log is at least as long,
		/// starts with the same byte order mark, and the first and last few KB of what was scanned are the same.
		/// </summary>
		/// <remarks>
		/// The log is identified by its content only, so an index stays valid when the files are copied elsewhere.
		/// A change in the middle of the log goes unseen. If the log has grown since, call <see cref="Update"/> to scan the new content.
		/// </remarks>
		public bool Matches(Stream log)
		{
			if (log.Length < this.IndexedLength)
			{
				return false;
			}

			var originalPosition = log.Position;

			try
			{
				if (LogEncoding.Detect(log) != this.Encoding)
				{
					return false;
				}

				var (headHash, tailHash) = ComputeFingerprint(log, this.IndexedLength);

				return headHash.AsSpan().SequenceEqual(_headHash) && tailHash.AsSpan().SequenceEqual(_tailHash);
			}
			finally
			{
				log.Position = originalPosition;
			}
		}

		/// <summary>
		/// The offset of every <see cref="CheckpointInterval"/>th line, from the first.
		/// </summary>
		internal IReadOnlyList<long> Checkpoints => _checkpoints;

		/// <summary>
		/// The index's values, as the header of a saved index keeps them, with no section after the checkpoints.
		/// </summary>
		internal IndexHeader ToHeader()
		{
			return new IndexHeader
			{
				CheckpointInterval = _checkpointInterval,
				Encoding = this.Encoding,
				LineCount = this.LineCount,
				IndexedLength = this.IndexedLength,
				EndState = _endState,
				HeadHash = _headHash,
				TailHash = _tailHash,
				CheckpointCount = _checkpoints.Count,
			};
		}

		/// <summary>
		/// The index a saved index holds, from its header and its checkpoints, which must agree: see <see cref="IndexHeader.IsConsistent"/>.
		/// </summary>
		internal static LineIndex FromSaved(IndexHeader header, List<long> checkpoints)
		{
			return new LineIndex(header.CheckpointInterval, checkpoints)
			{
				Encoding = header.Encoding,
				LineCount = header.LineCount,
				IndexedLength = header.IndexedLength,
				_endState = header.EndState,
				_headHash = header.HeadHash,
				_tailHash = header.TailHash,
			};
		}

		/// <summary>
		/// Identifies the scanned content by hashing its first and last few KB.
		/// </summary>
		private static (byte[] HeadHash, byte[] TailHash) ComputeFingerprint(Stream log, long length)
		{
			var count = (int)Math.Min(FingerprintLength, length);

			return (HashRange(log, 0, count), HashRange(log, length - count, count));
		}

		private static byte[] HashRange(Stream log, long offset, int count)
		{
			var bytes = new byte[count];

			log.Position = offset;
			log.ReadExactly(bytes);

			return SHA256.HashData(bytes);
		}

		/// <summary>
		/// Reports the start of each line from <paramref name="startOffset"/>, until <paramref name="onLineStart"/> returns false or the log ends.
		/// </summary>
		/// <returns>The offset after the last whole code unit scanned.</returns>
		private long Scan(Stream log, long startOffset, ref ScanState state, int bufferSize, Func<long, bool> onLineStart)
		{
			var buffer = new byte[Math.Max(bufferSize, MinimumBufferSize)];
			var unitSize = this.Encoding.UnitSize;
			var position = startOffset;
			var carried = 0;
			int read;

			log.Position = startOffset;

			while ((read = log.Read(buffer, carried, buffer.Length - carried)) > 0)
			{
				var available = carried + read;
				var wholeUnits = available - available % unitSize;

				position += ScanBytes(buffer.AsSpan(0, wholeUnits), position, ref state, onLineStart, out var isStopped);

				if (isStopped)
				{
					break;
				}

				// A code unit split across reads is completed by the next read.
				carried = available - wholeUnits;
				Array.Copy(buffer, wholeUnits, buffer, 0, carried);
			}

			return position;
		}

		/// <param name="bytes">Whole code units, from <paramref name="position"/> in the log.</param>
		/// <returns>The number of bytes scanned: all of them, unless <paramref name="onLineStart"/> stopped the scan at a line's start.</returns>
		private int ScanBytes(ReadOnlySpan<byte> bytes, long position, ref ScanState state, Func<long, bool> onLineStart, out bool isStopped)
		{
			LogEncoding encoding = this.Encoding;
			var unitSize = encoding.UnitSize;
			isStopped = false;
			var i = 0;

			while (i < bytes.Length)
			{
				if (state.IsCarriageReturnPending)
				{
					state.IsCarriageReturnPending = false;

					if (encoding.IsLineFeed(bytes.Slice(i, unitSize)))
					{
						// The line feed completes the \r\n line break.
						i += unitSize;
						continue;
					}
				}

				if (state.IsLineStartPending)
				{
					state.IsLineStartPending = false;

					if (!onLineStart(position + i))
					{
						isStopped = true;
						return i;
					}
				}

				var lineBreak = encoding.IndexOfLineBreak(bytes.Slice(i), out var isCarriageReturn);

				if (lineBreak < 0)
				{
					break;
				}

				i += lineBreak + unitSize;
				state.IsCarriageReturnPending = isCarriageReturn;
				state.IsLineStartPending = true;
			}

			return bytes.Length;
		}

		/// <summary>
		/// Where the scan stopped: in a line, after a line break, or after a <c>\r</c> that may be followed by <c>\n</c>.
		/// </summary>
		internal struct ScanState
		{
			public static readonly ScanState AtLineStart = new ScanState { IsLineStartPending = true };

			public bool IsLineStartPending;
			public bool IsCarriageReturnPending;
		}
	}
}
