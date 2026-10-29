namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Buffers.Binary;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Text;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// Reads a log one line at a time, as <see cref="StreamReader.ReadLine"/> does, and also says where each line's bytes are in the file,
	/// so records can keep the bytes instead of a string (#1133).
	/// </summary>
	/// <remarks>
	/// As with <see cref="StreamReader"/>:
	/// - The encoding is found from a byte order mark at the top of the file: UTF-8 when there is none.
	/// - <c>\n</c>, <c>\r</c> and <c>\r\n</c> end a line, and a line break at the end of the file is not followed by an empty line.
	/// - Invalid bytes become U+FFFD.
	/// The reader keeps every byte from the start of the current chunk, so <see cref="TakeChunk"/> can hand them over without reading the file again.
	/// </remarks>
	internal sealed class LogLineReader
	{
		/// <summary>
		/// About how many bytes a chunk holds: large enough to be few, and small enough to be released one at a time (#1022).
		/// </summary>
		public const int DefaultChunkSize = 16 * 1024 * 1024;

		private const int InitialBufferSize = 64 * 1024;

		private readonly Stream _source;
		private readonly int _unitSize;
		private readonly bool _isBigEndian;
		private readonly int _chunkSize;

		// The bytes read so far, from the start of the current chunk: `_buffer[0]` is at `_bufferOffset` in the file.
		private byte[] _buffer = new byte[InitialBufferSize];
		private long _bufferOffset;
		private int _filled;

		// Where the next line starts, and how far the search for its line break has got, both in `_buffer`.
		private int _lineStart;
		private int _searched;

		private long _chunkStart;
		private bool _isEndOfFile;

		/// <param name="source">The log, which must be able to seek.</param>
		/// <param name="startOffset">Where to start reading: the start of a line, or 0 for the top of the file.</param>
		/// <param name="chunkSize">About how many bytes each chunk holds.</param>
		public LogLineReader(Stream source, long startOffset = 0, int chunkSize = DefaultChunkSize)
		{
			_source = source ?? throw new ArgumentNullException(nameof(source));
			_chunkSize = chunkSize > 0 ? chunkSize : throw new ArgumentOutOfRangeException(nameof(chunkSize), chunkSize, "Value is expected to be greater than zero.");

			// The byte order mark is only at the top of the file, so it is read there even when reading starts further in.
			(_unitSize, _isBigEndian, var preambleLength) = LineIndex.DetectEncoding(source);

			this.Encoding = _unitSize switch
			{
				1 => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
				2 => new UnicodeEncoding(_isBigEndian, byteOrderMark: false),
				_ => new UTF32Encoding(_isBigEndian, byteOrderMark: false),
			};

			_bufferOffset = Math.Max(startOffset, preambleLength);
			_chunkStart = _bufferOffset;
			_source.Position = _bufferOffset;
		}

		/// <summary>
		/// The log's encoding, without a byte order mark.
		/// </summary>
		public Encoding Encoding { get; }

		/// <summary>
		/// Where the next line starts in the file.
		/// </summary>
		public long Position => _bufferOffset + _lineStart;

		/// <summary>
		/// Reads the next line, without its line break.
		/// </summary>
		/// <param name="offset">Where the line starts in the file.</param>
		/// <param name="length">The length of the line in bytes, without its line break.</param>
		/// <param name="text">The line, decoded.</param>
		/// <returns><see langword="false"/> at the end of the file.</returns>
		public bool TryReadLine(out long offset, out int length, out string text)
		{
			while (true)
			{
				var whole = (_filled - _searched) / _unitSize * _unitSize;
				var found = IndexOfLineBreak(_buffer.AsSpan(_searched, whole), out var isCarriageReturn);

				if (found >= 0)
				{
					var lineBreak = _searched + found;
					var afterBreak = lineBreak + _unitSize;

					if (isCarriageReturn)
					{
						if (afterBreak + _unitSize > _filled && !_isEndOfFile)
						{
							// Whether a \n follows the \r is not known yet.
							_searched = lineBreak;
							ReadMore();
							continue;
						}

						if (afterBreak + _unitSize <= _filled && IsLineFeed(_buffer.AsSpan(afterBreak, _unitSize)))
						{
							afterBreak += _unitSize;
						}
					}

					offset = _bufferOffset + _lineStart;
					length = lineBreak - _lineStart;
					text = this.Encoding.GetString(_buffer, _lineStart, length);

					_lineStart = afterBreak;
					_searched = afterBreak;
					return true;
				}

				_searched += whole;

				if (_isEndOfFile)
				{
					if (_lineStart < _filled)
					{
						// The last line has no line break, and may end with part of a code unit, which decodes as U+FFFD.
						offset = _bufferOffset + _lineStart;
						length = _filled - _lineStart;
						text = this.Encoding.GetString(_buffer, _lineStart, length);

						_lineStart = _filled;
						_searched = _filled;
						return true;
					}

					offset = this.Position;
					length = 0;
					text = null;
					return false;
				}

				ReadMore();
			}
		}

		/// <summary>
		/// Returns <see langword="true"/> when the current chunk would hold at least the chunk size if it ended at <paramref name="endOffset"/>.
		/// </summary>
		public bool IsChunkFull(long endOffset)
		{
			return endOffset - _chunkStart >= _chunkSize;
		}

		/// <summary>
		/// Hands over the bytes from the end of the previous chunk to <paramref name="endOffset"/>, a point already read, as a chunk.
		/// </summary>
		public ContentChunk TakeChunk(long endOffset)
		{
			var start = (int)(_chunkStart - _bufferOffset);

			// The chunk copies the bytes, so the buffer can be reused.
			var chunk = new ContentChunk(_chunkStart, _buffer.AsSpan(start, (int)(endOffset - _chunkStart)), this.Encoding);
			_chunkStart = endOffset;

			return chunk;
		}

		/// <summary>
		/// Lets go of the bytes from the end of the previous chunk to <paramref name="endOffset"/>, which no record needs.
		/// </summary>
		public void SkipChunk(long endOffset)
		{
			_chunkStart = endOffset;
		}

		/// <summary>
		/// Reads more of the file, first moving the current chunk's bytes to the start of the buffer, or growing the buffer, when it is full.
		/// </summary>
		private void ReadMore()
		{
			if (_filled == _buffer.Length)
			{
				var kept = (int)(_chunkStart - _bufferOffset);

				if (kept > 0)
				{
					Buffer.BlockCopy(_buffer, kept, _buffer, 0, _filled - kept);
					_bufferOffset += kept;
					_filled -= kept;
					_lineStart -= kept;
					_searched -= kept;
				}
				else
				{
					Array.Resize(ref _buffer, _buffer.Length * 2);
				}
			}

			var read = _source.Read(_buffer, _filled, _buffer.Length - _filled);

			if (read == 0)
			{
				_isEndOfFile = true;
			}
			else
			{
				_filled += read;
			}
		}

		/// <summary>
		/// Finds the first code unit that is <c>\r</c> or <c>\n</c>, in the file's byte order, and returns its offset in bytes.
		/// </summary>
		private int IndexOfLineBreak(ReadOnlySpan<byte> bytes, out bool isCarriageReturn)
		{
			var isSameByteOrder = _isBigEndian != BitConverter.IsLittleEndian;
			int index;

			switch (_unitSize)
			{
				case 1:
					index = bytes.IndexOfAny((byte)'\r', (byte)'\n');
					isCarriageReturn = index >= 0 && bytes[index] == (byte)'\r';
					return index;

				case 2:
					ReadOnlySpan<ushort> units16 = MemoryMarshal.Cast<byte, ushort>(bytes);
					var carriageReturn16 = isSameByteOrder ? (ushort)'\r' : BinaryPrimitives.ReverseEndianness((ushort)'\r');
					var lineFeed16 = isSameByteOrder ? (ushort)'\n' : BinaryPrimitives.ReverseEndianness((ushort)'\n');
					index = units16.IndexOfAny(carriageReturn16, lineFeed16);
					isCarriageReturn = index >= 0 && units16[index] == carriageReturn16;
					return index < 0 ? -1 : index * 2;

				default:
					ReadOnlySpan<uint> units32 = MemoryMarshal.Cast<byte, uint>(bytes);
					var carriageReturn32 = isSameByteOrder ? '\r' : BinaryPrimitives.ReverseEndianness((uint)'\r');
					var lineFeed32 = isSameByteOrder ? '\n' : BinaryPrimitives.ReverseEndianness((uint)'\n');
					index = units32.IndexOfAny(carriageReturn32, lineFeed32);
					isCarriageReturn = index >= 0 && units32[index] == carriageReturn32;
					return index < 0 ? -1 : index * 4;
			}
		}

		private bool IsLineFeed(ReadOnlySpan<byte> unit)
		{
			return _unitSize switch
			{
				1 => unit[0] == (byte)'\n',
				2 => (_isBigEndian ? BinaryPrimitives.ReadUInt16BigEndian(unit) : BinaryPrimitives.ReadUInt16LittleEndian(unit)) == '\n',
				_ => (_isBigEndian ? BinaryPrimitives.ReadUInt32BigEndian(unit) : BinaryPrimitives.ReadUInt32LittleEndian(unit)) == '\n',
			};
		}
	}
}
