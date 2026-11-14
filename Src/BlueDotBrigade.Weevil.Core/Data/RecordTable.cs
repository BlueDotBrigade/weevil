namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	/// <summary>
	/// A log's records in columns, 21 bytes per record, from which records are created when they are needed (#1021, #1138).
	/// </summary>
	/// <remarks>
	/// - Each column is an array, kept in blocks of <see cref="BlockSize"/> rows, so no array grows large, and building the table copies only the last block, to trim it.
	/// - A record that holds the log's bytes keeps its content's offset and length in its chunk. The chunk directory says where each chunk's
	///   records start, and each block knows the chunk its first record is in, so finding a record's chunk is a short search.
	/// - A record that keeps its content as a string (UTF-16 in the other byte order and UTF-32 in RAM mode, or text a plugin changed) has
	///   <see cref="RecordFacts.KeepsString"/>, and its block keeps the string: by row when most of its rows keep one, otherwise in a short list.
	/// - The table never changes once built.
	/// </remarks>
	internal sealed class RecordTable
	{
		public const int BlockSize = 65_536;

		private readonly Block[] _blocks;

		// The chunk directory: the record number each chunk's records start at, and the chunk.
		private readonly int[] _chunkStarts;
		private readonly ContentChunk[] _chunks;

		private RecordTable(Block[] blocks, int[] chunkStarts, ContentChunk[] chunks, int count)
		{
			_blocks = blocks;
			_chunkStarts = chunkStarts;
			_chunks = chunks;
			this.Count = count;
		}

		public int Count { get; }

		/// <summary>
		/// The record number each chunk's records start at, in order: the records between two of them read the same chunk (#1027).
		/// </summary>
		/// <remarks>
		/// Records that keep their content as a string read no chunk, so a table of only those has none.
		/// </remarks>
		public IReadOnlyList<int> ChunkStarts => _chunkStarts;

		public int GetLineNumber(int recordNumber) => GetBlock(recordNumber).LineNumbers[recordNumber % BlockSize];

		public DateTime GetCreatedAt(int recordNumber) => GetBlock(recordNumber).CreatedAt[recordNumber % BlockSize];

		/// <summary>
		/// Returns what the parser found about the record at <paramref name="recordNumber"/>, without creating it (#1150).
		/// </summary>
		/// <remarks>
		/// Whether the table keeps the content as a string is left out, as a record leaves it out.
		/// </remarks>
		public RecordFacts GetFacts(int recordNumber)
		{
			RecordFacts facts = GetBlock(recordNumber).Facts[recordNumber % BlockSize];

			return facts.KeepsString
				? new RecordFacts(facts.Severity, facts.IsMultiLine, facts.WasGeneratedByUi)
				: facts;
		}

		/// <summary>
		/// Creates the record at <paramref name="recordNumber"/>: a new object on each call.
		/// </summary>
		public IRecord CreateRecord(int recordNumber)
		{
			Block block = GetBlock(recordNumber);
			var row = recordNumber % BlockSize;
			RecordFacts facts = block.Facts[row];

			return facts.KeepsString
				? new Record(
					block.LineNumbers[row],
					block.CreatedAt[row],
					facts.Severity,
					block.GetString(row),
					facts.IsMultiLine,
					facts.WasGeneratedByUi)
				: new Record(
					block.LineNumbers[row],
					block.CreatedAt[row],
					facts.Severity,
					GetChunk(recordNumber),
					block.Offsets[row],
					block.Lengths[row],
					facts.IsMultiLine,
					facts.WasGeneratedByUi);
		}

		/// <summary>
		/// Returns the content of the record at <paramref name="recordNumber"/>, the same text its record's content would be, without creating the record (#1150).
		/// </summary>
		/// <param name="content">Decodes a row that holds the log's bytes. A row that keeps a string returns the string as it is.</param>
		public ReadOnlySpan<char> ReadContent(int recordNumber, ContentBuffer content)
		{
			Block block = GetBlock(recordNumber);
			var row = recordNumber % BlockSize;
			RecordFacts facts = block.Facts[row];

			return facts.KeepsString
				? block.GetString(row)
				: content.Read(GetChunk(recordNumber), block.Offsets[row], block.Lengths[row], facts.IsMultiLine);
		}

		/// <summary>
		/// Returns a new table of only the records at <paramref name="recordNumbers"/>, in that order (#1140).
		/// </summary>
		/// <remarks>
		/// Clear keeps one, so this table and the chunks that no kept record is read from can be freed, as Clear always freed the records it cleared.
		/// </remarks>
		public RecordTable Keep(IReadOnlyList<int> recordNumbers)
		{
			var kept = new Builder();

			foreach (var recordNumber in recordNumbers)
			{
				Block block = GetBlock(recordNumber);
				var row = recordNumber % BlockSize;
				RecordFacts facts = block.Facts[row];

				if (facts.KeepsString)
				{
					kept.Add(block.LineNumbers[row], block.CreatedAt[row], facts, block.GetString(row));
				}
				else
				{
					kept.Add(block.LineNumbers[row], block.CreatedAt[row], facts, GetChunk(recordNumber), block.Offsets[row], block.Lengths[row]);
				}
			}

			return kept.Build();
		}

		/// <summary>
		/// Returns where the record at <paramref name="recordNumber"/> starts in the log, when its row holds the log's bytes (#1023).
		/// </summary>
		/// <returns><see langword="false"/> for a row that keeps its content as a string.</returns>
		internal bool TryGetFileOffset(int recordNumber, out long fileOffset)
		{
			Block block = GetBlock(recordNumber);
			var row = recordNumber % BlockSize;

			fileOffset = block.Facts[row].KeepsString ? -1 : GetChunk(recordNumber).FileOffset + block.Offsets[row];
			return fileOffset >= 0;
		}

		/// <summary>
		/// Returns a new table of this table's first <paramref name="count"/> records, then every record of <paramref name="next"/>, for a log that grew (#1023).
		/// </summary>
		/// <remarks>
		/// The new table shares this table's full blocks before <paramref name="count"/>, which never change, rather than copying their rows,
		/// so the two tables together take little more memory than one (#1201). Only the rows after the last full block, and <paramref name="next"/>'s, are copied.
		/// </remarks>
		/// <param name="replaceChunk">Gives the chunk each row reads, so that on disk every chunk reads through one source: called once per chunk.</param>
		internal RecordTable Append(int count, RecordTable next, Func<ContentChunk, ContentChunk> replaceChunk)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(count);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(count, this.Count);
			ArgumentNullException.ThrowIfNull(next);
			ArgumentNullException.ThrowIfNull(replaceChunk);

			Func<int, ContentChunk> getChunk = ReplaceChunks(replaceChunk);
			var builder = new Builder();

			var sharedRows = builder.Share(this, count / BlockSize, getChunk);
			AddRows(builder, sharedRows, count, getChunk);
			next.AddRows(builder, 0, next.Count, next.ReplaceChunks(replaceChunk));

			return builder.Build();
		}

		/// <summary>
		/// Returns the chunk at each index of the directory, replaced once: the same object each time, so a builder sees one chunk.
		/// </summary>
		private Func<int, ContentChunk> ReplaceChunks(Func<ContentChunk, ContentChunk> replaceChunk)
		{
			var replaced = new ContentChunk[_chunks.Length];
			return index => replaced[index] ??= replaceChunk(_chunks[index]);
		}

		/// <summary>
		/// Adds the rows from <paramref name="start"/> to before <paramref name="end"/> to <paramref name="builder"/>,
		/// walking the chunk directory alongside rather than searching it for each row.
		/// </summary>
		/// <param name="getChunk">Gives the chunk at an index of the directory: the same object each time for the same index, so the builder sees one chunk.</param>
		private void AddRows(Builder builder, int start, int end, Func<int, ContentChunk> getChunk)
		{
			var chunkIndex = -1;

			for (var recordNumber = start; recordNumber < end; recordNumber++)
			{
				Block block = _blocks[recordNumber / BlockSize];
				var row = recordNumber % BlockSize;
				RecordFacts facts = block.Facts[row];

				if (facts.KeepsString)
				{
					builder.Add(block.LineNumbers[row], block.CreatedAt[row], facts, block.GetString(row));
					continue;
				}

				while (chunkIndex + 1 < _chunkStarts.Length && _chunkStarts[chunkIndex + 1] <= recordNumber)
				{
					chunkIndex++;
				}

				ContentChunk chunk = getChunk(chunkIndex);
				builder.Add(block.LineNumbers[row], block.CreatedAt[row], facts, chunk, block.Offsets[row], block.Lengths[row]);
			}
		}

		private Block GetBlock(int recordNumber)
		{
			if ((uint)recordNumber >= (uint)this.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(recordNumber), recordNumber, "The record number is expected to be within the table.");
			}

			return _blocks[recordNumber / BlockSize];
		}

		/// <summary>
		/// Returns the last chunk that starts at or before <paramref name="recordNumber"/>.
		/// </summary>
		/// <remarks>
		/// Only the chunks the record's block can use are searched: from its first chunk to the next block's.
		/// A chunk holds many records, so that is usually one or two.
		/// </remarks>
		internal ContentChunk GetChunk(int recordNumber)
		{
			var blockIndex = recordNumber / BlockSize;
			var low = _blocks[blockIndex].FirstChunk;
			var high = blockIndex + 1 < _blocks.Length
				? _blocks[blockIndex + 1].FirstChunk
				: _chunkStarts.Length - 1;

			while (low < high)
			{
				var middle = low + ((high - low + 1) / 2);

				if (_chunkStarts[middle] <= recordNumber)
				{
					low = middle;
				}
				else
				{
					high = middle - 1;
				}
			}

			return _chunks[low];
		}

		/// <summary>
		/// Writes the chunk directory, as the saved index keeps it (#1023): for each chunk, the record it starts at and the bytes of the log it holds.
		/// </summary>
		internal void WriteChunks(BinaryWriter writer)
		{
			writer.Write(_chunks.Length);

			for (var index = 0; index < _chunks.Length; index++)
			{
				writer.Write(_chunkStarts[index]);
				writer.Write(_chunks[index].FileOffset);
				writer.Write(_chunks[index].Length);
			}
		}

		/// <summary>
		/// Writes the strings of the rows that keep one, as the saved index keeps them (#1023): each row's number, then its string.
		/// </summary>
		internal void WriteStrings(BinaryWriter writer)
		{
			var count = 0;

			for (var recordNumber = 0; recordNumber < this.Count; recordNumber++)
			{
				if (GetBlock(recordNumber).Facts[recordNumber % BlockSize].KeepsString)
				{
					count++;
				}
			}

			writer.Write(count);

			for (var recordNumber = 0; recordNumber < this.Count; recordNumber++)
			{
				Block block = GetBlock(recordNumber);
				var row = recordNumber % BlockSize;

				if (block.Facts[row].KeepsString)
				{
					writer.Write(recordNumber);
					writer.Write(block.GetString(row));
				}
			}
		}

		/// <summary>
		/// Writes every row's columns, as the saved index keeps them (#1023): 22 bytes per row.
		/// </summary>
		/// <remarks>
		/// The timestamp is saved as its ticks and its kind, so it reads back exactly, whatever the time zone.
		/// </remarks>
		internal void WriteRows(BinaryWriter writer)
		{
			writer.Write(this.Count);

			for (var recordNumber = 0; recordNumber < this.Count; recordNumber++)
			{
				Block block = GetBlock(recordNumber);
				var row = recordNumber % BlockSize;

				writer.Write(block.LineNumbers[row]);
				writer.Write(block.Offsets[row]);
				writer.Write(block.Lengths[row]);
				writer.Write(block.Facts[row].Value);
				writer.Write(block.CreatedAt[row].Ticks);
				writer.Write((byte)block.CreatedAt[row].Kind);
			}
		}

		/// <summary>
		/// Builds a table from rows saved by <see cref="WriteRows"/>, the strings saved by <see cref="WriteStrings"/>,
		/// and chunks made for the directory saved by <see cref="WriteChunks"/>, through the same <see cref="Builder"/> a parse uses (#1023).
		/// </summary>
		/// <param name="chunks">The chunks, in the directory's order, with the record each starts at.</param>
		/// <param name="strings">The strings of the rows that keep one, by record number.</param>
		/// <returns>The table, or <see langword="null"/> when the rows do not fit the chunks and strings.</returns>
		internal static RecordTable ReadRows(BinaryReader reader, IReadOnlyList<(int Start, ContentChunk Chunk)> chunks, IReadOnlyDictionary<int, string> strings)
		{
			var count = reader.ReadInt32();

			if (count < 0)
			{
				return null;
			}

			var builder = new Builder();
			var chunkIndex = -1;

			for (var recordNumber = 0; recordNumber < count; recordNumber++)
			{
				var lineNumber = reader.ReadInt32();
				var offset = reader.ReadInt32();
				var length = reader.ReadInt32();
				var factsValue = reader.ReadByte();
				var ticks = reader.ReadInt64();
				var kind = (DateTimeKind)reader.ReadByte();

				if (!RecordFacts.TryFromValue(factsValue, out RecordFacts facts) ||
				    ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks ||
				    !Enum.IsDefined(kind))
				{
					return null;
				}

				var createdAt = new DateTime(ticks, kind);

				if (facts.KeepsString)
				{
					if (!strings.TryGetValue(recordNumber, out var content))
					{
						return null;
					}

					builder.Add(lineNumber, createdAt, facts, content);
				}
				else
				{
					while (chunkIndex + 1 < chunks.Count && chunks[chunkIndex + 1].Start <= recordNumber)
					{
						chunkIndex++;
					}

					if (chunkIndex < 0 || offset < 0 || length < 0 || offset > chunks[chunkIndex].Chunk.Length - length)
					{
						return null;
					}

					builder.Add(lineNumber, createdAt, facts, chunks[chunkIndex].Chunk, offset, length);
				}
			}

			return builder.Build();
		}

		private sealed class Block
		{
			public Block(int firstChunk)
			{
				this.FirstChunk = firstChunk;
			}

			/// <summary>
			/// The last chunk in the directory when the block's first record was added, or 0 when no chunk had started yet.
			/// </summary>
			public int FirstChunk { get; }

			public int[] LineNumbers = new int[BlockSize];

			public int[] Offsets = new int[BlockSize];

			public int[] Lengths = new int[BlockSize];

			public RecordFacts[] Facts = new RecordFacts[BlockSize];

			public DateTime[] CreatedAt = new DateTime[BlockSize];

			// The strings of the rows that keep one: listed while the block is built, then kept by row when most rows keep one,
			// otherwise as the rows, in order, and their strings, so a few such rows cost little.
			private List<(int Row, string Content)> _pendingStrings;
			private string[] _stringsByRow;
			private int[] _stringRows;
			private string[] _strings;

			public void AddString(int row, string content)
			{
				(_pendingStrings ??= new List<(int Row, string Content)>()).Add((row, content));
			}

			public string GetString(int row)
			{
				return _stringsByRow != null
					? _stringsByRow[row]
					: _strings[Array.BinarySearch(_stringRows, row)];
			}

			/// <summary>
			/// Ends the block at <paramref name="rowCount"/> rows: trims its columns, and stores its strings.
			/// </summary>
			public void Complete(int rowCount)
			{
				if (rowCount < this.LineNumbers.Length)
				{
					Array.Resize(ref this.LineNumbers, rowCount);
					Array.Resize(ref this.Offsets, rowCount);
					Array.Resize(ref this.Lengths, rowCount);
					Array.Resize(ref this.Facts, rowCount);
					Array.Resize(ref this.CreatedAt, rowCount);
				}

				if (_pendingStrings == null)
				{
					return;
				}

				if (_pendingStrings.Count * 2 > rowCount)
				{
					_stringsByRow = new string[rowCount];

					foreach ((var row, var content) in _pendingStrings)
					{
						_stringsByRow[row] = content;
					}
				}
				else
				{
					_stringRows = new int[_pendingStrings.Count];
					_strings = new string[_pendingStrings.Count];

					for (var index = 0; index < _pendingStrings.Count; index++)
					{
						(_stringRows[index], _strings[index]) = _pendingStrings[index];
					}
				}

				_pendingStrings = null;
			}
		}

		/// <summary>
		/// Adds records in line number order, then builds the table.
		/// </summary>
		internal sealed class Builder
		{
			private readonly List<Block> _blocks = new List<Block>();
			private readonly List<int> _chunkStarts = new List<int>();
			private readonly List<ContentChunk> _chunks = new List<ContentChunk>();
			private Block _current;

			public int Count { get; private set; }

			/// <summary>
			/// Starts an empty builder with <paramref name="table"/>'s first <paramref name="blockCount"/> blocks, all full, and the chunks their rows read (#1201).
			/// </summary>
			/// <remarks>
			/// A block never changes once built, so two tables can share it. The directory's first entries are the same, in the same order,
			/// so each shared block's first chunk stays at the same index.
			/// </remarks>
			/// <param name="getChunk">Gives the chunk at an index of <paramref name="table"/>'s directory.</param>
			/// <returns>The number of rows the shared blocks hold.</returns>
			internal int Share(RecordTable table, int blockCount, Func<int, ContentChunk> getChunk)
			{
				if (this.Count != 0)
				{
					throw new InvalidOperationException("Only an empty builder can start with another table's blocks.");
				}

				// Only full blocks: the last block may have been trimmed.
				ArgumentOutOfRangeException.ThrowIfGreaterThan(blockCount, table.Count / BlockSize);

				for (var index = 0; index < blockCount; index++)
				{
					_blocks.Add(table._blocks[index]);
				}

				this.Count = blockCount * BlockSize;

				for (var index = 0; index < table._chunkStarts.Length && table._chunkStarts[index] < this.Count; index++)
				{
					_chunkStarts.Add(table._chunkStarts[index]);
					_chunks.Add(getChunk(index));
				}

				return this.Count;
			}

			/// <summary>
			/// Adds a record whose content is <paramref name="length"/> bytes at <paramref name="offset"/> in <paramref name="chunk"/>.
			/// </summary>
			public void Add(int lineNumber, DateTime createdAt, RecordFacts facts, ContentChunk chunk, int offset, int length)
			{
				ArgumentNullException.ThrowIfNull(chunk);

				if (_chunks.Count == 0 || !ReferenceEquals(_chunks[_chunks.Count - 1], chunk))
				{
					_chunkStarts.Add(this.Count);
					_chunks.Add(chunk);
				}

				// The content is the chunk's bytes, whatever the caller's facts say.
				var holdsBytes = new RecordFacts(facts.Severity, facts.IsMultiLine, facts.WasGeneratedByUi, keepsString: false);

				var row = NextRow(lineNumber, createdAt, holdsBytes);
				_current.Offsets[row] = offset;
				_current.Lengths[row] = length;
			}

			/// <summary>
			/// Adds a record that keeps its content as a string.
			/// </summary>
			public void Add(int lineNumber, DateTime createdAt, RecordFacts facts, string content)
			{
				var keepsString = new RecordFacts(facts.Severity, facts.IsMultiLine, facts.WasGeneratedByUi, keepsString: true);

				var row = NextRow(lineNumber, createdAt, keepsString);
				_current.AddString(row, content ?? string.Empty);
			}

			public RecordTable Build()
			{
				var rowsInLastBlock = this.Count % BlockSize;
				_current?.Complete(rowsInLastBlock > 0 ? rowsInLastBlock : BlockSize);

				return new RecordTable(_blocks.ToArray(), _chunkStarts.ToArray(), _chunks.ToArray(), this.Count);
			}

			private int NextRow(int lineNumber, DateTime createdAt, RecordFacts facts)
			{
				var row = this.Count % BlockSize;

				if (row == 0)
				{
					_current?.Complete(BlockSize);
					_current = new Block(System.Math.Max(_chunks.Count - 1, 0));
					_blocks.Add(_current);
				}

				_current.LineNumbers[row] = lineNumber;
				_current.CreatedAt[row] = createdAt;
				_current.Facts[row] = facts;
				this.Count++;

				return row;
			}
		}
	}
}
