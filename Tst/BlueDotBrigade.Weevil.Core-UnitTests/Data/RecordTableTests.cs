namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Linq;
	using System.Text;

	[TestClass]
	public class RecordTableTests
	{
		private static readonly DateTime Start = new DateTime(2026, 10, 31, 9, 0, 0);

		[TestMethod]
		public void GivenRecordsThatHoldBytesOrStrings_WhenCreatedFromTheTable_ThenEachIsTheRecordThatWasAdded()
		{
			var chunk = new ContentChunk(1000, Encoding.UTF8.GetBytes("Alpha\nBeta\r\nGamma"), Encoding.UTF8);
			var builder = new RecordTable.Builder();

			builder.Add(1, Start, new RecordFacts(SeverityType.Information, isMultiLine: false, wasGeneratedByUi: true), chunk, 0, 5);
			builder.Add(2, Record.CreationTimeUnknown, new RecordFacts(SeverityType.Error, isMultiLine: false, wasGeneratedByUi: false), "Kept as a string");
			builder.Add(3, Start.AddSeconds(1), new RecordFacts(SeverityType.Warning, isMultiLine: true, wasGeneratedByUi: false), chunk, 6, 11);
			RecordTable table = builder.Build();

			table.Count.Should().Be(3);
			ShouldBe(table.CreateRecord(0), 1, Start, SeverityType.Information, isMultiLine: false, wasGeneratedByUi: true, "Alpha");
			ShouldBe(table.CreateRecord(1), 2, Record.CreationTimeUnknown, SeverityType.Error, isMultiLine: false, wasGeneratedByUi: false, "Kept as a string");
			ShouldBe(table.CreateRecord(2), 3, Start.AddSeconds(1), SeverityType.Warning, isMultiLine: true, wasGeneratedByUi: false, "Beta\r\nGamma");
			table.GetLineNumber(2).Should().Be(3);
			table.GetCreatedAt(1).Should().Be(Record.CreationTimeUnknown);
		}

		[TestMethod]
		public void GivenRecordsThatHoldBytesOrStrings_WhenTheirFactsAreRead_ThenEachHasTheFactsOfTheRecordThatWasAdded()
		{
			var chunk = new ContentChunk(0, Encoding.UTF8.GetBytes("Alpha"), Encoding.UTF8);
			var builder = new RecordTable.Builder();

			builder.Add(1, Start, new RecordFacts(SeverityType.Information, isMultiLine: false, wasGeneratedByUi: true), chunk, 0, 5);
			builder.Add(2, Start, new RecordFacts(SeverityType.Error, isMultiLine: true, wasGeneratedByUi: false), "Kept as a string");
			RecordTable table = builder.Build();

			// How the table keeps the content is not a fact about the record (#1150).
			table.GetFacts(0).Should().BeEquivalentTo(new { Severity = SeverityType.Information, IsMultiLine = false, WasGeneratedByUi = true, KeepsString = false });
			table.GetFacts(1).Should().BeEquivalentTo(new { Severity = SeverityType.Error, IsMultiLine = true, WasGeneratedByUi = false, KeepsString = false });
		}

		[TestMethod]
		public void GivenMoreRecordsThanABlockHolds_WhenCreatedFromTheTable_ThenRecordsAroundTheBlockAndChunkEdgesAreCorrect()
		{
			var first = new ContentChunk(0, Encoding.UTF8.GetBytes("ab"), Encoding.UTF8);
			var second = new ContentChunk(2, Encoding.UTF8.GetBytes("cd"), Encoding.UTF8);
			var facts = new RecordFacts(SeverityType.Information, isMultiLine: false, wasGeneratedByUi: false);
			var builder = new RecordTable.Builder();

			// The second block starts in the first chunk, and the second chunk starts one record later.
			for (var recordNumber = 0; recordNumber <= RecordTable.BlockSize; recordNumber++)
			{
				builder.Add(recordNumber + 1, Start, facts, first, recordNumber % 2, 1);
			}

			builder.Add(RecordTable.BlockSize + 2, Start, facts, second, 1, 1);
			builder.Add(RecordTable.BlockSize + 3, Start, facts, "Kept as a string");
			RecordTable table = builder.Build();

			table.Count.Should().Be(RecordTable.BlockSize + 3);
			table.CreateRecord(RecordTable.BlockSize - 1).Content.Should().Be("b");
			table.CreateRecord(RecordTable.BlockSize).Content.Should().Be("a");
			table.CreateRecord(RecordTable.BlockSize + 1).Content.Should().Be("d");
			table.CreateRecord(RecordTable.BlockSize + 2).Content.Should().Be("Kept as a string");
			table.GetLineNumber(RecordTable.BlockSize + 2).Should().Be(RecordTable.BlockSize + 3);
		}

		[TestMethod]
		public void GivenChunksAndStringRowsAcrossBlocks_WhenCreatedFromTheTable_ThenEachRecordReadsItsOwnContent()
		{
			ContentChunk[] chunks = Enumerable.Range(0, 5)
				.Select(index => new ContentChunk(index, Encoding.UTF8.GetBytes(((char)('A' + index)).ToString()), Encoding.UTF8))
				.ToArray();
			var facts = new RecordFacts(SeverityType.Information, isMultiLine: false, wasGeneratedByUi: false);
			var flagged = new RecordFacts(SeverityType.Error, isMultiLine: true, wasGeneratedByUi: true);
			var builder = new RecordTable.Builder();
			var recordNumber = 0;

			void AddBytes(int count, ContentChunk chunk)
			{
				for (var added = 0; added < count; added++, recordNumber++)
				{
					builder.Add(recordNumber + 1, Start, facts, chunk, 0, 1);
				}
			}

			void AddString(RecordFacts stringFacts)
			{
				builder.Add(recordNumber + 1, Start, stringFacts, $"String {recordNumber}");
				recordNumber++;
			}

			// The first block holds three chunks. The second block starts in the middle of the third chunk, holds a string row
			// between two rows of the fourth, and ends exactly with the block. The third block starts with a string row.
			AddBytes(10, chunks[0]);
			AddBytes(10, chunks[1]);
			AddBytes(RecordTable.BlockSize - 20 + 4, chunks[2]);
			AddBytes(1, chunks[3]);
			AddString(flagged);
			AddBytes(RecordTable.BlockSize - 6, chunks[3]);
			AddString(facts);
			AddBytes(1, chunks[4]);
			RecordTable table = builder.Build();

			const int SecondBlock = RecordTable.BlockSize;
			const int ThirdBlock = 2 * RecordTable.BlockSize;
			table.Count.Should().Be(ThirdBlock + 2);
			table.CreateRecord(9).Content.Should().Be("A");
			table.CreateRecord(10).Content.Should().Be("B");
			table.CreateRecord(20).Content.Should().Be("C");
			table.CreateRecord(SecondBlock - 1).Content.Should().Be("C");
			table.CreateRecord(SecondBlock + 3).Content.Should().Be("C");
			table.CreateRecord(SecondBlock + 4).Content.Should().Be("D");
			ShouldBe(table.CreateRecord(SecondBlock + 5), SecondBlock + 6, Start, SeverityType.Error, isMultiLine: true, wasGeneratedByUi: true, $"String {SecondBlock + 5}");
			table.CreateRecord(SecondBlock + 6).Content.Should().Be("D");
			table.CreateRecord(ThirdBlock - 1).Content.Should().Be("D");
			table.CreateRecord(ThirdBlock).Content.Should().Be($"String {ThirdBlock}");
			table.CreateRecord(ThirdBlock + 1).Content.Should().Be("E");
		}

		[TestMethod]
		public void GivenExactlyAFullBlock_WhenTheTableIsBuilt_ThenItsLastRecordIsKept()
		{
			var chunk = new ContentChunk(0, Encoding.UTF8.GetBytes("a"), Encoding.UTF8);
			var facts = new RecordFacts(SeverityType.Information, isMultiLine: false, wasGeneratedByUi: false);
			var builder = new RecordTable.Builder();

			for (var recordNumber = 0; recordNumber < RecordTable.BlockSize; recordNumber++)
			{
				builder.Add(recordNumber + 1, Start, facts, chunk, 0, 1);
			}

			RecordTable table = builder.Build();

			table.Count.Should().Be(RecordTable.BlockSize);
			table.CreateRecord(RecordTable.BlockSize - 1).LineNumber.Should().Be(RecordTable.BlockSize);
		}

		[TestMethod]
		public void GivenNoRecords_WhenTheTableIsBuilt_ThenItCreatesNone()
		{
			RecordTable table = new RecordTable.Builder().Build();

			table.Count.Should().Be(0);
			FluentActions.Invoking(() => table.CreateRecord(0)).Should().Throw<ArgumentOutOfRangeException>();
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(10)]
		[DataRow(RecordTable.BlockSize - 1)]
		[DataRow(RecordTable.BlockSize)]
		[DataRow(RecordTable.BlockSize + 5)]
		[DataRow(2 * RecordTable.BlockSize)]
		[DataRow((2 * RecordTable.BlockSize) + 3)]
		public void GivenATableOfSeveralBlocks_WhenItsFirstRecordsAreAppendedTo_ThenItHasThoseRecordsThenTheNewOnes(int count)
		{
			// A grown log keeps the saved table's first records, sharing its full blocks, and adds the records parsed since (#1023, #1201).
			AppendedTableShouldEqualOneBuiltRowByRow(count, chunk => chunk);
		}

		[TestMethod]
		[DataRow(RecordTable.BlockSize)]
		[DataRow((2 * RecordTable.BlockSize) + 3)]
		public void GivenChunksToReplace_WhenATableIsAppendedTo_ThenEveryRowReadsItsOwnReplacedChunk(int count)
		{
			// On disk, every chunk of a grown log's table reads through one source, so the shared blocks' rows must read the replaced chunks too (#1023, #1201).
			AppendedTableShouldEqualOneBuiltRowByRow(
				count,
				chunk => new ContentChunk(chunk.FileOffset, Encoding.UTF8.GetBytes(TableOfSeveralBlocks.GetText(chunk).ToUpperInvariant()), Encoding.UTF8));
		}

		[TestMethod]
		public void GivenFullBlocksToKeep_WhenATableIsAppendedTo_ThenTheirRowsAreNotCopied()
		{
			// A grown log's reopen shares the saved table's full blocks, rather than holding two copies of its rows, 21 bytes per record (#1201).
			var chunk = new ContentChunk(0, Encoding.UTF8.GetBytes("ab"), Encoding.UTF8);
			var facts = new RecordFacts(SeverityType.Information, isMultiLine: false, wasGeneratedByUi: false);
			var builder = new RecordTable.Builder();
			for (var recordNumber = 0; recordNumber < 4 * RecordTable.BlockSize; recordNumber++)
			{
				builder.Add(recordNumber + 1, Start, facts, chunk, recordNumber % 2, 1);
			}

			RecordTable saved = builder.Build();
			RecordTable nothingNew = new RecordTable.Builder().Build();

			var before = GC.GetAllocatedBytesForCurrentThread();
			RecordTable appended = saved.Append(saved.Count, nothingNew, kept => kept);
			var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

			appended.Count.Should().Be(saved.Count);
			allocated.Should().BeLessThan(RecordTable.BlockSize * 21 / 2, "copying the rows of the 4 blocks would allocate about 5.5 MB");
		}

		/// <summary>
		/// Appends a few records to a table's first <paramref name="count"/>, and compares the result with a table built row by row from the same records,
		/// each reading the chunk <paramref name="replaceChunk"/> gives, including where each row's content is in the log, and the chunk directory.
		/// </summary>
		private static void AppendedTableShouldEqualOneBuiltRowByRow(int count, Func<ContentChunk, ContentChunk> replaceChunk)
		{
			var saved = new TableOfSeveralBlocks("ab", (2 * RecordTable.BlockSize) + 10);
			var next = new TableOfSeveralBlocks("cd", 10);
			var replaced = new System.Collections.Generic.Dictionary<ContentChunk, ContentChunk>();
			ContentChunk Replace(ContentChunk chunk) => replaced.TryGetValue(chunk, out ContentChunk r) ? r : replaced[chunk] = replaceChunk(chunk);

			RecordTable appended = saved.Table.Append(count, next.Table, Replace);

			var expected = new RecordTable.Builder();
			saved.AddRows(expected, 0, count, Replace);

			// The records parsed since keep the line numbers their parse gave them.
			next.AddRows(expected, 0, next.Table.Count, Replace);
			RecordTable expectedTable = expected.Build();

			Describe(appended).Should().Equal(Describe(expectedTable));
			appended.ChunkStarts.Should().Equal(expectedTable.ChunkStarts);
		}

		private static string[] Describe(RecordTable table)
		{
			return Enumerable.Range(0, table.Count)
				.Select(recordNumber =>
				{
					IRecord record = table.CreateRecord(recordNumber);
					var fileOffset = table.TryGetFileOffset(recordNumber, out var offset) ? offset : -1;
					return $"{record.LineNumber}|{record.CreatedAt:O}|{record.Severity}|{fileOffset}|{record.Content}";
				})
				.ToArray();
		}

		/// <summary>
		/// A table of string rows and rows that read one of four chunks, each with text of its own: one ends with the first block, the next starts with
		/// the second block, and the last crosses into the third.
		/// </summary>
		private sealed class TableOfSeveralBlocks
		{
			private static readonly RecordFacts Facts = new RecordFacts(SeverityType.Information, isMultiLine: false, wasGeneratedByUi: false);
			private static readonly System.Collections.Generic.Dictionary<ContentChunk, string> Texts = new();

			private readonly ContentChunk[] _chunks;

			public TableOfSeveralBlocks(string letters, int rowCount)
			{
				// Four bytes each, such as "a0b0", at their own place in the log, so a row that reads another chunk reads other text from another offset.
				_chunks = Enumerable.Range(0, 4)
					.Select(index =>
					{
						var text = $"{letters[0]}{index}{letters[1]}{index}";
						var chunk = new ContentChunk(index * 4, Encoding.UTF8.GetBytes(text), Encoding.UTF8);
						lock (Texts)
						{
							Texts[chunk] = text;
						}

						return chunk;
					})
					.ToArray();

				var builder = new RecordTable.Builder();
				AddRows(builder, 0, rowCount, chunk => chunk);
				this.Table = builder.Build();
			}

			public RecordTable Table { get; }

			public static string GetText(ContentChunk chunk)
			{
				lock (Texts)
				{
					return Texts[chunk];
				}
			}

			/// <summary>
			/// Adds the rows from <paramref name="start"/> to before <paramref name="end"/>, each reading the chunk <paramref name="replaceChunk"/> gives.
			/// </summary>
			public void AddRows(RecordTable.Builder builder, int start, int end, Func<ContentChunk, ContentChunk> replaceChunk)
			{
				for (var row = start; row < end; row++)
				{
					if (row % 1_000 == 7 || row == RecordTable.BlockSize - 1)
					{
						builder.Add(row + 1, Start.AddSeconds(row), Facts, $"String {row}");
					}
					else
					{
						builder.Add(row + 1, Start.AddSeconds(row), Facts, replaceChunk(_chunks[GetChunkIndex(row)]), (row % 2) * 2, 2);
					}
				}
			}

			private static int GetChunkIndex(int row)
			{
				return row < 100 ? 0
					: row < RecordTable.BlockSize ? 1
					: row < (2 * RecordTable.BlockSize) - 5 ? 2
					: 3;
			}
		}

		private static void ShouldBe(IRecord record, int lineNumber, DateTime createdAt, SeverityType severity, bool isMultiLine, bool wasGeneratedByUi, string content)
		{
			record.LineNumber.Should().Be(lineNumber);
			record.CreatedAt.Should().Be(createdAt);
			record.Severity.Should().Be(severity);
			record.IsMultiLine.Should().Be(isMultiLine);
			record.WasGeneratedByUi.Should().Be(wasGeneratedByUi);
			record.Content.Should().Be(content);
		}
	}
}
