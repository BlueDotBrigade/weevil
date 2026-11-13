namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Hashing;
	using System.Text;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// Builds the record table saved with a log's index, instead of parsing the log, once every block of the log is checked against the saved checksums (#1023).
	/// </summary>
	/// <remarks>
	/// The log is read once, in order: in RAM mode its bytes fill the chunks as they are checked, and on disk the chunks read them back later,
	/// through a source made from the saved checksums. A block that changed means the table is not the log's, so nothing is built.
	/// </remarks>
	internal static class SavedTableReader
	{
		/// <param name="indexSource">The saved index, positioned where its record table starts.</param>
		/// <param name="log">The log, which is read from <see cref="SavedParse.StartOffset"/> to <see cref="SavedParse.EndOffset"/>.</param>
		/// <param name="logPath">The log's path, which chunks on file read back from.</param>
		/// <param name="parse">The parse that made the table, whose checksums the log's blocks must match.</param>
		/// <param name="encoding">The log's encoding, without a byte order mark.</param>
		/// <param name="storageMode">Whether the chunks hold their bytes, or read them back from the log.</param>
		/// <param name="source">On disk, the source the chunks read back through: otherwise <see langword="null"/>.</param>
		/// <returns><see langword="false"/> when the table is damaged, or a block of the log does not match its checksum.</returns>
		public static bool TryRead(
			Stream indexSource,
			Stream log,
			string logPath,
			SavedParse parse,
			Encoding encoding,
			StorageMode storageMode,
			out RecordTable table,
			out LogFileSource source)
		{
			table = null;
			source = null;

			if (!TryReadChunkDirectory(indexSource, parse, out List<(int Start, long FileOffset, int Length)> directory))
			{
				return false;
			}

			List<(int Start, ContentChunk Chunk)> chunks;

			if (storageMode == StorageMode.Disk)
			{
				if (!TryCheckLog(log, parse, onBlock: null))
				{
					return false;
				}

				source = LogFileSource.Create(logPath, parse.StartOffset, parse.EndOffset, log.Length, CopyChecksums(parse));
				chunks = new List<(int Start, ContentChunk Chunk)>(directory.Count);

				foreach ((var start, var fileOffset, var length) in directory)
				{
					chunks.Add((start, new ContentChunk(source, fileOffset, length, encoding)));
				}
			}
			else
			{
				var filler = new ChunkFiller(directory, encoding);

				if (!TryCheckLog(log, parse, filler.Add) || !filler.IsComplete)
				{
					return false;
				}

				chunks = filler.Chunks;
			}

			if (!TryReadStrings(indexSource, out Dictionary<int, string> strings) ||
			    !IndexSection.TryRead(indexSource, IndexSectionKind.Rows, out IndexSection.Reader rows))
			{
				source = null;
				return false;
			}

			using (rows)
			{
				table = RecordTable.ReadRows(rows.Binary, chunks, strings);

				if (table is null || !rows.IsIntact)
				{
					table = null;
					source = null;
					return false;
				}
			}

			return true;
		}

		private static bool TryReadChunkDirectory(Stream indexSource, SavedParse parse, out List<(int Start, long FileOffset, int Length)> directory)
		{
			directory = null;

			if (!IndexSection.TryRead(indexSource, IndexSectionKind.Chunks, out IndexSection.Reader section))
			{
				return false;
			}

			using (section)
			{
				var count = section.Binary.ReadInt32();

				// Each entry is 16 bytes, so a damaged count is caught before anything is allocated.
				if (count < 0 || count * 16L != section.Remaining)
				{
					return false;
				}

				directory = new List<(int Start, long FileOffset, int Length)>(count);
				var previousEnd = parse.StartOffset;
				var previousStart = -1;

				for (var index = 0; index < count; index++)
				{
					var start = section.Binary.ReadInt32();
					var fileOffset = section.Binary.ReadInt64();
					var length = section.Binary.ReadInt32();

					// The chunks are in the order of the log, within the bytes parsed, and do not overlap.
					if (start <= previousStart || length < 0 || fileOffset < previousEnd || fileOffset > parse.EndOffset - length)
					{
						return false;
					}

					directory.Add((start, fileOffset, length));
					previousStart = start;
					previousEnd = fileOffset + length;
				}

				return section.IsIntact;
			}
		}

		private static bool TryReadStrings(Stream indexSource, out Dictionary<int, string> strings)
		{
			strings = null;

			if (!IndexSection.TryRead(indexSource, IndexSectionKind.Strings, out IndexSection.Reader section))
			{
				return false;
			}

			using (section)
			{
				var count = section.Binary.ReadInt32();

				// Each string takes at least 5 bytes: its row number and its length.
				if (count < 0 || count * 5L > section.Remaining)
				{
					return false;
				}

				strings = new Dictionary<int, string>(count);

				for (var index = 0; index < count; index++)
				{
					var recordNumber = section.Binary.ReadInt32();

					if (!strings.TryAdd(recordNumber, section.Binary.ReadString()))
					{
						return false;
					}
				}

				return section.IsIntact;
			}
		}

		/// <summary>
		/// Reads the bytes parsed, one block at a time, as <see cref="LogFileSourceBuilder"/> divided them, and checks each against its checksum.
		/// </summary>
		/// <param name="onBlock">Given each block's bytes, once checked, with where they start in the log.</param>
		private static bool TryCheckLog(Stream log, SavedParse parse, Action<long, ReadOnlySpan<byte>> onBlock)
		{
			if (log.Length < parse.EndOffset)
			{
				return false;
			}

			var buffer = new byte[LogFileSource.BlockSize];
			var position = parse.StartOffset;
			var block = 0;
			log.Position = position;

			while (position < parse.EndOffset)
			{
				var blockEnd = System.Math.Min(((position / LogFileSource.BlockSize) + 1) * LogFileSource.BlockSize, parse.EndOffset);
				Span<byte> bytes = buffer.AsSpan(0, (int)(blockEnd - position));

				if (log.ReadAtLeast(bytes, bytes.Length, throwOnEndOfStream: false) != bytes.Length ||
				    block >= parse.Checksums.Count ||
				    XxHash3.HashToUInt64(bytes) != parse.Checksums[block])
				{
					return false;
				}

				onBlock?.Invoke(position, bytes);
				position = blockEnd;
				block++;
			}

			return block == parse.Checksums.Count;
		}

		private static ulong[] CopyChecksums(SavedParse parse)
		{
			var checksums = new ulong[parse.Checksums.Count];

			for (var index = 0; index < checksums.Length; index++)
			{
				checksums[index] = parse.Checksums[index];
			}

			return checksums;
		}

		/// <summary>
		/// Copies the log's bytes, as they are read, into the chunks of the directory, and makes each chunk once its bytes are all read.
		/// </summary>
		private sealed class ChunkFiller
		{
			private readonly List<(int Start, long FileOffset, int Length)> _directory;
			private readonly Encoding _encoding;
			private byte[] _buffer = [];
			private int _next;

			public ChunkFiller(List<(int Start, long FileOffset, int Length)> directory, Encoding encoding)
			{
				_directory = directory;
				_encoding = encoding;
				this.Chunks = new List<(int Start, ContentChunk Chunk)>(directory.Count);
			}

			public List<(int Start, ContentChunk Chunk)> Chunks { get; }

			public bool IsComplete => _next == _directory.Count;

			public void Add(long position, ReadOnlySpan<byte> bytes)
			{
				var end = position + bytes.Length;

				while (_next < _directory.Count && _directory[_next].FileOffset < end)
				{
					(var start, var chunkOffset, var length) = _directory[_next];
					var chunkEnd = chunkOffset + length;

					if (_buffer.Length < length)
					{
						_buffer = new byte[length];
					}

					// The part of this block that the chunk holds.
					var copyStart = System.Math.Max(chunkOffset, position);
					var copyEnd = System.Math.Min(chunkEnd, end);

					if (copyEnd > copyStart)
					{
						bytes.Slice((int)(copyStart - position), (int)(copyEnd - copyStart)).CopyTo(_buffer.AsSpan((int)(copyStart - chunkOffset)));
					}

					if (chunkEnd > end)
					{
						return;
					}

					// The chunk copies its bytes, so the buffer is reused for the next one.
					this.Chunks.Add((start, new ContentChunk(chunkOffset, _buffer.AsSpan(0, length), _encoding)));
					_next++;
				}
			}
		}
	}
}
