namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Input;
	using System.Windows.Threading;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Gui.Analysis;
	using BlueDotBrigade.Weevil.Gui.IO;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.IO;

	[TestClass]
	public class FilterViewModelTests : UiTestBase
	{
		private const string IncludeFilter = "Directives";

		private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

		private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(5);

		private string _folder = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			// Weevil does not save sidecars in a subfolder of the temp folder,
			// ... so each test starts with an empty filter history that Reload cannot restore.
			_folder = System.IO.Directory.CreateTempSubdirectory("WeevilGui.").FullName;
		}

		[TestCleanup]
		public void Cleanup()
		{
			System.IO.Directory.Delete(_folder, recursive: true);
		}

		// Regression: Issue #996 - running an analyzer froze the window until it finished.
		[TestMethod]
		public async Task GivenOpenLog_WhenAnalyzing_ThenAnalyzerRunsInBackgroundWithMenusDisabled()
		{
			using var releasePrompt = new ManualResetEventSlim();
			var wereMenusEnabledDuringAnalysis = true;
			FilterViewModel viewModel = null!;

			IDialogBoxService dialog = CreateDialog(() =>
			{
				wereMenusEnabledDuringAnalysis = viewModel.IsMenuEnabled;
				releasePrompt.Wait(GateTimeout);
			});

			var bulletinMediator = Substitute.For<IBulletinMediator>();
			viewModel = new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog);
			await OpenAsync(viewModel);

			Task analysis = viewModel.Analyze(AnalysisType.DetectData);

			try
			{
				analysis.IsCompleted.Should().BeFalse("Analyze returns while the analyzer waits on a background thread");
			}
			finally
			{
				releasePrompt.Set();
				await analysis;
			}

			wereMenusEnabledDuringAnalysis.Should().BeFalse("the menus are disabled while the analyzer runs");
			viewModel.IsMenuEnabled.Should().BeTrue("the menus are enabled again once the analyzer finishes");
			bulletinMediator.Received(1).Post(Arg.Is<AnalysisCompleteBulletin>(bulletin => bulletin.FlaggedRecords > 0));
		}

		// Regression: Issue #996 - the busy overlay was shown only after a prompt closed.
		[TestMethod]
		public async Task GivenOpenLog_WhenAnalysisStarts_ThenBusyOverlayIsShownExceptDuringPrompts()
		{
			using var uiDispatcher = new PausableUiDispatcher();
			var wasOverlayShownDuringPrompt = true;
			FilterViewModel viewModel = null!;

			IDialogBoxService dialog = CreateDialog(() => wasOverlayShownDuringPrompt = viewModel.IsProcessingLongOperation);

			viewModel = new FilterViewModel(uiDispatcher, Substitute.For<IBulletinMediator>(), dialog);
			await OpenAsync(viewModel);

			// The UI thread is busy, so the analyzer cannot show its prompt or finish yet.
			uiDispatcher.Pause();
			Task analysis = viewModel.Analyze(AnalysisType.DetectData);

			try
			{
				viewModel.IsProcessingLongOperation.Should().BeTrue("the busy overlay is shown as soon as the analysis starts");
			}
			finally
			{
				uiDispatcher.Resume();
				await analysis;
			}

			wasOverlayShownDuringPrompt.Should().BeFalse("the records are shown behind the analyzer's prompt");
			viewModel.IsProcessingLongOperation.Should().BeFalse("the busy overlay is hidden once the analysis finishes");
		}

		// Regression: Issue #996 - FilterView defers closing the window until AnalysisTask completes, then Exit saves the sidecar.
		// This covers the view-model side only; the deferral in FilterView.OnWindowClosing has no automated test.
		[TestMethod]
		public async Task GivenAnalysisInProgress_WhenAnalysisFinishes_ThenAnalysisTaskCompletesAndExitIsEnabled()
		{
			using var releasePrompt = new ManualResetEventSlim();

			IDialogBoxService dialog = CreateDialog(() => releasePrompt.Wait(GateTimeout));

			var viewModel = new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialog);
			await OpenAsync(viewModel);

			_ = viewModel.Analyze(AnalysisType.DetectData);

			try
			{
				viewModel.AnalysisTask.IsCompleted.Should().BeFalse("AnalysisTask is incomplete while the analyzer runs");
				viewModel.ExitCommand.CanExecute(null).Should().BeFalse("Exit is disabled while the analyzer is writing to the records");
			}
			finally
			{
				releasePrompt.Set();

				// FilterView continues on AnalysisTask, so Exit must be enabled by the time it completes.
				await viewModel.AnalysisTask;
			}

			viewModel.AnalysisTask.IsCompleted.Should().BeTrue("AnalysisTask completes once the analysis has finished");
			viewModel.ExitCommand.CanExecute(null).Should().BeTrue("Exit is enabled again once the analysis has finished");
		}

		// Regression: Issue #996 - a file dropped during an analysis was opened while the analyzer was still writing to the records.
		[TestMethod]
		public async Task GivenAnalysisInProgress_WhenFileDropped_ThenFileIsNotOpened()
		{
			using var releasePrompt = new ManualResetEventSlim();

			IDialogBoxService dialog = CreateDialog(() => releasePrompt.Wait(GateTimeout));

			var viewModel = new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialog);
			await OpenAsync(viewModel);
			viewModel.AreInsightsReady.Should().BeTrue("the log file has finished opening");

			Task analysis = viewModel.Analyze(AnalysisType.DetectData);

			try
			{
				viewModel.OnFileDropped(this, new DroppedFileEventArgs(new Daten().AsFilePath(From.GlobalDefault)));

				// Opening a file clears AreInsightsReady before its first await, so a drop that is not ignored shows up here.
				viewModel.AreInsightsReady.Should().BeTrue("a file dropped during an analysis is ignored");
			}
			finally
			{
				releasePrompt.Set();
				await analysis;
			}
		}

		// Regression: Issue #996 - an insight clicked on the dashboard during an analysis changed the flags the analyzer was setting.
		[TestMethod]
		public async Task GivenAnalysisInProgress_WhenInsightNavigationRequested_ThenFlagsAreUnchanged()
		{
			using var releasePrompt = new ManualResetEventSlim();

			IDialogBoxService dialog = CreateDialog(() => releasePrompt.Wait(GateTimeout));

			var bulletinMediator = new BulletinMediator();
			var viewModel = new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog);
			await OpenAsync(viewModel);

			viewModel.VisibleItems.Should().NotBeEmpty();
			IRecord insightRecord = viewModel.VisibleItems[0];

			Task analysis = viewModel.Analyze(AnalysisType.DetectData);

			try
			{
				bulletinMediator.Post(new NavigateToInsightRecordBulletin(ImmutableArray.Create(insightRecord)));

				viewModel.InclusiveFilter.Should().NotContain("@Flagged", "insight navigation, which flags records and shows them, waits for the analysis");
			}
			finally
			{
				releasePrompt.Set();
				await analysis;
			}
		}

		/// <remarks>Regression: #1011. A failed open unsubscribed the open log from its filter history changes.</remarks>
		[TestMethod]
		public async Task GivenOpeningAnotherLogFailed_WhenFilteringTheOpenLog_ThenIncludeHistoryShowsTheFilter()
		{
			var dialogBox = Substitute.For<IDialogBoxService>();
			var viewModel = new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialogBox);

			await viewModel.OpenAsync(CopyLog("A.log"));
			await viewModel.OpenAsync(CopyLogWithIncompatibleSidecar("B.log"));
			dialogBox.Received(1).ShowMessage(Arg.Any<string>(), Arg.Any<string>());

			viewModel.FilterManually(new object[] { IncludeFilter, string.Empty });
			WaitUntil(() => viewModel.InclusiveFilterHistory.Contains(IncludeFilter));

			viewModel.InclusiveFilterHistory.Should().Equal(IncludeFilter);
		}

		/// <remarks>Regression: #1011. Reload re-applied the filter to an empty history, and the GUI added the filter a second time.</remarks>
		[TestMethod]
		public async Task GivenFilteredLogWithoutSidecar_WhenReloading_ThenIncludeHistoryShowsTheFilterOnce()
		{
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			var viewModel = new FilterViewModel(this.UiDispatcher, bulletinMediator, Substitute.For<IDialogBoxService>());

			await viewModel.OpenAsync(CopyLog("A.log"));
			viewModel.FilterManually(new object[] { IncludeFilter, string.Empty });
			WaitUntil(() => viewModel.InclusiveFilterHistory.Contains(IncludeFilter) && viewModel.CanOpenLogFile);

			viewModel.Reload();
			WaitUntil(() => viewModel.IsFilterToolboxEnabled);

			// OpenAsync posts this once, and Reload posts it only after the engine has reloaded.
			bulletinMediator.Received(2).Post(Arg.Any<SourceFileOpenedBulletin>());

			viewModel.InclusiveFilterHistory.Should().Equal(IncludeFilter);
		}

		[TestMethod]
		[WorkItem(1116)]
		public async Task GivenALogWithRemarks_WhenAnotherLogIsOpened_ThenTheRemarksShownAreRefreshed()
		{
			// Opening a log never told the Remarks box to refresh, so it kept showing the previous log's remarks.
			var viewModel = new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), Substitute.For<IDialogBoxService>());
			await viewModel.OpenAsync(CopyLog("A.log"));
			viewModel.SourceFileRemarks = "Remarks about A";
			var changedProperties = new List<string>();
			viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

			await viewModel.OpenAsync(CopyLog("B.log"));

			changedProperties.Should().Contain(nameof(FilterViewModel.SourceFileRemarks));
			viewModel.SourceFileRemarks.Should().BeEmpty();
		}

		[TestMethod]
		[WorkItem(1071)]
		public async Task GivenABusyUiThread_WhenRecordsAreCleared_ThenClearRunsInTheBackground()
		{
			// Clear rebuilt the engine on the UI thread, which froze the window for seconds on a large log.
			using var uiDispatcher = new PausableUiDispatcher();
			var viewModel = new FilterViewModel(uiDispatcher, Substitute.For<IBulletinMediator>(), Substitute.For<IDialogBoxService>());
			await viewModel.OpenAsync(CopyLog("ClearRecords.log"));
			WaitUntil(() => viewModel.IsMenuEnabled);
			var recordCount = viewModel.VisibleItems.Count;
			viewModel.PressRow(viewModel.Rows.GetRow(10), isRightButton: false, ModifierKeys.None);

			uiDispatcher.Pause();
			Task clear = viewModel.ClearRecords(ClearOperation.BeforeSelected);

			try
			{
				viewModel.IsProcessingLongOperation.Should().BeTrue("the progress bar a filter shows is shown while Clear runs");
				clear.IsCompleted.Should().BeFalse("Clear needs the UI thread only to show its results");
			}
			finally
			{
				uiDispatcher.Resume();
				await clear;
			}

			WaitUntil(() => viewModel.CanOpenLogFile && !viewModel.IsFilterInProgress);
			viewModel.VisibleItems.Count.Should().Be(recordCount - 10);
		}

		[TestMethod]
		public async Task GivenMoreRecordsSelectedThanCopyAllows_WhenCopied_ThenAMessagePointsToExport()
		{
			var dialogBox = Substitute.For<IDialogBoxService>();
			FilterViewModel viewModel = await OpenWithMoreRecordsSelectedThanTheClipboardAllows(dialogBox);

			viewModel.ClipboardCopyRaw(readableCallstack: false);

			dialogBox.Received(1).ShowMessage(Arg.Is<string>(message => message.Contains("Export")), "Copy");
		}

		[TestMethod]
		public async Task GivenMoreRecordsSelectedThanPasteAllows_WhenPasted_ThenAMessageSaysSo()
		{
			// One comment on that many records is a mistake, and each commented record costs memory.
			var dialogBox = Substitute.For<IDialogBoxService>();
			FilterViewModel viewModel = await OpenWithMoreRecordsSelectedThanTheClipboardAllows(dialogBox);

			viewModel.ClipboardPaste(allowOverwrite: true);

			dialogBox.Received(1).ShowMessage(Arg.Is<string>(message => message.Contains("100,000")), "Paste");
		}

		private async Task<FilterViewModel> OpenWithMoreRecordsSelectedThanTheClipboardAllows(IDialogBoxService dialogBox)
		{
			var logPath = Path.Combine(_folder, "Large.log");
			System.IO.File.WriteAllLines(logPath, Enumerable.Range(1, 100_001).Select(n => $"Record {n}"));
			var viewModel = new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialogBox);
			await viewModel.OpenAsync(logPath);
			WaitUntil(() => viewModel.IsMenuEnabled);
			viewModel.PressKey(Key.A, ModifierKeys.Control, pageSize: 10, isOnRow: true);

			return viewModel;
		}

		/// <summary>
		/// Creates a dialog that answers the Detect Data prompt, after running <paramref name="prompt"/>.
		/// </summary>
		private static IDialogBoxService CreateDialog(Action prompt)
		{
			var dialog = Substitute.For<IDialogBoxService>();
			((IUserDialog)dialog).TryGetExpressions(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>()).Returns(x =>
			{
				prompt();
				x[2] = @"Cpu1=(?<Cpu1>\d+\.\d)%";
				return true;
			});

			return dialog;
		}

		private static async Task OpenAsync(FilterViewModel viewModel)
		{
			await viewModel.OpenAsync(new Daten().AsFilePath(From.GlobalDefault));

			var stopwatch = Stopwatch.StartNew();
			while (!viewModel.IsMenuEnabled && stopwatch.Elapsed < TimeSpan.FromSeconds(5))
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(100));
			}

			viewModel.IsMenuEnabled.Should().BeTrue("the log file should be open before the test continues");
		}

		private string CopyLog(string fileName)
		{
			var logPath = Path.Combine(_folder, fileName);
			System.IO.File.Copy(new Daten().AsFilePath(From.GlobalDefault), logPath);

			return logPath;
		}

		private string CopyLogWithIncompatibleSidecar(string fileName)
		{
			var logPath = CopyLog(fileName);

			// A sidecar from a newer version of Weevil: opening the log throws NotSupportedException.
			System.IO.File.WriteAllText(
				$"{logPath}.xml",
				"<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"<WeevilSidecar xmlns=\"http://schemas.datacontract.org/2004/07/BlueDotBrigade.Weevil.Configuration.Sidecar.v3\" />");

			return logPath;
		}

		private static void WaitUntil(Func<bool> condition)
		{
			var stopwatch = Stopwatch.StartNew();

			while (!condition() && stopwatch.Elapsed < WaitTimeout)
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(50));
			}
		}

		/// <summary>
		/// Mimics a UI thread that runs callbacks at once, except while it is paused.
		/// </summary>
		private sealed class PausableUiDispatcher : IUiDispatcher, IDisposable
		{
			private readonly ManualResetEventSlim _isRunning = new ManualResetEventSlim(true);

			public void Pause()
			{
				_isRunning.Reset();
			}

			public void Resume()
			{
				_isRunning.Set();
			}

			public void Invoke(Action callback)
			{
				_isRunning.Wait(GateTimeout);
				callback();
			}

			public void Invoke(Action callback, DispatcherPriority priority)
			{
				Invoke(callback);
			}

			public void BeginInvoke(Action callback, DispatcherPriority priority)
			{
				Invoke(callback);
			}

			public void Dispose()
			{
				_isRunning.Dispose();
			}
		}
	}
}
