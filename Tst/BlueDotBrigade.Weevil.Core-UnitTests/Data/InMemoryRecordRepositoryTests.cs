namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	[TestClass]
	public class InMemoryRecordRepositoryTests
	{
		[TestMethod]
		public void Test_ClearBeyondRegions_NoRegionsDefined()
		{
			// Arrange
			var regions = new List<Region>();
			var records = R.WithLineNumbers(11, 20);

			var repository = new InMemoryRecordRepository(
				records.ToImmutableArray(),
				ImmutableArray<IRecord>.Empty,
				ImmutableArray<IRecord>.Empty,
				ClearOperation.BeyondRegions,
				regions.ToImmutableArray());

			// Act
			var filteredRecords = repository.GetAll();

			// Assert
			filteredRecords.Length.Should().Be(10);
		}

		[TestMethod]
		public void Test_ClearBeyondRegions_ReturnsRecordsBetweenRegions()
		{
			// Arrange
			var regions = new List<Region>
			{
				new Region("RegionOfInterest", 14, 16),
			};
			var records = R.WithLineNumbers(11, 20);

			var repository = new InMemoryRecordRepository(
				records.ToImmutableArray(),
				ImmutableArray<IRecord>.Empty,
				ImmutableArray<IRecord>.Empty,
				ClearOperation.BeyondRegions,
				regions.ToImmutableArray());

			// Act
			var filteredRecords = repository.GetAll();

			// Assert
			filteredRecords.Length.Should().Be(3);
			filteredRecords[0].LineNumber.Should().Be(14);
			filteredRecords[1].LineNumber.Should().Be(15);
			filteredRecords[2].LineNumber.Should().Be(16);
		}

		//[TestMethod]
		//public void Test_ClearBeyondBookends_OneBookendFrom1To10()
		//{
		//	// Arrange
		//	var allRecords = Enumerable.Range(1, 50).Select(i => new Record { LineNumber = i }).ToList<IRecord>();
		//	var bookends = new List<Bookend> { new Bookend(1, 10) };

		//	// Act
		//	var filteredRecords = LogViewer.ClearBeyondBookends(allRecords, bookends);

		//	// Assert
		//	Assert.AreEqual(10, filteredRecords.Count);
		//	Assert.IsTrue(filteredRecords.All(record => record.LineNumber >= 1 && record.LineNumber <= 10));
		//}

		//[TestMethod]
		//public void Test_ClearBeyondBookends_OneBookendFrom40To50()
		//{
		//	// Arrange
		//	var allRecords = Enumerable.Range(1, 50).Select(i => new Record { LineNumber = i }).ToList<IRecord>();
		//	var bookends = new List<Bookend> { new Bookend(40, 50) };

		//	// Act
		//	var filteredRecords = LogViewer.ClearBeyondBookends(allRecords, bookends);

		//	// Assert
		//	Assert.AreEqual(11, filteredRecords.Count);
		//	Assert.IsTrue(filteredRecords.All(record => record.LineNumber >= 40 && record.LineNumber <= 50));
		//}

		//[TestMethod]
		//public void Test_ClearBeyondBookends_TwoBookends_10To20_30To40()
		//{
		//	// Arrange
		//	var allRecords = Enumerable.Range(1, 50).Select(i => new Record { LineNumber = i }).ToList<IRecord>();
		//	var bookends = new List<Bookend> { new Bookend(10, 20), new Bookend(30, 40) };

		//	// Act
		//	var filteredRecords = LogViewer.ClearBeyondBookends(allRecords, bookends);

		//	// Assert
		//	Assert.AreEqual(22, filteredRecords.Count);
		//	Assert.IsTrue(filteredRecords.All(record => (record.LineNumber >= 10 && record.LineNumber <= 20) || (record.LineNumber >= 30 && record.LineNumber <= 40)));
		//}
	}
}