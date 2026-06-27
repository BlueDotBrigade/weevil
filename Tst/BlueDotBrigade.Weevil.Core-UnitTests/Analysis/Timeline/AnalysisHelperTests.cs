namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
    using System;
    using System.Collections.Immutable;

    [TestClass]
    public class AnalysisHelperTests
    {
        [TestMethod]
        public void GivenRecordsWithFlags_WhenGetFlaggedIndices_ThenReturnsFlaggedIndexes()
        {
            var records = AnalysisHelper.BuildIntegerRecords("12345");
            records[1].Metadata.IsFlagged = true;
            records[3].Metadata.IsFlagged = true;

            var result = AnalysisHelper.GetFlaggedIndices(records);

            result.Should().Equal(1, 3);
        }

        [TestMethod]
        public void GivenExpectedPatternWithMarkers_WhenCountExpectedFlags_ThenReturnsMarkerCount()
        {
            var expected = "1^34^67^^";

            var result = AnalysisHelper.CountExpectedFlags(expected);

            result.Should().Be(4);
        }

        [TestMethod]
        public void GivenDigitPattern_WhenBuildIntegerRecords_ThenCreatesTimestampedIntegerContent()
        {
            var records = AnalysisHelper.BuildIntegerRecords("123");

            records.Should().HaveCount(3);
            records[0].Content.Should().Be("15:02:00.000 1");
            records[1].Content.Should().Be("15:02:01.000 2");
            records[2].Content.Should().Be("15:02:02.000 3");
        }

        [TestMethod]
        public void GivenDigitPattern_WhenBuildDecimalRecords_ThenCreatesTimestampedDecimalContent()
        {
            var records = AnalysisHelper.BuildDecimalRecords("123");

            records.Should().HaveCount(3);
            records[0].Content.Should().Be("15:02:00.000 1.0");
            records[1].Content.Should().Be("15:02:01.000 2.0");
            records[2].Content.Should().Be("15:02:02.000 3.0");
        }

        [TestMethod]
        public void GivenMatchingFlags_WhenAssertFlagsMatchExpected_ThenDoesNotThrow()
        {
            var pattern = "12345";
            var expected = "1^3^5";
            var records = AnalysisHelper.BuildIntegerRecords(pattern);
            records[1].Metadata.IsFlagged = true;
            records[3].Metadata.IsFlagged = true;

            Action act = () => AnalysisHelper.AssertFlagsMatchExpected(
                pattern,
                expected,
                records,
                "unit test",
                "custom mismatch");

            act.Should().NotThrow();
        }

        [TestMethod]
        public void GivenMismatchedFlags_WhenAssertFlagsMatchExpected_ThenThrowsWithDiagnostic()
        {
            var pattern = "12345";
            var expected = "1^345";
            var records = AnalysisHelper.BuildIntegerRecords(pattern);
            records[3].Metadata.IsFlagged = true;
            records[3].Metadata.Comment = "unexpected flag";

            Action act = () => AnalysisHelper.AssertFlagsMatchExpected(
                pattern,
                expected,
                records,
                "unit test",
                "custom mismatch");

            var exception = act.Should().Throw<Exception>().Which;
            exception.Message.Should().Contain("custom mismatch");
            exception.Message.Should().Contain("Expected: 1^345");
            exception.Message.Should().Contain("Actual  : 123^5");
            exception.Message.Should().Contain("unexpected flag");
        }

        [TestMethod]
        public void GivenLengthMismatch_WhenAssertFlagsMatchExpected_ThenThrowsLengthDiagnostic()
        {
            var pattern = "12345";
            var expected = "1^34";
            var records = AnalysisHelper.BuildIntegerRecords(pattern);

            Action act = () => AnalysisHelper.AssertFlagsMatchExpected(
                pattern,
                expected,
                records,
                "unit test",
                "custom mismatch");

            var exception = act.Should().Throw<Exception>().Which;
            exception.Message.Should().Contain("must be the same length");
        }
    }
}
