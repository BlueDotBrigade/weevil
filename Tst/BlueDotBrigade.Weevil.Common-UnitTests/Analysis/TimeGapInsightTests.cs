namespace BlueDotBrigade.Weevil.Common.Analysis
{
	using System;
	using System.Collections.Immutable;
	using global::BlueDotBrigade.Weevil.Analysis;
	using global::BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TimeGapInsightTests
	{
		[TestMethod]
		public void GivenAGapBetweenUiRecordsInAListThatCannotCreateThem_WhenRefreshed_ThenTheGapIsFound()
		{
			// The insight refreshes after each open, so it reads the timestamp and facts columns rather than creating every record (#1150).
			var start = new DateTime(2026, 11, 7, 9, 0, 0);
			RecordList records = ImmutableArray.Create<IRecord>(
				new Record(1, start, SeverityType.Information, "Clicked", isMultiLine: false, wasGeneratedByUi: true),
				new Record(2, start.AddSeconds(30), SeverityType.Information, "Worker", isMultiLine: false, wasGeneratedByUi: false),
				new Record(3, start.AddSeconds(90), SeverityType.Information, "Redrawn", isMultiLine: false, wasGeneratedByUi: true));
			var insight = new TimeGapInsight(TimeSpan.FromSeconds(60), "UI gaps", "None");

			insight.Refresh(new ColumnsOnlyRecordList(records));

			insight.IsAttentionRequired.Should().BeTrue();
			insight.RelatedLineNumbers.Should().Equal(3);
		}

		[TestMethod]
		[WorkItem(1226)]
		public void GivenAShortGapThenALongerOne_WhenRefreshed_ThenTheLongestGapIsNamedAndMeasuredInSeconds()
		{
			// The insight called the first gap over the threshold the longest, and showed the number of gaps as seconds.
			var start = new DateTime(2026, 11, 7, 10, 0, 0);
			RecordList records = ImmutableArray.Create<IRecord>(
				new Record(1, start, SeverityType.Information, "a", isMultiLine: false, wasGeneratedByUi: true),
				new Record(2, start.AddSeconds(1), SeverityType.Information, "b", isMultiLine: false, wasGeneratedByUi: true),
				new Record(3, start.AddSeconds(3), SeverityType.Information, "c", isMultiLine: false, wasGeneratedByUi: true),
				new Record(4, start.AddSeconds(4), SeverityType.Information, "d", isMultiLine: false, wasGeneratedByUi: true),
				new Record(5, start.AddSeconds(14), SeverityType.Information, "e", isMultiLine: false, wasGeneratedByUi: true));
			var insight = new TimeGapInsight();

			insight.Refresh(records);

			insight.RelatedLineNumbers.Should().Equal(5);
			insight.Details.Should().Contain("10:00:14").And.NotContain("10:00:03");
			insight.MetricValue.Should().Be(10.0.ToString("#,##0.0"));
			insight.MetricUnit.Should().Be("sec");
		}
	}
}
