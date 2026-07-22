namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;
	using NSubstitute;

	[TestClass]
	public class ThresholdCrossingsAnalyzerTests
	{
		#region Setup helpers

		private static Results Analyze(ImmutableArray<IRecord> records, string regex, string threshold, string comparison)
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
				.Returns(threshold, comparison);

			return analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);
		}

		#endregion

		#region Scenario helpers

		private static void AssertIntegerScenario(string pattern, string threshold, string comparison, string expected)
		{
			var records = AnalysisHelper.BuildIntegerRecords(pattern);
			var results = Analyze(records, AnalysisHelper.IntegerRegex, threshold, comparison);

			AssertFlagsMatchExpected(pattern, expected, records, results, $"integer scenario ({comparison} {threshold})");
		}

		private static void AssertDecimalScenario(string pattern, string threshold, string comparison, string expected)
		{
			var records = AnalysisHelper.BuildDecimalRecords(pattern);
			var results = Analyze(records, AnalysisHelper.DecimalRegex, threshold, comparison);

			AssertFlagsMatchExpected(pattern, expected, records, results, $"decimal scenario ({comparison} {threshold})");
		}

		private static void AssertFlagsMatchExpected(
			string pattern,
			string expected,
			ImmutableArray<IRecord> records,
			Results results,
			string label)
		{
			AnalysisHelper.AssertFlagsMatchExpected(
				pattern,
				expected,
				records,
				label,
				"threshold-crossing flag mismatch");

			var expectedCount = AnalysisHelper.CountExpectedFlags(expected);

			results.FlaggedRecords.Should().Be(expectedCount,
				because: $"[{label}] should report {expectedCount} threshold crossing(s)");
		}

		#endregion

		[TestMethod]
		[DataRow("123434567", "3", "^^3434567")]
		[DataRow("123434567", "4", "^^^4^4567")]
		[DataRow("123434567", "5", "^^^^^^567")]
		public void GivenValues_WhenComparisonIsLessThan_ThenOnlyValuesBelowThresholdAreFlagged(string pattern, string threshold, string expected)
		{
			// Regression: Issue #911
			AssertIntegerScenario(pattern, threshold, "<", expected);
		}

		[TestMethod]
		[DataRow("123434567", "3", "^^^4^4567")]
		[DataRow("123434567", "4", "^^^^^^567")]
		[DataRow("123434567", "5", "^^^^^^^67")]
		public void GivenValues_WhenComparisonIsLessThanOrEqual_ThenThresholdAndLowerValuesAreFlagged(string pattern, string threshold, string expected)
		{
			// Regression: Issue #911
			AssertIntegerScenario(pattern, threshold, "<=", expected);
		}

		[TestMethod]
		[DataRow("123434567", "3", "123^3^^^^")]
		[DataRow("123434567", "5", "1234345^^")]
		public void GivenValues_WhenComparisonIsGreaterThan_ThenOnlyValuesAboveThresholdAreFlagged(string pattern, string threshold, string expected)
		{
			// Regression: Issue #911
			AssertIntegerScenario(pattern, threshold, ">", expected);
		}

		[TestMethod]
		[DataRow("123434567", "3", "12^^^^^^^")]
		[DataRow("123434567", "5", "123434^^^")]
		public void GivenValues_WhenComparisonIsGreaterThanOrEqual_ThenThresholdAndHigherValuesAreFlagged(string pattern, string threshold, string expected)
		{
			// Regression: Issue #911
			AssertIntegerScenario(pattern, threshold, ">=", expected);
		}

		[TestMethod]
		[DataRow(">", "3")]
		[DataRow("<", "3")]
		[DataRow(">=", "^")]
		[DataRow("<=", "^")]
		public void GivenValueEqualsThreshold_WhenApplyingComparison_ThenInclusivityBehaviorIsClear(string comparison, string expected)
		{
			// Regression: Issue #911
			AssertIntegerScenario("3", "3", comparison, expected);
		}

		[TestMethod]
		[DataRow("123434567", "123^3^^^^")]
		public void GivenDecimalValues_WhenComparisonIsGreaterThan_ThenOnlyValuesAboveThresholdAreFlagged(string pattern, string expected)
		{
			// Regression: Issue #911
			AssertDecimalScenario(pattern, "3", ">", expected);
		}
	}
}
