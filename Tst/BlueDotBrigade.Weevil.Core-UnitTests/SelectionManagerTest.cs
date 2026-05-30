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

			selectedRecords.Length.Should().Be(1);
			selectedRecords[0].LineNumber.Should().Be(1);
		}
		[TestMethod]
		public void Select_LastRecord_Line32Selected()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: 32);

			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			selectedRecords.Length.Should().Be(1);
			selectedRecords[0].LineNumber.Should().Be(32);
		}

		[TestMethod]
		public void Select_NonExistentRecord_ThrowsRecordNotFound()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("SampleData.log"))
				.Open();

			Action act = () => engine.Selector.Select(lineNumber: int.MaxValue);
			act.Should().Throw<RecordNotFoundException>();

			engine.Selector.Selected.Count.Should().Be(0);
		}
	}
}
