namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.IO;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class AnalysisShould
	{
		[TestMethod]
		public void FlagRecordsWhenDataTransitionDetected()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Filter.Apply(
				FilterType.RegularExpression,
				new FilterCriteria(@"to new state (?<State>.*)"));

			engine
				.Analyzer
				.Analyze(AnalysisType.DetectDataTransition);

			foreach (IRecord record in engine.Filter.Results)
			{
				switch (record.LineNumber)
				{
					case 100:
					case 200:
					case 300:
					case 400:
					case 500:
						Assert.IsTrue(record.Metadata.IsFlagged);
						break;

					default:
						Assert.IsFalse(record.Metadata.IsFlagged);
						break;
				}
			}
		}

		[TestMethod]
		public void AddCommentWhenDataTransitionDetected()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Filter.Apply(
				FilterType.RegularExpression,
				new FilterCriteria(@"to new state (?<State>.*)"));

			engine
				.Analyzer
				.Analyze(AnalysisType.DetectDataTransition);

			foreach (IRecord record in engine.Filter.Results)
			{
				switch (record.LineNumber)
				{
					case 100:
					case 200:
					case 300:
					case 400:
					case 500:
						Assert.IsTrue(record.Metadata.HasComment);
						break;
				}
			}
		}

		// HACK: This integration test should be a unit test. It isn't because the analyzer depends on `FilterStrategy` (a complex object) as an input. Code smell.
		//[TestMethod]
		//public void DetectRisingEdges()
		//{
		//	var dectectMinuteIncreasing = @"\s12:(?<Minute>[0-9]{2})";

		//	var engine = Engine
		//		.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
		//		.Open();

		//	engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(dectectMinuteIncreasing));

		//	// Only a plugin knows what to ask the user.  Furthermore, the unit test has no idea about the implementation details
		//	// ... E.g. How many parameters are needed? What types of parameters is the plugin expecting?
		//	// TODO: re-write the `IUserDialog` interface so that the unit test doesn't care about the implementation details
		//	var userDialog = new Mock<IUserDialog>();
		//	userDialog.Setup(x => x.ShowUserPrompt(
		//		It.IsAny<string>(),
		//		It.IsAny<string>(),
		//		It.IsAny<string>())).Returns("Ascending");
		//	engine.Analyzer.Analyze(AnalysisType.DetectRisingEdges, userDialog.Object);

		//	var flaggedRecords = engine
		//		.Filter.Results
		//		.Count(x => x.Metadata.IsFlagged);

		//	// 8 transitions + 1 for the first value found
		//	Assert.AreEqual(9, flaggedRecords);
		//}

		//[TestMethod]
		//public void DetectFallingEdges()
		//{
		//	var detectSecondRollover = @"\s12:[0-9]{2}:(?<Second>[0-9]{2})";

		//	var engine = Engine
		//		.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
		//		.Open();

		//	engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(detectSecondRollover));

		//	// Only a plugin knows what to ask the user.  Furthermore, the unit test has no idea about the implementation details
		//	// ... E.g. How many parameters are needed? What types of parameters is the plugin expecting?
		//	// TODO: re-write the `IUserDialog` interface so that the unit test doesn't care about the implementation details
		//	var userDialog = new Mock<IUserDialog>();
		//	userDialog.Setup(x => x.ShowUserPrompt(
		//		It.IsAny<string>(),
		//		It.IsAny<string>(),
		//		It.IsAny<string>())).Returns("Ascending"); 
		//	engine.Analyzer.Analyze(AnalysisType.DetectFallingEdges, userDialog.Object);

		//	var flaggedRecords = engine
		//		.Filter.Results
		//		.Count(x => x.Metadata.IsFlagged);

		//	// 8 transitions + 1 for the first value found
		//	Assert.AreEqual(9, flaggedRecords);
		//}
	}
}
