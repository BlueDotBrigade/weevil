namespace BlueDotBrigade.Weevil.Common.Analysis
{
	using global::BlueDotBrigade.Weevil.Analysis;
	using global::BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TemporalAnomalyInsightTests
	{
		[TestMethod]
		public void GivenTimestampsOutOfOrderInAListThatCannotCreateRecords_WhenRefreshed_ThenTheAnomalyIsFound()
		{
			// The insight refreshes after each open, so it reads the timestamp column rather than creating every record (#1150).
			RecordList records = R.Create()
				.WithCreatedAt(1, "10:00:00")
				.WithCreatedAt(2, "10:15:00")
				.WithCreatedAt(3, "10:05:00")
				.WithCreatedAt(4, "10:30:00")
				.GetRecords();
			var insight = new TemporalAnomalyInsight();

			insight.Refresh(new ColumnsOnlyRecordList(records));

			insight.MetricValue.Should().Be("1");
			insight.RelatedLineNumbers.Should().Equal(2);
		}
	}
}
