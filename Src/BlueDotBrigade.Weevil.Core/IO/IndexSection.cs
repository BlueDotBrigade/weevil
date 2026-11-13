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
		/// Peeks at the kind of the next section, and leaves the stream where it was.
		/// </summary>
		public static bool TryPeekKind(Stream source, out IndexSectionKind kind)
		{
			kind = IndexSectionKind.End;
			var position = source.Position;

			try
			{
				using var header = new BinaryReader(source, Encoding.UTF8, leaveOpen: true);
				kind = (IndexSectionKind)header.ReadInt32();
				return true;
			}
			catch (EndOfStreamException)
			{
				return false;
			}
			finally
			{
				source.Position = position;
			}
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
		private sealed class HashingStream : Stream
		{
			private readonly Stream _destination;
			private readonly XxHash3 _hash = new XxHash3();
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
				_hash.Append(buffer);
				_destination.Write(buffer);
				_length += buffer.Length;
			}

			public override void Flush() => _destination.Flush();

			public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

			public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

			public override void SetLength(long value) => throw new NotSupportedException();
		}

		/// <summary>
		/// Reads at most a section's bytes from another stream, computing their checksum as they are read.
		/// </summary>
		private sealed class HashingReadStream : Stream
		{
			private readonly Stream _source;
			private readonly long _length;
			private readonly ulong _expectedChecksum;
			private readonly XxHash3 _hash = new XxHash3();
			private long _position;

			public HashingReadStream(Stream source, long length, ulong expectedChecksum)
			{
				_source = source;
				_length = length;
				_expectedChecksum = expectedChecksum;
			}

			public bool IsChecksumMatched => _position == _length && _hash.GetCurrentHashAsUInt64() == _expectedChecksum;

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
				var count = (int)System.Math.Min(buffer.Length, _length - _position);

				if (count <= 0)
				{
					return 0;
				}

				var read = _source.Read(buffer.Slice(0, count));
				_hash.Append(buffer.Slice(0, read));
				_position += read;

				return read;
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
