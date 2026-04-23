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
			var shouldNormalize = FilterView.ShouldCollapseSelectionAfterCtrlShiftNavigation(
				ModifierKeys.Control | ModifierKeys.Shift);

			shouldNormalize.Should().BeTrue();
		}

		[DataTestMethod]
		[DataRow(ModifierKeys.None)]
		[DataRow(ModifierKeys.Control)]
		[DataRow(ModifierKeys.Shift)]
		[DataRow(ModifierKeys.Alt)]
		[DataRow(ModifierKeys.Control | ModifierKeys.Alt)]
		[DataRow(ModifierKeys.Shift | ModifierKeys.Alt)]
		public void GivenMissingRequiredModifiers_WhenCheckingNavigationSelectionNormalization_ThenReturnsFalse(ModifierKeys modifiers)
		{
			var shouldNormalize = FilterView.ShouldCollapseSelectionAfterCtrlShiftNavigation(modifiers);

			shouldNormalize.Should().BeFalse();
		}
	}
}
