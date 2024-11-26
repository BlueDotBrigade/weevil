namespace BlueDotBrigade.Weevil.Common.Analysis
{
	using System;
	using System.Collections.Immutable;
	using global::BlueDotBrigade.Weevil.Analysis;
	using global::BlueDotBrigade.Weevil.Data; 
	using global::BlueDotBrigade.Weevil.Linq;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

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

			var metrics = new TemporalAnomalyMetrics(TimeSpan.Zero);

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

			var metrics = new TemporalAnomalyMetrics(TimeSpan.Zero);

			records.ForEach(r => metrics.Count(r));

			Assert.AreSame(Record.Dummy, metrics.FirstOccurredAt);
		}

		[TestMethod]
		public void Threshold_ErrorLessThanThreshold_Returns0()
		{
			ImmutableArray<IRecord> records = R.Create()
				.WithCreatedAt(0, "10:00:00")
				.WithCreatedAt(1, "10:15:00")
				.WithCreatedAt(2, "10:45:00")
				.WithCreatedAt(3, "10:30:00") // 15min error
				.WithCreatedAt(4, "11:00:00")
				.GetRecords();

			var metrics = new TemporalAnomalyMetrics(TimeSpan.FromMinutes(-20));

			records.ForEach(r => metrics.Count(r));

			Assert.AreEqual(0, metrics.Counter);
		}

		[TestMethod]
		public void Threshold_ErrorGreaterThanThreshold_Returns1()
		{
			ImmutableArray<IRecord> records = R.Create()
				.WithCreatedAt(0, "10:00:00")
				.WithCreatedAt(1, "10:15:00")
				.WithCreatedAt(2, "10:45:00")
				.WithCreatedAt(3, "10:30:00") // 15min error
				.WithCreatedAt(4, "11:00:00")
				.GetRecords();

			var metrics = new TemporalAnomalyMetrics(TimeSpan.FromMinutes(-10));

			records.ForEach(r => metrics.Count(r));

			Assert.AreEqual(1, metrics.Counter);
		}

		[TestMethod]
		public void Count_NotInChronologicalOrder_Returns1()
		{
			ImmutableArray<IRecord> records = R.Create()
				.WithCreatedAt(0, "10:00:00")
				.WithCreatedAt(1, "10:15:00")
				.WithCreatedAt(2, "10:45:00")
				.WithCreatedAt(3, "10:30:00") // out of order
				.WithCreatedAt(4, "11:00:00")
				.GetRecords();

			var metrics = new TemporalAnomalyMetrics(TimeSpan.Zero);

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
				.WithCreatedAt(3, "10:30:00") // out of order
				.WithCreatedAt(4, "11:00:00")
				.GetRecords();

			var metrics = new TemporalAnomalyMetrics(TimeSpan.Zero);

			records.ForEach(r => metrics.Count(r));

			Assert.AreEqual(3, metrics.FirstOccurredAt.LineNumber);
		}


		[TestMethod]
		public void BiggestAnomaly_NotInChronologicalOrder_Returns9()
		{
			ImmutableArray<IRecord> records = R.Create()
				.WithCreatedAt(0, "10:00:00")
				.WithCreatedAt(1, "10:39:00") // 9min error
				.WithCreatedAt(2, "10:30:00")
				.WithCreatedAt(3, "11:02:00") // 2min error
				.WithCreatedAt(4, "11:00:00")
				.GetRecords();

			var metrics = new TemporalAnomalyMetrics(TimeSpan.Zero);

			records.ForEach(r => metrics.Count(r));

			Assert.AreEqual(-9.0, metrics.BiggestAnomaly.TotalMinutes);
		}

		[TestMethod]
		public void BiggestAnomalyAt_NotInChronologicalOrder_Returns1()
		{
			ImmutableArray<IRecord> records = R.Create()
				.WithCreatedAt(0, "10:00:00")
				.WithCreatedAt(1, "10:39:00") // 9min error
				.WithCreatedAt(2, "10:30:00")
				.WithCreatedAt(3, "11:02:00") // 2min error
				.WithCreatedAt(4, "11:00:00")
				.GetRecords();

			var metrics = new TemporalAnomalyMetrics(TimeSpan.Zero);

			records.ForEach(r => metrics.Count(r));

			Assert.AreEqual(1, metrics.BiggestAnomalyAt.LineNumber);
		}
	}
}