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
	using Moq;

	internal class Builder
	{
		public MainWindowViewModel GetMainWindow()
		{
			var window = new Mock<Window>();

			var uiDispatcher = new Mock<IUiDispatcher>();
			uiDispatcher
				.Setup(x => x.Invoke(It.IsAny<Action>()))
				.Callback((Action a) => a.Invoke());

			var bulletinMediator = new Mock<IBulletinMediator>();

			var viewModel = new MainWindowViewModel(
				uiDispatcher.Object, 
				window.Object, 
				bulletinMediator.Object);

			return viewModel;
		}

		public FilterResultsViewModel Get()
		{
			var window = new Mock<Window>();

			var uiDispatcher = new Mock<IUiDispatcher>();
			uiDispatcher
				.Setup(x => x.Invoke(It.IsAny<Action>()))
				.Callback((Action a) => a.Invoke());

			var bulletinMediator = new Mock<IBulletinMediator>();

			var viewModel = new FilterResultsViewModel(
				window.Object,
				uiDispatcher.Object,
				bulletinMediator.Object);

			return viewModel;
		}
	}
}
