namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Linq;
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.IO;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Moq;

	[TestClass]
	public class AnalysisShould
	{
		private static IUserDialog GetUserDialog()
		{
			var userDialog = new Mock<IUserDialog>();
			return userDialog.Object;
		}

		[TestMethod]
		public void FlagRecordsWhenDataTransitionDetected()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
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
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine.Filter.Apply(
				FilterType.RegularExpression,
				new FilterCriteria(@"to new state (?<State>.*)"));

			engine
				.Analyzer
				.Analyze(AnalysisType.DetectDataTransition, GetUserDialog());

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
		[TestMethod]
		public void DetectRisingEdges()
		{
			var dectectMinuteIncreasing = @"\s12:(?<Minute>[0-9]{2})";

			var engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(dectectMinuteIncreasing));

			engine.Analyzer.Analyze(AnalysisType.DetectRisingEdges);

			var flaggedRecords = engine
				.Filter.Results
				.Count(x => x.Metadata.IsFlagged);

			// 8 transitions + 1 for the first value found
			Assert.AreEqual(9, flaggedRecords);
		}

		[TestMethod]
		public void DetectFallingEdges()
		{
			var detectSecondRollover = @"\s12:[0-9]{2}:(?<Second>[0-9]{2})";

			var engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(detectSecondRollover));

			engine.Analyzer.Analyze(AnalysisType.DetectFallingEdges);

			var flaggedRecords = engine
				.Filter.Results
				.Count(x => x.Metadata.IsFlagged);

			// 8 transitions + 1 for the first value found
			Assert.AreEqual(9, flaggedRecords);
		}
	}
}
