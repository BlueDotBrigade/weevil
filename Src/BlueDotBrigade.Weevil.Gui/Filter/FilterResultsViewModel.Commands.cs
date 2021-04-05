﻿/*
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
	using Microsoft.Practices.Prism.Commands;
	using PostSharp.Patterns.Model;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Gui.Input;

	internal partial class FilterResultsViewModel
	{
		#region Commands: General
		[SafeForDependencyAnalysis]
		public ICommand OpenCommand => new UiBoundCommand(Open, () => !this.IsCommandExecuting);
		[SafeForDependencyAnalysis]
		public ICommand ReloadCommand => new UiBoundCommand(Reload, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand SaveStateCommand => new UiBoundCommand(SaveState, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand SaveSelectedAsRawCommand => new UiBoundCommand(() => SaveSelected(FileFormatType.Raw), () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand SaveSelectedAsTsvCommand => new UiBoundCommand(() => SaveSelected(FileFormatType.Tsv), () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ExitCommand => new UiBoundCommand(Exit, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClipboardCopyRawCommand => new UiBoundCommand(ClipboardCopyRaw, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClipboardCopyCommentCommand => new UiBoundCommand(ClipboardCopyComment, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClipboardPasteCommand => new UiBoundCommand(() => ClipboardPaste(allowOverwrite: false), () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClipboardPasteOverwriteCommand => new UiBoundCommand(() => ClipboardPaste(allowOverwrite: true), () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ShowHelpCommand => new UiBoundCommand(ShowHelp);
		[SafeForDependencyAnalysis]
		public ICommand ShowAboutCommand => new UiBoundCommand(ShowAbout);
		[SafeForDependencyAnalysis]
		public ICommand ShowFileExplorerCommand => new UiBoundCommand(ShowFileExplorer, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ShowApplicationLogFileCommand => new UiBoundCommand(ShowApplicationLogFile);
		[SafeForDependencyAnalysis]
		public ICommand SplitCurrentLogCommand => new UiBoundCommand(SplitCurrentLog, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand ForceGarbageCollectionCommand => new UiBoundCommand(ForceGarbageCollection, () => this.IsMenuEnabled);

		#endregion

		#region Commands: Filtering
		[SafeForDependencyAnalysis]
		public ICommand ClearBeforeSelectedRecordCommand => new UiBoundCommand(
			() => ClearRecords(ClearRecordsOperation.BeforeSelected), 
			() => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClearAfterSelectedRecordCommand => new UiBoundCommand(
			() => ClearRecords(ClearRecordsOperation.AfterSelected),
			() => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClearBeforeAndAfterSelectionCommand => new UiBoundCommand(
			() => ClearRecords(ClearRecordsOperation.BeforeAndAfterSelected),
			() => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClearSelectedRecordsCommand => new UiBoundCommand(
			() => ClearRecords(ClearRecordsOperation.Selected),
			() => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ClearUnselectedRecordsCommand => new UiBoundCommand(
			() => ClearRecords(ClearRecordsOperation.Unselected),
			() => this.IsMenuEnabled);
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
		public ICommand ToggleFiltersCommand => new UiBoundCommand(ToggleFilters, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand ReApplyFiltersCommand => new UiBoundCommand(ReApplyFilters, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand AbortFilterCommand => new UiBoundCommand(AbortFilter, () => this.IsFilterInProgress);

		[SafeForDependencyAnalysis]
		public ICommand ToggleFilterOptionsVisibilityCommand => new UiBoundCommand(() => this.AreFilterOptionsVisible = !this.AreFilterOptionsVisible, () => this.IsFilterToolboxEnabled);
		#endregion

		#region Commands: Navigation
		[SafeForDependencyAnalysis]
		public ICommand GoToNextPinCommand => new UiBoundCommand(GoToNextPin, () => this.IsMenuEnabled);
		[SafeForDependencyAnalysis]
		public ICommand GoToPreviousPinCommand => new UiBoundCommand(GoToPreviousPin, () => this.IsMenuEnabled);
		#endregion

		#region Commands: Analysis
		[SafeForDependencyAnalysis]
		public ICommand SaveCommentSummaryCommand => new UiBoundCommand(SaveCommentSummary, () => this.IsMenuEnabled);

		[SafeForDependencyAnalysis]
		public ICommand DetectUnresponsiveUiCommand => new UiBoundCommand(
			() => Analyze(AnalysisType.DetectUnresponsiveUi), 
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
	}
}