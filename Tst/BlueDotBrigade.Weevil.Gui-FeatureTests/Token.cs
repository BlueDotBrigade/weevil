namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Threading;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using NSubstitute;

	internal class Token
	{
		private MainWindowViewModel _mainViewModel;

		public Token()
		{
		}

		internal async Task OpenFileAsync(string logFilePath)
		{
			var uiDispatcher = Substitute.For<IUiDispatcher>();
			uiDispatcher
				.When(x => x.Invoke(Arg.Any<Action>()))
				.Do(callInfo => callInfo.Arg<Action>()());

			uiDispatcher
				.When(x => x.Invoke(Arg.Any<Action>(), Arg.Any<DispatcherPriority>()))
				.Do(callInfo => callInfo.Arg<Action>()());

			uiDispatcher
				.When(x => x.BeginInvoke(Arg.Any<Action>(), Arg.Any<DispatcherPriority>()))
				.Do(callInfo => callInfo.Arg<Action>()());

			var bulletinMediator = new BulletinMediator();

			_mainViewModel = new MainWindowViewModel(
				uiDispatcher,
				bulletinMediator);

			// The grid shows and focuses the row the view-model asks for.
			_mainViewModel.FilterViewModel.ScrollRequested += (_, position) => this.FocusRequestedPosition = position;

			await _mainViewModel.FilterViewModel.OpenAsync(logFilePath);
		}

		#region Input Values
		#endregion

		#region Output Values

		/// <summary>
		/// The position of the row that the filter's view-model last asked the grid to show and focus, or -1 when there is none.
		/// </summary>
		public int FocusRequestedPosition { get; private set; } = -1;

		#endregion

		#region View Models

		public FilterViewModel Filter => _mainViewModel == null
					? throw new UninitializedValueException("Gherkin scenario is missing an initialization step.")
					: _mainViewModel.FilterViewModel;

		public StatusBarViewModel StatusBar => _mainViewModel == null
			? throw new UninitializedValueException("Gherkin scenario is missing an initialization step.")
			: _mainViewModel.StatusBarViewModel;

		#endregion
	}
}
