namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;
	using System.IO.Hashing;
	using System.Text;

	/// <summary>
	/// The kinds of section in a saved index, in the order they are written (#1023).
	/// </summary>
	internal enum IndexSectionKind
	{
		/// <summary>
		/// Marks the end of the file, so a truncated file is rejected.
		/// </summary>
		End = 0,

		Header = 1,

		Checkpoints = 2,

		/// <summary>
		/// What parsed the log, and the checksums of the bytes it read. Only an index saved after a full parse has it.
		/// </summary>
		Parse = 3,

		/// <summary>
		/// The record table's chunk directory. This and the next two sections hold the table a full parse made.
		/// </summary>
		Chunks = 4,

		/// <summary>
		/// The strings of the rows that keep one.
		/// </summary>
		Strings = 5,

		/// <summary>
		/// Every row's columns.
		/// </summary>
		Rows = 6,
	}

	/// <summary>
	/// Writes and reads a section of a saved index: its kind, its length and an XxHash3 checksum of its bytes, then the bytes (#1023).
	/// </summary>
	/// <remarks>
	/// A section streams, and its length is a 64-bit number, so no buffer holds a whole section, and an index can be larger than 2 GB.
	/// The checksum lets a damaged index be rejected, so it is rebuilt.
	/// </remarks>
	internal static class IndexSection
	{
		// Kind, length and checksum.
		private const int SectionHeaderLength = sizeof(int) + sizeof(long) + sizeof(ulong);

		/// <summary>
		/// Writes a section whose bytes <paramref name="writeBytes"/> writes, then fills in its length and checksum.
		/// </summary>
		/// <param name="destination">A stream that can seek, such as a file, positioned where the section starts.</param>
		public static void Write(Stream destination, IndexSectionKind kind, Action<BinaryWriter> writeBytes)
		{
			ArgumentNullException.ThrowIfNull(destination);

			if (!destination.CanSeek)
			{
				throw new ArgumentException("A saved index is written to a stream that can seek.", nameof(destination));
			}

			var sectionStart = destination.Position;
			destination.Write(new byte[SectionHeaderLength]);

			var bytes = new HashingStream(destination);

			using (var writer = new BinaryWriter(bytes, Encoding.UTF8, leaveOpen: true))
			{
				writeBytes?.Invoke(writer);
			}

			bytes.Flush();

			var sectionEnd = destination.Position;
			destination.Position = sectionStart;

			using (var header = new BinaryWriter(destination, Encoding.UTF8, leaveOpen: true))
			{
				header.Write((int)kind);
				header.Write(bytes.Length);
				header.Write(bytes.GetHash());
			}

			destination.Position = sectionEnd;
		}

		/// <summary>
		/// Reads the header of the next section, and returns a stream of its bytes when it is of <paramref name="expectedKind"/>.
		/// </summary>
		/// <remarks>
		/// Once its bytes are read, call <see cref="Reader.IsIntact"/>: the checksum can be checked only after every byte.
		/// </remarks>
		public static bool TryRead(Stream source, IndexSectionKind expectedKind, out Reader reader)
		{
			reader = null;

			using var header = new BinaryReader(source, Encoding.UTF8, leaveOpen: true);

			try
			{
				var kind = (IndexSectionKind)header.ReadInt32();
				var length = header.ReadInt64();
				var checksum = header.ReadUInt64();

				if (kind != expectedKind || length < 0 || length > source.Length - source.Position)
				{
					return false;
				}

				reader = new Reader(source, length, checksum);
				return true;
			}
			catch (EndOfStreamException)
			{
				return false;
			}
		}

		/// <summary>
		/// Moves past the next section, when it is of <paramref name="expectedKind"/>, without reading or checking its bytes.
		/// </summary>
		/// <remarks>
		/// For the record table, which is read and checked only when it is used, so a damaged table never costs the line index.
		/// </remarks>
		public static bool TrySkip(Stream source, IndexSectionKind expectedKind)
		{
			if (!TryRead(source, expectedKind, out Reader reader))
			{
				return false;
			}

			using (reader)
			{
				source.Position += reader.Remaining;
			}

			return true;
		}

		/// <summary>
		/// The bytes of one section, read through a <see cref="BinaryReader"/>, and checked against the section's checksum once all are read.
		/// </summary>
		internal sealed class Reader : IDisposable
		{
			private readonly HashingReadStream _bytes;

			internal Reader(Stream source, long length, ulong checksum)
			{
				_bytes = new HashingReadStream(source, length, checksum);
				this.Binary = new BinaryReader(_bytes, Encoding.UTF8, leaveOpen: true);
			}

			public BinaryReader Binary { get; }

			/// <summary>
			/// The bytes not read yet.
			/// </summary>
			public long Remaining => _bytes.Length - _bytes.Position;

			/// <summary>
			/// Every byte of the section was read, and they match its checksum.
			/// </summary>
			public bool IsIntact => this.Remaining == 0 && _bytes.IsChecksumMatched;

			public void Dispose()
			{
				this.Binary.Dispose();
			}
		}

		/// <summary>
		/// Passes the bytes written on to another stream, counting them and computing their checksum.
		/// </summary>
		/// <remarks>
		/// A table's rows are written a field at a time, so the bytes are gathered into a buffer, and hashed and written a buffer at a time.
		/// </remarks>
		private sealed class HashingStream : Stream
		{
			private const int BufferSize = 64 * 1024;

			private readonly Stream _destination;
			private readonly XxHash3 _hash = new XxHash3();
			private readonly byte[] _buffer = new byte[BufferSize];
			private int _buffered;
			private long _length;

			public HashingStream(Stream destination)
			{
				_destination = destination;
			}

			public override bool CanRead => false;

			public override bool CanSeek => false;

			public override bool CanWrite => true;

			public override long Length => _length;

			public override long Position
			{
				get => _length;
				set => throw new NotSupportedException();
			}

			public ulong GetHash() => _hash.GetCurrentHashAsUInt64();

			public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

			public override void Write(ReadOnlySpan<byte> buffer)
			{
				_length += buffer.Length;

				while (!buffer.IsEmpty)
				{
					var count = System.Math.Min(buffer.Length, BufferSize - _buffered);
					buffer.Slice(0, count).CopyTo(_buffer.AsSpan(_buffered));
					_buffered += count;
					buffer = buffer.Slice(count);

					if (_buffered == BufferSize)
					{
						Flush();
					}
				}
			}

			public override void WriteByte(byte value) => Write([value]);

			/// <summary>
			/// Hashes and writes the bytes gathered so far: call it before reading <see cref="GetHash"/>.
			/// </summary>
			public override void Flush()
			{
				if (_buffered > 0)
				{
					_hash.Append(_buffer.AsSpan(0, _buffered));
					_destination.Write(_buffer, 0, _buffered);
					_buffered = 0;
				}
			}

			public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

			public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

			public override void SetLength(long value) => throw new NotSupportedException();
		}

		/// <summary>
		/// Reads at most a section's bytes from another stream, computing their checksum as they are read.
		/// </summary>
		/// <remarks>
		/// A table's rows are read a field at a time, so the section is read, and hashed, a buffer at a time.
		/// <see cref="Position"/> counts the bytes handed out, so a reader still knows how many it has not read.
		/// </remarks>
		private sealed class HashingReadStream : Stream
		{
			private const int BufferSize = 64 * 1024;

			private readonly Stream _source;
			private readonly long _length;
			private readonly ulong _expectedChecksum;
			private readonly XxHash3 _hash = new XxHash3();
			private readonly byte[] _buffer;
			private int _bufferStart;
			private int _bufferEnd;
			private long _position;
			private long _sourceRead;

			public HashingReadStream(Stream source, long length, ulong expectedChecksum)
			{
				_source = source;
				_length = length;
				_expectedChecksum = expectedChecksum;
				_buffer = new byte[(int)System.Math.Min(BufferSize, System.Math.Max(length, 1))];
			}

			public bool IsChecksumMatched => _position == _length && _sourceRead == _length && _hash.GetCurrentHashAsUInt64() == _expectedChecksum;

			public override bool CanRead => true;

			public override bool CanSeek => false;

			public override bool CanWrite => false;

			public override long Length => _length;

			public override long Position
			{
				get => _position;
				set => throw new NotSupportedException();
			}

			public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

			public override int Read(Span<byte> buffer)
			{
				if (_bufferStart == _bufferEnd && !Fill())
				{
					return 0;
				}

				var count = System.Math.Min(buffer.Length, _bufferEnd - _bufferStart);
				_buffer.AsSpan(_bufferStart, count).CopyTo(buffer);
				_bufferStart += count;
				_position += count;

				return count;
			}

			public override int ReadByte()
			{
				if (_bufferStart == _bufferEnd && !Fill())
				{
					return -1;
				}

				_position++;
				return _buffer[_bufferStart++];
			}

			private bool Fill()
			{
				var count = (int)System.Math.Min(_buffer.Length, _length - _sourceRead);

				if (count <= 0)
				{
					return false;
				}

				var read = _source.Read(_buffer, 0, count);
				_hash.Append(_buffer.AsSpan(0, read));
				_sourceRead += read;
				_bufferStart = 0;
				_bufferEnd = read;

				return read > 0;
			}

			public override void Flush()
			{
				// nothing to do
			}

			public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

			public override void SetLength(long value) => throw new NotSupportedException();

			public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		}
	}
}
