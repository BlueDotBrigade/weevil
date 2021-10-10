namespace BlueDotBrigade.Weevil.Common.Data.Comparers
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestingTools.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class RecordSearchTests
	{
		[TestMethod]
		[DataRow(0, 0)]
		[DataRow(12, 0)]
		[DataRow(20, 1)]
		[DataRow(28, 2)]
		[DataRow(40, 2)]
		public void IndexOfLineNumber_ClosestMatch_ReturnsIndex(int requestedLineNumber, int expectedIndex)
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10), 
				R.WithLineNumber(20), 
				R.WithLineNumber(30),
			};

			var actualIndex =
				RecordSearch.IndexOfLineNumber(records.ToImmutableArray(), requestedLineNumber, SearchType.ClosestMatch);

			Assert.AreEqual(
				expectedIndex,
				actualIndex,
				$"Requested={requestedLineNumber}, Expected={expectedIndex}, Actual={actualIndex}");
		}

		[TestMethod]
		[DataRow("9:59", 0)]
		[DataRow("10:30", 2)]
		[DataRow("10:31", 2)]
		[DataRow("10:34", 2)]
		[DataRow("10:44", 3)]
		[DataRow("12:00", 4)]
		public void IndexOfCreatedAt_ClosestMatch_ReturnsIndex(string createdAt, int expectedIndex)
		{
			var records = R.Create()
				.WithCreatedAt(0, "10:00:00")
				.WithCreatedAt(1, "10:15:00")
				.WithCreatedAt(2, "10:30:00")
				.WithCreatedAt(3, "10:45:00")
				.WithCreatedAt(4, "11:00:00")
				.GetRecords();

			var createdAtTimestamp = DateTime.Parse(createdAt);

			var actualIndex = RecordSearch.IndexOfCreatedAt(records, createdAtTimestamp, SearchType.ClosestMatch);

			Assert.AreEqual(
				expectedIndex,
				actualIndex);
		}
	}
}