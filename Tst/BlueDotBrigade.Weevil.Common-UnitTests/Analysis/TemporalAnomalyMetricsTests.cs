namespace BlueDotBrigade.Weevil.Common.Analysis
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Linq;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using BlueDotBrigade.Weevil.TestingTools.Data;

	[TestClass]
	public class TemporalAnomalyMetricsTests
	{
		[TestMethod]
		public void Count_InChronologicalOrder_Returns0()
		{
			ImmutableArray<IRecord> records = R.Create()
				.WithCreatedAt(0, "10:00:00")
				.WithCreatedAt(1, "10:15:00")
				.WithCreatedAt(2, "10:30:00")
				.WithCreatedAt(3, "10:45:00")
				.WithCreatedAt(4, "11:00:00")
				.GetRecords();

			var metrics = new TemporalAnomalyMetrics();

			records.ForEach(r => metrics.Count(r));

			Assert.AreEqual(0, metrics.Counter);
		}

		[TestMethod]
		public void FirstOccurredAt_InChronologicalOrder_ReturnsDummyRecord()
		{
			ImmutableArray<IRecord> records = R.Create()
				.WithCreatedAt(0, "10:00:00")
				.WithCreatedAt(1, "10:15:00")
				.WithCreatedAt(2, "10:30:00")
				.WithCreatedAt(3, "10:45:00")
				.WithCreatedAt(4, "11:00:00")
				.GetRecords();

			var metrics = new TemporalAnomalyMetrics();

			records.ForEach(r => metrics.Count(r));

			Assert.AreSame(Record.Dummy, metrics.FirstOccurredAt);
		}

		[TestMethod]
		public void Count_NotInChronologicalOrder_Returns1()
		{
			ImmutableArray<IRecord> records = R.Create()
				.WithCreatedAt(0, "10:00:00")
				.WithCreatedAt(1, "10:15:00")
				.WithCreatedAt(2, "10:45:00")
				.WithCreatedAt(3, "10:30:00") // out of sequence
				.WithCreatedAt(4, "11:00:00")
				.GetRecords();

			var metrics = new TemporalAnomalyMetrics();

			records.ForEach(r => metrics.Count(r));

			Assert.AreEqual(1, metrics.Counter);
		}

		[TestMethod]
		public void FirstOccurredAt_NotInChronologicalOrder_Returns3()
		{
			ImmutableArray<IRecord> records = R.Create()
				.WithCreatedAt(0, "10:00:00")
				.WithCreatedAt(1, "10:15:00")
				.WithCreatedAt(2, "10:45:00")
				.WithCreatedAt(3, "10:30:00") // out of sequence
				.WithCreatedAt(4, "11:00:00")
				.GetRecords();

			var metrics = new TemporalAnomalyMetrics();

			records.ForEach(r => metrics.Count(r));

			Assert.AreEqual(3, metrics.FirstOccurredAt.LineNumber);
		}
	}
}