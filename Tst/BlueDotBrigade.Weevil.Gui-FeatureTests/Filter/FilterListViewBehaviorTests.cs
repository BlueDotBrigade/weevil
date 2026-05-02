namespace BlueDotBrigade.Weevil.Gui.Filter
{
	[TestClass]
	public class FilterListViewBehaviorTests
	{
		[TestMethod]
		public void GivenMultipleSelectedRecords_WhenCheckingSelectionNormalization_ThenReturnsTrue()
		{
			// Regression: Issue #768
			var shouldNormalize = FilterListViewBehavior.ShouldNormalizeSelection(
				selectedItemCount: 12,
				selectedIndex: 9,
				activeRecordIndex: 9);

			shouldNormalize.Should().BeTrue();
		}

		[TestMethod]
		public void GivenSingleSelectedRecordMatchingActiveIndex_WhenCheckingSelectionNormalization_ThenReturnsFalse()
		{
			var shouldNormalize = FilterListViewBehavior.ShouldNormalizeSelection(
				selectedItemCount: 1,
				selectedIndex: 9,
				activeRecordIndex: 9);

			shouldNormalize.Should().BeFalse();
		}

		[TestMethod]
		public void GivenSingleSelectedRecordAtDifferentIndex_WhenCheckingSelectionNormalization_ThenReturnsTrue()
		{
			var shouldNormalize = FilterListViewBehavior.ShouldNormalizeSelection(
				selectedItemCount: 1,
				selectedIndex: 5,
				activeRecordIndex: 9);

			shouldNormalize.Should().BeTrue();
		}

		[DataTestMethod]
		[DataRow(-1, 10)]
		[DataRow(10, 10)]
		[DataRow(11, 10)]
		public void GivenIndexOutsideVisibleItems_WhenCheckingActiveRecordVisibility_ThenReturnsFalse(
			int activeRecordIndex,
			int itemCount)
		{
			var isVisible = FilterListViewBehavior.HasVisibleItemAtIndex(activeRecordIndex, itemCount);

			isVisible.Should().BeFalse();
		}

		[DataTestMethod]
		[DataRow(0, 1)]
		[DataRow(9, 10)]
		public void GivenIndexInsideVisibleItems_WhenCheckingActiveRecordVisibility_ThenReturnsTrue(
			int activeRecordIndex,
			int itemCount)
		{
			var isVisible = FilterListViewBehavior.HasVisibleItemAtIndex(activeRecordIndex, itemCount);

			isVisible.Should().BeTrue();
		}
	}
}
