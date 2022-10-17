namespace BlueDotBrigade.Weevil
{
	using System;
	using BlueDotBrigade.DatenLokator.TestsTools;
	using BlueDotBrigade.Weevil.Navigation;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

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
	}
}
