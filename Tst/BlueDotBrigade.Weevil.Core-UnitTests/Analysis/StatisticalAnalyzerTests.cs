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
		[WorkItem(931)]
		public void GivenRecordsWithNoNumericMatches_WhenAnalyzeRuns_ThenResultContainsNullableMetrics()
		{
			// Statistics with no numeric matches threw instead of reporting empty metrics.
			var records = R.Create()
				.WithContent("NoNumbers")
				.WithContent("AlsoNoNumbers")
				.GetRecords();

			var analyzer = new StatisticalAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+)");

			Results results = analyzer.Analyze(records, new RecordState(), string.Empty, userDialog, canUpdateMetadata: false);

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

			var state = new RecordState();

			Results results = analyzer.Analyze(records, state, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(0);
			records.Should().NotContain(record => state.IsFlagged(record));
			results.Data.Should().ContainKey("Mean");
			results.Data["Mean"].Should().Be(2.167d);
			results.Data["Count"].Should().Be(3d);
		}

		[DataTestMethod]
		[DataRow("de-DE")]
		[DataRow("fr-FR")]
		[WorkItem(928)]
		public void GivenInvariantDecimalValueAndNonEnglishCulture_WhenAnalyzeRuns_ThenStatisticsAreCalculated(string cultureName)
		{
			// Statistics parsed numbers with the workstation's culture, so 2.5 failed where the decimal separator is a comma.
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

				var state = new RecordState();

				Results results = analyzer.Analyze(records, state, string.Empty, userDialog, canUpdateMetadata: true);

				results.FlaggedRecords.Should().Be(0);
				records.Should().NotContain(record => state.IsFlagged(record));
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
		[WorkItem(929)]
		public void GivenRecordsWithoutCreationTime_WhenAnalyzeRuns_ThenRangeHasNoTimestamps()
		{
			// Statistics counted unknown timestamps, so the time range it reported was huge.
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, Record.CreationTimeUnknown, SeverityType.Information, "Value=10"),
				new Record(2, Record.CreationTimeUnknown, SeverityType.Information, "Value=20"));

			var analyzer = new StatisticalAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+)");

			Results results = analyzer.Analyze(records, new RecordState(), string.Empty, userDialog, canUpdateMetadata: false);

			results.Data["Count"].Should().Be(2d);
			results.Data["Mean"].Should().Be(15d);
			var range = (RangeResult)results.Data["Range"];
			range.StartAt.Should().BeNull();
			range.EndAt.Should().BeNull();
		}

		[TestMethod]
		[WorkItem(929)]
		public void GivenMixedTimestampedAndUntimestampedRecords_WhenAnalyzeRuns_ThenRangeOnlyIncludesValidTimestamps()
		{
			// Statistics counted unknown timestamps, so the time range it reported was huge.
			var knownTime1 = new DateTime(2024, 1, 1, 10, 0, 0);
			var knownTime2 = new DateTime(2024, 1, 1, 10, 0, 5);

			var records = ImmutableArray.Create<IRecord>(
				new Record(1, knownTime1, SeverityType.Information, "Value=1"),
				new Record(2, Record.CreationTimeUnknown, SeverityType.Information, "Value=2"),
				new Record(3, knownTime2, SeverityType.Information, "Value=3"));

			var analyzer = new StatisticalAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+)");

			Results results = analyzer.Analyze(records, new RecordState(), string.Empty, userDialog, canUpdateMetadata: false);

			results.Data["Count"].Should().Be(3d);
			results.Data["Mean"].Should().Be(2d);
			var range = (RangeResult)results.Data["Range"];
			range.StartAt.Should().Be(knownTime1);
			range.EndAt.Should().Be(knownTime2);
		}
	}
}
