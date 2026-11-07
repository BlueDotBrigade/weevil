namespace BlueDotBrigade.Weevil.Common.Analysis
{
	using System;
	using System.Collections.Immutable;
	using global::BlueDotBrigade.Weevil.Analysis;
	using global::BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class CriticalErrorsInsightTests
	{
		[TestMethod]
		public void GivenCriticalRecordsInAListThatCannotCreateThem_WhenRefreshed_ThenTheyAreCountedAndTheFirstIsRelated()
		{
			// The insight refreshes after each open, so it reads the severity column rather than creating every record (#1150).
			var start = new DateTime(2026, 11, 7, 9, 0, 0);
			RecordList records = ImmutableArray.Create<IRecord>(
				new Record(1, start, SeverityType.Information, "Started"),
				new Record(2, start.AddSeconds(1), SeverityType.Critical, "Out of memory"),
				new Record(3, start.AddSeconds(2), SeverityType.Critical, "Out of memory again"));
			var insight = new CriticalErrorsInsight();

			insight.Refresh(new ColumnsOnlyRecordList(records));

			insight.MetricValue.Should().Be("2");
			insight.IsAttentionRequired.Should().BeTrue();
			insight.RelatedLineNumbers.Should().Equal(2);
		}
	}
}
