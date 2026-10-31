namespace BlueDotBrigade.Weevil.Data
{
	using System;
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
		public void GivenNoRecords_WhenTheTableIsBuilt_ThenItCreatesNone()
		{
			RecordTable table = new RecordTable.Builder().Build();

			table.Count.Should().Be(0);
			table.CreateRecords().Should().BeEmpty();
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
