namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows;
	using BlueDotBrigade.DatenLokator.TestsTools;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using BlueDotBrigade.Weevil.Gui.Threading;

	internal class Builder
	{
		public MainWindowViewModel GetMainWindow()
		{
			//var window = Substitute.For<Window>();

			//var uiDispatcher = Substitute.For<IUiDispatcher>();
			//uiDispatcher
			//	.Setup(x => x.Invoke(It.IsAny<Action>()))
			//	.Callback((Action a) => a.Invoke());

			//var bulletinMediator = Substitute.For<IBulletinMediator>();

			//var viewModel = new MainWindowViewModel(
			//	uiDispatcher.Object, 
			//	window.Object, 
			//	bulletinMediator.Object);

			//return viewModel;

			return null;
		}

		public FilterResultsViewModel Get()
		{
			//var window = Substitute.For<Window>();

			//var uiDispatcher = Substitute.For<IUiDispatcher>();
			//uiDispatcher
			//	.Setup(x => x.Invoke(It.IsAny<Action>()))
			//	.Callback((Action a) => a.Invoke());

			//var bulletinMediator = Substitute.For<IBulletinMediator>();

			//var viewModel = new FilterResultsViewModel(
			//	window.Object,
			//	uiDispatcher.Object,
			//	bulletinMediator.Object);

			//return viewModel;

			return null;
		}
	}
}
