/*
 * The boilerplate code contained within this file, exposes public methods to the WPF user interface.
 *
 * The `NotObservable` attribute signals to the `Metalama` AOP library, that the `PropertyChanged`
 * event will not be raised for commands.
 */
namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Windows;
	using System.Windows.Input;
	using Metalama.Patterns.Observability;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Input;
	using Prism.Commands;

	internal partial class FilterViewModel
	{
		#region Commands: General
		[NotObservable]
		public ICommand OpenLogCommand => new UiBoundCommand(OpenAsync, () => this.CanOpenLogFile);
		[NotObservable]
		public ICommand SaveLogCommand => new UiBoundCommand(SaveMetadata, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand ReloadCommand => new UiBoundCommand(Reload, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand SaveSelectedAsRawCommand => new UiBoundCommand(() => SaveSelected(FileFormatType.Raw), () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand SaveSelectedAsTsvCommand => new UiBoundCommand(() => SaveSelected(FileFormatType.Tsv), () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand ExitCommand => new UiBoundCommand(Exit, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand ClipboardCopySimpleCallStackCommand => new UiBoundCommand(
			() => ClipboardCopyRaw(true), 
			() => this.IsMenuEnabled);
		[NotObservable]
		public ICommand ClipboardCopyRawCommand => new UiBoundCommand(
			() => ClipboardCopyRaw(false), 
			() => this.IsMenuEnabled);
		[NotObservable]
		public ICommand ClipboardCopyLineNumbersCommand => new UiBoundCommand(ClipboardCopyLineNumbers, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand ClipboardCopyTimestampsCommand => new UiBoundCommand(ClipboardCopyTimestamps, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand ClipboardCopyCommentCommand => new UiBoundCommand(ClipboardCopyComment, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand ClipboardPasteCommand => new UiBoundCommand(() => ClipboardPaste(allowOverwrite: false), () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand ClipboardPasteOverwriteCommand => new UiBoundCommand(() => ClipboardPaste(allowOverwrite: true), () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand GoToCommand => new UiBoundCommand(() => GoTo(), () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand ShowHelpCommand => new UiBoundCommand(ShowHelp);
		[NotObservable]
		public ICommand ShowAboutCommand => new UiBoundCommand(ShowAbout);
		[NotObservable]
		public ICommand ShowDashboardCommand => new UiBoundCommand(ShowDashboard, () => this.IsDashboardEnabled);
		[NotObservable]
		public ICommand GraphDataCommand => new UiBoundCommand(GraphData, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand ShowFileExplorerCommand => new UiBoundCommand(ShowFileExplorer, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand ShowRegExToolCommand => new UiBoundCommand(ShowRegExTool);
		[NotObservable]
		public ICommand ShowApplicationLogFileCommand => new UiBoundCommand(ShowApplicationLogFile);
		[NotObservable]
		public ICommand SplitCurrentLogCommand => new UiBoundCommand(SplitCurrentLog, () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand ForceGarbageCollectionCommand => new UiBoundCommand(ForceGarbageCollection, () => this.IsMenuEnabled);

		#endregion

		#region Commands: Clear Records
		[NotObservable]
		public ICommand ClearBeforeSelectedRecordCommand => new UiBoundCommand(
			() => ClearRecords(ClearOperation.BeforeSelected), 
			() => this.IsMenuEnabled);

		[NotObservable]
		public ICommand ClearAfterSelectedRecordCommand => new UiBoundCommand(
			() => ClearRecords(ClearOperation.AfterSelected),
			() => this.IsMenuEnabled);

		[NotObservable]
		public ICommand ClearBeforeAndAfterSelectionCommand => new UiBoundCommand(
			() => ClearRecords(ClearOperation.BeforeAndAfterSelected),
			() => this.IsMenuEnabled);

		[NotObservable]
		public ICommand ClearBetweenSelectedRecordsCommand => new UiBoundCommand(
			() => ClearRecords(ClearOperation.BetweenSelected),
			() => this.IsMenuEnabled);

		[NotObservable]
		public ICommand ClearUnselectedRecordsCommand => new UiBoundCommand(
			() => ClearRecords(ClearOperation.Unselected),
			() => this.IsMenuEnabled);

		[NotObservable]
		public ICommand ClearBeyondRegionsCommand => new UiBoundCommand(
			() => ClearRecords(ClearOperation.BeyondRegions),
			() => this.IsMenuEnabled);
		#endregion

		#region Commands: Filtering

		[NotObservable]
		public ICommand FilterCommand => new UiBoundCommand(Filter, () => this.IsMenuEnabled);

		[NotObservable]
		public DelegateCommand<object[]> FilterOrCancelCommand => new DelegateCommand<object[]>(parameters =>
		{
			try
			{
				Log.Default.Write(
					LogSeverityType.Information,
					$"User initiated command is executing... CommandName={nameof(this.FilterOrCancelCommand)}");

				FilterOrCancel(parameters);
			}
			catch (Exception e)
			{
				var message =
					$"Unable to perform the requested operation. CommandName={nameof(this.FilterOrCancelCommand)}";
				Log.Default.Write(
					LogSeverityType.Information,
					e,
					message);
				MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		});

		[NotObservable]
		public DelegateCommand<object[]> FilterManuallyCommand => new DelegateCommand<object[]>(parameters =>
		{
			try
			{
				Log.Default.Write(
					LogSeverityType.Information,
					$"User initiated command is executing... CommandName={nameof(this.FilterManuallyCommand)}");

				FilterManually(parameters);
			}
			catch (Exception e)
			{
				var message =
					$"Unable to perform the requested operation. CommandName={nameof(this.FilterManuallyCommand)}";
				Log.Default.Write(
					LogSeverityType.Information,
					e,
					message);
				MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}, (x) => true);

		[NotObservable]
		public ICommand FilterByCommentCommand => new UiBoundCommand(FilterByComment, () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand FilterByPinnedCommand => new UiBoundCommand(FilterByPinned, () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand FilterByRegionsCommand => new UiBoundCommand(FilterByRegions, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand FilterByBookmarksCommand => new UiBoundCommand(FilterByBookmarks, () => this.IsMenuEnabled);


		[NotObservable]
		public ICommand ToggleFiltersCommand => new UiBoundCommand(ToggleFilters, () => this.IsMenuEnabled);
		
		[NotObservable]
		public ICommand RefreshCommand => new UiBoundCommand(Refresh, () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand AbortFilterCommand => new UiBoundCommand(AbortFilter, () => this.IsFilterInProgress);

		[NotObservable]
		public ICommand ToggleFilterOptionsVisibilityCommand => new UiBoundCommand(() => this.AreFilterOptionsVisible = !this.AreFilterOptionsVisible, () => this.IsFilterToolboxEnabled);
		#endregion

		#region Commands: Navigation
		[NotObservable]
		public ICommand FindTextCommand => new UiBoundCommand(() => FindText(), () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand FindNextCommand => new UiBoundCommand(() => FindNext(), () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand FindPreviousCommand => new UiBoundCommand(() => FindPrevious(), () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand GoToNextCommentCommand => new UiBoundCommand(GoToNextComment, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand GoToPreviousCommentCommand => new UiBoundCommand(GoToPreviousComment, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand GoToNextPinCommand => new UiBoundCommand(GoToNextPin, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand GoToPreviousPinCommand => new UiBoundCommand(GoToPreviousPin, () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand GoToNextFlagCommand => new UiBoundCommand(GoToNextFlag, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand GoToPreviousFlagCommand => new UiBoundCommand(GoToPreviousFlag, () => this.IsMenuEnabled);
		#endregion

		#region Commands: Analysis
		[NotObservable]
		public ICommand SaveCommentSummaryCommand => new UiBoundCommand(SaveCommentSummary, () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand MeasureElapsedTimeUiThreadCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.ElapsedTimeUiThread), 
			() => this.IsMenuEnabled);

		[NotObservable]
		public ICommand MeasureElapsedTimeCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.ElapsedTime),
			() => this.IsMenuEnabled);

                [NotObservable]
                public ICommand DetectDataCommand => new UiBoundCommand(
                        () => Analyze(AnalysisType.DetectData),
                        () => this.IsMenuEnabled);

                [NotObservable]
                public ICommand DetectStableValuesCommand => new UiBoundCommand(
                        () => Analyze(AnalysisType.DetectStableValues),
                        () => this.IsMenuEnabled);

                [NotObservable]
                public ICommand DetectFirstCommand => new UiBoundCommand(
                        () => Analyze(AnalysisType.DetectFirst),
                        () => this.IsMenuEnabled);

                [NotObservable]
                public ICommand DetectDataTransitionsCommand => new UiBoundCommand(
                        () => Analyze(AnalysisType.DetectDataTransition),
                        () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand DataTransitionsFallingEdgeCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.DetectFallingEdges),
			() => this.IsMenuEnabled);

		[NotObservable]
		public ICommand DataTransitionsRisingEdgeCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.DetectRisingEdges),
			() => this.IsMenuEnabled);

		[NotObservable]
		public ICommand DetectTemporalAnomalyCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.TemporalAnomaly),
			() => this.IsMenuEnabled);

		[NotObservable]
		public ICommand DetectRepeatingRecordsCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.DetectRepeatingRecords),
			() => this.IsMenuEnabled);

		[NotObservable]
		public ICommand CalculateStatisticsCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.Statistical),
			() => this.IsMenuEnabled);


		[NotObservable]
		public ICommand RemoveAllFlagsCommand => new UiBoundCommand(RemoveAllFlags, () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand RemoveAllCommentsCommand => new UiBoundCommand(() => RemoveComments(true), () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand RemoveSelectedCommentsCommand => new UiBoundCommand(() => RemoveComments(false), () => this.IsMenuEnabled);
		[NotObservable]
		public ICommand UnpinAllCommand => new UiBoundCommand(UnpinAll, () => this.IsMenuEnabled);

		[NotObservable]
		public DelegateCommand<object[]> CustomAnalyzerCommand => new DelegateCommand<object[]>(parameters =>
		{
			try
			{
				var customAnalyzerKey = parameters[0].ToString();

				Log.Default.Write(
					LogSeverityType.Information,
					$"A command bound to the user interface is executing. CommandName={customAnalyzerKey}");

				Analyze(customAnalyzerKey);
			}
			catch (Exception e)
			{
				var message =
					$"Unable to perform the requested operation. CommandName={nameof(this.FilterOrCancelCommand)}";
				Log.Default.Write(
					LogSeverityType.Information,
					e,
					message);
				MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		});
		#endregion

		#region Commands: Selection
		[NotObservable]
		public ICommand ToggleIsPinnedCommand => new UiBoundCommand(ToggleIsPinned, () => this.IsMenuEnabled);
		#endregion

		#region Commands: Regions of Interest
		[NotObservable]
		public ICommand AddRegionCommand => new UiBoundCommand(AddRegion, () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand RemoveRegionCommand => new UiBoundCommand(RemoveRegion, () => this.IsMenuEnabled);

		[NotObservable]
		public ICommand RemoveAllRegionsCommand => new UiBoundCommand(RemoveAllRegions, () => this.IsMenuEnabled);
		#endregion

		#region Commands: Bookmarks
		[NotObservable]
		public ICommand RemoveBookmarkCommand => new UiBoundCommand(RemoveBookmark, () => this.IsMenuEnabled);

		[NotObservable]
                public ICommand RemoveAllBookmarksCommand => new UiBoundCommand(RemoveAllBookmarks, () => this.IsMenuEnabled);

                [NotObservable]
                public ICommand SetBookmark1Command => new UiBoundCommand(() => SetBookmark(1), () => this.IsMenuEnabled);

                [NotObservable]
                public ICommand SetBookmark2Command => new UiBoundCommand(() => SetBookmark(2), () => this.IsMenuEnabled);

                [NotObservable]
                public ICommand SetBookmark3Command => new UiBoundCommand(() => SetBookmark(3), () => this.IsMenuEnabled);

                [NotObservable]
                public ICommand SetBookmark4Command => new UiBoundCommand(() => SetBookmark(4), () => this.IsMenuEnabled);

                [NotObservable]
                public ICommand SetBookmark5Command => new UiBoundCommand(() => SetBookmark(5), () => this.IsMenuEnabled);

                [NotObservable]
                public ICommand GoToBookmark1Command => new UiBoundCommand(() => GoToBookmark(1), () => this.IsMenuEnabled);

                [NotObservable]
                public ICommand GoToBookmark2Command => new UiBoundCommand(() => GoToBookmark(2), () => this.IsMenuEnabled);

                [NotObservable]
                public ICommand GoToBookmark3Command => new UiBoundCommand(() => GoToBookmark(3), () => this.IsMenuEnabled);

                [NotObservable]
                public ICommand GoToBookmark4Command => new UiBoundCommand(() => GoToBookmark(4), () => this.IsMenuEnabled);

                [NotObservable]
                public ICommand GoToBookmark5Command => new UiBoundCommand(() => GoToBookmark(5), () => this.IsMenuEnabled);
                #endregion
        }
}