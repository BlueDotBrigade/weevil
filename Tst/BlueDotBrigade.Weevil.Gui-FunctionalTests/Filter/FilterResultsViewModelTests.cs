namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using BlueDotBrigade.DatenLokator.TestsTools;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.TestingTools;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[StaTestClass]
	public class FilterResultsViewModelTests : UiTestBase
	{
		[TestMethod]
		public async Task OpenAsync()
		{
			var window = Substitute.For<Window>();

			var bulletinMediator = Substitute.For<IBulletinMediator>();

			var viewModel = new FilterResultsViewModel(
				window,
				this.UiDispatcher,
				bulletinMediator);

			await viewModel.OpenAsync(new Daten().AsFilePath(From.GlobalDefault));

			viewModel.IsManualFilter = true;
			viewModel.FilterManually(new object[]
			{
				"sample-inclusive-filter",
				string.Empty,
			});

			var stopwatch = Stopwatch.StartNew();
			do
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(100));
			} while (viewModel.IsFilterInProgress && stopwatch.Elapsed < TimeSpan.FromSeconds(5));

			Assert.AreEqual(512, viewModel.VisibleItems.Count);
		}
	}
}
