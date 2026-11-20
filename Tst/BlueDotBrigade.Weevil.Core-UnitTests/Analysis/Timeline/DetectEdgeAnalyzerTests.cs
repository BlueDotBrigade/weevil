namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class DetectEdgeAnalyzerTests
	{
		private const AnalysisType Rising = AnalysisType.DetectRisingEdges;
		private const AnalysisType Falling = AnalysisType.DetectFallingEdges;

		#region Setup helpers

		private static Results Analyze(AnalysisType analysisType, RecordList records, IRecordState state, string regex, string analysisOrder = "Ascending")
		{
			var analyzer = new DetectEdgeAnalyzer(analysisType, RecordAnalyzerTestContext.CreateFilterStrategy());
			IUserDialog userDialog = RecordAnalyzerTestContext.CreateDialog(regex, analysisOrder);

			return analyzer.Analyze(
				records,
				state,
				string.Empty,
				userDialog,
				canUpdateMetadata: true);
		}

		private static RecordList BuildRecords(params (string Timestamp, int Value)[] entries)
		{
			var records = new (int LineNumber, string Timestamp, string Content)[entries.Length];

			for (var i = 0; i < entries.Length; i++)
			{
				records[i] = (i + 1, entries[i].Timestamp, $"Value={entries[i].Value}");
			}

			return AnalysisHelper.BuildRecords(records);
		}

		#endregion

		#region Scenario helpers

		// Each scenario is expressed as two parallel strings:
		//   pattern  : nine single-digit values, e.g. "987656789"
		//   expected : same length as pattern, with '^' replacing any digit whose record should be flagged,
		//              e.g. "9876^6789" means "flag the record at index 4 (the valley)"
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
				because: $"[{label}] pattern '{pattern}' should report {expectedCount} {analysisType} transition(s)");
		}

		#endregion

		#region Pattern catalogue

		// Each row runs the analyzer against a 9-value sequence (single digits, range 1-9) twice:
		// once with integer values, once with decimal values. The expected string visualises the
		// flagged record(s) using the '^' character: the record before each run starts,
		// the valley for a rising edge and the peak for a falling edge.

		[TestMethod]
		[DataRow(Rising, "555555555", "555555555")] // Plateau: nothing
		[DataRow(Falling, "555555555", "555555555")]
		[DataRow(Rising, "123456789", "^23456789")] // Rising values: the first record, or nothing
		[DataRow(Falling, "123456789", "123456789")]
		[DataRow(Rising, "987654321", "987654321")] // Falling values: nothing, or the first record
		[DataRow(Falling, "987654321", "^87654321")]
		[DataRow(Rising, "123454321", "^23454321")] // Sharp pyramid: the first record, or the peak
		[DataRow(Falling, "123454321", "1234^4321")]
		[DataRow(Rising, "123444321", "^23444321")] // Plateau pyramid: the first record, or the last peak value
		[DataRow(Falling, "123444321", "12344^321")]
		[DataRow(Rising, "987656789", "9876^6789")] // Sharp valley: the valley, or the first record
		[DataRow(Falling, "987656789", "^87656789")]
		[DataRow(Rising, "987666789", "98766^789")] // Plateau valley: the last valley value, or the first record
		[DataRow(Falling, "987666789", "^87666789")]
		[DataRow(Rising, "123123123", "^23^23^23")] // Sawtooth: each valley, or each peak
		[DataRow(Falling, "123123123", "12^12^123")]
		[DataRow(Rising, "321321321", "32^32^321")] // Inverted sawtooth: each valley, or each peak
		[DataRow(Falling, "321321321", "^21^21^21")]
		[DataRow(Rising, "555555554", "555555554")] // Late falling edge: nothing, or the last plateau value
		[DataRow(Falling, "555555554", "5555555^4")]
		public void GivenAPatternOfValues_WhenAnalyzed_ThenTheRecordBeforeEachEdgeIsFlagged(AnalysisType analysisType, string pattern, string expected)
			=> AssertScenario(analysisType, pattern, expected);

		#endregion

		#region Real-world noise (decimal only)

		[TestMethod]
		[DataRow(Rising, new[] { 2, 5 })]
		[DataRow(Falling, new[] { 1, 3, 6 })]
		public void GivenANoisyDecimalPlateau_WhenAnalyzed_ThenEachSubUnitChangeIsAnEdge(AnalysisType analysisType, int[] expectedIndices)
		{
			// Mirrors a real UaAngle log: a "flat" plateau actually contains sub-unit dips that
			// the analyzer reports as separate edges (no minimum-delta threshold).
			// Sequence: 33.000 33.000 32.999 33.000 32.998 32.998 33.000 32.997 32.997
			// Falling flags the record before each dip: indices 1, 3 and 6.
			// Rising flags the last record below 33.000 before each recovery: indices 2 and 5 (index 7's 32.997 never recovers).
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

		[TestMethod]
		[WorkItem(927)]
		[DataRow(Rising, 2, 9, 1, 0)]
		[DataRow(Falling, 1, 0, 2, 3)]
		public void GivenOutOfOrderTimestampsAndUnknownTimestamp_WhenAnalyzeAscending_ThenUsesChronologicalOrder(
			AnalysisType analysisType, int first, int second, int third, int fourth)
		{
			// Rising and falling edges followed the file's order instead of the timestamps' order.
			RecordList records = BuildRecords(
				("2024-01-01 00:00:02", first),
				(null, second),
				("2024-01-01 00:00:01", third),
				("2024-01-01 00:00:03", fourth));

			var state = new RecordState();
			Results results = Analyze(analysisType, records, state, @"(?<Value>\d+)$");

			AnalysisHelper.GetFlaggedIndices(records, state).Should().Equal(2);
			results.FlaggedRecords.Should().Be(1);
		}

		[TestMethod]
		[WorkItem(927)]
		[DataRow(Rising, 2, 1, 0)]
		[DataRow(Falling, 1, 2, 3)]
		public void GivenOutOfOrderTimestamps_WhenAnalyzeDescending_ThenUsesReverseChronologicalOrder(
			AnalysisType analysisType, int first, int second, int third)
		{
			// Rising and falling edges followed the file's order instead of the timestamps' order.
			RecordList records = BuildRecords(
				("2024-01-01 00:00:02", first),
				("2024-01-01 00:00:03", second),
				("2024-01-01 00:00:01", third));

			var state = new RecordState();
			Results results = Analyze(analysisType, records, state, @"(?<Value>\d+)$", "Descending");

			AnalysisHelper.GetFlaggedIndices(records, state).Should().Equal(1);
			results.FlaggedRecords.Should().Be(1);
		}

		[TestMethod]
		[WorkItem(927)]
		[DataRow(Rising, 1, 2, 0)]
		[DataRow(Falling, 2, 1, 3)]
		public void GivenMatchingTimestamps_WhenAnalyzeDescending_ThenUsesLineNumberAsStableTieBreaker(
			AnalysisType analysisType, int first, int second, int third)
		{
			// Rising and falling edges followed the file's order instead of the timestamps' order.
			RecordList records = BuildRecords(
				("2024-01-01 00:00:03", first),
				("2024-01-01 00:00:03", second),
				("2024-01-01 00:00:01", third));

			var state = new RecordState();
			Results results = Analyze(analysisType, records, state, @"(?<Value>\d+)$", "Descending");

			AnalysisHelper.GetFlaggedIndices(records, state).Should().Equal(0);
			results.FlaggedRecords.Should().Be(1);
		}

		#endregion
	}
}
