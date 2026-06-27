namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Immutable;
 using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class StateTransitionsAnalyzerTests
	{
		#region Setup helpers

		private static Results Analyze(ImmutableArray<IRecord> records, string regex)
		{
			var analyzer = new StateTransitionsAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(regex);

			return analyzer.Analyze(
				records,
				string.Empty,
				userDialog,
				canUpdateMetadata: true);
		}

		#endregion

		#region Scenario helpers

		// StateTransitionsAnalyzer flags the record where each value first appears
		// or where the value differs from its previous value (per regex group key).

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
				"data-transition flag mismatch");

			var expectedCount = AnalysisHelper.CountExpectedFlags(expected);
			results.FlaggedRecords.Should().Be(expectedCount,
				because: $"[{label}] pattern '{pattern}' should report {expectedCount} value transition(s)");
		}

		#endregion

		#region Pattern catalogue

		[TestMethod]
		[DataRow("555555555", "^55555555")]
		public void Analyze_Plateau_FlagsTheFirstRecord(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123456789", "^^^^^^^^^")]
		public void Analyze_RisingValues_FlagsEveryTransition(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987654321", "^^^^^^^^^")]
		public void Analyze_FallingValues_FlagsEveryTransition(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123454321", "^^^^^^^^^")]
		public void Analyze_SharpPyramid_FlagsEveryTransition(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123444321", "^^^^44^^^")]
		public void Analyze_PlateauPyramid_DoesNotFlagInsideThePlateau(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987656789", "^^^^^^^^^")]
		public void Analyze_SharpValley_FlagsEveryTransition(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987666789", "^^^^66^^^")]
		public void Analyze_PlateauValley_DoesNotFlagInsideThePlateau(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123123123", "^^^^^^^^^")]
		public void Analyze_Sawtooth_FlagsEveryTransition(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("321321321", "^^^^^^^^^")]
		public void Analyze_InvertedSawtooth_FlagsEveryTransition(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("555555554", "^5555555^")]
		public void Analyze_LateFallingEdge_FlagsThePlateauBoundary(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		#endregion

		#region Real-world noise (decimal only)

		[TestMethod]
		public void Analyze_NoisyDecimalPlateau_FlagsEveryDistinctValueChange()
		{
			// Same noisy sequence as the other analyzers. StateTransitionsAnalyzer flags every
			// record where the value differs from the previous record (plus the first record).
			// 33.000(0,first) 33.000(1) 32.999(2,change) 33.000(3,change) 32.998(4,change)
			// 32.998(5) 33.000(6,change) 32.997(7,change) 32.997(8)
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

            AnalysisHelper.GetFlaggedIndices(records).Should().Equal(0, 2, 3, 4, 6, 7);
			results.FlaggedRecords.Should().Be(6);
		}

		#endregion
	}
}
