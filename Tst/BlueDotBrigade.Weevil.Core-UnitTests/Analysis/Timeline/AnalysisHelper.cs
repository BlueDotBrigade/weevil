namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using BlueDotBrigade.Weevil.Data;
    using BlueDotBrigade.Weevil.TestTools.Data;

    internal static class AnalysisHelper
    {
        internal const string IntegerRegex = @"(?<Value>\d+)$";
        internal const string DecimalRegex = @"(?<Value>\d+\.\d+)$";
        internal const char FlagMarker = '^';

        internal static int[] GetFlaggedIndices(ImmutableArray<IRecord> records) =>
            Enumerable.Range(0, records.Length)
                .Where(i => records[i].Metadata.IsFlagged)
                .ToArray();

        internal static int CountExpectedFlags(string expected) =>
            Enumerable.Count(expected, c => c == FlagMarker);

        internal static ImmutableArray<IRecord> BuildIntegerRecords(string pattern)
        {
            var builder = R.Create();
            for (var i = 0; i < pattern.Length; i++)
            {
                builder = builder.WithContent($"15:02:{i:D2}.000 {pattern[i]}");
            }

            return builder.GetRecords();
        }

        internal static ImmutableArray<IRecord> BuildDecimalRecords(string pattern)
        {
            var builder = R.Create();
            for (var i = 0; i < pattern.Length; i++)
            {
                builder = builder.WithContent($"15:02:{i:D2}.000 {pattern[i]}.0");
            }

            return builder.GetRecords();
        }

        internal static void AssertFlagsMatchExpected(
            string pattern,
            string expected,
            ImmutableArray<IRecord> records,
            string label,
            string mismatchLabel)
        {
            pattern.Length.Should().Be(expected.Length,
                because: $"[{label}] pattern '{pattern}' and expected '{expected}' must be the same length");

            var expectedIndices = ParseFlaggedIndices(expected);
            var actualIndices = GetFlaggedIndices(records);
            var actualRendered = RenderFlags(pattern, actualIndices);

            actualRendered.Should().Be(expected,
                because: BuildDiagnostic(mismatchLabel, label, pattern, expected, actualRendered, records, expectedIndices, actualIndices));
        }

        private static int[] ParseFlaggedIndices(string expected) =>
            Enumerable.Range(0, expected.Length)
                .Where(i => expected[i] == FlagMarker)
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
            string mismatchLabel,
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
                $"[{label}] {mismatchLabel}.",
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
    }
}
