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
