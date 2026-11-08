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
	/// A <see cref="LogFileSourceBuilder"/> makes it while the log is parsed, so chunks on file can refer to it, and completes it at the end.
	///
	/// Thread-safe once complete. Each read opens its own handle, which lets other applications read, write and delete the log, and closes it before returning.
	/// It never memory-maps the log, never retries and keeps no cache.
	///
	/// A checksum of each <see cref="BlockSize"/> block of the range parsed tells whether the log changed since:
	/// - A log that is shorter, or whose block read has another checksum, was truncated, replaced or corrupted: the read throws <see cref="LogFileChangedException"/>.
	/// - A log that is only longer than when it was opened has grown, which is normal: a warning is logged once, and reads go on.
	/// </remarks>
	public sealed class LogFileSource
	{
		/// <summary>
		/// The size of the blocks a checksum covers, and that a read reads whole: blocks start at multiples of it in the file.
		/// </summary>
		public const int BlockSize = 64 * 1024;

		private readonly long _lengthWhenOpened;

		private long _endOffset;

		// The checksum of each block from the one holding `StartOffset`, over its bytes within the range parsed: null until the source is complete.
		private ulong[] _checksums;

		private int _hasGrown;

		internal LogFileSource(string path, long startOffset, long lengthWhenOpened)
		{
			this.Path = path;
			this.StartOffset = startOffset;
			_endOffset = startOffset;
			_lengthWhenOpened = lengthWhenOpened;
		}

		public string Path { get; }

		/// <summary>
		/// Where the range parsed starts in the file.
		/// </summary>
		public long StartOffset { get; }

		/// <summary>
		/// Where the range parsed ends in the file: the length of the log, when it was parsed to the end.
		/// </summary>
		/// <remarks>
		/// While the log is parsed, where the bytes parsed so far end.
		/// </remarks>
		public long EndOffset => Volatile.Read(ref _endOffset);

		/// <summary>
		/// A read found the log longer than when it was opened, and the blocks it read unchanged.
		/// </summary>
		public bool HasGrown => Volatile.Read(ref _hasGrown) == 1;

		/// <summary>
		/// Reads the bytes at <paramref name="fileOffset"/> into <paramref name="destination"/>, after checking the blocks that hold them.
		/// </summary>
		/// <remarks>
		/// When the read throws, what <paramref name="destination"/> holds is undefined.
		/// </remarks>
		/// <exception cref="InvalidOperationException">The log is still being parsed.</exception>
		/// <exception cref="LogFileChangedException">The log is shorter than the range parsed, or a block read has changed.</exception>
		/// <exception cref="IOException">The log or its folder is gone, or another application holds the log without sharing reading. A read never waits for it.</exception>
		/// <exception cref="UnauthorizedAccessException">The log can no longer be opened for reading.</exception>
		public void Read(long fileOffset, Span<byte> destination)
		{
			var checksums = Volatile.Read(ref _checksums) ?? throw new InvalidOperationException("The log is still being parsed, so its bytes cannot be read back yet.");

			if (fileOffset < this.StartOffset || fileOffset > this.EndOffset - destination.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(fileOffset), fileOffset, "The bytes are expected to be within the range parsed.");
			}

			if (destination.IsEmpty)
			{
				return;
			}

			var readEnd = fileOffset + destination.Length;
			var firstChecksum = this.StartOffset / BlockSize;
			long length;

			// One block at a time, so the memory a read uses does not grow with what it reads.
			var buffer = ArrayPool<byte>.Shared.Rent(BlockSize);

			try
			{
				using (SafeFileHandle handle = System.IO.File.OpenHandle(this.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					length = RandomAccess.GetLength(handle);

					if (length < this.EndOffset)
					{
						throw new LogFileChangedException(this.Path, this.EndOffset, length);
					}

					for (var block = fileOffset / BlockSize; block * BlockSize < readEnd; block++)
					{
						var blockStart = System.Math.Max(block * BlockSize, this.StartOffset);
						var blockEnd = System.Math.Min((block + 1) * BlockSize, this.EndOffset);
						Span<byte> bytes = buffer.AsSpan(0, (int)(blockEnd - blockStart));

						ReadExactly(handle, bytes, blockStart);

						if (XxHash3.HashToUInt64(bytes) != checksums[block - firstChecksum])
						{
							throw new LogFileChangedException(this.Path, blockStart);
						}

						var copyStart = System.Math.Max(blockStart, fileOffset);
						var copyEnd = System.Math.Min(blockEnd, readEnd);
						bytes.Slice((int)(copyStart - blockStart), (int)(copyEnd - copyStart)).CopyTo(destination.Slice((int)(copyStart - fileOffset)));
					}
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}

			// Only once the blocks read are known to be unchanged: a longer log that was replaced has not grown.
			if (length > System.Math.Max(_lengthWhenOpened, this.EndOffset) && Interlocked.Exchange(ref _hasGrown, 1) == 0)
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					$"The log has grown since it was opened. Path={this.Path}, ExpectedLength={System.Math.Max(_lengthWhenOpened, this.EndOffset)}, ActualLength={length}");
			}
		}

		/// <summary>
		/// Records that the bytes parsed so far end at <paramref name="endOffset"/>.
		/// </summary>
		internal void Extend(long endOffset)
		{
			Volatile.Write(ref _endOffset, endOffset);
		}

		/// <summary>
		/// Makes the source readable, once the log has been parsed.
		/// </summary>
		internal void Complete(ulong[] checksums)
		{
			Volatile.Write(ref _checksums, checksums);
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
