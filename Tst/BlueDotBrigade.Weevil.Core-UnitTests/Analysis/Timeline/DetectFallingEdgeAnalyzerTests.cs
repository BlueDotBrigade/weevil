namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Immutable;
 using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class DetectFallingEdgeAnalyzerTests
	{
		#region Setup helpers

		private static Results Analyze(ImmutableArray<IRecord> records, string regex)
		{
			var analyzer = new DetectFallingEdgeAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
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
		//   pattern  : nine single-digit values, e.g. "123454321"
		//   expected : same length as pattern, with '^' replacing any digit whose record should be flagged,
		//              e.g. "1234^4321" means "flag the record at index 4 (the peak)"
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
				"falling-edge flag mismatch");

            var expectedCount = AnalysisHelper.CountExpectedFlags(expected);

			results.FlaggedRecords.Should().Be(expectedCount,
				because: $"[{label}] pattern '{pattern}' should report {expectedCount} falling-edge transition(s)");
		}

		#endregion

		#region Pattern catalogue

		// Each test runs the analyzer against a 9-value sequence (single digits, range 1-9) twice:
		// once with integer values, once with decimal values. The expected string visualises the
		// flagged record(s) using the '^' character — making test intent obvious at a glance.

		[TestMethod]
		[DataRow("555555555", "555555555")]
		public void Analyze_Plateau_FlagsNothing(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123456789", "123456789")]
		public void Analyze_RisingValues_FlagsNothing(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987654321", "^87654321")]
		public void Analyze_FallingValues_FlagsTheFirstRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123454321", "1234^4321")]
		public void Analyze_SharpPyramid_FlagsThePeak(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123444321", "12344^321")]
		public void Analyze_PlateauPyramid_FlagsTheLastPeakValue(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987656789", "^87656789")]
		public void Analyze_SharpValley_FlagsTheFirstRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987666789", "^87666789")]
		public void Analyze_PlateauValley_FlagsTheFirstRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123123123", "12^12^123")]
		public void Analyze_Sawtooth_FlagsEachPeak(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("321321321", "^21^21^21")]
		public void Analyze_InvertedSawtooth_FlagsEachPeak(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("555555554", "5555555^4")]
		public void Analyze_LateFallingEdge_FlagsTheLastPlateauValue(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		#endregion

		#region Real-world noise (decimal only)

		[TestMethod]
		public void Analyze_NoisyDecimalPlateau_FlagsEachSubUnitDip()
		{
			// Mirrors a real UaAngle log: a "flat" plateau actually contains sub-unit dips that
			// the analyzer reports as separate falling edges (no minimum-delta threshold).
			// Sequence: 33.000 33.000 32.999 33.000 32.998 32.998 33.000 32.997 32.997
			//                  ^           ^                   ^
			// Under the "flag the record before the fall" convention, indices 1, 3, and 6 are flagged.
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

			AnalysisHelper.GetFlaggedIndices(records).Should().Equal(1, 3, 6);
			results.FlaggedRecords.Should().Be(3);
		}

		#endregion
	}
}
