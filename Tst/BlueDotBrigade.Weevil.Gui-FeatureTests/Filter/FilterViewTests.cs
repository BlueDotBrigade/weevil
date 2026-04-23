namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System.Windows.Input;

	[TestClass]
	public class FilterViewTests
	{
		[TestMethod]
		public void GivenControlAndShiftModifiers_WhenCheckingNavigationSelectionNormalization_ThenReturnsTrue()
		{
			// Regression: Issue #768
			var shouldNormalize = FilterView.ShouldNormalizeSelectionAfterKeyboardNavigation(
				ModifierKeys.Control | ModifierKeys.Shift);

			shouldNormalize.Should().BeTrue();
		}
	}
}
