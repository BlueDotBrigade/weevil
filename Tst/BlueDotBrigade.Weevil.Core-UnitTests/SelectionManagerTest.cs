namespace BlueDotBrigade.Weevil
{
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SelectionManagerTest
	{
		[TestMethod]
		public void Select_FirstRecord_Line1Selected()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("SampleData.log"))
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
				.UsingPath(InputData.GetFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: 32);

			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			Assert.AreEqual(1, selectedRecords.Length);
			Assert.AreEqual(32, selectedRecords[0].LineNumber);
		}

		[TestMethod]
		public void Select_NonExistentRecord_NothingSelected()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("SampleData.log"))
				.Open();

			engine.Selector.Select(lineNumber: int.MaxValue);

			System.Collections.Immutable.ImmutableArray<Data.IRecord> selectedRecords = engine.Selector.GetSelected();

			Assert.AreEqual(0, selectedRecords.Length);
		}
	}
}
