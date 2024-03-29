﻿namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Timers;
	using System.Windows;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Gui.Analysis;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using BlueDotBrigade.Weevil.Gui.IO;
	using BlueDotBrigade.Weevil.Gui.Threading;

	/// <summary>
	/// Listens for application events, and updates the status bar as needed.
	/// </summary>
	internal class MainStatusBarViewModel : DependencyObject
	{
		private static readonly TimeSpan DefaultTimerPeriod = TimeSpan.FromSeconds(0.5);
		private static readonly TimeSpan DisplayMetricsDuration = TimeSpan.FromSeconds(8);

		#region  Dependency Properties
		public static readonly DependencyProperty SourceFileDetailsProperty = DependencyProperty.Register(
			nameof(SourceFileDetails),
			typeof(SourceFileOpenedBulletin),
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

		public static readonly DependencyProperty StatusMessageProperty = DependencyProperty.Register(
			nameof(StatusMessage),
			typeof(string),
			typeof(MainStatusBarViewModel));

		public static readonly DependencyProperty TotalRecordCountProperty = DependencyProperty.Register(
			nameof(TotalRecordCount),
			typeof(int),
			typeof(MainStatusBarViewModel));

		public static readonly DependencyProperty TotalRecordCountChangedProperty = DependencyProperty.Register(
			nameof(TotalRecordCountChanged),
			typeof(bool),
			typeof(MainStatusBarViewModel));

		public static readonly DependencyProperty HasSourceFileRemarksProperty = DependencyProperty.Register(
			nameof(HasSourceFileRemarks),
			typeof(bool),
			typeof(MainStatusBarViewModel));
		#endregion

		private readonly Timer _timer;
		private readonly Stopwatch _filterChangedStopwatch;

		private readonly IUiDispatcher _uiDispatcher;

		private bool _wasFileJustOpened;

		public MainStatusBarViewModel()
		{
			this.SourceFileDetails = new SourceFileOpenedBulletin();
			this.FilterDetails = new FilterChangedBulletin();
			this.SelectionDetails = new SelectionChangedBulletin();
			this.AnalysisDetails = new AnalysisCompleteBulletin();
			this.InsightDetails = new InsightChangedBulletin();
			this.SoftwareDetails = new SoftwareDetailsBulletin();

			// XAML is explicitly looking for these values in the dictionary
			// ... so lets assign some default values
			this.FilterDetails.SeverityMetrics["Information"] = 0;
			this.FilterDetails.SeverityMetrics["Warnings"] = 0;
			this.FilterDetails.SeverityMetrics["Errors"] = 0;
			this.FilterDetails.SeverityMetrics["Fatals"] = 0;

			this.HasSourceFileRemarks = false;

			_filterChangedStopwatch = new Stopwatch();

			_timer = new Timer
			{
				Interval = DefaultTimerPeriod.TotalMilliseconds, 
				AutoReset = true, 
				Enabled = false,
			};
			_timer.Elapsed += OnTimerElapsed;
			_timer.Start();
		}

		public MainStatusBarViewModel(IUiDispatcher uiDispatcher, IBulletinMediator bulletinMediator) : this()
		{
			_uiDispatcher = uiDispatcher;

			// Note: All dependency property read and write operations must be performed by the UI dispatcher.
			bulletinMediator.Subscribe<SourceFileOpenedBulletin>(this, x => OnFileChanged(x));
			bulletinMediator.Subscribe<ClearRecordsBulletin>(this, x => OnClearOperation(x));
			bulletinMediator.Subscribe<FilterChangedBulletin>(this, x => OnFilterChanged(x));
			bulletinMediator.Subscribe<SelectionChangedBulletin>(this, x => OnSelectionChanged(x));
			bulletinMediator.Subscribe<AnalysisCompleteBulletin>(this, x => OnAnalysisComplete(x));
			bulletinMediator.Subscribe<InsightChangedBulletin>(this, x => OnNewInsight(x));
			bulletinMediator.Subscribe<SoftwareDetailsBulletin>(this, x => OnSoftwareDetailsReceived(x));
			bulletinMediator.Subscribe<SourceFileRemarksChangedBulletin>(this, x => OnFileRemarksChanged(x));
		}

		#region Event Handlers
		private void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			if (_filterChangedStopwatch.Elapsed >= DisplayMetricsDuration)
			{
				_filterChangedStopwatch.Reset();
				_uiDispatcher.Invoke(() => this.StatusMessage = this.SourceFileDetails.SourceFilePath);
			}
		}

		private void OnFileChanged(SourceFileOpenedBulletin bulletin)
		{
			_wasFileJustOpened = true;

			_uiDispatcher.Invoke(() =>
			{
				this.SourceFileDetails = bulletin;
				this.StatusMessage = bulletin.SourceFilePath;
				this.TotalRecordCount = bulletin.TotalRecordCount;
				this.TotalRecordCountChanged = false;

				this.StatusMessage = $"Disk Loading Period: {bulletin.SourceFileLoadingPeriod.ToHumanReadable()}";
			});

			_filterChangedStopwatch.Restart();
		}

		private void OnClearOperation(ClearRecordsBulletin clearRecordsBulletin)
		{
			_uiDispatcher.Invoke(() =>
			{
				this.TotalRecordCount = clearRecordsBulletin.TotalRecordCount;
				this.TotalRecordCountChanged = true;
			});
		}

		private void OnFilterChanged(FilterChangedBulletin bulletin)
		{
			_uiDispatcher.Invoke(() =>
			{
				if (_wasFileJustOpened)
				{
					this.StatusMessage = $"Disk Loading Period: {this.SourceFileDetails.SourceFileLoadingPeriod.ToHumanReadable()}, ";
					this.StatusMessage += $"Filter duration: {bulletin.ExecutionTime.ToHumanReadable()}";
				}
				else
				{
					this.StatusMessage = $"Filter duration: {bulletin.ExecutionTime.ToHumanReadable()}";
				}

				this.FilterDetails = bulletin;
			});

			_wasFileJustOpened = false;
			_filterChangedStopwatch.Restart();
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

		private void OnFileRemarksChanged(SourceFileRemarksChangedBulletin bulletin)
		{
			_uiDispatcher.Invoke(() => this.HasSourceFileRemarks = bulletin.HasSourceFileRemarks);
		}
		#endregion

		#region Properties
		public SourceFileOpenedBulletin SourceFileDetails
		{
			get => (SourceFileOpenedBulletin)GetValue(SourceFileDetailsProperty);
			private set => SetValue(SourceFileDetailsProperty, value);
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

		public string StatusMessage
		{
			get => (string)GetValue(StatusMessageProperty);
			private set => SetValue(StatusMessageProperty, value);
		}

		public int TotalRecordCount
		{
			get => (int)GetValue(TotalRecordCountProperty);
			private set => SetValue(TotalRecordCountProperty, value);
		}

		public bool TotalRecordCountChanged
		{
			get => (bool)GetValue(TotalRecordCountChangedProperty);
			private set => SetValue(TotalRecordCountChangedProperty, value);
		}

		public bool HasSourceFileRemarks
		{
			get => (bool)GetValue(HasSourceFileRemarksProperty);
			private set => SetValue(HasSourceFileRemarksProperty, value);
		}
		#endregion
	}
}
