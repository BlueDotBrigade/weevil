namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
    using System.Collections.Concurrent;
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

        [TestMethod]
        // Regression: Issue #???
        public void GivenFlatlineThenFallingEdgeWithDecimalValues_WhenAnalyzeRuns_ThenFirstLowerValueRecordIsFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:32.8257 64.1")
                .WithContent("15:02:33.3308 64.1")
                .WithContent("15:02:33.8373 64.1")
                .WithContent("15:02:34.3489 32.1")
                .WithContent("15:02:34.8520 16.1")
                .WithContent("15:02:35.3481 8.1")
                .WithContent("15:02:35.8456 4.1")
                .GetRecords();

            var analyzer = new DetectFallingEdgeAnalyzer(CreateFilterStrategy());
            var userDialog = GetDialog(@"(?<Value>\d+\.\d+)$");

            Results results = analyzer.Analyze(
                records,
                string.Empty,
                userDialog,
                canUpdateMetadata: true);

            records[3].Metadata.IsFlagged.Should().BeTrue();
            records[3].Metadata.Comment.Should().Contain("64.1 => 32.1");
            results.FlaggedRecords.Should().BeGreaterOrEqualTo(1);
        }

        [TestMethod]
        public void GivenFallingValuesWithIntegers_WhenAnalyzeRuns_ThenFirstLowerValueRecordIsFlagged()
        {
            var records = R.Create()
                .WithContent("15:02:32.8257 64")
                .WithContent("15:02:33.3308 64")
                .WithContent("15:02:33.8373 32")
                .WithContent("15:02:34.3489 16")
                .WithContent("15:02:34.8520 16")
                .GetRecords();

            var analyzer = new DetectFallingEdgeAnalyzer(CreateFilterStrategy());
            var userDialog = GetDialog(@"(?<Value>\d+)$");

            Results results = analyzer.Analyze(
                records,
                string.Empty,
                userDialog,
                canUpdateMetadata: true);

            records[2].Metadata.IsFlagged.Should().BeTrue();
            records[2].Metadata.Comment.Should().Contain("64 => 32");
            results.FlaggedRecords.Should().BeGreaterOrEqualTo(1);
        }
    }
}
