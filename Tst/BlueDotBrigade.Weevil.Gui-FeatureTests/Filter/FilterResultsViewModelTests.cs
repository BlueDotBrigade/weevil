namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Threading.Tasks;
	using System.Windows;
	using BlueDotBrigade.Weevil.Gui.Threading;

	// Adding this attribute prevents the exception: InvalidOperationException The calling thread must be STA, because many UI components require this.
	// Mention this in: https://github.com/BlueDotBrigade/weevil/issues/191
	//[StaTestClass]
	public class FilterResultsViewModelTests : UiTestBase
	{
		[TestMethod]
		public async Task OpenAsync()
		{
			var bulletinMediator = Substitute.For<IBulletinMediator>();

			var viewModel = Track(new FilterViewModel(
				this.UiDispatcher,
				bulletinMediator));

			await viewModel.OpenAsync(new Daten().AsFilePath(From.GlobalDefault));

			viewModel.FilterManually(new object[]
			{
				"@IsPinned",
				string.Empty,
			});

			await viewModel.WhenIdle();

			viewModel.VisibleItems.Count.Should().Be(512);
		}
	}
}