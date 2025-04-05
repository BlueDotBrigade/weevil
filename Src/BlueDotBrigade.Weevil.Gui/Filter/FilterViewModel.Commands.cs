/*
 * The boilerplate code contained within this file, exposes public methods to the WPF user interface.
 *
 * The `SafeForDependencyAnalysis` attribute signals to the `PostSharp` AOP library, that the `PropertyChanged`
 * event will not be raised for commands.
 */
namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Windows;
	using System.Windows.Input;
	using PostSharp.Patterns.Model;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Input;
	using Prism.Commands;

	internal partial class FilterViewModel
	{
		#region Commands: General
		[SafeForDependencyAnalysis]
		public ICommand OpenLogCommand => new UiBoundCommand(OpenAsync, () => this.CanOpenLogFile);
		[SafeForDependencyAnalysis]
		public ICommand SaveLogCommand => new UiBoundCommand(SaveMetadata, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ReloadCommand => new UiBoundCommand(Reload, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand SaveSelectedAsRawCommand => new UiBoundCommand(() => SaveSelected(FileFormatType.Raw), () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand SaveSelectedAsTsvCommand => new UiBoundCommand(() => SaveSelected(FileFormatType.Tsv), () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ExitCommand => new UiBoundCommand(Exit, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClipboardCopySimpleCallStackCommand => new UiBoundCommand(
			() => ClipboardCopyRaw(true), 
			() => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClipboardCopyRawCommand => new UiBoundCommand(
			() => ClipboardCopyRaw(false), 
			() => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClipboardCopyLineNumbersCommand => new UiBoundCommand(ClipboardCopyLineNumbers, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClipboardCopyTimestampsCommand => new UiBoundCommand(ClipboardCopyTimestamps, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClipboardCopyCommentCommand => new UiBoundCommand(ClipboardCopyComment, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClipboardPasteCommand => new UiBoundCommand(() => ClipboardPaste(allowOverwrite: false), () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClipboardPasteOverwriteCommand => new UiBoundCommand(() => ClipboardPaste(allowOverwrite: true), () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand GoToCommand => new UiBoundCommand(() => GoTo(), () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand ShowHelpCommand => new UiBoundCommand(ShowHelp);
		[SafeForDependencyAnalysis]
		public ICommand ShowAboutCommand => new UiBoundCommand(ShowAbout);
		[SafeForDependencyAnalysis]
		public ICommand ShowDashboardCommand => new UiBoundCommand(ShowDashboard, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand GraphDataCommand => new UiBoundCommand(GraphData, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ShowFileExplorerCommand => new UiBoundCommand(ShowFileExplorer, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ShowRegExToolCommand => new UiBoundCommand(ShowRegExTool);
		[SafeForDependencyAnalysis]
		public ICommand ShowApplicationLogFileCommand => new UiBoundCommand(ShowApplicationLogFile);
		[SafeForDependencyAnalysis]
		public ICommand SplitCurrentLogCommand => new UiBoundCommand(SplitCurrentLog, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand ForceGarbageCollectionCommand => new UiBoundCommand(ForceGarbageCollection, () => this.IsMenuEnabled);

		#endregion

		#region Commands: Clear Records
		[SafeForDependencyAnalysis]
		public ICommand ClearBeforeSelectedRecordCommand => new UiBoundCommand(
			() => ClearRecords(ClearOperation.BeforeSelected), 
			() => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand ClearAfterSelectedRecordCommand => new UiBoundCommand(
			() => ClearRecords(ClearOperation.AfterSelected),
			() => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand ClearBeforeAndAfterSelectionCommand => new UiBoundCommand(
			() => ClearRecords(ClearOperation.BeforeAndAfterSelected),
			() => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand ClearBetweenSelectedRecordsCommand => new UiBoundCommand(
			() => ClearRecords(ClearOperation.BetweenSelected),
			() => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand ClearSelectedRecordsCommand => new UiBoundCommand(
			() => ClearRecords(ClearOperation.Selected),
			() => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand ClearUnselectedRecordsCommand => new UiBoundCommand(
			() => ClearRecords(ClearOperation.Unselected),
			() => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand ClearBeyondRegionsCommand => new UiBoundCommand(
			() => ClearRecords(ClearOperation.BeyondRegions),
			() => this.IsMenuEnabled);
		#endregion

		#region Commands: Filtering

		[SafeForDependencyAnalysis]
		public ICommand FilterCommand => new UiBoundCommand(Filter, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
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

		[SafeForDependencyAnalysis]
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
		}, (x) => this.IsManualFilter);

		[SafeForDependencyAnalysis]
		public ICommand FilterByCommentCommand => new UiBoundCommand(FilterByComment, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand FilterByPinnedCommand => new UiBoundCommand(FilterByPinned, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand FilterByRegionsCommand => new UiBoundCommand(FilterByRegions, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand FilterByBookmarksCommand => new UiBoundCommand(FilterByBookmarks, () => this.IsMenuEnabled);


		[SafeForDependencyAnalysis]
		public ICommand ToggleFiltersCommand => new UiBoundCommand(ToggleFilters, () => this.IsMenuEnabled);
		
		[SafeForDependencyAnalysis]
		public ICommand RefreshCommand => new UiBoundCommand(Refresh, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand AbortFilterCommand => new UiBoundCommand(AbortFilter, () => this.IsFilterInProgress);

		[SafeForDependencyAnalysis]
		public ICommand ToggleFilterOptionsVisibilityCommand => new UiBoundCommand(() => this.AreFilterOptionsVisible = !this.AreFilterOptionsVisible, () => this.IsFilterToolboxEnabled);
		#endregion

		#region Commands: Navigation
		[SafeForDependencyAnalysis]
		public ICommand FindTextCommand => new UiBoundCommand(() => FindText(), () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand FindNextCommand => new UiBoundCommand(() => FindNext(), () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand FindPreviousCommand => new UiBoundCommand(() => FindPrevious(), () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand GoToNextCommentCommand => new UiBoundCommand(GoToNextComment, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand GoToPreviousCommentCommand => new UiBoundCommand(GoToPreviousComment, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand GoToNextPinCommand => new UiBoundCommand(GoToNextPin, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand GoToPreviousPinCommand => new UiBoundCommand(GoToPreviousPin, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand GoToNextFlagCommand => new UiBoundCommand(GoToNextFlag, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand GoToPreviousFlagCommand => new UiBoundCommand(GoToPreviousFlag, () => this.IsMenuEnabled);
		#endregion

		#region Commands: Analysis
		[SafeForDependencyAnalysis]
		public ICommand SaveCommentSummaryCommand => new UiBoundCommand(SaveCommentSummary, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand MeasureElapsedTimeUiThreadCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.ElapsedTimeUiThread), 
			() => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand MeasureElapsedTimeCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.ElapsedTime),
			() => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand DetectDataCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.DetectData),
			() => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand DetectDataTransitionsCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.DetectDataTransition),
			() => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand DataTransitionsFallingEdgeCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.DetectFallingEdges),
			() => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand DataTransitionsRisingEdgeCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.DetectRisingEdges),
			() => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand DetectTemporalAnomaly => new UiBoundCommand(
			() => Analyze(AnalysisType.TemporalAnomaly),
			() => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand RemoveAllFlagsCommand => new UiBoundCommand(RemoveAllFlags, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand RemoveAllCommentsCommand => new UiBoundCommand(() => RemoveComments(true), () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand RemoveSelectedCommentsCommand => new UiBoundCommand(() => RemoveComments(false), () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand UnpinAllCommand => new UiBoundCommand(UnpinAll, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
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
		[SafeForDependencyAnalysis]
		public ICommand ToggleIsPinnedCommand => new UiBoundCommand(ToggleIsPinned, () => this.IsMenuEnabled);
		#endregion

		#region Commands: Regions of Interest
		[SafeForDependencyAnalysis]
		public ICommand AddRegionCommand => new UiBoundCommand(AddRegion, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand RemoveRegionCommand => new UiBoundCommand(RemoveRegion, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand RemoveAllRegionsCommand => new UiBoundCommand(RemoveAllRegions, () => this.IsMenuEnabled);
		#endregion

		#region Commands: Bookmarks
		[SafeForDependencyAnalysis]
		public ICommand AddBookmarkCommand => new UiBoundCommand(AddBookmark, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand RemoveBookmarkCommand => new UiBoundCommand(RemoveBookmark, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand RemoveAllBookmarksCommand => new UiBoundCommand(RemoveAllBookmarks, () => this.IsMenuEnabled);
		#endregion
	}
}