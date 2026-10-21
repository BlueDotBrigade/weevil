namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;
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
		private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(5);

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

				viewModel.VisibleItems.Should().NotContain(record => record.Metadata.IsFlagged, "only the analyzer sets flags while it runs");
			}
			finally
			{
				releasePrompt.Set();
				await analysis;
			}
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

			public void Dispose()
			{
				_isRunning.Dispose();
			}
		}
	}
}
