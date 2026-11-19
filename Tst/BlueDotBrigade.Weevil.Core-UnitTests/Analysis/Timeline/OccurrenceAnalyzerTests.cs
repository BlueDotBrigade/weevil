namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class OccurrenceAnalyzerTests
	{
		private const AnalysisType First = AnalysisType.FirstOccurrence;
		private const AnalysisType Last = AnalysisType.LastOccurrence;

		#region Setup helpers

		private static Results Analyze(AnalysisType analysisType, RecordList records, IRecordState state, string regex)
		{
			var analyzer = new OccurrenceAnalyzer(analysisType, RecordAnalyzerTestContext.CreateFilterStrategy());
			IUserDialog userDialog = RecordAnalyzerTestContext.CreateDialog(regex);

			return analyzer.Analyze(
				records,
				state,
				string.Empty,
				userDialog,
				canUpdateMetadata: true);
		}

		#endregion

		#region Scenario helpers

		// The analyzer flags the first, or the last, record where each unique value appears.
		// Each scenario is two parallel strings:
		//   pattern  : nine single-digit values, e.g. "123454321"
		//   expected : same length as pattern, with '^' replacing each digit whose record should be flagged.
		// The same pattern is exercised twice: once as integers, once as decimals (".0" suffix).

		private static void AssertScenario(AnalysisType analysisType, string pattern, string expected)
		{
			AssertScenario(analysisType, pattern, expected, AnalysisHelper.BuildIntegerRecords(pattern), AnalysisHelper.IntegerRegex, "integer scenario");
			AssertScenario(analysisType, pattern, expected, AnalysisHelper.BuildDecimalRecords(pattern), AnalysisHelper.DecimalRegex, "decimal scenario");
		}

		private static void AssertScenario(AnalysisType analysisType, string pattern, string expected, RecordList records, string regex, string label)
		{
			var state = new RecordState();
			Results results = Analyze(analysisType, records, state, regex);

			AnalysisHelper.AssertFlagsMatchExpected(
				pattern,
				expected,
				records,
				state,
				label,
				$"{analysisType} flag mismatch");

			var expectedCount = AnalysisHelper.CountExpectedFlags(expected);
			results.FlaggedRecords.Should().Be(expectedCount,
				because: $"[{label}] pattern '{pattern}' should report {expectedCount} unique-value occurrence(s)");
		}

		#endregion

		#region Pattern catalogue

		[TestMethod]
		[DataRow(First, "555555555", "^55555555")] // Plateau
		[DataRow(Last, "555555555", "55555555^")]
		[DataRow(First, "123456789", "^^^^^^^^^")] // Rising values: every record
		[DataRow(Last, "123456789", "^^^^^^^^^")]
		[DataRow(First, "987654321", "^^^^^^^^^")] // Falling values: every record
		[DataRow(Last, "987654321", "^^^^^^^^^")]
		[DataRow(First, "123454321", "^^^^^4321")] // Sharp pyramid: through the peak, or from the peak back
		[DataRow(Last, "123454321", "1234^^^^^")]
		[DataRow(First, "123444321", "^^^^44321")] // Plateau pyramid: through the plateau's start, or from its end
		[DataRow(Last, "123444321", "12344^^^^")]
		[DataRow(First, "987656789", "^^^^^6789")] // Sharp valley: through the valley, or from the valley back
		[DataRow(Last, "987656789", "9876^^^^^")]
		[DataRow(First, "987666789", "^^^^66789")] // Plateau valley: through the valley's start, or from its end
		[DataRow(Last, "987666789", "98766^^^^")]
		[DataRow(First, "123123123", "^^^123123")] // Sawtooth: the first cycle
		[DataRow(First, "321321321", "^^^321321")] // Inverted sawtooth: the first cycle, or the last
		[DataRow(Last, "321321321", "321321^^^")]
		[DataRow(First, "555555554", "^5555555^")] // Late falling edge: both unique values
		[DataRow(Last, "555555554", "5555555^^")]
		public void GivenAPatternOfValues_WhenAnalyzed_ThenOneOccurrenceOfEachValueIsFlagged(AnalysisType analysisType, string pattern, string expected)
			=> AssertScenario(analysisType, pattern, expected);

		[TestMethod]
		[WorkItem(529)]
		public void GivenASawtooth_WhenTheLastOccurrenceIsAnalyzed_ThenTheLastCycleIsFlagged()
		{
			// Last Occurrence did not flag the last cycle of a repeating pattern.
			AssertScenario(Last, "123123123", "123123^^^");
		}

		#endregion

		#region Real-world noise (decimal only)

		[TestMethod]
		[DataRow(First, new[] { 0, 2, 4, 7 })]
		[DataRow(Last, new[] { 2, 5, 6, 8 })]
		public void GivenANoisyDecimalPlateau_WhenAnalyzed_ThenEachUniqueValueIsFlaggedOnce(AnalysisType analysisType, int[] expectedIndices)
		{
			// Same noisy sequence as the edge tests. Each distinct decimal value is flagged once:
			// 33.000, 32.999, 32.998 and 32.997 at their first (0, 2, 4, 7) or last (6, 2, 5, 8) occurrence.
			RecordList records = R.Create()
				.WithContent("15:02:00.000 33.000")
				.WithContent("15:02:01.000 33.000")
				.WithContent("15:02:02.000 32.999")
				.WithContent("15:02:03.000 33.000")
				.WithContent("15:02:04.000 32.998")
				.WithContent("15:02:05.000 32.998")
				.WithContent("15:02:06.000 33.000")
				.WithContent("15:02:07.000 32.997")
				.WithContent("15:02:08.000 32.997")
				.GetRecords();

			var state = new RecordState();
			Results results = Analyze(analysisType, records, state, AnalysisHelper.DecimalRegex);

			AnalysisHelper.GetFlaggedIndices(records, state).Should().Equal(expectedIndices);
			results.FlaggedRecords.Should().Be(expectedIndices.Length);
		}

		#endregion
	}
}
