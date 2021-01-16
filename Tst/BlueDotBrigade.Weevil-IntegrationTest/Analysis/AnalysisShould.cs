namespace BlueDotBrigade.Weevil.Analysis
{
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
	}
}
