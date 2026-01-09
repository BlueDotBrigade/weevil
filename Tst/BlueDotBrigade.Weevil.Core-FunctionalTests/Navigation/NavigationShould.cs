namespace BlueDotBrigade.Weevil.Navigation
{
	using Data;
	using BlueDotBrigade.Weevil.Analysis;
	using Filter;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class NavigationShould
	{
		[TestMethod]
		public void SupportNavigatingToNextPinnedRecord()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Filter.Results[9].Metadata.IsPinned = true;

			engine.Selector.Select(1);
			IRecord pinnedRecord = engine.Navigate.NextPin();

			// Reminder: although they are often similar, the line number and index are NOT the same!
			Assert.AreEqual(10, pinnedRecord.LineNumber);
		}

		[TestMethod]
		public void SupportNavigatingToNextRecordWithComment()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Analyzer.RemoveComments(true);

			engine.Records[12].Metadata.Comment = "First";
			engine.Records[24].Metadata.Comment = "Second";

			engine.Selector.Select(1);

			IRecord record = engine.Navigate.NextComment();
			Assert.AreEqual("First", record.Metadata.Comment);

			record = engine.Navigate.NextComment();
			Assert.AreEqual("Second", record.Metadata.Comment);
		}

		[TestMethod]
		public void SupportNavigatingToNextCommentContainingText()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Analyzer.RemoveComments(true);

			engine.Records[12].Metadata.Comment = "First note";
			engine.Records[24].Metadata.Comment = "Second item";
			engine.Records[36].Metadata.Comment = "Third note";

			engine.Selector.Select(1);

			// Search for "note" - should find records 12 and 36
			IRecord record = engine.Navigate.NextCommentWithText("note", false);
			Assert.AreEqual("First note", record.Metadata.Comment);

			record = engine.Navigate.NextCommentWithText("note", false);
			Assert.AreEqual("Third note", record.Metadata.Comment);
		}

		[TestMethod]
		public void SupportNavigatingToPreviousCommentContainingText()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Analyzer.RemoveComments(true);

			engine.Records[12].Metadata.Comment = "First note";
			engine.Records[24].Metadata.Comment = "Second item";
			engine.Records[36].Metadata.Comment = "Third note";

			engine.Selector.Select(50);

			// Search backwards for "note" - should find records 36 and 12
			IRecord record = engine.Navigate.PreviousCommentWithText("note", false);
			Assert.AreEqual("Third note", record.Metadata.Comment);

			record = engine.Navigate.PreviousCommentWithText("note", false);
			Assert.AreEqual("First note", record.Metadata.Comment);
		}

		[TestMethod]
		public void SupportCaseSensitiveCommentSearch()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Analyzer.RemoveComments(true);

			engine.Records[12].Metadata.Comment = "First NOTE";
			engine.Records[24].Metadata.Comment = "Second note";

			engine.Selector.Select(1);

			// Case-sensitive search for "note" (lowercase) should only find record 24
			IRecord record = engine.Navigate.NextCommentWithText("note", true);
			Assert.AreEqual("Second note", record.Metadata.Comment);
		}

		[TestMethod]
		public void SupportRegexCommentSearch()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Analyzer.RemoveComments(true);

			engine.Records[12].Metadata.Comment = "Bug #123";
			engine.Records[24].Metadata.Comment = "Feature request";
			engine.Records[36].Metadata.Comment = "Bug #456";

			engine.Selector.Select(1);

			// Regex search for bug numbers
			IRecord record = engine.Navigate.NextCommentWithText(@"Bug #\d+", false, true);
			Assert.AreEqual("Bug #123", record.Metadata.Comment);

			record = engine.Navigate.NextCommentWithText(@"Bug #\d+", false, true);
			Assert.AreEqual("Bug #456", record.Metadata.Comment);
		}

		[TestMethod]
		public void SupportNavigatingToNextFlaggedRecord()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Filter.Apply(
				FilterType.RegularExpression,
				new FilterCriteria(@"This is a sample log message\. Id=(?<Hundredths>\d)"));

			engine.Analyzer.Analyze(AnalysisType.DetectDataTransition);

			IRecord record = engine.Navigate.NextFlag();
			Assert.AreEqual(1, record.LineNumber);

			record = engine.Navigate.NextFlag();
			Assert.AreEqual(101, record.LineNumber);
		}

		[TestMethod]
		public void HandleNavigatingWhenPinnedRecordsHidden()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria("Nothing Should Match This Filter"));

			Assert.AreEqual(Record.Dummy, engine.Navigate.NextPin());
		}
	}
}
