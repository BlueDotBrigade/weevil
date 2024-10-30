namespace BlueDotBrigade.Weevil.Gui;

	using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlueDotBrigade.Weevil.Gui.IO;
using BlueDotBrigade.Weevil.Gui.Threading;
using BlueDotBrigade.Weevil.Gui;
using NSubstitute;
using BlueDotBrigade.DatenLokator.TestsTools.Configuration;
using BlueDotBrigade.DatenLokator.TestsTools;

[TestClass]
[Binding]
public class SandboxTests
{
	[TestMethod]
	public void UnitTestExample()
	{
		var uiDispatcher = Substitute.For<IUiDispatcher>();
		var bulletinMediator = Substitute.For<IBulletinMediator>();
		var viewModel = new MainWindowViewModel(uiDispatcher, bulletinMediator);

		// This works
		Assert.IsTrue(viewModel.StatusBarViewModel.TotalRecordCount == 0);
	}

	[Then("try to read ViewModel property")]
	public void ThenTryToReadViewModelProperty()
	{
		var uiDispatcher = Substitute.For<IUiDispatcher>();
		var bulletinMediator = Substitute.For<IBulletinMediator>();
		var viewModel = new MainWindowViewModel(uiDispatcher, bulletinMediator);

		// This works
		Assert.IsTrue(viewModel.StatusBarViewModel.TotalRecordCount == 0);
	}
}
