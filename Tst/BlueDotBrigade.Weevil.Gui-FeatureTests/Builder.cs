//namespace BlueDotBrigade.Weevil.Gui
//{
//	using System;
//	using System.Collections.Generic;
//	using System.Linq;
//	using System.Text;
//	using System.Threading.Tasks;
//	using System.Windows;
//	using BlueDotBrigade.DatenLokator.TestsTools;
//	using BlueDotBrigade.Weevil.Gui.Filter;
//	using BlueDotBrigade.Weevil.Gui.Threading;
//	using NSubstitute;

//	// DELETE this class
//	internal class Builder
//	{
//		public MainWindowViewModel GetMainWindow()
//		{
//			var window = Substitute.For<Window>();

//			var uiDispatcher = Substitute.For<IUiDispatcher>();
//			uiDispatcher.Invoke(Arg.Invoke());
//			var bulletinMediator = Substitute.For<IBulletinMediator>();

//			var viewModel = new MainWindowViewModel(
//				uiDispatcher,
//				window,
//				bulletinMediator);

//			return viewModel;
//		}

//		public FilterViewModel Get()
//		{
//			var window = Substitute.For<Window>();

//			var uiDispatcher = Substitute.For<IUiDispatcher>();
//			uiDispatcher.Invoke(Arg.Invoke());

//			var bulletinMediator = Substitute.For<IBulletinMediator>();

//			var viewModel = new FilterViewModel(
//				window,
//				uiDispatcher,
//				bulletinMediator);

//			return viewModel;
//		}
//	}
//}