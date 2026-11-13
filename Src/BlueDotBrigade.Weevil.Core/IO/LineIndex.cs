namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Buffers.Binary;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Security.Cryptography;
	using System.Text;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// The byte offset of every line in a log file, found by scanning the file's bytes for line breaks.
	/// </summary>
	/// <remarks>
	/// Lines are counted the way <see cref="StreamReader.ReadLine"/> counts them when the encoding is detected from a byte order mark:
	/// <c>\n</c>, <c>\r</c> and <c>\r\n</c> end a line, and the byte order mark is skipped.
	/// To keep the index small, only the offset of every <see cref="CheckpointInterval"/>th line is stored.
	/// Other offsets are found by scanning forward from the nearest stored one.
	/// Methods that read the log restore the position of its stream.
	/// </remarks>
	internal sealed class LineIndex
	{
		public const int DefaultCheckpointInterval = 64;
		public const int DefaultBufferSize = 1024 * 1024;
		public const int DefaultLookupBufferSize = 16 * 1024;

		// Version 2 adds sections, each with its own checksum, and what parsed the log (#1023). An index of another version is rebuilt.
		private const int FormatVersion = 2;
		private const int FingerprintLength = 4096;
		private const int HashLength = 32;
		private const int MinimumBufferSize = 4;

		private static readonly byte[] Signature = "WIDX"u8.ToArray();

		private readonly int _checkpointInterval;
		private readonly List<long> _checkpoints;

		private int _unitSize;
		private bool _isBigEndian;
		private int _preambleLength;

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
		/// What parsed the log, and the checksums of the bytes it read, after a full parse: otherwise <see langword="null"/> (#1023).
		/// </summary>
		/// <remarks>
		/// It describes the log as it was parsed, so it may cover less of the log than the index, after <see cref="Update"/>,
		/// or more, for a log written to while it was parsed after being indexed.
		/// </remarks>
		public SavedParse Parse { get; set; }

		/// <summary>
		/// The saved index holds the record table its <see cref="Parse"/> made, from <see cref="TableOffset"/> in the file (#1023).
		/// </summary>
		public bool HasTable { get; private set; }

		/// <summary>
		/// Where the record table's sections start in the saved index, when it has one.
		/// </summary>
		public long TableOffset { get; private set; }

		/// <summary>
		/// The log's encoding, without a byte order mark, as the parser decodes it.
		/// </summary>
		public Encoding ContentEncoding => CreateEncoding(_unitSize, _isBigEndian);

		/// <summary>
		/// Scans the whole log.
		/// </summary>
		public static LineIndex Build(Stream source, int checkpointInterval = DefaultCheckpointInterval, int bufferSize = DefaultBufferSize)
		{
			if (checkpointInterval < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(checkpointInterval), checkpointInterval, "At least one line per checkpoint was expected.");
			}

			var index = new LineIndex(checkpointInterval, new List<long>());
			index.Update(source, bufferSize);

			return index;
		}

		/// <summary>
		/// Scans the content added to the log since it was last scanned.
		/// </summary>
		/// <remarks>
		/// Logs only grow by appending, so the content already scanned is assumed to be unchanged.
		/// Use <see cref="TryRead"/> to check this for a saved index.
		/// </remarks>
		public void Update(Stream source, int bufferSize = DefaultBufferSize)
		{
			var originalPosition = source.Position;

			try
			{
				// When the log was last scanned, its byte order mark may not have been written yet, or only in part.
				(int UnitSize, bool IsBigEndian, int PreambleLength) encoding = DetectEncoding(source);

				if (encoding != (_unitSize, _isBigEndian, _preambleLength))
				{
					Reset(encoding);
				}

				var lineCount = LineCount;
				ScanState state = _endState;

				IndexedLength = Scan(source, IndexedLength, ref state, bufferSize, lineStart =>
				{
					lineCount = checked(lineCount + 1);

					if ((lineCount - 1) % _checkpointInterval == 0)
					{
						_checkpoints.Add(lineStart);
					}

					return true;
				});

				LineCount = lineCount;
				_endState = state;
				(_headHash, _tailHash) = ComputeFingerprint(source, IndexedLength);
			}
			finally
			{
				source.Position = originalPosition;
			}
		}

		/// <summary>
		/// Forgets everything scanned, so that scanning restarts after the byte order mark of the given encoding.
		/// </summary>
		private void Reset((int UnitSize, bool IsBigEndian, int PreambleLength) encoding)
		{
			(_unitSize, _isBigEndian, _preambleLength) = encoding;
			_checkpoints.Clear();
			this.Parse = null;
			this.HasTable = false;
			LineCount = 0;
			IndexedLength = encoding.PreambleLength;
			_endState = ScanState.AtLineStart;
		}

		/// <summary>
		/// Returns the byte offset where the specified line begins.
		/// </summary>
		public long GetByteOffset(Stream source, int lineNumber, int bufferSize = DefaultLookupBufferSize)
		{
			if (lineNumber < 1 || lineNumber > LineCount)
			{
				throw new ArgumentOutOfRangeException(nameof(lineNumber), lineNumber, $"A line number from 1 to {LineCount} was expected.");
			}

			var checkpointOffset = _checkpoints[(lineNumber - 1) / _checkpointInterval];
			var linesAfterCheckpoint = (lineNumber - 1) % _checkpointInterval;

			if (linesAfterCheckpoint == 0)
			{
				return checkpointOffset;
			}

			var originalPosition = source.Position;

			try
			{
				// The checkpoint is a line start, so it is reported first.
				var linesFound = -1;
				var byteOffset = -1L;
				ScanState state = ScanState.AtLineStart;

				Scan(source, checkpointOffset, ref state, bufferSize, lineStart =>
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
				source.Position = originalPosition;
			}
		}

		/// <summary>
		/// Saves the index in a binary format that <see cref="TryRead"/> can read.
		/// </summary>
		/// <remarks>
		/// After the signature and the format version come sections, each with its own checksum (#1023): the header, the checkpoints,
		/// what parsed the log when <see cref="Parse"/> is set, and an end marker. They stream, so the file can be larger than 2 GB.
		/// </remarks>
		/// <param name="destination">A stream that can seek, such as a file.</param>
		/// <param name="table">The record table that <see cref="Parse"/> made, to save after it, or <see langword="null"/> (#1023).</param>
		public void Write(Stream destination, RecordTable table = null)
		{
			if (table != null && this.Parse == null)
			{
				throw new InvalidOperationException("A record table is saved only with the parse that made it.");
			}

			using (var writer = new BinaryWriter(destination, Encoding.UTF8, leaveOpen: true))
			{
				writer.Write(Signature);
				writer.Write(FormatVersion);
			}

			IndexSection.Write(destination, IndexSectionKind.Header, writer =>
			{
				writer.Write(_checkpointInterval);
				writer.Write((byte)_unitSize);
				writer.Write(_isBigEndian);
				writer.Write((byte)_preambleLength);
				writer.Write(LineCount);
				writer.Write(IndexedLength);
				writer.Write(_endState.IsLineStartPending);
				writer.Write(_endState.IsCarriageReturnPending);
				writer.Write(_headHash);
				writer.Write(_tailHash);
				writer.Write(_checkpoints.Count);
				writer.Write(this.Parse != null);
				writer.Write(table != null);
			});

			IndexSection.Write(destination, IndexSectionKind.Checkpoints, writer =>
			{
				foreach (var checkpoint in _checkpoints)
				{
					writer.Write(checkpoint);
				}
			});

			if (this.Parse != null)
			{
				IndexSection.Write(destination, IndexSectionKind.Parse, this.Parse.Write);
			}

			if (table != null)
			{
				this.TableOffset = destination.Position;
				IndexSection.Write(destination, IndexSectionKind.Chunks, table.WriteChunks);
				IndexSection.Write(destination, IndexSectionKind.Strings, table.WriteStrings);
				IndexSection.Write(destination, IndexSectionKind.Rows, table.WriteRows);
			}

			this.HasTable = table != null;

			IndexSection.Write(destination, IndexSectionKind.End, writeBytes: null);
		}

		/// <summary>
		/// Reads the line count and the length of the log it covers from the header of an index saved by <see cref="Write"/>, without checking the rest of the index or the log.
		/// </summary>
		/// <remarks>
		/// For an estimate only, such as the memory a log needs (#1028): the index may be damaged, or belong to a log that has changed since.
		/// </remarks>
		public static bool TryReadLineCount(Stream indexSource, out int lineCount, out long indexedLength)
		{
			lineCount = 0;
			indexedLength = 0;

			try
			{
				if (!TryReadSignature(indexSource) || !IndexSection.TryRead(indexSource, IndexSectionKind.Header, out IndexSection.Reader header))
				{
					return false;
				}

				using (header)
				{
					BinaryReader reader = header.Binary;
					reader.ReadInt32(); // checkpoint interval
					reader.ReadByte(); // unit size
					reader.ReadBoolean(); // big endian
					reader.ReadByte(); // preamble length
					lineCount = reader.ReadInt32();
					indexedLength = reader.ReadInt64();
				}
			}
			catch (EndOfStreamException)
			{
				return false;
			}

			// Each line but the last holds at least its line break, so a damaged header cannot claim more lines than bytes.
			return lineCount >= 0 && indexedLength >= 0 && lineCount <= indexedLength + 1;
		}

		/// <summary>
		/// Reads an index saved by <see cref="Write"/>, provided it is valid and the content it scanned is unchanged.
		/// </summary>
		/// <remarks>
		/// The log is identified by its content only, so an index stays valid when the files are copied elsewhere.
		/// If the log has grown since, call <see cref="Update"/> to scan the new content.
		/// The index is read as it streams, so its size has no limit.
		/// </remarks>
		public static bool TryRead(Stream indexSource, Stream logSource, out LineIndex index)
		{
			index = null;

			try
			{
				if (!TryReadSignature(indexSource) || !IndexSection.TryRead(indexSource, IndexSectionKind.Header, out IndexSection.Reader header))
				{
					return false;
				}

				int checkpointInterval;
				int unitSize;
				bool isBigEndian;
				int preambleLength;
				int lineCount;
				long indexedLength;
				ScanState endState;
				byte[] headHash;
				byte[] tailHash;
				int checkpointCount;
				bool hasParse;
				bool hasTable;

				using (header)
				{
					BinaryReader reader = header.Binary;
					checkpointInterval = reader.ReadInt32();
					unitSize = reader.ReadByte();
					isBigEndian = reader.ReadBoolean();
					preambleLength = reader.ReadByte();
					lineCount = reader.ReadInt32();
					indexedLength = reader.ReadInt64();
					endState = new ScanState
					{
						IsLineStartPending = reader.ReadBoolean(),
						IsCarriageReturnPending = reader.ReadBoolean(),
					};
					headHash = reader.ReadBytes(HashLength);
					tailHash = reader.ReadBytes(HashLength);
					checkpointCount = reader.ReadInt32();
					hasParse = reader.ReadBoolean();
					hasTable = reader.ReadBoolean();

					if (!header.IsIntact)
					{
						return false;
					}
				}

				var isConsistent =
					(hasParse || !hasTable) &&
					checkpointInterval >= 1 &&
					lineCount >= 0 &&
					checkpointCount == (lineCount + (long)checkpointInterval - 1) / checkpointInterval &&
					headHash.Length == HashLength &&
					tailHash.Length == HashLength &&
					indexedLength >= preambleLength &&
					logSource.Length >= indexedLength;

				if (!isConsistent || !IndexSection.TryRead(indexSource, IndexSectionKind.Checkpoints, out IndexSection.Reader checkpointSection))
				{
					return false;
				}

				List<long> checkpoints;

				using (checkpointSection)
				{
					// Checked before the list is made: the checksum detects damage, not a file made to claim billions of lines.
					if (checkpointSection.Remaining != checkpointCount * (long)sizeof(long))
					{
						return false;
					}

					checkpoints = new List<long>(checkpointCount);

					for (var i = 0; i < checkpointCount; i++)
					{
						checkpoints.Add(checkpointSection.Binary.ReadInt64());
					}

					if (!checkpointSection.IsIntact)
					{
						return false;
					}
				}

				SavedParse parse = null;

				if (hasParse)
				{
					if (!IndexSection.TryRead(indexSource, IndexSectionKind.Parse, out IndexSection.Reader parseSection))
					{
						return false;
					}

					using (parseSection)
					{
						parse = SavedParse.Read(parseSection, logSource.Length);

						if (parse is null || !parseSection.IsIntact)
						{
							return false;
						}
					}
				}

				// The record table is checked only when it is used, so a damaged table costs only the table (#1023).
				long tableOffset = 0;

				if (hasTable)
				{
					tableOffset = indexSource.Position;

					if (!IndexSection.TrySkip(indexSource, IndexSectionKind.Chunks) ||
					    !IndexSection.TrySkip(indexSource, IndexSectionKind.Strings) ||
					    !IndexSection.TrySkip(indexSource, IndexSectionKind.Rows))
					{
						return false;
					}
				}

				// The end marker, at the end of the file, so a truncated or extended file is rejected.
				if (!IndexSection.TryRead(indexSource, IndexSectionKind.End, out IndexSection.Reader end))
				{
					return false;
				}

				using (end)
				{
					if (!end.IsIntact || indexSource.Position != indexSource.Length)
					{
						return false;
					}
				}

				var originalPosition = logSource.Position;

				try
				{
					if (DetectEncoding(logSource) != (unitSize, isBigEndian, preambleLength))
					{
						return false;
					}

					(byte[] actualHeadHash, byte[] actualTailHash) = ComputeFingerprint(logSource, indexedLength);

					if (!actualHeadHash.AsSpan().SequenceEqual(headHash) || !actualTailHash.AsSpan().SequenceEqual(tailHash))
					{
						return false;
					}
				}
				finally
				{
					logSource.Position = originalPosition;
				}

				index = new LineIndex(checkpointInterval, checkpoints)
				{
					_unitSize = unitSize,
					_isBigEndian = isBigEndian,
					_preambleLength = preambleLength,
					LineCount = lineCount,
					IndexedLength = indexedLength,
					_endState = endState,
					_headHash = headHash,
					_tailHash = tailHash,
					Parse = parse,
					HasTable = hasTable,
					TableOffset = tableOffset,
				};

				return true;
			}
			catch (Exception exception) when (exception is EndOfStreamException || exception is FormatException || exception is ArgumentException)
			{
				// A damaged file: a string's length, or a build ID, that is not what was written.
				return false;
			}
		}

		/// <summary>
		/// The encoding of a log whose characters' code units are <paramref name="unitSize"/> bytes, without a byte order mark.
		/// </summary>
		internal static Encoding CreateEncoding(int unitSize, bool isBigEndian)
		{
			return unitSize switch
			{
				1 => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
				2 => new UnicodeEncoding(isBigEndian, byteOrderMark: false),
				_ => new UTF32Encoding(isBigEndian, byteOrderMark: false),
			};
		}

		private static bool TryReadSignature(Stream indexSource)
		{
			using var reader = new BinaryReader(indexSource, Encoding.UTF8, leaveOpen: true);

			return reader.ReadBytes(Signature.Length).AsSpan().SequenceEqual(Signature) &&
			       reader.ReadInt32() == FormatVersion;
		}

		/// <summary>
		/// Detects the encoding from a byte order mark, with the same checks, in the same order, as <see cref="StreamReader"/>.
		/// </summary>
		/// <returns>The size of each character's code unit in bytes, its byte order, and the length of the byte order mark.</returns>
		internal static (int UnitSize, bool IsBigEndian, int PreambleLength) DetectEncoding(Stream source)
		{
			Span<byte> b = stackalloc byte[4];

			source.Position = 0;
			var count = source.ReadAtLeast(b, b.Length, throwOnEndOfStream: false);

			if (count >= 2 && b[0] == 0xFE && b[1] == 0xFF)
			{
				return (2, true, 2);
			}

			if (count >= 2 && b[0] == 0xFF && b[1] == 0xFE)
			{
				return count >= 4 && b[2] == 0 && b[3] == 0
					? (4, false, 4)
					: (2, false, 2);
			}

			if (count >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF)
			{
				return (1, false, 3);
			}

			if (count >= 4 && b[0] == 0 && b[1] == 0 && b[2] == 0xFE && b[3] == 0xFF)
			{
				return (4, true, 4);
			}

			return (1, false, 0);
		}

		/// <summary>
		/// Identifies the scanned content by hashing its first and last few KB.
		/// </summary>
		private static (byte[] HeadHash, byte[] TailHash) ComputeFingerprint(Stream source, long length)
		{
			var count = (int)Math.Min(FingerprintLength, length);

			return (HashRange(source, 0, count), HashRange(source, length - count, count));
		}

		private static byte[] HashRange(Stream source, long offset, int count)
		{
			var bytes = new byte[count];

			source.Position = offset;
			source.ReadExactly(bytes);

			return SHA256.HashData(bytes);
		}

		/// <summary>
		/// Reports the start of each line from <paramref name="startOffset"/>, until <paramref name="onLineStart"/> returns false or the log ends.
		/// </summary>
		/// <returns>The offset after the last whole code unit scanned.</returns>
		private long Scan(Stream source, long startOffset, ref ScanState state, int bufferSize, Func<long, bool> onLineStart)
		{
			var buffer = new byte[Math.Max(bufferSize, MinimumBufferSize)];
			var position = startOffset;
			var carried = 0;
			int read;

			source.Position = startOffset;

			while ((read = source.Read(buffer, carried, buffer.Length - carried)) > 0)
			{
				var available = carried + read;
				var wholeUnits = available - available % _unitSize;

				var unitsScanned = ScanUnits(buffer.AsSpan(0, wholeUnits), position, ref state, onLineStart, out var isStopped);
				position += unitsScanned * (long)_unitSize;

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

		private int ScanUnits(ReadOnlySpan<byte> bytes, long position, ref ScanState state, Func<long, bool> onLineStart, out bool isStopped)
		{
			// Each line break is searched for as a code unit in the file's byte order.
			var isSameByteOrder = _isBigEndian != BitConverter.IsLittleEndian;

			return _unitSize switch
			{
				1 => ScanUnits(bytes, position, (byte)'\r', (byte)'\n', ref state, onLineStart, out isStopped),
				2 => ScanUnits(
					MemoryMarshal.Cast<byte, ushort>(bytes), position,
					isSameByteOrder ? (ushort)'\r' : BinaryPrimitives.ReverseEndianness((ushort)'\r'),
					isSameByteOrder ? (ushort)'\n' : BinaryPrimitives.ReverseEndianness((ushort)'\n'),
					ref state, onLineStart, out isStopped),
				_ => ScanUnits(
					MemoryMarshal.Cast<byte, uint>(bytes), position,
					isSameByteOrder ? (uint)'\r' : BinaryPrimitives.ReverseEndianness((uint)'\r'),
					isSameByteOrder ? (uint)'\n' : BinaryPrimitives.ReverseEndianness((uint)'\n'),
					ref state, onLineStart, out isStopped),
			};
		}

		private int ScanUnits<T>(ReadOnlySpan<T> units, long position, T carriageReturn, T lineFeed, ref ScanState state, Func<long, bool> onLineStart, out bool isStopped)
			where T : unmanaged, IEquatable<T>
		{
			isStopped = false;
			var i = 0;

			while (i < units.Length)
			{
				if (state.IsCarriageReturnPending)
				{
					state.IsCarriageReturnPending = false;

					if (units[i].Equals(lineFeed))
					{
						// The line feed completes the \r\n line break.
						i++;
						continue;
					}
				}

				if (state.IsLineStartPending)
				{
					state.IsLineStartPending = false;

					if (!onLineStart(position + i * (long)_unitSize))
					{
						isStopped = true;
						return i;
					}
				}

				var lineBreak = units.Slice(i).IndexOfAny(carriageReturn, lineFeed);

				if (lineBreak < 0)
				{
					break;
				}

				i += lineBreak;
				state.IsCarriageReturnPending = units[i].Equals(carriageReturn);
				state.IsLineStartPending = true;
				i++;
			}

			return units.Length;
		}

		/// <summary>
		/// Where the scan stopped: in a line, after a line break, or after a <c>\r</c> that may be followed by <c>\n</c>.
		/// </summary>
		private struct ScanState
		{
			public static readonly ScanState AtLineStart = new ScanState { IsLineStartPending = true };

			public bool IsLineStartPending;
			public bool IsCarriageReturnPending;
		}
	}
}
