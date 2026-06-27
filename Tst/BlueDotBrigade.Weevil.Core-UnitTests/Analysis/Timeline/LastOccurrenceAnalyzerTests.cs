namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class LastOccurrenceAnalyzerTests
	{
		#region Setup helpers

		private static Results Analyze(ImmutableArray<IRecord> records, string regex)
		{
			var analyzer = new LastOccurrenceAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(regex);

			return analyzer.Analyze(
				records,
				string.Empty,
				userDialog,
				canUpdateMetadata: true);
		}

		#endregion

		#region Scenario helpers

		// LastOccurrenceAnalyzer flags the last record where each unique value appears.
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
				"last-occurrence flag mismatch");

			var expectedCount = AnalysisHelper.CountExpectedFlags(expected);
			results.FlaggedRecords.Should().Be(expectedCount,
				because: $"[{label}] pattern '{pattern}' should report {expectedCount} unique-value last occurrence(s)");
		}

		#endregion

		#region Pattern catalogue

		[TestMethod]
		[DataRow("555555555", "55555555^")]
		public void Analyze_Plateau_FlagsTheLastRecord(string pattern, string expected)
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
		[DataRow("123454321", "1234^^^^^")]
		public void Analyze_SharpPyramid_FlagsFromThePeakBack(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123444321", "12344^^^^")]
		public void Analyze_PlateauPyramid_FlagsFromThePlateauEnd(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987656789", "9876^^^^^")]
		public void Analyze_SharpValley_FlagsFromTheValleyBack(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("987666789", "98766^^^^")]
		public void Analyze_PlateauValley_FlagsFromTheValleyEnd(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("123123123", "123123^^^")]
		public void Analyze_Sawtooth_FlagsTheLastCycle(string pattern, string expected)
		{
			// Regression: Issue #529
			AssertScenario(pattern, expected);
		}

		[TestMethod]
		[DataRow("321321321", "321321^^^")]
		public void Analyze_InvertedSawtooth_FlagsTheLastCycle(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		[TestMethod]
		[DataRow("555555554", "5555555^^")]
		public void Analyze_LateFallingEdge_FlagsBothUniqueValues(string pattern, string expected)
			=> AssertScenario(pattern, expected);

		#endregion

		#region Real-world noise (decimal only)

		[TestMethod]
		public void Analyze_NoisyDecimalPlateau_FlagsEachUniqueValueAtItsLastOccurrence()
		{
			// Same noisy sequence as the other analyzers. LastOccurrenceAnalyzer flags the last
			// occurrence of each distinct decimal value: 32.999 (idx 2), 32.998 (idx 5),
			// 33.000 (idx 6), 32.997 (idx 8).
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

			AnalysisHelper.GetFlaggedIndices(records).Should().Equal(2, 5, 6, 8);
			results.FlaggedRecords.Should().Be(4);
		}

		#endregion
	}
}
