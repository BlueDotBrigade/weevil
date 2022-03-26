namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.Windows;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Gui.Analysis;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using BlueDotBrigade.Weevil.Gui.Threading;

	internal class MainStatusBarViewModel : DependencyObject
	{
		private readonly IUiDispatcher _uiDispatcher;

		public static readonly DependencyProperty FileDetailsProperty = DependencyProperty.Register(
			nameof(FileDetails), 
			typeof(FileChangedBulletin),
			typeof(MainStatusBarViewModel)
		);

		public static readonly DependencyProperty FilterDetailsProperty = DependencyProperty.Register(
			nameof(FilterDetails), 
			typeof(FilterChangedBulletin),
			typeof(MainStatusBarViewModel));

		public static readonly DependencyProperty SelectionDetailsProperty = DependencyProperty.Register(
			nameof(SelectionDetails), 
			typeof(SelectionChangedBulletin),
			typeof(MainStatusBarViewModel));

		public static readonly DependencyProperty AnalysisDetailsProperty = DependencyProperty.Register(
			nameof(AnalysisDetails), 
			typeof(AnalysisCompleteBulletin),
			typeof(MainStatusBarViewModel));

		public static readonly DependencyProperty InsightDetailsProperty = DependencyProperty.Register(
			nameof(InsightDetails), 
			typeof(InsightChangedBulletin),
			typeof(MainStatusBarViewModel));

		public static readonly DependencyProperty SoftwareDetailsProperty = DependencyProperty.Register(
			nameof(SoftwareDetails), 
			typeof(SoftwareDetailsBulletin),
			typeof(MainStatusBarViewModel));

		public MainStatusBarViewModel()
		{
			this.FileDetails = new FileChangedBulletin(String.Empty, ContextDictionary.Empty, 0, false);
			this.FilterDetails = new FilterChangedBulletin(0, 0, new Dictionary<string, object>());
			this.SelectionDetails = new SelectionChangedBulletin(0, Metadata.ElapsedTimeUnknown, string.Empty);
			this.AnalysisDetails = new AnalysisCompleteBulletin(0);
			this.InsightDetails = new InsightChangedBulletin(false, 0);
			this.SoftwareDetails = new SoftwareDetailsBulletin();

			// XAML is explicitly looking for these values in the dictionary
			// ... so lets assign some default values
			this.FilterDetails.SeverityMetrics["Information"] = 0;
			this.FilterDetails.SeverityMetrics["Warnings"] = 0;
			this.FilterDetails.SeverityMetrics["Errors"] = 0;
			this.FilterDetails.SeverityMetrics["Fatals"] = 0;
		}

		public MainStatusBarViewModel(IUiDispatcher uiDispatcher, IBulletinMediator bulletinMediator)
		{
			_uiDispatcher = uiDispatcher;

			bulletinMediator.Subscribe<FileChangedBulletin>(this, x => OnFileChanged(x));
			bulletinMediator.Subscribe<FilterChangedBulletin>(this, x => OnFilterChanged(x));
			bulletinMediator.Subscribe<SelectionChangedBulletin>(this, x => OnSelectionChanged(x));
			bulletinMediator.Subscribe<AnalysisCompleteBulletin>(this, x => OnAnalysisComplete(x));
			bulletinMediator.Subscribe<InsightChangedBulletin>(this, x => OnNewInsight(x));
			bulletinMediator.Subscribe<SoftwareDetailsBulletin>(this, x => OnSoftwareDetailsReceived(x));
		}

		private void OnFileChanged(FileChangedBulletin bulletin)
		{
			_uiDispatcher.Invoke(() => this.FileDetails = bulletin);
		}

		private void OnFilterChanged(FilterChangedBulletin bulletin)
		{
			_uiDispatcher.Invoke(() => this.FilterDetails = bulletin);
		}

		private void OnSelectionChanged(SelectionChangedBulletin bulletin)
		{
			_uiDispatcher.Invoke(() => this.SelectionDetails = bulletin);
		}

		private void OnAnalysisComplete(AnalysisCompleteBulletin bulletin)
		{
			_uiDispatcher.Invoke(() => this.AnalysisDetails = bulletin);
		}
		private void OnNewInsight(InsightChangedBulletin bulletin)
		{
			_uiDispatcher.Invoke(() => this.InsightDetails = bulletin);
		}
		private void OnSoftwareDetailsReceived(SoftwareDetailsBulletin bulletin)
		{
			_uiDispatcher.Invoke(() => this.SoftwareDetails = bulletin);
		}

		public FileChangedBulletin FileDetails
		{
			get => (FileChangedBulletin)GetValue(FileDetailsProperty);
			private set => SetValue(FileDetailsProperty, value);
		}

		public FilterChangedBulletin FilterDetails
		{
			get => (FilterChangedBulletin)GetValue(FilterDetailsProperty);
			private set => SetValue(FilterDetailsProperty, value);
		}

		public SelectionChangedBulletin SelectionDetails
		{
			get => (SelectionChangedBulletin)GetValue(SelectionDetailsProperty);
			private set => SetValue(SelectionDetailsProperty, value);
		}

		public AnalysisCompleteBulletin AnalysisDetails
		{
			get => (AnalysisCompleteBulletin)GetValue(AnalysisDetailsProperty);
			private set => SetValue(AnalysisDetailsProperty, value);
		}

		public InsightChangedBulletin InsightDetails
		{
			get => (InsightChangedBulletin)GetValue(InsightDetailsProperty);
			private set => SetValue(InsightDetailsProperty, value);
		}

		public SoftwareDetailsBulletin SoftwareDetails
		{
			get => (SoftwareDetailsBulletin)GetValue(SoftwareDetailsProperty);
			private set => SetValue(SoftwareDetailsProperty, value);
		}
	}
}
