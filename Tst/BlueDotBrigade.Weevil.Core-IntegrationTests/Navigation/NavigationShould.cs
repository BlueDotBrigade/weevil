﻿namespace BlueDotBrigade.Weevil
{
	using Data;
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using BlueDotBrigade.Weevil.Navigation;
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
			IRecord pinnedRecord = engine.Navigate.Using<IPinNavigator>().FindNext();

			// Reminder: although they are often similar, the line number and index are NOT the same!
			Assert.AreEqual(10, pinnedRecord.LineNumber);
		}

		[TestMethod]
		public void HandleNavigatingWhenPinnedRecordsHidden()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria("Nothing Should Match This Filter"));

			Assert.AreEqual(Record.Dummy, engine.Navigate.Using<IPinNavigator>().FindNext());
		}
	}
}
