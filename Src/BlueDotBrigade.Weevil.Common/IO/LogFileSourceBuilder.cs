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
		private readonly string _path;
		private readonly long _startOffset;
		private readonly List<ulong> _checksums = new List<ulong>();
		private readonly XxHash3 _hash = new XxHash3();

		// Where the next byte appended is in the file, and whether the block it falls in has bytes already.
		private long _position;
		private bool _isBlockStarted;

		/// <param name="path">The log.</param>
		/// <param name="startOffset">Where the first byte appended is in the file.</param>
		public LogFileSourceBuilder(string path, long startOffset)
		{
			ArgumentException.ThrowIfNullOrEmpty(path);
			ArgumentOutOfRangeException.ThrowIfNegative(startOffset);

			_path = path;
			_startOffset = startOffset;
			_position = startOffset;
		}

		/// <summary>
		/// Adds the next bytes of the log.
		/// </summary>
		public void Append(ReadOnlySpan<byte> bytes)
		{
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
		}

		/// <summary>
		/// Returns a source that reads back the bytes appended.
		/// </summary>
		public LogFileSource Build()
		{
			if (_isBlockStarted)
			{
				// The last block, which the range parsed ends within.
				AddChecksum();
			}

			return new LogFileSource(_path, _startOffset, _position, _checksums.ToArray());
		}

		private void AddChecksum()
		{
			_checksums.Add(_hash.GetCurrentHashAsUInt64());
			_hash.Reset();
			_isBlockStarted = false;
		}
	}
}
