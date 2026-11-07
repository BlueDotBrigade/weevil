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
	}
}
