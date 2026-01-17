namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Linq;
	using System.Timers;
	using System.Windows;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Gui.Analysis;
	using BlueDotBrigade.Weevil.Gui.Filter;
	using BlueDotBrigade.Weevil.Gui.IO;
	using BlueDotBrigade.Weevil.Gui.Navigation;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using Metalama.Patterns.Observability;

	/// <summary>
	/// Listens for application events, and updates the status bar as needed.
	/// </summary>
	[Observable]
	internal class StatusBarViewModel
	{
		private static readonly TimeSpan DefaultTimerPeriod = TimeSpan.FromSeconds(0.5);
		private static readonly TimeSpan DisplayMetricsDuration = TimeSpan.FromSeconds(8);

		private readonly Timer _timer;
		private readonly Stopwatch _filterChangedStopwatch;

		private readonly IUiDispatcher _uiDispatcher;

		private bool _wasFileJustOpened;
		private bool _wereStatisticsJustPublished;

		public event PropertyChangedEventHandler PropertyChanged;

		public StatusBarViewModel()
		{
			this.SourceFileDetails = new SourceFileOpenedBulletin();
			this.FilterDetails = new FilterChangedBulletin();
			this.BookmarkDetails = new BookmarksChangedBulletin();
			this.RegionDetails = new RegionsChangedBulletin();
			this.SelectionDetails = new SelectionChangedBulletin();
			this.AnalysisDetails = new AnalysisCompleteBulletin(0);
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

		public StatusBarViewModel(IUiDispatcher uiDispatcher, IBulletinMediator bulletinMediator) : this()
		{
			_uiDispatcher = uiDispatcher;

			// Note: All dependency property read and write operations must be performed by the UI dispatcher.
			bulletinMediator.Subscribe<SourceFileOpenedBulletin>(this, x => OnFileChanged(x));
			bulletinMediator.Subscribe<ClearRecordsBulletin>(this, x => OnClearOperation(x));
			bulletinMediator.Subscribe<FilterChangedBulletin>(this, x => OnFilterChanged(x));
			bulletinMediator.Subscribe<BookmarksChangedBulletin>(this, x => OnBookmarksChanged(x));
			bulletinMediator.Subscribe<RegionsChangedBulletin>(this, x => OnRegionsChanged(x));
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
				_uiDispatcher.Invoke(() =>
				{
					if (_wereStatisticsJustPublished)
					{
						_wereStatisticsJustPublished = false;
					}
					else
					{
						this.StatusMessage = this.SourceFileDetails.SourceFilePath;
					}
				});
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

		private void OnBookmarksChanged(BookmarksChangedBulletin bulletin)
		{
			_uiDispatcher.Invoke(() =>
			{
				this.BookmarkDetails = bulletin;
			});
		}

		private void OnRegionsChanged(RegionsChangedBulletin bulletin)
		{
			_uiDispatcher.Invoke(() =>
			{
				this.RegionDetails = bulletin;
			});
		}

		private void OnSelectionChanged(SelectionChangedBulletin bulletin)
		{
			_uiDispatcher.Invoke(() => this.SelectionDetails = bulletin);
		}

		private void OnAnalysisComplete(AnalysisCompleteBulletin bulletin)
		{
			_uiDispatcher.Invoke(() =>
			{
				this.AnalysisDetails = bulletin;

				if (bulletin.Data.Count > 0)
				{
					_wereStatisticsJustPublished = true;
					this.StatusMessage = string.Join("; ", bulletin.Data.Select(x => $"{x.Key}={x.Value}"));
				}
			});
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
		public SourceFileOpenedBulletin SourceFileDetails { get; private set; }

		public FilterChangedBulletin FilterDetails { get; private set; }

		public BookmarksChangedBulletin BookmarkDetails { get; private set; }

		public RegionsChangedBulletin RegionDetails { get; private set; }

		public SelectionChangedBulletin SelectionDetails { get; private set; }

		public AnalysisCompleteBulletin AnalysisDetails { get; private set; }

		public InsightChangedBulletin InsightDetails { get; private set; }

		public SoftwareDetailsBulletin SoftwareDetails { get; private set; }

		public string StatusMessage { get; private set; }

		public int TotalRecordCount { get; private set; }

		public bool TotalRecordCountChanged { get; private set; }

		public bool HasSourceFileRemarks { get; private set; }
		#endregion
	}
}