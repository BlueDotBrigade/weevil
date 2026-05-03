namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Gui.Threading;

	/// <summary>
	/// Verifies that <see cref="FilterViewModel.ActiveRecordIndex"/> is updated correctly
	/// when navigating to commented records via <see cref="FilterViewModel.GoToNextComment"/>
	/// and <see cref="FilterViewModel.GoToPreviousComment"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// These tests cover the View Model layer only. Issue #806 is a View-layer bug —
	/// the WPF range-extend behavior under <c>Ctrl+Shift+Plus</c> can only be reproduced
	/// against a live <see cref="System.Windows.Controls.ListView"/>. The matching manual
	/// regression test is documented in
	/// <c>Doc/Notes/Design/Issue806-CommentNavigationBug.md</c>.
	/// </para>
	/// <para>
	/// The wrap-around assertion below pins down the engine-side contract that the View
	/// relies on: <c>NextComment</c> must return to the first comment after the last one.
	/// If the engine is ever changed in a way that breaks wrap, this test will fail
	/// loudly — an important safety net, because the bug at the View layer only manifests
	/// when wrap returns the same filter index that the View Model was last told about.
	/// </para>
	/// </remarks>
	[TestClass]
	public class CommentNavigationTests : UiTestBase
	{
		private const int LineNumberA = 10;
		private const int LineNumberB = 20;

		private const string CommentA = "First comment";
		private const string CommentB = "Second comment";

		[TestMethod]
		public async Task GivenTwoCommentedRecords_WhenGoToNextCommentCalledThreeTimes_ThenActiveRecordIndexWrapsToFirstComment()
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

			viewModel.IsLogFileOpen.Should().BeTrue("the log file should be open before the test continues");

			// GlobalDefault.log is 1-indexed and sequential, so line N is at 0-based index N-1.
			viewModel.VisibleItems[LineNumberA - 1].Metadata.Comment = CommentA;
			viewModel.VisibleItems[LineNumberB - 1].Metadata.Comment = CommentB;

			// Act & Assert — first "Next Comment" should land on line 10 (index 9).
			viewModel.GoToNextComment();
			viewModel.ActiveRecordIndex.Should().Be(
				LineNumberA - 1,
				"the first call to GoToNextComment() should land on line {0}",
				LineNumberA);

			// Act & Assert — second "Next Comment" should land on line 20 (index 19).
			viewModel.GoToNextComment();
			viewModel.ActiveRecordIndex.Should().Be(
				LineNumberB - 1,
				"the second call to GoToNextComment() should land on line {0}",
				LineNumberB);

			// Act & Assert — third "Next Comment" should wrap back to line 10 (index 9).
			// Regression: Issue #806. The engine must return the first comment again so
			// the View Model can tell the View to scroll there. If wrap stops working,
			// the View-layer suppression bug becomes meaningless because the user would
			// never reach the wrap state in the first place.
			viewModel.GoToNextComment();
			viewModel.ActiveRecordIndex.Should().Be(
				LineNumberA - 1,
				"the third call to GoToNextComment() should wrap back to line {0}",
				LineNumberA);
		}
	}
}
