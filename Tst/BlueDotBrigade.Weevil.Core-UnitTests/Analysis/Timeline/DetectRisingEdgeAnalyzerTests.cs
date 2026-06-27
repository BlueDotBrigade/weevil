namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Immutable;
  using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class DetectRisingEdgeAnalyzerTests
	{
		#region Setup helpers

		private static Results Analyze(ImmutableArray<IRecord> records, string regex)
		{
			var analyzer = new DetectRisingEdgeAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(regex);

			return analyzer.Analyze(
				records,
				string.Empty,
				userDialog,
				canUpdateMetadata: true);
		}

		#endregion

		#region Scenario helpers

		// Each scenario is expressed as two parallel strings:
		//   pattern  : nine single-digit values, e.g. "987656789"
		//   expected : same length as pattern, with '^' replacing any digit whose record should be flagged,
		//              e.g. "9876^6789" means "flag the record at index 4 (the valley)"
		// The same pattern is exercised twice: once as integers, once as decimals (".0" suffix).

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
				"rising-edge flag mismatch");

			var expectedCount = AnalysisHelper.CountExpectedFlags(expected);
			results.FlaggedRecords.Should().Be(expectedCount,
				because: $"[{label}] pattern '{pattern}' should report {expectedCount} rising-edge transition(s)");
		}

		#endregion

		#region Pattern catalogue

		// Each test runs the analyzer against a 9-value sequence (single digits, range 1-9) twice:
		// once with integer values, once with decimal values. The expected string visualises the
		// flagged record(s) using the '^' character — flag the VALLEY (record before each rising run starts).

		[TestMethod]
		[DataRow("555555555", "555555555")]
		public void Analyze_Plateau_FlagsNothing(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123456789", "^23456789")]
		public void Analyze_RisingValues_FlagsTheFirstRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987654321", "987654321")]
		public void Analyze_FallingValues_FlagsNothing(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123454321", "^23454321")]
		public void Analyze_SharpPyramid_FlagsTheFirstRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123444321", "^23444321")]
		public void Analyze_PlateauPyramid_FlagsTheFirstRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987656789", "9876^6789")]
		public void Analyze_SharpValley_FlagsTheValley(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987666789", "98766^789")]
		public void Analyze_PlateauValley_FlagsTheLastValleyValue(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123123123", "^23^23^23")]
		public void Analyze_Sawtooth_FlagsEachValley(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("321321321", "32^32^321")]
		public void Analyze_InvertedSawtooth_FlagsEachValley(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("555555554", "555555554")]
		public void Analyze_LateFallingEdge_FlagsNothing(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		#endregion

		#region Real-world noise (decimal only)

		[TestMethod]
		public void Analyze_NoisyDecimalPlateau_FlagsEachSubUnitRise()
		{
			// Same noisy sequence as the falling-edge test, but flagged from the rising perspective:
			// each time the value recovers back to 33.000 after a sub-unit dip, the analyzer flags
			// the last record below 33.000 (the local valley).
			// Sequence: 33.000 33.000 32.999 33.000 32.998 32.998 33.000 32.997 32.997
			//                         ^                ^
			// Indices 2 and 5 are flagged (the two recoveries; index 7's 32.997 never recovers).
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

            AnalysisHelper.GetFlaggedIndices(records).Should().Equal(2, 5);
			results.FlaggedRecords.Should().Be(2);
		}

		#endregion
	}
}
