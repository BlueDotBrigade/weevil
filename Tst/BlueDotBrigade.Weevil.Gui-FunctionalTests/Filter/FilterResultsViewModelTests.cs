namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Moq;

	[TestClass]
	public class FilterResultsViewModelTests
	{
		//[TestMethod]
		//public async Task OpenAsync()
		//{
		//	var window = new Mock<Window>();
			
		//	var uiDispatcher = new Mock<IUiDispatcher>();
		//	uiDispatcher
		//		.Setup(x => x.Invoke(It.IsAny<Action>()))
		//		.Callback((Action a) => a.Invoke());

		//	var bulletinMediator = new Mock<IBulletinMediator>();

		//	var viewModel = new FilterResultsViewModel(
		//		window.Object, 
		//		uiDispatcher.Object,
		//		bulletinMediator.Object);

		//	await viewModel.OpenAsync(InputData.GetFilePath("GenericBaseline.log"));

		//	viewModel.IsManualFilter = true;
		//	viewModel.FilterManually(new object[]
		//	{
		//		"sample-inclusive-filter",
		//		string.Empty,
		//	});

		//	var stopwatch = Stopwatch.StartNew();
		//	do
		//	{
		//		Thread.Sleep(TimeSpan.FromMilliseconds(100));
		//	} while (viewModel.IsFilterInProgress && stopwatch.Elapsed < TimeSpan.FromSeconds(5));

		//	Assert.AreEqual(512, viewModel.VisibleItems.Count);
		//}
	}
}
