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

                [TestMethod]
                public void ClearSelected_RemovesOnlySelectedRecords()
                {
                        // Arrange
                        var allRecords = R.WithLineNumbers(1, 5);
                        var selectedRecords = new List<IRecord>
                        {
                                R.WithLineNumber(2),
                                R.WithLineNumber(4),
                        };

                        var repository = new InMemoryRecordRepository(
                                allRecords.ToImmutableArray(),
                                ImmutableArray<IRecord>.Empty,
                                selectedRecords.ToImmutableArray(),
                                ClearOperation.Selected,
                                ImmutableArray<Region>.Empty);

                        // Act
                        var filteredRecords = repository.GetAll();

                        // Assert
                        filteredRecords.Select(r => r.LineNumber)
                                .Should().Equal(new[] { 1, 3, 5 });
                }
        }
}