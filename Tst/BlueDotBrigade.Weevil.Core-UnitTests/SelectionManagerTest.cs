namespace BlueDotBrigade.Weevil
{
	using System;
	using BlueDotBrigade.Weevil.Navigation;

	[TestClass]
	public class SelectionManagerTest
	{
		[TestMethod]
		public void GetSelected_OneRecordSelected_ReturnsAllVisibleRecords()
		{
			// Arrange
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: 1);

			// Act
			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			// Assert - When only 1 record is selected, all visible records should be returned
			Assert.IsTrue(selectedRecords.Length > 1, "Expected more than 1 record when only 1 is selected");
			Assert.AreEqual(engine.Filter.Results.Length, selectedRecords.Length);
		}

		[TestMethod]
		public void GetSelected_OneRecordSelectedWithFilter_ReturnsAllFilteredRecords()
		{
			// Arrange
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			// Apply a filter to reduce visible records
			engine.Filter.Apply(Filter.FilterType.RegularExpression, new Filter.FilterCriteria("Id=00[1-5]"));

			// Select one record
			engine.Selector.Select(lineNumber: 1);

			// Act
			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			// Assert - Should return all filtered/visible records, not just the one selected
			Assert.AreEqual(engine.Filter.Results.Length, selectedRecords.Length);
			Assert.AreEqual(5, selectedRecords.Length, "Expected 5 filtered records matching Id=00[1-5]");
		}

		[TestMethod]
		public void GetSelected_TwoRecordsSelected_ReturnsExactlyTwoRecords()
		{
			// Arrange
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: 1);
			engine.Selector.Select(lineNumber: 5);

			// Act
			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			// Assert - When 2 or more records are selected, return exactly those records
			Assert.AreEqual(2, selectedRecords.Length);
			Assert.AreEqual(1, selectedRecords[0].LineNumber);
			Assert.AreEqual(5, selectedRecords[1].LineNumber);
		}

		[TestMethod]
		public void GetSelected_NoRecordsSelected_ReturnsAllVisibleRecords()
		{
			// Arrange
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			// Act - No selection made
			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			// Assert - When 0 records are selected, all visible records should be returned
			Assert.AreEqual(engine.Filter.Results.Length, selectedRecords.Length);
		}

		[TestMethod]
		public void GetSelected_MultipleRecordsSelected_ReturnsExactlySelectedRecords()
		{
			// Arrange
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: 1);
			engine.Selector.Select(lineNumber: 10);
			engine.Selector.Select(lineNumber: 20);

			// Act
			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			// Assert - When 2+ records are selected, return exactly those records
			Assert.AreEqual(3, selectedRecords.Length);
		}

		[TestMethod]
		[ExpectedException(typeof(RecordNotFoundException))]
		public void Select_NonExistentRecord_ThrowsRecordNotFound()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			try
			{
				engine.Selector.Select(lineNumber: int.MaxValue);
			}
			catch (RecordNotFoundException)
			{
				throw;
			}
			finally
			{
				Assert.AreEqual(0, engine.Selector.Selected.Count);
			}
		}
	}
}
