namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Buffers;
	using System.IO;
	using System.IO.Hashing;
	using System.Threading;
	using BlueDotBrigade.Weevil.Diagnostics;
	using Microsoft.Win32.SafeHandles;

	/// <summary>
	/// Reads a log's bytes back from the file, and checks that they are still the bytes that were parsed (#1025).
	/// </summary>
	/// <remarks>
	/// Thread-safe. Each read opens its own handle, which lets other applications read, write and delete the log, and closes it before returning.
	/// It never memory-maps the log, never retries and keeps no cache.
	///
	/// A checksum of each <see cref="BlockSize"/> block of the range parsed tells whether the log changed since:
	/// - A log that is shorter, or whose block read has another checksum, was truncated, replaced or corrupted: the read throws <see cref="LogFileChangedException"/>.
	/// - A log that is only longer has grown, which is normal: a warning is logged once, and reads go on.
	/// </remarks>
	public sealed class LogFileSource
	{
		/// <summary>
		/// The size of the blocks a checksum covers, and that a read reads whole: blocks start at multiples of it in the file.
		/// </summary>
		public const int BlockSize = 64 * 1024;

		// The checksum of each block from the one holding `StartOffset`, over its bytes within the range parsed.
		private readonly ulong[] _checksums;
		private int _hasGrown;

		internal LogFileSource(string path, long startOffset, long endOffset, ulong[] checksums)
		{
			this.Path = path;
			this.StartOffset = startOffset;
			this.EndOffset = endOffset;
			_checksums = checksums;
		}

		public string Path { get; }

		/// <summary>
		/// Where the range parsed starts in the file.
		/// </summary>
		public long StartOffset { get; }

		/// <summary>
		/// Where the range parsed ends in the file: the length of the log, when it was parsed to the end.
		/// </summary>
		public long EndOffset { get; }

		/// <summary>
		/// A read found the log longer than the range parsed, and the blocks it read unchanged.
		/// </summary>
		public bool HasGrown => Volatile.Read(ref _hasGrown) == 1;

		/// <summary>
		/// Reads the bytes at <paramref name="fileOffset"/> into <paramref name="destination"/>, after checking the blocks that hold them.
		/// </summary>
		/// <exception cref="LogFileChangedException">The log is shorter than the range parsed, or a block read has changed.</exception>
		/// <exception cref="FileNotFoundException">The log is gone. A read never waits for it.</exception>
		public void Read(long fileOffset, Span<byte> destination)
		{
			if (fileOffset < this.StartOffset || fileOffset > this.EndOffset - destination.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(fileOffset), fileOffset, "The bytes are expected to be within the range parsed.");
			}

			if (destination.IsEmpty)
			{
				return;
			}

			var firstBlock = fileOffset / BlockSize;
			var lastBlock = (fileOffset + destination.Length - 1) / BlockSize;
			var readStart = System.Math.Max(firstBlock * BlockSize, this.StartOffset);
			var readEnd = System.Math.Min((lastBlock + 1) * BlockSize, this.EndOffset);
			var count = checked((int)(readEnd - readStart));

			var blocks = ArrayPool<byte>.Shared.Rent(count);

			try
			{
				using (SafeFileHandle handle = System.IO.File.OpenHandle(this.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					CheckLength(RandomAccess.GetLength(handle));
					ReadExactly(handle, blocks.AsSpan(0, count), readStart);
				}

				for (var block = firstBlock; block <= lastBlock; block++)
				{
					var blockStart = System.Math.Max(block * BlockSize, this.StartOffset);
					var blockEnd = System.Math.Min((block + 1) * BlockSize, this.EndOffset);
					ReadOnlySpan<byte> bytes = blocks.AsSpan((int)(blockStart - readStart), (int)(blockEnd - blockStart));

					if (XxHash3.HashToUInt64(bytes) != _checksums[block - (this.StartOffset / BlockSize)])
					{
						throw new LogFileChangedException(this.Path, blockStart);
					}
				}

				blocks.AsSpan((int)(fileOffset - readStart), destination.Length).CopyTo(destination);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(blocks);
			}
		}

		private void CheckLength(long length)
		{
			if (length < this.EndOffset)
			{
				throw new LogFileChangedException(this.Path, this.EndOffset, length);
			}

			if (length > this.EndOffset && Interlocked.Exchange(ref _hasGrown, 1) == 0)
			{
				// A log that grows is normal, and the bytes parsed are unchanged: nothing is shown.
				Log.Default.Write(
					LogSeverityType.Warning,
					$"The log has grown since it was opened. Path={this.Path}, ExpectedLength={this.EndOffset}, ActualLength={length}");
			}
		}

		private void ReadExactly(SafeFileHandle handle, Span<byte> destination, long fileOffset)
		{
			var done = 0;

			while (done < destination.Length)
			{
				var read = RandomAccess.Read(handle, destination.Slice(done), fileOffset + done);

				if (read == 0)
				{
					// The log was truncated after its length was read.
					throw new LogFileChangedException(this.Path, this.EndOffset, fileOffset + done);
				}

				done += read;
			}
		}
	}
}
