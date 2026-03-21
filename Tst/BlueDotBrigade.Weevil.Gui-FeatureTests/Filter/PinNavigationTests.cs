namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Gui.Threading;

	/// <summary>
	/// Verifies that <see cref="FilterViewModel.ActiveRecordIndex"/> is updated correctly
	/// when navigating to pinned records via <see cref="FilterViewModel.GoToNextPin"/> and
	/// <see cref="FilterViewModel.GoToPreviousPin"/>.
	/// </summary>
	/// <remarks>
	/// These tests validate the ViewModel layer only. The corresponding View-layer fix
	/// (keyboard focus synchronization) is in <c>FilterView.xaml.cs</c> and cannot be
	/// verified without a live WPF dispatcher.
	/// </remarks>
	[TestClass]
	public class PinNavigationTests : UiTestBase
	{
		private const int LineNumberA = 10;
		private const int LineNumberB = 20;

		/// <summary>
		/// Verifies that navigating to the next pinned record updates <c>ActiveRecordIndex</c>
		/// to the 0-based index of that record within the filter results.
		/// </summary>
		/// <remarks>
		/// Regression: Issue #699 — After "Next Pinned" navigation, pressing the down arrow
		/// jumped to an unexpected record because <c>ActiveRecordIndex</c> was not reflecting
		/// the correct position.
		/// </remarks>
		[TestMethod]
		public async Task GivenTwoPinnedRecords_WhenGoToNextPinCalledTwice_ThenActiveRecordIndexMatchesPinnedRecord()
		{
			// Arrange
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			var viewModel = new FilterViewModel(this.UiDispatcher, bulletinMediator);

			await viewModel.OpenAsync(new Daten().AsFilePath(From.GlobalDefault));

			var stopwatch = Stopwatch.StartNew();
			while (!viewModel.IsLogFileOpen && stopwatch.Elapsed < TimeSpan.FromSeconds(5))
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(100));
			}

			Assert.IsTrue(viewModel.IsLogFileOpen, "Log file should be open before the test continues.");

			// GlobalDefault.log is 1-indexed and sequential, so line N is at 0-based index N-1.
			viewModel.VisibleItems[LineNumberA - 1].Metadata.IsPinned = true;
			viewModel.VisibleItems[LineNumberB - 1].Metadata.IsPinned = true;

			// Act & Assert — first "Next Pinned" should land on line 10 (index 9)
			viewModel.GoToNextPin();
			Assert.AreEqual(LineNumberA - 1, viewModel.ActiveRecordIndex,
				$"After first GoToNextPin(), ActiveRecordIndex should be {LineNumberA - 1} (line {LineNumberA}).");

			// Act & Assert — second "Next Pinned" should land on line 20 (index 19)
			viewModel.GoToNextPin();
			Assert.AreEqual(LineNumberB - 1, viewModel.ActiveRecordIndex,
				$"After second GoToNextPin(), ActiveRecordIndex should be {LineNumberB - 1} (line {LineNumberB}).");
		}
	}
}
