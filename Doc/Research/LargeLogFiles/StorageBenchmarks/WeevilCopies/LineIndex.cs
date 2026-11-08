namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Buffers.Binary;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Security.Cryptography;
	using System.Text;

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

		private const int FormatVersion = 1;
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
		public void Write(Stream destination)
		{
			using var payload = new MemoryStream();
			using var writer = new BinaryWriter(payload, Encoding.UTF8, leaveOpen: true);

			writer.Write(Signature);
			writer.Write(FormatVersion);
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

			foreach (var checkpoint in _checkpoints)
			{
				writer.Write(checkpoint);
			}

			writer.Flush();

			// The checksum identifies a damaged index file, so that it is rebuilt.
			var payloadBytes = payload.GetBuffer().AsSpan(0, (int)payload.Length);
			destination.Write(payloadBytes);
			destination.Write(SHA256.HashData(payloadBytes));
		}

		/// <summary>
		/// Reads an index saved by <see cref="Write"/>, provided it is valid and the content it scanned is unchanged.
		/// </summary>
		/// <remarks>
		/// The log is identified by its content only, so an index stays valid when the files are copied elsewhere.
		/// If the log has grown since, call <see cref="Update"/> to scan the new content.
		/// </remarks>
		public static bool TryRead(Stream indexSource, Stream logSource, out LineIndex index)
		{
			index = null;

			var length = indexSource.Length - indexSource.Position;

			if (length < HashLength || length > int.MaxValue)
			{
				return false;
			}

			var bytes = new byte[length];
			indexSource.ReadExactly(bytes);

			var payloadLength = bytes.Length - HashLength;

			if (!SHA256.HashData(bytes.AsSpan(0, payloadLength)).AsSpan().SequenceEqual(bytes.AsSpan(payloadLength)))
			{
				return false;
			}

			try
			{
				using var reader = new BinaryReader(new MemoryStream(bytes, 0, payloadLength));

				if (!reader.ReadBytes(Signature.Length).AsSpan().SequenceEqual(Signature) ||
				    reader.ReadInt32() != FormatVersion)
				{
					return false;
				}

				var checkpointInterval = reader.ReadInt32();
				int unitSize = reader.ReadByte();
				var isBigEndian = reader.ReadBoolean();
				int preambleLength = reader.ReadByte();
				var lineCount = reader.ReadInt32();
				var indexedLength = reader.ReadInt64();
				var endState = new ScanState
				{
					IsLineStartPending = reader.ReadBoolean(),
					IsCarriageReturnPending = reader.ReadBoolean(),
				};
				var headHash = reader.ReadBytes(HashLength);
				var tailHash = reader.ReadBytes(HashLength);
				var checkpointCount = reader.ReadInt32();

				var isConsistent =
					checkpointInterval >= 1 &&
					lineCount >= 0 &&
					checkpointCount == (lineCount + (long)checkpointInterval - 1) / checkpointInterval &&
					reader.BaseStream.Length - reader.BaseStream.Position == checkpointCount * (long)sizeof(long) &&
					headHash.Length == HashLength &&
					tailHash.Length == HashLength &&
					indexedLength >= preambleLength &&
					logSource.Length >= indexedLength;

				if (!isConsistent)
				{
					return false;
				}

				var checkpoints = new List<long>(checkpointCount);

				for (var i = 0; i < checkpointCount; i++)
				{
					checkpoints.Add(reader.ReadInt64());
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
				};

				return true;
			}
			catch (EndOfStreamException)
			{
				return false;
			}
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
