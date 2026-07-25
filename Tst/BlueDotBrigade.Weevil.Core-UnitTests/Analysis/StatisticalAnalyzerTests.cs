namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;
	using System.Globalization;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Math;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class StatisticalAnalyzerTests
	{
		[TestMethod]
		public void GivenRecordsWithNoNumericMatches_WhenAnalyzeRuns_ThenResultContainsNullableMetrics()
		{
			// Regression: Issue #931
			var records = R.Create()
				.WithContent("NoNumbers")
				.WithContent("AlsoNoNumbers")
				.GetRecords();

			var analyzer = new StatisticalAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+)");

			Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: false);

			results.FlaggedRecords.Should().Be(0);
			results.Data.Should().ContainKey("Min");
			results.Data["Min"].Should().BeNull();
			results.Data.Should().ContainKey("Max");
			results.Data["Max"].Should().BeNull();
		}

		[TestMethod]
		public void GivenIntegerAndDecimalValues_WhenAnalyzeRuns_ThenStatisticsAreCalculated()
		{
			var records = R.Create()
				.WithContent("Value=0")
				.WithContent("Value=2")
				.WithContent("Value=4.5")
				.GetRecords();

			var analyzer = new StatisticalAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+(?:\.\d+)?)");

			Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(3);
			results.Data.Should().ContainKey("Mean");
			results.Data["Mean"].Should().Be(2.167d);
			results.Data["Count"].Should().Be(3d);
		}

		[DataTestMethod]
		[DataRow("de-DE")]
		[DataRow("fr-FR")]
		public void GivenInvariantDecimalValueAndNonEnglishCulture_WhenAnalyzeRuns_ThenStatisticsAreCalculated(string cultureName)
		{
			// Regression: Issue #928
			var originalCulture = CultureInfo.CurrentCulture;
			var originalUiCulture = CultureInfo.CurrentUICulture;

			try
			{
				CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
				CultureInfo.CurrentCulture = culture;
				CultureInfo.CurrentUICulture = culture;

				var records = R.Create()
					.WithContent("Value=2.5")
					.GetRecords();

				var analyzer = new StatisticalAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
				var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+(?:\.\d+)?)");

				Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

				results.FlaggedRecords.Should().Be(1);
				results.Data["Count"].Should().Be(1d);
				results.Data["Mean"].Should().Be(2.5d);
			}
			finally
			{
				CultureInfo.CurrentCulture = originalCulture;
				CultureInfo.CurrentUICulture = originalUiCulture;
			}
		}

		[TestMethod]
		public void GivenRecordsWithoutCreationTime_WhenAnalyzeRuns_ThenRangeHasNoTimestamps()
		{
			// Regression: Issue #929
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, Record.CreationTimeUnknown, SeverityType.Information, "Value=10", new Metadata()),
				new Record(2, Record.CreationTimeUnknown, SeverityType.Information, "Value=20", new Metadata()));

			var analyzer = new StatisticalAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+)");

			Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: false);

			var range = (RangeResult)results.Data["Range"];
			range.StartAt.Should().BeNull();
			range.EndAt.Should().BeNull();
		}

		[TestMethod]
		public void GivenMixedTimestampedAndUntimestampedRecords_WhenAnalyzeRuns_ThenRangeOnlyIncludesValidTimestamps()
		{
			// Regression: Issue #929
			var knownTime1 = new DateTime(2024, 1, 1, 10, 0, 0);
			var knownTime2 = new DateTime(2024, 1, 1, 10, 0, 5);

			var records = ImmutableArray.Create<IRecord>(
				new Record(1, knownTime1, SeverityType.Information, "Value=1", new Metadata()),
				new Record(2, Record.CreationTimeUnknown, SeverityType.Information, "Value=2", new Metadata()),
				new Record(3, knownTime2, SeverityType.Information, "Value=3", new Metadata()));

			var analyzer = new StatisticalAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+)");

			Results results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: false);

			var range = (RangeResult)results.Data["Range"];
			range.StartAt.Should().Be(knownTime1);
			range.EndAt.Should().Be(knownTime2);
		}
	}
}
