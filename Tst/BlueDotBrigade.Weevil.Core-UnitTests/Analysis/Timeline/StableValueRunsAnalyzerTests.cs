namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class StableValueRunsAnalyzerTests
	{
		#region Setup helpers

		private static Results Analyze(ImmutableArray<IRecord> records, string regex)
		{
			var analyzer = new StableValueRunsAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(regex);

			return analyzer.Analyze(
				records,
				string.Empty,
				userDialog,
				canUpdateMetadata: true);
		}

		#endregion

		#region Scenario helpers

		// StableValueRunsAnalyzer flags the start AND end record of every consecutive same-value run.
		// Single-value runs (length 1) flag the same record twice (idempotent — one flag, one record).

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
				"stable-value flag mismatch");

			// Note: results.FlaggedRecords counts run-boundary EVENTS (one for start, one for finalize),
			// which is roughly twice the number of distinct flagged records. We only assert flag positions,
			// not the event count, since the event count is an internal accounting detail.
		}


		#endregion

		#region Pattern catalogue

		[TestMethod]
		[DataRow("555555555", "^5555555^")]
		public void Analyze_Plateau_FlagsTheRunBoundaries(string pattern, string expected)
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
		[DataRow("123454321", "^^^^^^^^^")]
		public void Analyze_SharpPyramid_FlagsEveryRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123444321", "^^^^4^^^^")]
		public void Analyze_PlateauPyramid_DoesNotFlagInsideThePlateau(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987656789", "^^^^^^^^^")]
		public void Analyze_SharpValley_FlagsEveryRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987666789", "^^^^6^^^^")]
		public void Analyze_PlateauValley_DoesNotFlagInsideThePlateau(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123123123", "^^^^^^^^^")]
		public void Analyze_Sawtooth_FlagsEveryRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("321321321", "^^^^^^^^^")]
		public void Analyze_InvertedSawtooth_FlagsEveryRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("555555554", "^555555^^")]
		public void Analyze_LateFallingEdge_FlagsThePlateauBoundariesAndChange(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		#endregion

		#region Real-world noise (decimal only)

		[TestMethod]
		public void Analyze_NoisyDecimalPlateau_FlagsEveryNoiseBoundary()
		{
			// Same noisy sequence as the other analyzers. Every sub-unit dip ends a stable run, so
			// every record is at a run boundary — every record gets flagged.
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

         Analyze(records, AnalysisHelper.DecimalRegex);

           AnalysisHelper.GetFlaggedIndices(records).Should().Equal(0, 1, 2, 3, 4, 5, 6, 7, 8);
		}

		#endregion
	}
}
