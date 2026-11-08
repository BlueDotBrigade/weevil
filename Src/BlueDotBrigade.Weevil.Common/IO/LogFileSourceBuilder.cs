namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO.Hashing;

	/// <summary>
	/// Makes a <see cref="LogFileSource"/> from the bytes of a log as they are parsed, computing a checksum of each block as it goes (#1025).
	/// </summary>
	/// <remarks>
	/// Not thread-safe. Append the bytes in the order they are in the file, then call <see cref="Build"/> once.
	/// </remarks>
	public sealed class LogFileSourceBuilder
	{
		private readonly List<ulong> _checksums = new List<ulong>();
		private readonly XxHash3 _hash = new XxHash3();

		// Where the next byte appended is in the file, and whether the block it falls in has bytes already.
		private long _position;
		private bool _isBlockStarted;
		private bool _isBuilt;

		/// <param name="path">The log.</param>
		/// <param name="startOffset">Where the first byte appended is in the file.</param>
		/// <param name="logLength">The log's length when it was opened, so a log parsed only in part is not taken for one that grew.</param>
		public LogFileSourceBuilder(string path, long startOffset, long logLength)
		{
			ArgumentException.ThrowIfNullOrEmpty(path);
			ArgumentOutOfRangeException.ThrowIfNegative(startOffset);
			ArgumentOutOfRangeException.ThrowIfNegative(logLength);

			_position = startOffset;
			this.Source = new LogFileSource(path, startOffset, logLength);
		}

		/// <summary>
		/// The source being built: chunks on file can refer to it while the log is parsed, and read through it once <see cref="Build"/> has completed it.
		/// </summary>
		public LogFileSource Source { get; }

		/// <summary>
		/// Adds the next bytes of the log.
		/// </summary>
		public void Append(ReadOnlySpan<byte> bytes)
		{
			ThrowIfBuilt();

			while (!bytes.IsEmpty)
			{
				var blockEnd = ((_position / LogFileSource.BlockSize) + 1) * LogFileSource.BlockSize;
				var count = (int)System.Math.Min(bytes.Length, blockEnd - _position);

				_hash.Append(bytes.Slice(0, count));
				_isBlockStarted = true;
				_position += count;
				bytes = bytes.Slice(count);

				if (_position == blockEnd)
				{
					AddChecksum();
				}
			}

			this.Source.Extend(_position);
		}

		/// <summary>
		/// Completes the source, so it reads back the bytes appended, and returns it.
		/// </summary>
		public LogFileSource Build()
		{
			ThrowIfBuilt();

			if (_isBlockStarted)
			{
				// The last block, which the range parsed ends within.
				AddChecksum();
			}

			_isBuilt = true;
			this.Source.Complete(_checksums.ToArray());

			return this.Source;
		}

		private void AddChecksum()
		{
			_checksums.Add(_hash.GetCurrentHashAsUInt64());
			_hash.Reset();
			_isBlockStarted = false;
		}

		private void ThrowIfBuilt()
		{
			if (_isBuilt)
			{
				throw new InvalidOperationException("The source has already been built.");
			}
		}
	}
}
