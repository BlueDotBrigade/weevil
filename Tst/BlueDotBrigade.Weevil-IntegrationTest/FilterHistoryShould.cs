namespace BlueDotBrigade.Weevil
{
	using Data;
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using Filter;
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

			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=2"));

			engine.Selector.Select(32);
			engine.Clear(ClearRecordsOperation.BeforeSelected);

			Assert.IsTrue(engine.Filter.IncludeHistory.Count > 0, "The filter history is missing.");
			Assert.AreEqual("Id=2", engine.Filter.IncludeHistory[0]);
		}
	}
}
