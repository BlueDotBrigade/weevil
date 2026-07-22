namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.TestTools.Data;
	using NSubstitute;

	[TestClass]
	public class ThresholdCrossingsAnalyzerTests
	{
		private static Results Analyze(ImmutableArray<IRecord> records, string regex, string threshold, string direction)
		{
			var analyzer = new ThresholdCrossingsAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = Substitute.For<IUserDialog>();

			userDialog
				.TryGetExpressions(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
				.Returns(callInfo =>
				{
					callInfo[2] = regex;
					return true;
				});

			userDialog
				.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns(threshold, direction);

			return analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);
		}

		[TestMethod]
		public void GivenValues_WhenAboveThresholdSelected_ThenFlagsOnlyValuesAboveThreshold()
		{
			// Regression: Issue #911
			var records = R.Create()
				.WithContent("15:02:00.000 9")
				.WithContent("15:02:01.000 10")
				.WithContent("15:02:02.000 11")
				.WithContent("15:02:03.000 8")
				.WithContent("15:02:04.000 12")
				.GetRecords();

			var results = Analyze(records, AnalysisHelper.IntegerRegex, "10", "Above");

			AnalysisHelper.GetFlaggedIndices(records).Should().Equal(2, 4);
			results.FlaggedRecords.Should().Be(2);
		}

		[TestMethod]
		public void GivenValues_WhenBelowThresholdSelected_ThenFlagsOnlyValuesBelowThreshold()
		{
			// Regression: Issue #911
			var records = R.Create()
				.WithContent("15:02:00.000 9")
				.WithContent("15:02:01.000 10")
				.WithContent("15:02:02.000 11")
				.WithContent("15:02:03.000 8")
				.WithContent("15:02:04.000 12")
				.GetRecords();

			var results = Analyze(records, AnalysisHelper.IntegerRegex, "10", "Below");

			AnalysisHelper.GetFlaggedIndices(records).Should().Equal(0, 3);
			results.FlaggedRecords.Should().Be(2);
		}

		[TestMethod]
		public void GivenDecimalThreshold_WhenAboveThresholdSelected_ThenFlagsDecimalValuesAboveThreshold()
		{
			// Regression: Issue #911
			var records = R.Create()
				.WithContent("15:02:00.000 10.4")
				.WithContent("15:02:01.000 10.5")
				.WithContent("15:02:02.000 10.6")
				.GetRecords();

			var results = Analyze(records, AnalysisHelper.DecimalRegex, "10.5", "Above");

			AnalysisHelper.GetFlaggedIndices(records).Should().Equal(2);
			results.FlaggedRecords.Should().Be(1);
		}
	}
}
