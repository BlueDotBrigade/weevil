namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;

	/// <summary>
	/// A log's records in columns, 21 bytes per record, from which records are created when they are needed (#1021, #1138).
	/// </summary>
	/// <remarks>
	/// - Each column is an array, kept in blocks of <see cref="BlockSize"/> rows, so no array grows large and building the table never copies one.
	/// - A record that holds the log's bytes keeps its content's offset and length in its chunk. The chunk directory says where each chunk's
	///   records start, and each block knows the chunk its first record is in, so finding a record's chunk is a short search.
	/// - A record that keeps its content as a string (UTF-16 in the other byte order, UTF-32, or text a plugin changed) has
	///   <see cref="RecordFacts.KeepsString"/>, and its block keeps the string. A block allocates strings only if it holds such a record.
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

		public int GetLineNumber(int recordNumber) => GetBlock(recordNumber).LineNumbers[recordNumber % BlockSize];

		public DateTime GetCreatedAt(int recordNumber) => GetBlock(recordNumber).CreatedAt[recordNumber % BlockSize];

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
					block.Strings[row],
					facts.IsMultiLine,
					facts.WasGeneratedByUi)
				: new Record(
					block.LineNumbers[row],
					block.CreatedAt[row],
					facts.Severity,
					GetChunk(block, recordNumber),
					block.Offsets[row],
					block.Lengths[row],
					facts.IsMultiLine,
					facts.WasGeneratedByUi);
		}

		/// <summary>
		/// Creates every record, in order, as the array Core holds until records are created when they are read (#1140).
		/// </summary>
		public ImmutableArray<IRecord> CreateRecords()
		{
			ImmutableArray<IRecord>.Builder records = ImmutableArray.CreateBuilder<IRecord>(this.Count);

			for (var recordNumber = 0; recordNumber < this.Count; recordNumber++)
			{
				records.Add(CreateRecord(recordNumber));
			}

			return records.MoveToImmutable();
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
		/// Returns the last chunk that starts at or before <paramref name="recordNumber"/>, searching from the block's first chunk.
		/// </summary>
		private ContentChunk GetChunk(Block block, int recordNumber)
		{
			var low = block.FirstChunk;
			var high = _chunkStarts.Length - 1;

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

		private sealed class Block
		{
			public Block(int firstChunk)
			{
				this.FirstChunk = firstChunk;
			}

			/// <summary>
			/// The chunk in the directory that the block's first record is in, or 0 when no chunk has started yet.
			/// </summary>
			public int FirstChunk { get; }

			public int[] LineNumbers = new int[BlockSize];

			public int[] Offsets = new int[BlockSize];

			public int[] Lengths = new int[BlockSize];

			public RecordFacts[] Facts = new RecordFacts[BlockSize];

			public DateTime[] CreatedAt = new DateTime[BlockSize];

			// Allocated only when the block holds a record that keeps a string.
			public string[] Strings;

			public void Trim(int rowCount)
			{
				Array.Resize(ref this.LineNumbers, rowCount);
				Array.Resize(ref this.Offsets, rowCount);
				Array.Resize(ref this.Lengths, rowCount);
				Array.Resize(ref this.Facts, rowCount);
				Array.Resize(ref this.CreatedAt, rowCount);

				if (this.Strings != null)
				{
					Array.Resize(ref this.Strings, rowCount);
				}
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

				var row = NextRow(lineNumber, createdAt, facts);
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
				_current.Strings ??= new string[BlockSize];
				_current.Strings[row] = content ?? string.Empty;
			}

			public RecordTable Build()
			{
				var rowsInLastBlock = this.Count % BlockSize;

				if (_current != null && rowsInLastBlock > 0)
				{
					_current.Trim(rowsInLastBlock);
				}

				return new RecordTable(_blocks.ToArray(), _chunkStarts.ToArray(), _chunks.ToArray(), this.Count);
			}

			private int NextRow(int lineNumber, DateTime createdAt, RecordFacts facts)
			{
				var row = this.Count % BlockSize;

				if (row == 0)
				{
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
