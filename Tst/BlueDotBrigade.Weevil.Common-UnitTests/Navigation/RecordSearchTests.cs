namespace BlueDotBrigade.Weevil.Common.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Navigation;
	using BlueDotBrigade.Weevil.TestingTools.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class RecordSearchTests
	{
		[TestMethod]
		[DataRow(19, 10)] // -2: 
		[DataRow(20, 20)] // 1
		[DataRow(21, 20)] // -3
		public void IndexOfLineNumber_ExactOrPrevious_ReturnsIndex(int requestedLine, int expectedLine)
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10),
				R.WithLineNumber(20),
				R.WithLineNumber(30),
			};

			var index = RecordSearch.IndexOfLineNumber(
				records.ToImmutableArray(),
				requestedLine,
				RecordSearchType.ExactOrPrevious);

			var actualLine = records[index].LineNumber;

			Assert.AreEqual(
				expectedLine,
				actualLine,
				$"Requested={requestedLine}, Expected={expectedLine}, Actual={actualLine}");
		}

		[TestMethod]
		[DataRow(19, 20)]
		[DataRow(20, 20)]
		[DataRow(21, 30)]
		public void IndexOfLineNumber_ExactOrNext_ReturnsIndex(int requestedLine, int expectedLine)
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10),
				R.WithLineNumber(20),
				R.WithLineNumber(30),
			};

			var index = RecordSearch.IndexOfLineNumber(
				records.ToImmutableArray(),
				requestedLine,
				RecordSearchType.ExactOrNext);

			var actualLine = records[index].LineNumber;

			Assert.AreEqual(
				expectedLine,
				actualLine,
				$"Requested={requestedLine}, Expected={expectedLine}, Actual={actualLine}");
		}

		[TestMethod]
		[DataRow(0, 10, "way before")]
		[DataRow(12, 10, "close to 10")]
		[DataRow(18, 20, "close to 20")]
		[DataRow(20, 20 , "exactly 20")]
		[DataRow(22, 20, "close to 20")]
		[DataRow(28, 30, "close to 30")]
		[DataRow(40, 30, "way after")]
		public void IndexOfLineNumber_NearestNeighbor_ReturnsIndex(int requestedLine, int expectedLine, string description)
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10),
				R.WithLineNumber(20),
				R.WithLineNumber(30),
			};

			var index = RecordSearch.IndexOfLineNumber(
				records.ToImmutableArray(), 
				requestedLine, 
				RecordSearchType.NearestNeighbor);

			var actualLine = records[index].LineNumber;

			Assert.AreEqual(
				expectedLine,
				actualLine,
				$"Requested={requestedLine}, Expected={expectedLine}, Actual={actualLine}");
		}	

		[TestMethod]
		[DataRow("9:59", 0)]
		[DataRow("10:30", 2)]
		[DataRow("10:31", 2)]
		[DataRow("10:34", 2)]
		[DataRow("10:44", 3)]
		[DataRow("12:00", 4)]
		public void IndexOfCreatedAt_NearestNeighbor_ReturnsIndex(string createdAt, int expectedIndex)
		{
			var records = R.Create()
				.WithCreatedAt(0, "10:00:00") 
				.WithCreatedAt(1, "10:15:00")
				.WithCreatedAt(2, "10:30:00")
				.WithCreatedAt(3, "10:45:00")
				.WithCreatedAt(4, "11:00:00")
				.GetRecords();

			var createdAtTimestamp = DateTime.Parse(createdAt);

			var actualIndex = RecordSearch.IndexOfCreatedAt(records, createdAtTimestamp, RecordSearchType.NearestNeighbor);

			Assert.AreEqual(
				expectedIndex,
				actualIndex);
		}
	}
}