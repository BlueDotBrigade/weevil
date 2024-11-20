﻿namespace BlueDotBrigade.Weevil.Gui
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
			//this.IncludeFilter = string.Empty;
			//this.ExcludeFilter = string.Empty;

			//this.FilterType = FilterType.RegularExpression;

			//this.Configuration = new Dictionary<string, object>()
			//{
			//	{ "IncludePinned", true },
			//	{ "IsCaseSensitive", true },
			//	{ "HideDebugRecords", false },
			//	{ "HideTraceRecords", false },
			//};
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

			var bulletinMediator = new BulletinMediator();

			_mainViewModel = new MainWindowViewModel(
				uiDispatcher,
				bulletinMediator);

			await _mainViewModel.FilterViewModel.OpenAsync(logFilePath);
		}

		#region Input Values
		//public string IncludeFilter { private get; set; }

		//public string ExcludeFilter { private get; set; }

		//public FilterType FilterType { get; set; }

		//public Dictionary<string, object> Configuration { get; set; }

		//public FilterCriteria FilterCriteria => new FilterCriteria(
		//	this.IncludeFilter,
		//	this.ExcludeFilter,
		//	this.Configuration);
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
