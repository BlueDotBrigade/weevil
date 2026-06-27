namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class FirstOccurrenceAnalyzerTests
	{
		#region Setup helpers

		private static Results Analyze(ImmutableArray<IRecord> records, string regex)
		{
			var analyzer = new FirstOccurrenceAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(regex);

			return analyzer.Analyze(
				records,
				string.Empty,
				userDialog,
				canUpdateMetadata: true);
		}

		#endregion

		#region Scenario helpers

		// FirstOccurrenceAnalyzer flags the first record where each unique value appears.
		// Each scenario is two parallel strings:
		//   pattern  : nine single-digit values, e.g. "123454321"
		//   expected : same length as pattern, with '^' replacing each digit whose record should be flagged.

		private static void AssertScenario(string pattern, string expected)
		{
			AssertIntegerScenario(pattern, expected);
			AssertDecimalScenario(pattern, expected);
		}

		private static void AssertIntegerScenario(string pattern, string expected)
		{
         var records = AnalysisHelper.BuildIntegerRecords(pattern);
			var results = Analyze(records, AnalysisHelper.IntegerRegex);

			AssertFlagsMatchExpected(pattern, expected, records, results, label: "integer scenario");
		}

		private static void AssertDecimalScenario(string pattern, string expected)
		{
         var records = AnalysisHelper.BuildDecimalRecords(pattern);
			var results = Analyze(records, AnalysisHelper.DecimalRegex);

			AssertFlagsMatchExpected(pattern, expected, records, results, label: "decimal scenario");
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
				"first-occurrence flag mismatch");

			var expectedCount = AnalysisHelper.CountExpectedFlags(expected);
			results.FlaggedRecords.Should().Be(expectedCount,
				because: $"[{label}] pattern '{pattern}' should report {expectedCount} unique-value first occurrence(s)");
		}

		#endregion

		#region Pattern catalogue

		[TestMethod]
		[DataRow("555555555", "^55555555")]
		public void Analyze_Plateau_FlagsTheFirstRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123456789", "^^^^^^^^^")]
		public void Analyze_RisingValues_FlagsEveryRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987654321", "^^^^^^^^^")]
		public void Analyze_FallingValues_FlagsEveryRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123454321", "^^^^^4321")]
		public void Analyze_SharpPyramid_FlagsThroughThePeak(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123444321", "^^^^44321")]
		public void Analyze_PlateauPyramid_FlagsThroughThePeakStart(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987656789", "^^^^^6789")]
		public void Analyze_SharpValley_FlagsThroughTheValley(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987666789", "^^^^66789")]
		public void Analyze_PlateauValley_FlagsThroughTheValleyStart(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123123123", "^^^123123")]
		public void Analyze_Sawtooth_FlagsTheFirstCycle(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("321321321", "^^^321321")]
		public void Analyze_InvertedSawtooth_FlagsTheFirstCycle(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("555555554", "^5555555^")]
		public void Analyze_LateFallingEdge_FlagsBothUniqueValues(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		#endregion

		#region Real-world noise (decimal only)

		[TestMethod]
		public void Analyze_NoisyDecimalPlateau_FlagsEachUniqueValue()
		{
			// Same noisy sequence as the falling/rising tests. FirstOccurrenceAnalyzer flags the first
			// occurrence of each distinct decimal value: 33.000 (idx 0), 32.999 (idx 2),
			// 32.998 (idx 4), 32.997 (idx 7).
			var records = R.Create()
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

           var results = Analyze(records, AnalysisHelper.DecimalRegex);

          AnalysisHelper.GetFlaggedIndices(records).Should().Equal(0, 2, 4, 7);
			results.FlaggedRecords.Should().Be(4);
		}

		#endregion
	}
}
