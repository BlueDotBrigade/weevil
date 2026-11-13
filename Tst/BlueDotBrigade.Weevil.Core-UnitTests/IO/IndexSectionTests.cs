namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;

	[TestClass]
	public class IndexSectionTests
	{
		[TestMethod]
		public void GivenASectionLargerThan2Gb_WhenWrittenAndReadBack_ThenItIsIntact()
		{
			// A saved record table can be several GB (#1023): a section's length is 64 bits, and it streams, so no buffer holds it.
			const long SectionLength = (long)int.MaxValue + (64 * 1024);
			using var file = new ZerosBeyondStream(keptLength: 64);

			IndexSection.Write(file, IndexSectionKind.Rows, writer =>
			{
				var zeros = new byte[1024 * 1024];

				for (long written = 0; written < SectionLength; written += zeros.Length)
				{
					writer.Write(zeros, 0, (int)Math.Min(zeros.Length, SectionLength - written));
				}
			});

			file.Position = 0;
			IndexSection.TryRead(file, IndexSectionKind.Rows, out IndexSection.Reader section).Should().BeTrue();

			using (section)
			{
				section.Remaining.Should().Be(SectionLength);
				var buffer = new byte[1024 * 1024];

				while (section.Binary.Read(buffer, 0, buffer.Length) > 0)
				{
					// Reads every byte, so its checksum is checked.
				}

				section.IsIntact.Should().BeTrue();
			}
		}

		/// <summary>
		/// A stream that keeps only its first bytes, and reads back zeros beyond them, so a test can write and read several GB without the disk or memory.
		/// </summary>
		private sealed class ZerosBeyondStream : Stream
		{
			private readonly byte[] _kept;
			private long _length;
			private long _position;

			public ZerosBeyondStream(int keptLength)
			{
				_kept = new byte[keptLength];
			}

			public override bool CanRead => true;

			public override bool CanSeek => true;

			public override bool CanWrite => true;

			public override long Length => _length;

			public override long Position
			{
				get => _position;
				set => _position = value;
			}

			public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

			public override void Write(ReadOnlySpan<byte> buffer)
			{
				var keptCount = (int)Math.Clamp(_kept.Length - _position, 0, buffer.Length);
				buffer.Slice(0, keptCount).CopyTo(_kept.AsSpan((int)Math.Min(_position, _kept.Length)));

				if (buffer.Slice(keptCount).IndexOfAnyExcept((byte)0) >= 0)
				{
					throw new InvalidOperationException("Only zeros are expected beyond the bytes kept.");
				}

				_position += buffer.Length;
				_length = Math.Max(_length, _position);
			}

			public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

			public override int Read(Span<byte> buffer)
			{
				var count = (int)Math.Max(0, Math.Min(buffer.Length, _length - _position));
				var keptCount = (int)Math.Clamp(_kept.Length - _position, 0, count);
				_kept.AsSpan((int)Math.Min(_position, _kept.Length), keptCount).CopyTo(buffer);
				buffer.Slice(keptCount, count - keptCount).Clear();

				_position += count;
				return count;
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				_position = origin switch
				{
					SeekOrigin.Begin => offset,
					SeekOrigin.Current => _position + offset,
					_ => _length + offset,
				};

				return _position;
			}

			public override void Flush()
			{
				// nothing to do
			}

			public override void SetLength(long value) => _length = value;
		}
	}
}
