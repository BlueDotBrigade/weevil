namespace BlueDotBrigade.Weevil.Data
{
	[TestClass]
	public class TsvRecordParserTest
	{
		/// <summary>
		///     Represents the line number for the first log entry.
		/// </summary>
		private const int LineOne = 1;

		[TestMethod]
		public void TryParse_NoRecords_ReturnsEmptyRecord()
		{
			var content = new Daten().AsString();
			var wasSuccessful = new TsvRecordParser().TryParse(LineOne, content, out IRecord record);

			wasSuccessful.Should().BeFalse();
			record.Should().Be(Record.Dummy);
		}

		[TestMethod]
		public void TryParse_CompleteRecord_ReturnsRecord()
		{
			var content = new Daten().AsString();

			var wasSuccessful = new TsvRecordParser().TryParse(LineOne, content, out IRecord record);

			wasSuccessful.Should().BeTrue();
			record.LineNumber.Should().Be(LineOne);
			record.Severity.Should().Be(SeverityType.Warning);
			record.Content.Should().Be(@"2019-12-31 23:59:59.000	123	7890	Warning	UserInterface	Application is initializing...");
		}

		[TestMethod]
		public void TryParse_CompleteRecordHasTrailingWhiteSpace_WhiteSpaceIsKept()
		{
			var content = new Daten().AsString();

			new TsvRecordParser().TryParse(LineOne, content, out IRecord record);

			(record.Content.EndsWith("            ")).Should().BeTrue();
		}

		[TestMethod]
		public void TryParse_PartialRecordMissingMessage_ReturnsRecord()
		{
			var content = new Daten().AsString();

			var wasSuccessful = new TsvRecordParser().TryParse(LineOne, content, out IRecord record);

			wasSuccessful.Should().BeTrue();
			record.Content.Should().Be(@"2019-12-31 23:59:59.000	123	7890	Warning	UserInterface");
		}

		[TestMethod]
		public void TryParse_PartialRecordMissingContext_ReturnsRecord()
		{
			var content = new Daten().AsString();

			(new TsvRecordParser().TryParse(LineOne, content, out _)).Should().BeFalse();
		}

		[TestMethod]
		public void TryParse_PartialRecordMissingThreadId_ReturnsRecord()
		{
			var content = new Daten().AsString();

			(new TsvRecordParser().TryParse(LineOne, content, out _)).Should().BeFalse();
		}

		[TestMethod]
		public void TryParse_PartialRecordMissingSeverity_ReturnsRecord()
		{
			var content = new Daten().AsString();

			(new TsvRecordParser().TryParse(LineOne, content, out _)).Should().BeFalse();
		}

		[TestMethod]
		public void TryParse_PartialRecordMissingTime_ReturnsRecord()
		{
			var content = new Daten().AsString();

			(new TsvRecordParser().TryParse(LineOne, content, out _)).Should().BeFalse();
		}
	}
}