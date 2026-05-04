namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class StableValueAnalyzerTests
	{
		private const string IntegerRegex = @"(?<Value>\d+)$";
		private const string DecimalRegex = @"(?<Value>\d+\.\d+)$";
		private const char FlagMarker = '^';

		#region Setup helpers

		private static Results Analyze(ImmutableArray<IRecord> records, string regex)
		{
			var analyzer = new StableValueAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(regex);

			return analyzer.Analyze(
				records,
				string.Empty,
				userDialog,
				canUpdateMetadata: true);
		}

		#endregion

		#region Scenario helpers

		// StableValueAnalyzer flags the start AND end record of every consecutive same-value run.
		// Single-value runs (length 1) flag the same record twice (idempotent — one flag, one record).

		private static void AssertScenario(string pattern, string expected)
		{
			AssertIntegerScenario(pattern, expected);
			AssertDecimalScenario(pattern, expected);
		}

		private static void AssertIntegerScenario(string pattern, string expected)
		{
			var records = BuildIntegerRecords(pattern);
			var results = Analyze(records, IntegerRegex);

			AssertFlagsMatchExpected(pattern, expected, records, results, label: "integer scenario");
		}

		private static void AssertDecimalScenario(string pattern, string expected)
		{
			var records = BuildDecimalRecords(pattern);
			var results = Analyze(records, DecimalRegex);

			AssertFlagsMatchExpected(pattern, expected, records, results, label: "decimal scenario");
		}

		private static void AssertFlagsMatchExpected(
			string pattern,
			string expected,
			ImmutableArray<IRecord> records,
			Results results,
			string label)
		{
			pattern.Length.Should().Be(expected.Length,
				because: $"[{label}] pattern '{pattern}' and expected '{expected}' must be the same length");

			var expectedIndices = ParseFlaggedIndices(expected);
			var actualIndices = GetFlaggedIndices(records);
			var actualRendered = RenderFlags(pattern, actualIndices);

			actualRendered.Should().Be(expected,
				because: BuildDiagnostic(label, pattern, expected, actualRendered, records, expectedIndices, actualIndices));

			// Note: results.FlaggedRecords counts run-boundary EVENTS (one for start, one for finalize),
			// which is roughly twice the number of distinct flagged records. We only assert flag positions,
			// not the event count, since the event count is an internal accounting detail.
		}

		private static int[] ParseFlaggedIndices(string expected) =>
			Enumerable.Range(0, expected.Length)
				.Where(i => expected[i] == FlagMarker)
				.ToArray();

		private static int[] GetFlaggedIndices(ImmutableArray<IRecord> records) =>
			Enumerable.Range(0, records.Length)
				.Where(i => records[i].Metadata.IsFlagged)
				.ToArray();

		private static string RenderFlags(string pattern, int[] flaggedIndices)
		{
			var chars = pattern.ToCharArray();
			foreach (var i in flaggedIndices)
			{
				if (i >= 0 && i < chars.Length)
				{
					chars[i] = FlagMarker;
				}
			}
			return new string(chars);
		}

		private static string BuildDiagnostic(
			string label,
			string pattern,
			string expected,
			string actualRendered,
			ImmutableArray<IRecord> records,
			int[] expectedIndices,
			int[] actualIndices)
		{
			var disagreements = expectedIndices
				.Union(actualIndices)
				.OrderBy(i => i)
				.Where(i => System.Array.IndexOf(expectedIndices, i) < 0
				         || System.Array.IndexOf(actualIndices, i) < 0)
				.ToArray();

			var details = new List<string>
			{
				$"[{label}] stable-value flag mismatch.",
				$"  Pattern : {pattern}",
				$"  Expected: {expected}",
				$"  Actual  : {actualRendered}",
				$"  Expected indices: [{string.Join(", ", expectedIndices)}]",
				$"  Actual indices  : [{string.Join(", ", actualIndices)}]",
			};

			if (disagreements.Length > 0)
			{
				details.Add("  Disagreements:");
				foreach (var i in disagreements)
				{
					var comment = records[i].Metadata.Comment ?? string.Empty;
					details.Add($"    index {i} (value '{pattern[i]}'): comment=\"{comment}\"");
				}
			}

			return string.Join(System.Environment.NewLine, details);
		}

		private static ImmutableArray<IRecord> BuildIntegerRecords(string pattern)
		{
			var builder = R.Create();
			for (var i = 0; i < pattern.Length; i++)
			{
				builder = builder.WithContent($"15:02:{i:D2}.000 {pattern[i]}");
			}
			return builder.GetRecords();
		}

		private static ImmutableArray<IRecord> BuildDecimalRecords(string pattern)
		{
			var builder = R.Create();
			for (var i = 0; i < pattern.Length; i++)
			{
				builder = builder.WithContent($"15:02:{i:D2}.000 {pattern[i]}.0");
			}
			return builder.GetRecords();
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

			Analyze(records, DecimalRegex);

			GetFlaggedIndices(records).Should().Equal(0, 1, 2, 3, 4, 5, 6, 7, 8);
		}

		#endregion
	}
}
