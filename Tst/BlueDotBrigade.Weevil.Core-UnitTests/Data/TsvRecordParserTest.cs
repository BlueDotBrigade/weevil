namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

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
			var content = InputData.GetAsString();
			var wasSuccessful = new TsvRecordParser().TryParse(LineOne, content, out Record record);

			Assert.IsFalse(wasSuccessful);
			Assert.AreEqual(Record.Dummy, record);
		}

		[TestMethod]
		public void TryParse_CompleteRecord_ReturnsRecord()
		{
			var content = InputData.GetAsString();

			var wasSuccessful = new TsvRecordParser().TryParse(LineOne, content, out Record record);

			Assert.IsTrue(wasSuccessful);
			Assert.AreEqual(LineOne, record.LineNumber);
			Assert.AreEqual(SeverityType.Warning, record.Severity);
			Assert.AreEqual(@"2019-12-31 23:59:59.000	123	7890	Warning	UserInterface	Application is initializing...",
				record.Content);
		}

		[TestMethod]
		public void TryParse_CompleteRecordHasTrailingWhiteSpace_WhiteSpaceIsKept()
		{
			var content = InputData.GetAsString();

			new TsvRecordParser().TryParse(LineOne, content, out Record record);

			Assert.IsTrue(record.Content.EndsWith("            "));
		}

		[TestMethod]
		public void TryParse_PartialRecordMissingMessage_ReturnsRecord()
		{
			var content = InputData.GetAsString();

			var wasSuccessful = new TsvRecordParser().TryParse(LineOne, content, out Record record);

			Assert.IsTrue(wasSuccessful);
			Assert.AreEqual(@"2019-12-31 23:59:59.000	123	7890	Warning	UserInterface", record.Content);
		}

		[TestMethod]
		public void TryParse_PartialRecordMissingContext_ReturnsRecord()
		{
			var content = InputData.GetAsString();

			Assert.IsFalse(new TsvRecordParser().TryParse(LineOne, content, out _));
		}

		[TestMethod]
		public void TryParse_PartialRecordMissingThreadId_ReturnsRecord()
		{
			var content = InputData.GetAsString();

			Assert.IsFalse(new TsvRecordParser().TryParse(LineOne, content, out _));
		}

		[TestMethod]
		public void TryParse_PartialRecordMissingSeverity_ReturnsRecord()
		{
			var content = InputData.GetAsString();

			Assert.IsFalse(new TsvRecordParser().TryParse(LineOne, content, out _));
		}

		[TestMethod]
		public void TryParse_PartialRecordMissingTime_ReturnsRecord()
		{
			var content = InputData.GetAsString();

			Assert.IsFalse(new TsvRecordParser().TryParse(LineOne, content, out _));
		}
	}
}