namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class FilterHistoryShould
	{
		[TestMethod]
		public void KeepLatestInclusiveFilterAtTopOfList()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=2"));
			engine.Filter.IncludeHistory[0].Should().Be("Id=2");

			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=3"));
			engine.Filter.IncludeHistory[0].Should().Be("Id=3");
		}

		[TestMethod]
		public void KeepLatestExclusiveFilterAtTopOfList()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria(string.Empty, "Id=4"));
			engine.Filter.ExcludeHistory[0].Should().Be("Id=4");

			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria(string.Empty, "Id=5"));
			engine.Filter.ExcludeHistory[0].Should().Be("Id=5");
		}


		[TestMethod]
		public void RemainIntactAfterClearOperation()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			var uniqueFilter = $"Info(?#RegEx comment created at {DateTime.Now.ToLongTimeString()})";
			engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(uniqueFilter));

			engine.Selector.Select(32);

			var filtersBeforeClear = engine.Filter.IncludeHistory.Count;

			engine.Clear(ClearOperation.BeforeSelected);

			engine.Filter.IncludeHistory.Count.Should().Be(filtersBeforeClear);
			engine.Filter.IncludeHistory[0].Should().Be(uniqueFilter);
		}
	}
}
