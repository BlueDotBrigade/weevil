namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class FilterHistoryShould
	{
		[TestMethod]
		public void KeepLatestInclusiveFilterAtTopOfList()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=2"));
			Assert.AreEqual("Id=2", engine.Filter.IncludeHistory[0]);

			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=3"));
			Assert.AreEqual("Id=3", engine.Filter.IncludeHistory[0]);
		}

		[TestMethod]
		public void KeepLatestExclusiveFilterAtTopOfList()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria(string.Empty, "Id=4"));
			Assert.AreEqual("Id=4", engine.Filter.ExcludeHistory[0]);

			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria(string.Empty, "Id=5"));
			Assert.AreEqual("Id=5", engine.Filter.ExcludeHistory[0]);
		}


		[TestMethod]
		public void RemainIntactAfterClearOperation()
		{
			IEngine engine = Engine
				.UsingPath(InputData.GetFilePath("GenericBaseline.log"))
				.Open();

			var uniqueFilter = $"Info(?#RegEx comment created at {DateTime.Now.ToLongTimeString()})";
			engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(uniqueFilter));

			engine.Selector.Select(32);

			var filtersBeforeClear = engine.Filter.IncludeHistory.Count;

			engine.Clear(ClearOperation.BeforeSelected);

			Assert.AreEqual(filtersBeforeClear, engine.Filter.IncludeHistory.Count);
			Assert.AreEqual(uniqueFilter, engine.Filter.IncludeHistory[0]);
		}
	}
}
