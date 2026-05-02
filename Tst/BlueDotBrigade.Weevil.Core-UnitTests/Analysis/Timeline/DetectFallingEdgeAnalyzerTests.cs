namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Collections.Concurrent;
    using BlueDotBrigade.Weevil.Data;
    using BlueDotBrigade.Weevil.Filter;
    using BlueDotBrigade.Weevil.IO;
    using BlueDotBrigade.Weevil.TestTools.Data;
    using NSubstitute;

    [TestClass]
    public class DetectFallingEdgeAnalyzerTests
    {
        private static FilterStrategy CreateFilterStrategy()
        {
            var coreExtension = Substitute.For<ICoreExtension>();
            var context = new ContextDictionary();
            var filterAliasExpander = Substitute.For<IFilterAliasExpander>();
            filterAliasExpander.Expand(Arg.Any<string>()).Returns(x => x.Arg<string>());

            var filterCriteria = new FilterCriteria(string.Empty, string.Empty, new ConcurrentDictionary<string, object>());
            var regionManager = Substitute.For<IRegionManager>();
            var bookmarkManager = Substitute.For<IBookmarkManager>();

            return new FilterStrategy(
                coreExtension,
                context,
                filterAliasExpander,
                FilterType.RegularExpression,
                filterCriteria,
                regionManager,
                bookmarkManager);
        }

        private static IUserDialog GetDialog(string regex)
        {
            var userDialog = Substitute.For<IUserDialog>();

            userDialog
                .ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns("Ascending");

            userDialog
                .TryGetExpressions(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
                .Returns(callInfo =>
                {
                    callInfo[2] = regex;
                    return true;
                });

            return userDialog;
        }

        private static Results Analyze(ImmutableArray<IRecord> records, string regex)
        {
            var analyzer = new DetectFallingEdgeAnalyzer(CreateFilterStrategy());
            var userDialog = GetDialog(regex);

            return analyzer.Analyze(
                records,
                string.Empty,
                userDialog,
                canUpdateMetadata: true);
        }

        private static void AssertOnlyFlaggedIndices(ImmutableArray<IRecord> records, params int[] flaggedIndices)
        {
            var expectedFlags = new HashSet<int>(flaggedIndices);

            for (var i = 0; i < records.Length; i++)
            {
                records[i].Metadata.IsFlagged.Should().Be(expectedFlags.Contains(i));
            }
        }

        [TestMethod]
        public void GivenFlatIntegers_WhenAnalyzed_ThenNoRecordsAreFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:00.000 10")
                .WithContent("15:02:01.000 10")
                .WithContent("15:02:02.000 10")
                .WithContent("15:02:03.000 10")
                .GetRecords();

            Results results = Analyze(records, @"(?<Value>\d+)$");

            AssertOnlyFlaggedIndices(records);
            results.FlaggedRecords.Should().Be(0);
        }

        [TestMethod]
        public void GivenFlatDecimals_WhenAnalyzed_ThenNoRecordsAreFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:00.000 10.0")
                .WithContent("15:02:01.000 10.0")
                .WithContent("15:02:02.000 10.0")
                .WithContent("15:02:03.000 10.0")
                .GetRecords();

            Results results = Analyze(records, @"(?<Value>\d+\.\d+)$");

            AssertOnlyFlaggedIndices(records);
            results.FlaggedRecords.Should().Be(0);
        }

        [TestMethod]
        public void GivenIncreasingIntegers_WhenAnalyzed_ThenNoRecordsAreFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:00.000 1")
                .WithContent("15:02:01.000 2")
                .WithContent("15:02:02.000 3")
                .WithContent("15:02:03.000 4")
                .WithContent("15:02:04.000 4")
                .GetRecords();

            Results results = Analyze(records, @"(?<Value>\d+)$");

            AssertOnlyFlaggedIndices(records);
            results.FlaggedRecords.Should().Be(0);
        }

        [TestMethod]
        public void GivenIncreasingDecimals_WhenAnalyzed_ThenNoRecordsAreFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:00.000 1.1")
                .WithContent("15:02:01.000 2.1")
                .WithContent("15:02:02.000 3.1")
                .WithContent("15:02:03.000 4.1")
                .WithContent("15:02:04.000 4.1")
                .GetRecords();

            Results results = Analyze(records, @"(?<Value>\d+\.\d+)$");

            AssertOnlyFlaggedIndices(records);
            results.FlaggedRecords.Should().Be(0);
        }

        [TestMethod]
        public void GivenDecreasingIntegers_WhenAnalyzed_ThenFirstFallingTransitionIsFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:00.000 5")
                .WithContent("15:02:01.000 4")
                .WithContent("15:02:02.000 3")
                .WithContent("15:02:03.000 2")
                .WithContent("15:02:04.000 1")
                .GetRecords();

            Results results = Analyze(records, @"(?<Value>\d+)$");

            AssertOnlyFlaggedIndices(records, 1);
            records[1].Metadata.Comment.Should().Contain("5 => 4");
            results.FlaggedRecords.Should().Be(1);
        }

        [TestMethod]
        public void GivenDecreasingDecimals_WhenAnalyzed_ThenFirstFallingTransitionIsFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:00.000 5.5")
                .WithContent("15:02:01.000 4.5")
                .WithContent("15:02:02.000 3.5")
                .WithContent("15:02:03.000 2.5")
                .WithContent("15:02:04.000 1.5")
                .GetRecords();

            Results results = Analyze(records, @"(?<Value>\d+\.\d+)$");

            AssertOnlyFlaggedIndices(records, 1);
            records[1].Metadata.Comment.Should().Contain("5.5 => 4.5");
            results.FlaggedRecords.Should().Be(1);
        }

        [TestMethod]
        public void GivenPyramidIntegers_WhenAnalyzed_ThenOnlyMiddleFallingTransitionIsFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:00.000 1")
                .WithContent("15:02:01.000 2")
                .WithContent("15:02:02.000 3")
                .WithContent("15:02:03.000 2")
                .WithContent("15:02:04.000 1")
                .WithContent("15:02:05.000 2")
                .WithContent("15:02:06.000 3")
                .GetRecords();

            Results results = Analyze(records, @"(?<Value>\d+)$");

            AssertOnlyFlaggedIndices(records, 3);
            results.FlaggedRecords.Should().Be(1);
        }

        [TestMethod]
        public void GivenPyramidDecimals_WhenAnalyzed_ThenOnlyMiddleFallingTransitionIsFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:00.000 1.1")
                .WithContent("15:02:01.000 2.1")
                .WithContent("15:02:02.000 3.1")
                .WithContent("15:02:03.000 2.1")
                .WithContent("15:02:04.000 1.1")
                .WithContent("15:02:05.000 2.1")
                .WithContent("15:02:06.000 3.1")
                .GetRecords();

            Results results = Analyze(records, @"(?<Value>\d+\.\d+)$");

            AssertOnlyFlaggedIndices(records, 3);
            results.FlaggedRecords.Should().Be(1);
        }

        [TestMethod]
        public void GivenInversePyramidIntegers_WhenAnalyzed_ThenEachFallingRunStartIsFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:00.000 3")
                .WithContent("15:02:01.000 2")
                .WithContent("15:02:02.000 1")
                .WithContent("15:02:03.000 2")
                .WithContent("15:02:04.000 3")
                .WithContent("15:02:05.000 2")
                .WithContent("15:02:06.000 1")
                .GetRecords();

            Results results = Analyze(records, @"(?<Value>\d+)$");

            AssertOnlyFlaggedIndices(records, 1, 5);
            results.FlaggedRecords.Should().Be(2);
        }

        [TestMethod]
        public void GivenInversePyramidDecimals_WhenAnalyzed_ThenEachFallingRunStartIsFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:00.000 3.3")
                .WithContent("15:02:01.000 2.2")
                .WithContent("15:02:02.000 1.1")
                .WithContent("15:02:03.000 2.2")
                .WithContent("15:02:04.000 3.3")
                .WithContent("15:02:05.000 2.2")
                .WithContent("15:02:06.000 1.1")
                .GetRecords();

            Results results = Analyze(records, @"(?<Value>\d+\.\d+)$");

            AssertOnlyFlaggedIndices(records, 1, 5);
            results.FlaggedRecords.Should().Be(2);
        }

        [TestMethod]
        public void GivenLateFallingEdgeInIntegers_WhenAnalyzed_ThenOnlyLastRecordIsFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:00.000 2")
                .WithContent("15:02:01.000 2")
                .WithContent("15:02:02.000 2")
                .WithContent("15:02:03.000 1")
                .GetRecords();

            Results results = Analyze(records, @"(?<Value>\d+)$");

            AssertOnlyFlaggedIndices(records, 3);
            records[3].Metadata.Comment.Should().Contain("2 => 1");
            results.FlaggedRecords.Should().Be(1);
        }

        [TestMethod]
        public void GivenLateFallingEdgeInDecimals_WhenAnalyzed_ThenOnlyLastRecordIsFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:00.000 2.2")
                .WithContent("15:02:01.000 2.2")
                .WithContent("15:02:02.000 2.2")
                .WithContent("15:02:03.000 1.2")
                .GetRecords();

            Results results = Analyze(records, @"(?<Value>\d+\.\d+)$");

            AssertOnlyFlaggedIndices(records, 3);
            records[3].Metadata.Comment.Should().Contain("2.2 => 1.2");
            results.FlaggedRecords.Should().Be(1);
        }
    }
}