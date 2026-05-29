namespace BlueDotBrigade.Weevil.Navigation
{
	using Data;
	using BlueDotBrigade.Weevil.Analysis;
	using Filter;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class NavigationShould
	{
		// Regression: Issue #499 - GoTo timestamp does not always appear to work
		[TestMethod]
		public void GoToExactTimestampWhenOnlyOneMatchExists()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("GoToTimestamp.log"))
				.Open();

			engine.Selector.Select(1);

			IRecord result = engine.Navigate.GoTo("06:59:39.0207", RecordSearchType.NearestNeighbor);

			(result.LineNumber).Should().Be(4);
		}

		// Regression: Issue #499 - GoTo timestamp does not always appear to work
		[TestMethod]
		public void GoToNearestRecordWhenTimestampIsSlightlyBeforeRecord()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("GoToTimestamp.log"))
				.Open();

			engine.Selector.Select(1);

			// Search for a time 7ms before line 4 (06:59:39.0207); line 4 is the nearest neighbor
			IRecord result = engine.Navigate.GoTo("06:59:39.020", RecordSearchType.NearestNeighbor);

			(result.LineNumber).Should().Be(4);
		}

		// Regression: Issue #499 - GoTo timestamp does not always appear to work
		[TestMethod]
		public void GoToNearestRecordWhenTimestampIsSlightlyAfterRecord()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("GoToTimestamp.log"))
				.Open();

			engine.Selector.Select(1);

			// Search for a time 3ms after line 4 (06:59:39.0207); line 4 is the nearest neighbor
			IRecord result = engine.Navigate.GoTo("06:59:39.021", RecordSearchType.NearestNeighbor);

			(result.LineNumber).Should().Be(4);
		}


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
			(pinnedRecord.LineNumber).Should().Be(10);
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
			(record.Metadata.Comment).Should().Be("First");

			record = engine.Navigate.NextComment();
			(record.Metadata.Comment).Should().Be("Second");
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
			(record.Metadata.Comment).Should().Be("First note");

			record = engine.Navigate.NextCommentWithText("note", false);
			(record.Metadata.Comment).Should().Be("Third note");
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
			(record.Metadata.Comment).Should().Be("Third note");

			record = engine.Navigate.PreviousCommentWithText("note", false);
			(record.Metadata.Comment).Should().Be("First note");
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
			(record.Metadata.Comment).Should().Be("Second note");
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
			(record.Metadata.Comment).Should().Be("Bug #123");

			record = engine.Navigate.NextCommentWithText(@"Bug #\d+", false, true);
			(record.Metadata.Comment).Should().Be("Bug #456");
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

			engine.Analyzer.Analyze(AnalysisType.StateTransitions);

			IRecord record = engine.Navigate.NextFlag();
			(record.LineNumber).Should().Be(1);

			record = engine.Navigate.NextFlag();
			(record.LineNumber).Should().Be(101);
		}

		[TestMethod]
		public void HandleNavigatingWhenPinnedRecordsHidden()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria("Nothing Should Match This Filter"));

			(engine.Navigate.NextPin()).Should().Be(Record.Dummy);
		}
	}
}
