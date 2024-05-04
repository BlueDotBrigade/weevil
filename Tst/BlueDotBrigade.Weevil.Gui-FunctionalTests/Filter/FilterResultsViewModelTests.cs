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
	public class FilterResultsViewModelTests
	{
		[TestMethod]
		public async Task OpenAsync()
		{
			var window = Substitute.For<Window>();

			var uiDispatcher = Substitute.For<IUiDispatcher>();
			uiDispatcher
				.When(x => x.Invoke(Arg.Any<Action>()))
				.Do(x => (x.Arg<Action>()).Invoke());

			var bulletinMediator = Substitute.For<IBulletinMediator>();

			var viewModel = new FilterResultsViewModel(
				window,
				uiDispatcher,
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
