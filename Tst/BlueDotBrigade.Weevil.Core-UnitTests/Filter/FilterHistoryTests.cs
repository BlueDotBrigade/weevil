namespace BlueDotBrigade.Weevil.Filter
{
	using System.Linq;

	[TestClass]
	public class FilterHistoryTests
	{
		[TestMethod]
		[WorkItem(1109)]
		public void GivenAValueAlreadyInTheHistory_WhenAddedAgain_ThenItMovesFirstAndTheMoveIsReported()
		{
			var history = new FilterHistory();
			history.Load(["Id=1", "Id=2", "Id=3"]);

			var changes = history.Add("Id=3");

			history.Should().Equal("Id=3", "Id=1", "Id=2");
			changes.Select(c => (c.ChangeType, c.Index, c.Value)).Should().Equal((HistoryChangeType.Moved, 2, "Id=3"));
		}

		[TestMethod]
		[WorkItem(1109)]
		public void GivenAFullHistory_WhenANewValueIsAdded_ThenTheOldestIsRemovedBeforeTheNewOneIsAddedFirst()
		{
			// The GUI applies the changes to its own lists in this order.
			var history = new FilterHistory();
			history.Load(Enumerable.Range(1, FilterManager.MaxFilterHistory).Select(i => $"Id={i}"));

			var changes = history.Add("New");

			history.Should().HaveCount(FilterManager.MaxFilterHistory);
			history[0].Should().Be("New");
			changes.Select(c => (c.ChangeType, c.Index, c.Value)).Should().Equal(
				(HistoryChangeType.Removed, FilterManager.MaxFilterHistory - 1, $"Id={FilterManager.MaxFilterHistory}"),
				(HistoryChangeType.Added, 0, "New"));
		}
	}
}
