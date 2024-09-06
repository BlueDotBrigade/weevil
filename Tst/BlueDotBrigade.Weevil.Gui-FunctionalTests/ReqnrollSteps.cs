namespace BlueDotBrigade.Weevil.Gui
{
	using System.Collections.Generic;
	using BlueDotBrigade.DatenLokator.TestsTools;
	using System.Windows;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using NSubstitute;
	using BlueDotBrigade.Weevil.Gui.Threading;

	public abstract class ReqnrollSteps
	{
		private readonly ScenarioContext _scenario;

		private readonly FilterResultsViewModel _weevil;

		protected ReqnrollSteps(ScenarioContext scenario)
		{
			_scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));

			if (_scenario.TryGetValue<FilterResultsViewModel>(nameof(FilterResultsViewModel), out var value))
			{
				_weevil = value;
			}
		}

		protected async Task Initialize()
		{
			var logFilePath = new Daten().AsFilePath(From.GlobalDefault);

			await Initialize(logFilePath);
		}

		protected async Task Initialize(string logFilePath)
		{
			var window = Substitute.For<Window>();
			var uiDispatcher = new UiDispatcherFake();
			var bulletinMediator = Substitute.For<IBulletinMediator>();

			var viewModel = new FilterResultsViewModel(
				window,
				uiDispatcher,
				bulletinMediator);

			await viewModel.OpenAsync(logFilePath);
		}

		internal FilterResultsViewModel Weevil => _scenario.Get<FilterResultsViewModel>(nameof(FilterResultsViewModel))
			?? throw new UninitializedValueException("Gherkin scenario is missing an initialization step.");
	}
}