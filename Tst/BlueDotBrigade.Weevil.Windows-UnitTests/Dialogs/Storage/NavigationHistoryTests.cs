namespace BlueDotBrigade.Weevil.Windows.Dialogs.Storage
{
	[TestClass]
	public class NavigationHistoryTests
	{
		private static readonly Location First = Location.ForFileSystem(Path.Combine(Path.GetTempPath(), "First"));
		private static readonly Location Second = Location.ForFileSystem(Path.Combine(Path.GetTempPath(), "Second"));
		private static readonly Location Third = Location.ForFileSystem(Path.Combine(Path.GetTempPath(), "Third"));

		[TestMethod]
		public void GivenTwoPlacesVisited_WhenGoingBack_ThenTheFirstIsReturnedAndForwardIsPossible()
		{
			var history = new NavigationHistory();
			history.Visit(First);
			history.Visit(Second);

			history.GoBack().Should().Be(First);
			history.CanGoForward.Should().BeTrue();
		}

		[TestMethod]
		public void GivenAPlaceToGoForwardTo_WhenANewPlaceIsVisited_ThenForwardIsCleared()
		{
			var history = new NavigationHistory();
			history.Visit(First);
			history.Visit(Second);
			history.GoBack();

			history.Visit(Third);

			history.CanGoForward.Should().BeFalse();
			history.CanGoBack.Should().BeTrue();
		}
	}
}
