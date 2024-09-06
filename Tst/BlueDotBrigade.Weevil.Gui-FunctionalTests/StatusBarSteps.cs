namespace BlueDotBrigade.Weevil.Gui
{
	using BlueDotBrigade.DatenLokator.TestsTools;

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

		[Given(@"Weevil has started")]
		public void GivenWeevilHasJustStarted()
		{
			_mainWindowViewModel = new Builder().GetMainWindow();
		}

		[When(@"the user has opened the default log file")]
		public async Task TheUserHasOpenedTheDefaultLogFile()
		{
			var path = new Daten().AsFilePath(From.GlobalDefault);
			await _mainWindowViewModel.CurrrentFilter.OpenAsync(path);
		}

		[When(@"the user has opened the log file `(.*)`")]
		public async Task WhenTheUserHasOpenedTheLogFile(string sourceFilePath)
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
