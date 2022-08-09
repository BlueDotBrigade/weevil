namespace BlueDotBrigade.Weevil.Gui
{
	using TechTalk.SpecFlow;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[Binding]
	public class StatusBarSteps
	{
		private readonly TestContext _testContext;
		private readonly ScenarioContext _scenarioContext;

		private MainWindowViewModel _mainWindowViewModel;

		public StatusBarSteps(TestContext testContext, ScenarioContext scenarioContext)
		{
			_testContext = testContext;
			_scenarioContext = scenarioContext;
		}

		[Given(@"Weevil has just started")]
		public void GivenWeevilHasJustStarted()
		{
			_mainWindowViewModel = new Builder().GetMainWindow();
		}

		[When(@"`(.*)` is opened")]
		public async void WhenEmptyFile_LogIsOpened(string sourceFilePath)
		{
			await _mainWindowViewModel.CurrrentFilter.OpenAsync(sourceFilePath);
		}

		[Then(@"the record count will be (.*)")]
		public void ThenTheRecordCountWillBe(int recordCount)
		{
			Assert.AreEqual(
				recordCount,
				_mainWindowViewModel.CurrentStatus.TotalRecordCount);
		}
	}
}
