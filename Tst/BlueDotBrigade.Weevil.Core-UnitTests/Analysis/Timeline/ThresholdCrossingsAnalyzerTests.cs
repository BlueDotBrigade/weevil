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
		private static Results Analyze(ImmutableArray<IRecord> records, string threshold, string comparison)
		{
			var analyzer = new ThresholdCrossingsAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = Substitute.For<IUserDialog>();

			userDialog
				.TryGetExpressions(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
				.Returns(callInfo =>
				{
					callInfo[2] = AnalysisHelper.IntegerRegex;
					return true;
				});

			userDialog
				.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns(threshold, comparison);

			return analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);
		}

		private static ImmutableArray<IRecord> BuildRecords(params int[] values)
		{
			var builder = R.Create();
			for (var i = 0; i < values.Length; i++)
			{
				builder = builder.WithContent($"15:02:{i:D2}.000 {values[i]}");
			}

			return builder.GetRecords();
		}

		private static void AssertScenario(int[] values, string threshold, string comparison, params int[] expectedFlaggedIndices)
		{
			var records = BuildRecords(values);
			var results = Analyze(records, threshold, comparison);

			var actualFlaggedIndices = AnalysisHelper.GetFlaggedIndices(records);
			actualFlaggedIndices.Should().Equal(expectedFlaggedIndices,
				because: $"comparison '{comparison} {threshold}' should flag only the expected threshold crossings");
			results.FlaggedRecords.Should().Be(expectedFlaggedIndices.Length);
		}

		[TestMethod]
		public void GivenValues_WhenComparisonIsGreaterThan_ThenOnlyValuesGreaterThanThresholdAreFlagged()
		{
			// Regression: Issue #911
			AssertScenario([9, 10, 11, 8, 12], "10", ">", 2, 4);
		}

		[TestMethod]
		public void GivenValues_WhenComparisonIsLessThan_ThenOnlyValuesLessThanThresholdAreFlagged()
		{
			// Regression: Issue #911
			AssertScenario([9, 10, 11, 8, 12], "10", "<", 0, 3);
		}

		[TestMethod]
		public void GivenValues_WhenComparisonIsGreaterThanOrEqual_ThenThresholdAndHigherValuesAreFlagged()
		{
			// Regression: Issue #911
			AssertScenario([9, 10, 11, 8, 12], "10", ">=", 1, 2, 4);
		}

		[TestMethod]
		public void GivenValues_WhenComparisonIsLessThanOrEqual_ThenThresholdAndLowerValuesAreFlagged()
		{
			// Regression: Issue #911
			AssertScenario([9, 10, 11, 8, 12], "10", "<=", 0, 1, 3);
		}

		[TestMethod]
		[DataRow(">", false)]
		[DataRow("<", false)]
		[DataRow(">=", true)]
		[DataRow("<=", true)]
		public void GivenValueEqualsThreshold_WhenRunningAnalysis_ThenInclusivityIsApplied(string comparison, bool shouldFlag)
		{
			// Regression: Issue #911
			var records = R.Create()
				.WithContent("15:02:00.000 10")
				.GetRecords();

			var results = Analyze(records, "10", comparison);

			if (shouldFlag)
			{
				AnalysisHelper.GetFlaggedIndices(records).Should().Equal(0);
				results.FlaggedRecords.Should().Be(1);
			}
			else
			{
				AnalysisHelper.GetFlaggedIndices(records).Should().BeEmpty();
				results.FlaggedRecords.Should().Be(0);
			}
		}
	}
}
