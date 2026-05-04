namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class DataTransitionAnalyzerTests
	{
		private const string IntegerRegex = @"(?<Value>\d+)$";
		private const string DecimalRegex = @"(?<Value>\d+\.\d+)$";
		private const char FlagMarker = '^';

		#region Setup helpers

		private static Results Analyze(ImmutableArray<IRecord> records, string regex)
		{
			var analyzer = new DataTransitionAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(regex);

			return analyzer.Analyze(
				records,
				string.Empty,
				userDialog,
				canUpdateMetadata: true);
		}

		#endregion

		#region Scenario helpers

		// DataTransitionAnalyzer flags the record where each value first appears
		// or where the value differs from its previous value (per regex group key).

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

			results.FlaggedRecords.Should().Be(expectedIndices.Length,
				because: $"[{label}] pattern '{pattern}' should report {expectedIndices.Length} value transition(s)");
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
				$"[{label}] data-transition flag mismatch.",
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
			// Same noisy sequence as the other analyzers. DataTransitionAnalyzer flags every
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

			var results = Analyze(records, DecimalRegex);

			GetFlaggedIndices(records).Should().Equal(0, 2, 3, 4, 6, 7);
			results.FlaggedRecords.Should().Be(6);
		}

		#endregion
	}
}
