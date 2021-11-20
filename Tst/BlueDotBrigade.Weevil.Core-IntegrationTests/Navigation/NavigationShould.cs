namespace BlueDotBrigade.Weevil
{
	using Data;
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Navigation;
	using Filter;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class NavigationShould
	{
		[TestMethod]
		public void SupportNavigatingToNextPinnedRecord()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
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
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
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
		public void SupportNavigatingToNextFlaggedRecord()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
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
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria("Nothing Should Match This Filter"));

			Assert.AreEqual(Record.Dummy, engine.Navigate.NextPin());
		}
	}
}
