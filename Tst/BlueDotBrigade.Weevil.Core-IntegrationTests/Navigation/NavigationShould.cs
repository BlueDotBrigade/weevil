namespace BlueDotBrigade.Weevil
{
	using Data;
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using Filter;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class NavigationShould
	{
		[TestMethod]
		public void SupportNavigatingToPinnedRecord()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine.Filter.Results[9].Metadata.IsPinned = true;

			engine.Selector.Select(1);
			IRecord pinnedRecord = engine.Navigator.Pinned.GoToNext();

			// Reminder: although they are often similar, the line number and index are NOT the same!
			Assert.AreEqual(10, pinnedRecord.LineNumber);
		}

		[TestMethod]
		public void HandleNavigatingWhenThereAreNoPinnedRecords()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria("ThisWillHidePinnedRecord9"));

			Assert.AreEqual(Record.Dummy, engine.Navigator.Pinned.GoToNext());
		}
	}
}
