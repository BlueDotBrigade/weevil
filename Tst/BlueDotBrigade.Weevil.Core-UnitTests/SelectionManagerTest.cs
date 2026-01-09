namespace BlueDotBrigade.Weevil
{
	using System;
	using BlueDotBrigade.Weevil.Navigation;

	[TestClass]
	public class SelectionManagerTest
	{
		[TestMethod]
		public void Select_FirstRecord_Line1Selected()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: 1);

			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			Assert.AreEqual(1, selectedRecords.Length);
			Assert.AreEqual(1, selectedRecords[0].LineNumber);
		}

		[TestMethod]
		public void Select_LastRecord_Line32Selected()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: 32);

			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			Assert.AreEqual(1, selectedRecords.Length);
			Assert.AreEqual(32, selectedRecords[0].LineNumber);
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

		[TestMethod]
		public void GetSelected_NoRecordsSelected_ReturnsAllVisibleRecords()
		{
			// Arrange
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			// Act - No selection made, default behavior
			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			// Assert - When 0 records are selected, all visible records should be returned
			Assert.AreEqual(engine.Filter.Results.Length, selectedRecords.Length);
			Assert.IsTrue(selectedRecords.Length > 0, "Expected visible records to be returned when nothing is selected");
		}

		[TestMethod]
		public void GetSelected_NoRecordsSelectedWithFilter_ReturnsAllFilteredRecords()
		{
			// Arrange
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			// Apply a filter to reduce visible records
			engine.Filter.Apply(Filter.FilterType.RegularExpression, new Filter.FilterCriteria("Id=00[1-5]"));

			// Act - No selection made
			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			// Assert - Should return all filtered/visible records
			Assert.AreEqual(engine.Filter.Results.Length, selectedRecords.Length);
			Assert.AreEqual(5, selectedRecords.Length, "Expected 5 filtered records matching Id=00[1-5]");
		}

		[TestMethod]
		public void GetSelected_OneRecordSelected_DefaultBehavior_ReturnsOneRecord()
		{
			// Arrange
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: 1);

			// Act - Default behavior (oneIsMany=false)
			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			// Assert - With default parameter, 1 selected record returns that 1 record
			Assert.AreEqual(1, selectedRecords.Length);
			Assert.AreEqual(1, selectedRecords[0].LineNumber);
		}

		[TestMethod]
		public void GetSelected_OneRecordSelected_OneIsMany_ReturnsAllVisibleRecords()
		{
			// Arrange
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: 1);

			// Act - With oneIsMany=true
			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected(oneIsMany: true);

			// Assert - When oneIsMany=true and 1 record selected, all visible records should be returned
			Assert.AreEqual(engine.Filter.Results.Length, selectedRecords.Length);
			Assert.IsTrue(selectedRecords.Length > 1, "Expected more than 1 record when oneIsMany=true with 1 selected");
		}

		[TestMethod]
		public void GetSelected_OneRecordSelectedWithFilter_OneIsMany_ReturnsAllFilteredRecords()
		{
			// Arrange
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			// Apply a filter to reduce visible records
			engine.Filter.Apply(Filter.FilterType.RegularExpression, new Filter.FilterCriteria("Id=00[1-5]"));

			// Select one record
			engine.Selector.Select(lineNumber: 1);

			// Act - With oneIsMany=true
			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected(oneIsMany: true);

			// Assert - Should return all filtered/visible records
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

			// Act - Default behavior
			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			// Assert - When 2 records are selected, return exactly those records
			Assert.AreEqual(2, selectedRecords.Length);
		}

		[TestMethod]
		public void GetSelected_TwoRecordsSelected_OneIsMany_ReturnsExactlyTwoRecords()
		{
			// Arrange
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: 1);
			engine.Selector.Select(lineNumber: 5);

			// Act - With oneIsMany=true
			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected(oneIsMany: true);

			// Assert - When 2+ records selected, oneIsMany should have no effect
			Assert.AreEqual(2, selectedRecords.Length);
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
	}
}
