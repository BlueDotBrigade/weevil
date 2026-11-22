namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.IO.Compression;
	using System.Runtime.CompilerServices;
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

		// Where logs opened from zips are extracted. Not under the temporary folder, where Weevil saves no sidecar or index (#1253).
		private string _copiesFolder = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			// Weevil does not save sidecars in a subfolder of the temp folder,
			// ... so each test starts with an empty filter history that Reload cannot restore.
			_folder = System.IO.Directory.CreateTempSubdirectory("WeevilGui.").FullName;
			_copiesFolder = Path.Combine(AppContext.BaseDirectory, nameof(FilterViewModelTests), "Zips", Guid.NewGuid().ToString("N"));
		}

		[TestCleanup]
		public void Cleanup()
		{
			// The logs' locks are in the folders.
			CloseViewModels();

			System.IO.Directory.Delete(_folder, recursive: true);

			if (System.IO.Directory.Exists(_copiesFolder))
			{
				System.IO.Directory.Delete(_copiesFolder, recursive: true);
			}
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
			viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog));
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

		[TestMethod]
		public async Task GivenAnalysisInProgress_WhenAborted_ThenItStopsWithoutResultsAndTheMenusAreEnabledAgain()
		{
			// Abort (Escape) cancels an analysis: it posts no results, shows no error, and gives the menus back (#1027).
			var couldAbort = false;
			FilterViewModel viewModel = null!;

			// Pressed while the analyzer's prompt is open, so the analyzer stops at its first record.
			IDialogBoxService dialog = CreateDialog(() =>
			{
				couldAbort = viewModel.AbortFilterCommand.CanExecute(null);
				viewModel.AbortFilterCommand.Execute(null);
			});

			var bulletinMediator = Substitute.For<IBulletinMediator>();
			viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog));
			await OpenAsync(viewModel);
			bulletinMediator.ClearReceivedCalls();

			await viewModel.Analyze(AnalysisType.DetectData);

			couldAbort.Should().BeTrue("Abort is available while the analyzer runs");
			bulletinMediator.DidNotReceive().Post(Arg.Any<AnalysisCompleteBulletin>());
			viewModel.IsMenuEnabled.Should().BeTrue("the menus are enabled again once the analysis stops");
			viewModel.AbortFilterCommand.CanExecute(null).Should().BeFalse("nothing is left to abort");
		}

		[TestMethod]
		public async Task GivenOpenLog_WhenSavingTheSelectedRecords_ThenTheSaveRunsInBackgroundWhereAbortIsAvailable()
		{
			// A save reads every record, from the log itself in disk mode, so it runs in the background, where Abort (Escape) can stop it (#1028).
			using var uiDispatcher = new PausableUiDispatcher();
			var viewModel = Track(new FilterViewModel(uiDispatcher, Substitute.For<IBulletinMediator>(), Substitute.For<IDialogBoxService>()));
			await viewModel.OpenAsync(CopyLog("Save.log"));
			WaitUntil(() => viewModel.IsMenuEnabled);

			// The UI thread is busy, so the save cannot finish yet.
			uiDispatcher.Pause();
			viewModel.SaveSelectedAsRawCommand.Execute(null);

			try
			{
				viewModel.BackgroundTask.IsCompleted.Should().BeFalse("the command returns while the save runs in the background");
				viewModel.IsMenuEnabled.Should().BeFalse("the menus are disabled while the save runs");
				viewModel.AbortFilterCommand.CanExecute(null).Should().BeTrue("Abort is available while the save runs");
			}
			finally
			{
				uiDispatcher.Resume();
				await viewModel.BackgroundTask;
			}

			System.IO.File.Exists(Path.Combine(_folder, "SelectedRecords.log")).Should().BeTrue("the save ran to the end");
			viewModel.IsMenuEnabled.Should().BeTrue("the menus are enabled again once the save finishes");
		}

		[TestMethod]
		public async Task GivenOpenLog_WhenFindingText_ThenTheSearchRunsInBackgroundAndGoesToTheRecordFound()
		{
			// A search for text reads each record, from the log itself in disk mode, so it runs in the background, where Abort (Escape) can stop it (#1028).
			using var uiDispatcher = new PausableUiDispatcher();
			var dialog = Substitute.For<IDialogBoxService>();
			((IUserDialog)dialog).TryShowFind(
					Arg.Any<string>(),
					out Arg.Any<bool>(),
					out Arg.Any<bool>(),
					out Arg.Any<bool>(),
					out Arg.Any<string>(),
					out Arg.Any<bool>(),
					out Arg.Any<int?>(),
					out Arg.Any<int?>(),
					out Arg.Any<bool>())
				.Returns(x =>
				{
					x[1] = false; // case sensitive
					x[2] = true; // find next
					x[3] = false; // regular expression
					x[4] = "Holonet"; // in one record of the log, on line 233
					x[5] = false; // elapsed time
					x[6] = null;
					x[7] = null;
					x[8] = false; // comments
					return true;
				});

			var viewModel = Track(new FilterViewModel(uiDispatcher, Substitute.For<IBulletinMediator>(), dialog));
			await viewModel.OpenAsync(CopyLog("Find.log"));
			WaitUntil(() => viewModel.IsMenuEnabled);

			// The record found takes the focus, so the next F3 reaches the view: the records and menus must be back by then.
			var wereMenusEnabledWhenScrolling = false;
			viewModel.ScrollRequested += (_, _) => wereMenusEnabledWhenScrolling = viewModel.IsMenuEnabled && !viewModel.IsProcessingLongOperation;

			// The UI thread is busy, so the search cannot go to the record yet.
			uiDispatcher.Pause();
			viewModel.FindTextCommand.Execute(null);

			try
			{
				viewModel.BackgroundTask.IsCompleted.Should().BeFalse("the command returns while the search runs in the background");
				viewModel.AbortFilterCommand.CanExecute(null).Should().BeTrue("Abort is available while the search runs");
			}
			finally
			{
				uiDispatcher.Resume();
				await viewModel.BackgroundTask;
			}

			viewModel.Rows.GetRow(viewModel.ActiveRecordIndex).LineNumber.Should().Be(233, "the only record that mentions the Holonet is on line 233");
			dialog.DidNotReceiveWithAnyArgs().ShowMessage(default, default);
			wereMenusEnabledWhenScrolling.Should().BeTrue("the view goes to the record found once the records and menus are back");
			viewModel.IsMenuEnabled.Should().BeTrue("the menus are enabled again once the search finishes");
		}

		[TestMethod]
		public async Task GivenALogOnDiskThatWasDeleted_WhenItsRowsAreShown_ThenEachShowsAPlaceholderAndOneMessageIsShown()
		{
			// WPF does not catch a converter's exception, so a row that read a log on disk that was gone would end Weevil (#1028).
			var uiDispatcher = new DeferredUiDispatcherFake();
			var dialog = Substitute.For<IDialogBoxService>();
			var viewModel = Track(new FilterViewModel(uiDispatcher, Substitute.For<IBulletinMediator>(), dialog));
			var logPath = CopyLog("Deleted.log");
			await OpenOnDiskAsync(viewModel, logPath);
			uiDispatcher.RunQueued();

			System.IO.File.Delete(logPath);

			viewModel.Rows.GetRow(0).ReadContent().Should().Be(RowViewModel.UnreadableLogPlaceholder);
			viewModel.Rows.GetRow(1).ReadContent().Should().Be(RowViewModel.UnreadableLogPlaceholder);
			uiDispatcher.RunQueued();

			dialog.Received(1).ShowMessage(Arg.Is<string>(message => message.Contains("can't read the log")), "Error");
		}

		[TestMethod]
		[WorkItem(1247)]
		public async Task GivenALogOnDiskThatWasTruncated_WhenARowIsShown_ThenOneMessageAsksForCtrlF5AndNothingReloads()
		{
			// Weevil reloaded a log that changed on disk by itself, rather than leaving the analyst to choose when.
			var uiDispatcher = new DeferredUiDispatcherFake();
			var dialog = Substitute.For<IDialogBoxService>();
			var viewModel = Track(new FilterViewModel(uiDispatcher, Substitute.For<IBulletinMediator>(), dialog));
			var logPath = CopyLog("Truncated.log");
			await OpenOnDiskAsync(viewModel, logPath);
			uiDispatcher.RunQueued();
			var recordCount = viewModel.Rows.Count;
			RowViewModel lastRow = viewModel.Rows.GetRow(recordCount - 1);

			var bytes = System.IO.File.ReadAllBytes(logPath);
			System.IO.File.WriteAllBytes(logPath, bytes.AsSpan(0, bytes.Length / 2).ToArray());

			lastRow.ReadContent().Should().Be(RowViewModel.ChangedLogPlaceholder);
			lastRow.ReadContent();
			uiDispatcher.RunQueued();

			// Reload would disable the menus at once, so they show that nothing reloads until the analyst presses Ctrl+F5.
			viewModel.IsMenuEnabled.Should().BeTrue();
			viewModel.CanOpenLogFile.Should().BeTrue();
			viewModel.Rows.Count.Should().Be(recordCount);

			// Ctrl+F5 reloads. A row of the log as it was before is then ignored.
			viewModel.Reload();
			await viewModel.WhenIdle();
			lastRow.ReadContent();
			uiDispatcher.RunQueued();

			viewModel.Rows.Count.Should().BeInRange(1, recordCount - 1, "Ctrl+F5 reloaded the log, with about half of its records");
			dialog.Received(1).ShowMessage(Arg.Is<string>(message => message.Contains("Ctrl+F5")), "Log Changed");
			dialog.ReceivedWithAnyArgs(1).ShowMessage(default, default);
		}

		[TestMethod]
		[WorkItem(1245)]
		public async Task GivenAFilteredLogOnDisk_WhenRefreshed_ThenTheFilterRunsAgainInBackgroundWhereAbortIsAvailable()
		{
			// F5 re-ran the filter on the UI thread, so on disk it read the whole log there, and Escape could not stop it.
			using var uiDispatcher = new PausableUiDispatcher();
			var viewModel = Track(new FilterViewModel(uiDispatcher, Substitute.For<IBulletinMediator>(), Substitute.For<IDialogBoxService>()));
			await OpenOnDiskAsync(viewModel, CopyLog("Refresh.log"));
			var totalCount = viewModel.VisibleItems.Count;

			// The history changes only once the filter has run; the filter box changes at once.
			viewModel.FilterManually(new object[] { IncludeFilter, string.Empty });
			await viewModel.WhenIdle();
			var filteredCount = viewModel.VisibleItems.Count;
			filteredCount.Should().BeLessThan(totalCount, "the filter should hide records before F5 runs it again");

			var resultsChanged = 0;
			viewModel.ResultsChanged += (_, _) => resultsChanged++;

			// The UI thread is busy, so the filter cannot show its results yet.
			uiDispatcher.Pause();
			viewModel.RefreshCommand.Execute(null);

			try
			{
				WaitUntil(() => viewModel.IsFilterInProgress);
				viewModel.AbortFilterCommand.CanExecute(null).Should().BeTrue("F5 returns while the filter runs in the background, where Escape can stop it");
			}
			finally
			{
				uiDispatcher.Resume();
			}

			await viewModel.WhenIdle();
			resultsChanged.Should().Be(1, "F5 refreshes the results, and with them the columns' widths");
			viewModel.InclusiveFilter.Should().Be(IncludeFilter, "F5 runs the same filter again");
			viewModel.VisibleItems.Count.Should().Be(filteredCount, "the log has not changed");
		}

		[TestMethod]
		public async Task GivenALogOnDiskThatWasDeleted_WhenFiltered_ThenTheProgressBarIsHiddenAndOneMessageIsShown()
		{
			// A filter from the filter box caught only an invalid expression, so a failed read left the progress bar running (#1028).
			var dialog = Substitute.For<IDialogBoxService>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialog));
			var logPath = CopyLog("Filtered.log");
			await OpenOnDiskAsync(viewModel, logPath);

			System.IO.File.Delete(logPath);
			dialog.ClearReceivedCalls();
			viewModel.FilterManually(new object[] { IncludeFilter, string.Empty });
			await viewModel.WhenIdle();

			// The message waits for the filter to end.
			WaitUntil(() => dialog.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IDialogBoxService.ShowMessage)));

			viewModel.IsProcessingLongOperation.Should().BeFalse("the progress bar is hidden once the filter stops");
			viewModel.IsMenuEnabled.Should().BeTrue("the menus come back once the filter stops");
			dialog.Received(1).ShowMessage(Arg.Is<string>(message => message.Contains("can't read the log")), "Error");
		}

		[TestMethod]
		[WorkItem(1295)]
		public async Task GivenALogOnDiskThatWasDeleted_WhenAnalyzed_ThenOneMessageSaysTheLogCantBeRead()
		{
			// The read threw FileNotFoundException, not the log's own exception, so the analysis showed its own error rather than the log's.
			IDialogBoxService dialog = CreateDialog(() => { });
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialog));
			var logPath = CopyLog("Analyzed.log");
			await OpenOnDiskAsync(viewModel, logPath);

			System.IO.File.Delete(logPath);
			dialog.ClearReceivedCalls();
			await viewModel.Analyze(AnalysisType.DetectData);
			WaitUntil(() => viewModel.IsMenuEnabled && dialog.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IDialogBoxService.ShowMessage)));

			dialog.Received(1).ShowMessage(Arg.Is<string>(message => message.Contains("can't read the log")), "Error");
			dialog.ReceivedWithAnyArgs(1).ShowMessage(default, default);
		}

		[TestMethod]
		public async Task GivenALogOnDisk_WhenItsRecordsAreGraphed_ThenTheGraphKeepsTheirTextAfterTheLogIsDeleted()
		{
			// A graph window can outlive its log, so it copies the text of its records when it opens (#1028).
			var dialog = Substitute.For<IDialogBoxService>();

			// The graph asks through the dialog service's own prompt, not the one analyzers use (IUserDialog).
			dialog.TryGetExpressions(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>()).Returns(x =>
			{
				x[2] = @"Cpu1=(?<Cpu1>\d+\.\d)%";
				return true;
			});
			RecordList graphed = RecordList.Empty;
			dialog.When(d => d.ShowGraph(Arg.Any<RecordList>(), Arg.Any<string>(), Arg.Any<string>()))
				.Do(x => graphed = x.Arg<RecordList>());

			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialog));
			var logPath = CopyLog("Graphed.log");
			await OpenOnDiskAsync(viewModel, logPath);
			viewModel.PressKey(Key.A, ModifierKeys.Control, pageSize: 10, isOnRow: true);

			// On disk the copy runs in the background.
			viewModel.GraphDataCommand.Execute(null);
			await viewModel.BackgroundTask;
			System.IO.File.Delete(logPath);

			graphed.Count.Should().Be(viewModel.Rows.Count, "every record was selected");
			graphed[0].Content.Should().NotBeNullOrEmpty("the graph reads its own copy, not the log");
		}

		[TestMethod]
		[WorkItem(1217)]
		public void GivenALogDroppedOnTheWindow_WhenDiskIsChosenInOpenAs_ThenTheLogOpensOnDisk()
		{
			// Every way to open a log shows the Open As dialog, with the log dropped selected (decision 8 of the #1157 plan).
			var dialog = Substitute.For<IDialogBoxService>();
			OpenWhatOpenAsShows(dialog, StorageMode.Disk);
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog));
			viewModel.CanOpenLogFile.Should().BeTrue("nothing else is running");
			var droppedLog = CopyLog("Dropped.log");

			viewModel.OnFileDropped(this, new DroppedFileEventArgs(droppedLog));

			// Until the insights are ready, they still read the log on disk.
			WaitUntil(() => viewModel.IsMenuEnabled && viewModel.AreInsightsReady);

			dialog.Received(1).TryShowOpenAs(droppedLog, Arg.Any<Action<string>>(), out Arg.Any<OpenAsChoice>());
			bulletinMediator.Received(1).Post(Arg.Is<SourceFileOpenedBulletin>(bulletin => bulletin.StorageMode == StorageMode.Disk));
		}

		[TestMethod]
		public void GivenTheOpenButton_WhenALogAndDiskAreChosen_ThenTheLogOpensOnDisk()
		{
			// The Open button, Ctrl+O and the menu's Open… show one dialog, which finds the log and chooses RAM or Disk (#1215).
			var dialog = Substitute.For<IDialogBoxService>();
			ChooseInOpenAs(dialog, new OpenAsChoice(CopyLog("Chosen.log"), ExtractedFolder: null, StorageMode.Disk, Advice: null));
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog));

			viewModel.OpenLogCommand.Execute(null);
			WaitUntil(() => viewModel.IsMenuEnabled && viewModel.AreInsightsReady);

			bulletinMediator.Received(1).Post(Arg.Is<SourceFileOpenedBulletin>(bulletin =>
				Path.GetFileName(bulletin.SourceFilePath) == "Chosen.log" && bulletin.StorageMode == StorageMode.Disk));
		}

		[TestMethod]
		public void GivenTheOpenButton_WhenLinesAreChosen_ThenOnlyThoseLinesOpen()
		{
			// The part chosen in the Open As dialog is the part opened (#1214), and the status bar names it (#1219).
			var dialog = Substitute.For<IDialogBoxService>();
			ChooseInOpenAs(dialog, new OpenAsChoice(CopyLog("Lines.log"), ExtractedFolder: null, StorageMode.Ram, Advice: null, Part: LogPart.ForLines(new Range(10, 40))));
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog));

			viewModel.OpenLogCommand.Execute(null);
			WaitUntil(() => viewModel.IsMenuEnabled && viewModel.AreInsightsReady);

			bulletinMediator.Received(1).Post(Arg.Is<SourceFileOpenedBulletin>(bulletin =>
				bulletin.Part.Lines.Equals(new Range(10, 40)) && bulletin.TotalRecordCount > 0 && bulletin.TotalRecordCount <= 31));
		}

		[TestMethod]
		public void GivenAPluginsOwnOptions_WhenTheLogIsOpened_ThenTheEngineReadsItWithTheirContext()
		{
			// A plugin's options in the Open As dialog say how to read its log (#1216), as its own window's did.
			var dialog = Substitute.For<IDialogBoxService>();
			var context = new ContextDictionary { ["FileFormatVersion"] = "2" };
			ChooseInOpenAs(dialog, new OpenAsChoice(CopyLog("Vendor.log"), ExtractedFolder: null, StorageMode.Ram, Advice: null, Context: context));
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog));

			viewModel.OpenLogCommand.Execute(null);
			WaitUntil(() => viewModel.IsMenuEnabled && viewModel.AreInsightsReady);

			bulletinMediator.Received(1).Post(Arg.Is<SourceFileOpenedBulletin>(bulletin =>
				bulletin.Context.ContainsKey("FileFormatVersion") && bulletin.Context["FileFormatVersion"] == "2"));
		}

		[TestMethod]
		public void GivenTheOpenButton_WhenCancelled_ThenNothingOpensAndOpenIsAvailableAgain()
		{
			var dialog = Substitute.For<IDialogBoxService>();
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog));

			// The substitute answers false: the user cancelled.
			viewModel.OpenLogCommand.Execute(null);

			dialog.Received(1).TryShowOpenAs(Arg.Any<string>(), Arg.Any<Action<string>>(), out Arg.Any<OpenAsChoice>());
			bulletinMediator.DidNotReceive().Post(Arg.Any<SourceFileOpenedBulletin>());
			viewModel.CanOpenLogFile.Should().BeTrue();
		}

		[TestMethod]
		[WorkItem(1217)]
		public async Task GivenAnOpenLog_WhenTheOpenButtonIsPressed_ThenOpenAsStartsInTheLogsFolder()
		{
			var dialog = Substitute.For<IDialogBoxService>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialog));
			await viewModel.OpenAsync(CopyLog("Open.log"));
			WaitUntil(() => viewModel.IsMenuEnabled);

			viewModel.OpenLogCommand.Execute(null);
			WaitUntil(() => viewModel.CanOpenLogFile);

			dialog.Received(1).TryShowOpenAs(_folder, Arg.Any<Action<string>>(), out Arg.Any<OpenAsChoice>());
		}

		[TestMethod]
		[WorkItem(1217)]
		public async Task GivenALogOpenedFromAZip_WhenTheOpenButtonIsPressed_ThenOpenAsStartsInTheZip()
		{
			// The log is named by its zip, read like a folder (#1312), so its folder is the zip, not the copy Weevil reads.
			ZippedLogViewModel zipped = CreateViewModelForZip();
			var zipPath = CreateZip("Logs.zip", "A.log");

			try
			{
				await zipped.ViewModel.ShowOpenAsAsync(zipPath);
				WaitUntil(() => zipped.ViewModel.CanOpenLogFile);
				zipped.DialogBox.ClearReceivedCalls();

				zipped.ViewModel.OpenLogCommand.Execute(null);
				WaitUntil(() => zipped.ViewModel.CanOpenLogFile);

				zipped.DialogBox.Received(1).TryShowOpenAs(zipPath, Arg.Any<Action<string>>(), out Arg.Any<OpenAsChoice>());
			}
			finally
			{
				zipped.ViewModel.Exit();
			}
		}

		[TestMethod]
		[WorkItem(1217)]
		public void GivenNoOpenLog_WhenTheOpenButtonIsPressed_ThenOpenAsStartsInTheLastFolderUsed()
		{
			// The dialog itself shows the last folder used when it is given none.
			var dialog = Substitute.For<IDialogBoxService>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialog));

			viewModel.OpenLogCommand.Execute(null);

			dialog.Received(1).TryShowOpenAs(null, Arg.Any<Action<string>>(), out Arg.Any<OpenAsChoice>());
		}

		[TestMethod]
		public async Task GivenTheOpenButton_WhenALogInAZipIsChosen_ThenItsCopyIsKeptUntilAnotherLogOpens()
		{
			// The dialog extracts a log in a zip to a folder of its own. Weevil deletes the log once nothing reads it (#1162), and keeps the folder (#1253).
			var extractedFolder = System.IO.Directory.CreateTempSubdirectory("WeevilGui.Extracted.").FullName;
			var extractedLog = Path.Combine(extractedFolder, "A.log");
			System.IO.File.Copy(new Daten().AsFilePath(From.GlobalDefault), extractedLog);
			var dialog = Substitute.For<IDialogBoxService>();
			ChooseInOpenAs(dialog, new OpenAsChoice(extractedLog, extractedFolder, StorageMode.Ram, Advice: null));
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialog));

			try
			{
				viewModel.OpenLogCommand.Execute(null);
				WaitUntil(() => viewModel.IsMenuEnabled && viewModel.AreInsightsReady);
				viewModel.IsLogFileOpen.Should().BeTrue();
				System.IO.File.Exists(extractedLog).Should().BeTrue("Reload and disk mode read the log again");
				OpenWhatOpenAsShows(dialog, StorageMode.Ram);

				await viewModel.ShowOpenAsAsync(CopyLog("B.log"));

				System.IO.File.Exists(extractedLog).Should().BeFalse("nothing reads the previous log's copy any more");
				System.IO.Directory.Exists(extractedFolder).Should().BeTrue("the folder keeps what was saved beside the log for its next open (#1253)");
			}
			finally
			{
				DeleteExtractedFolder(extractedLog);
			}
		}

		[TestMethod]
		public async Task GivenAnOpenLog_WhenOpenAsIsCancelledForAnother_ThenTheOpenLogStays()
		{
			var dialog = Substitute.For<IDialogBoxService>();
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog));
			await viewModel.OpenAsync(CopyLog("Open.log"));
			WaitUntil(() => viewModel.IsMenuEnabled);
			bulletinMediator.ClearReceivedCalls();

			// The substitute answers false: the user cancelled.
			await viewModel.ShowOpenAsAsync(CopyLog("Other.log"));

			dialog.Received(1).TryShowOpenAs(Arg.Any<string>(), Arg.Any<Action<string>>(), out Arg.Any<OpenAsChoice>());
			bulletinMediator.DidNotReceive().Post(Arg.Any<SourceFileOpenedBulletin>());
			viewModel.IsLogFileOpen.Should().BeTrue("the log that was open stays open");
		}

		[TestMethod]
		[WorkItem(1254)]
		public async Task GivenAnOpenLog_WhenTheSameLogIsOpenedAgain_ThenItOpens()
		{
			// The engine shown still holds the log's lock while the new one opens, and hands it over.
			var dialogBox = Substitute.For<IDialogBoxService>();
			var openedPaths = new List<string>();
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			bulletinMediator.Post(Arg.Do<SourceFileOpenedBulletin>(bulletin => openedPaths.Add(bulletin.SourceFilePath)));
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialogBox));
			var logPath = CopyLog("A.log");
			await viewModel.OpenAsync(logPath);

			await viewModel.OpenAsync(logPath);

			dialogBox.DidNotReceive().ShowMessage(Arg.Any<string>(), Arg.Any<string>());
			openedPaths.Should().Equal(logPath, logPath);
			FluentActions.Invoking(() => Engine.UsingPath(logPath).Open())
				.Should().Throw<LogInUseException>("the log stays locked while it is open");
		}

		[TestMethod]
		[WorkItem(1254)]
		public async Task GivenALogAnotherWeevilHasOpen_WhenOpened_ThenAMessageNamesWhoHasItAndTheOpenLogStays()
		{
			var dialogBox = Substitute.For<IDialogBoxService>();
			var openedPaths = new List<string>();
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			bulletinMediator.Post(Arg.Do<SourceFileOpenedBulletin>(bulletin => openedPaths.Add(bulletin.SourceFilePath)));
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialogBox));
			var openLog = CopyLog("A.log");
			await viewModel.OpenAsync(openLog);
			var otherLog = CopyLog("B.log");
			using IEngine otherWeevil = Engine.UsingPath(otherLog).Open();

			await viewModel.OpenAsync(otherLog);

			dialogBox.Received(1).ShowMessage($"B.log is already open in Weevil by {Environment.UserName} on {Environment.MachineName}.", "Log Already Open");
			openedPaths.Should().Equal(openLog);
			viewModel.IsLogFileOpen.Should().BeTrue("the log that was open stays open");
			viewModel.IsFilterToolboxEnabled.Should().BeTrue("the open has ended");
		}

		[TestMethod]
		[WorkItem(1254)]
		public async Task GivenTheOpenButton_WhenTheLogChosenIsOpenInAnotherWeevil_ThenOpenAsRefusesItButNotTheLogOpenHere()
		{
			// Open As checks before it extracts a log from a zip, or closes: the log open here may be opened again.
			var dialogBox = Substitute.For<IDialogBoxService>();
			Action<string> checkBeforeOpening = null!;
			dialogBox.TryShowOpenAs(Arg.Any<string>(), Arg.Do<Action<string>>(check => checkBeforeOpening = check), out Arg.Any<OpenAsChoice>()).Returns(false);
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialogBox));
			var openLog = CopyLog("A.log");
			await viewModel.OpenAsync(openLog);
			var otherLog = CopyLog("B.log");
			using IEngine otherWeevil = Engine.UsingPath(otherLog).Open();

			viewModel.OpenLogCommand.Execute(null);
			WaitUntil(() => viewModel.CanOpenLogFile);

			checkBeforeOpening.Should().NotBeNull();
			FluentActions.Invoking(() => checkBeforeOpening(otherLog)).Should().Throw<LogInUseException>();
			FluentActions.Invoking(() => checkBeforeOpening(openLog)).Should().NotThrow();
			FluentActions.Invoking(() => checkBeforeOpening(CopyLog("C.log"))).Should().NotThrow();
		}

		[TestMethod]
		[WorkItem(1254)]
		public async Task GivenALogFromAZipAnotherWeevilHasOpen_WhenTheSameFileIsOpenedFromTheZip_ThenNothingIsExtractedAndItsCopyIsKept()
		{
			// Two instances share the copy of the same file of the same zip: the one refused must not delete it.
			// The Open As dialog refuses it before extracting, and shows why; the analyst then cancels.
			ZippedLogViewModel otherWeevil = CreateViewModelForZip();
			ZippedLogViewModel zipped = CreateViewModelForZip();
			var zipPath = CreateZip("Logs.zip", "A.log");
			var extractedCopy = ExtractedCopy("Logs.zip", "A.log");

			try
			{
				await otherWeevil.ViewModel.ShowOpenAsAsync(zipPath);
				WaitUntil(() => otherWeevil.ViewModel.CanOpenLogFile);

				await zipped.ViewModel.ShowOpenAsAsync(zipPath);

				zipped.OpenedPaths.Should().BeEmpty();
				zipped.ViewModel.CanOpenLogFile.Should().BeTrue("the open has ended");
				System.IO.File.Exists(extractedCopy).Should().BeTrue("the other Weevil reads it");

				otherWeevil.ViewModel.Reload();
				await otherWeevil.ViewModel.WhenIdle();
				otherWeevil.DialogBox.DidNotReceive().ShowMessage(Arg.Any<string>(), Arg.Any<string>());
				otherWeevil.ViewModel.VisibleItems.Should().NotBeNullOrEmpty();
			}
			finally
			{
				otherWeevil.ViewModel.Exit();
				zipped.ViewModel.Exit();
			}
		}

		[TestMethod]
		public async Task GivenALogOpenedOnDisk_WhenReloaded_ThenOpenAsIsNotShownAgain()
		{
			// Reload keeps the mode, and never asks again (the #986 plan).
			var dialog = Substitute.For<IDialogBoxService>();
			OpenWhatOpenAsShows(dialog, StorageMode.Disk);
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog));
			await viewModel.ShowOpenAsAsync(CopyLog("Reloaded.log"));
			WaitUntil(() => viewModel.IsMenuEnabled && viewModel.AreInsightsReady);
			bulletinMediator.ClearReceivedCalls();

			viewModel.Reload();
			await viewModel.WhenIdle();

			dialog.Received(1).TryShowOpenAs(Arg.Any<string>(), Arg.Any<Action<string>>(), out Arg.Any<OpenAsChoice>());
			bulletinMediator.Received(1).Post(Arg.Is<SourceFileOpenedBulletin>(bulletin => bulletin.StorageMode == StorageMode.Disk));
		}

		// Regression: Issue #996 - the busy overlay was shown only after a prompt closed.
		[TestMethod]
		public async Task GivenOpenLog_WhenAnalysisStarts_ThenBusyOverlayIsShownExceptDuringPrompts()
		{
			using var uiDispatcher = new PausableUiDispatcher();
			var wasOverlayShownDuringPrompt = true;
			FilterViewModel viewModel = null!;

			IDialogBoxService dialog = CreateDialog(() => wasOverlayShownDuringPrompt = viewModel.IsProcessingLongOperation);

			viewModel = Track(new FilterViewModel(uiDispatcher, Substitute.For<IBulletinMediator>(), dialog));
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

		// Regression: Issue #996 - FilterView defers closing the window until BackgroundTask completes, then Exit saves the sidecar.
		// This covers the view-model side only; the deferral in FilterView.OnWindowClosing has no automated test.
		[TestMethod]
		public async Task GivenAnalysisInProgress_WhenAnalysisFinishes_ThenBackgroundTaskCompletesAndExitIsEnabled()
		{
			using var releasePrompt = new ManualResetEventSlim();

			IDialogBoxService dialog = CreateDialog(() => releasePrompt.Wait(GateTimeout));

			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialog));
			await OpenAsync(viewModel);

			_ = viewModel.Analyze(AnalysisType.DetectData);

			try
			{
				viewModel.BackgroundTask.IsCompleted.Should().BeFalse("BackgroundTask is incomplete while the analyzer runs");
				viewModel.ExitCommand.CanExecute(null).Should().BeFalse("Exit is disabled while the analyzer is writing to the records");
			}
			finally
			{
				releasePrompt.Set();

				// FilterView continues on BackgroundTask, so Exit must be enabled by the time it completes.
				await viewModel.BackgroundTask;
			}

			viewModel.BackgroundTask.IsCompleted.Should().BeTrue("BackgroundTask completes once the analysis has finished");
			viewModel.ExitCommand.CanExecute(null).Should().BeTrue("Exit is enabled again once the analysis has finished");
		}

		// Regression: Issue #996 - a file dropped during an analysis was opened while the analyzer was still writing to the records.
		[TestMethod]
		public async Task GivenAnalysisInProgress_WhenFileDropped_ThenFileIsNotOpened()
		{
			using var releasePrompt = new ManualResetEventSlim();

			IDialogBoxService dialog = CreateDialog(() => releasePrompt.Wait(GateTimeout));

			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialog));
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
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog));
			await OpenAsync(viewModel);

			viewModel.VisibleItems.Should().NotBeEmpty();
			IRecord insightRecord = viewModel.VisibleItems[0];

			Task analysis = viewModel.Analyze(AnalysisType.DetectData);

			try
			{
				bulletinMediator.Post(new NavigateToInsightRecordBulletin(new[] { insightRecord.LineNumber }));

				viewModel.InclusiveFilter.Should().NotContain("@Flagged", "insight navigation, which flags records and shows them, waits for the analysis");
			}
			finally
			{
				releasePrompt.Set();
				await analysis;
			}
		}

		[TestMethod]
		[WorkItem(998)]
		public async Task GivenAnInsightRecordTheFilterHides_WhenGoingToIt_ThenTheFilterShowsFlaggedRecordsAndTheRecordIsActive()
		{
			// Going to an insight's record only set the filter box's text, and after 500 ms went to the nearest record still shown.
			var bulletinMediator = new BulletinMediator();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, Substitute.For<IDialogBoxService>()));

			// A copy without the sidecar, so the filter history is empty until the filter below has run.
			await viewModel.OpenAsync(CopyLog("Insight.log"));
			WaitUntil(() => viewModel.IsMenuEnabled);
			var hiddenLineNumber = viewModel.VisibleItems.First(record => !record.Content.Contains(IncludeFilter)).LineNumber;

			viewModel.FilterManually(new object[] { IncludeFilter, string.Empty });
			await viewModel.WhenIdle();
			viewModel.VisibleItems.Should().NotContain(record => record.LineNumber == hiddenLineNumber, "the filter should hide the insight's record");

			int? scrolledTo = null;
			viewModel.ScrollRequested += (_, position) => scrolledTo = position;
			bulletinMediator.Post(new NavigateToInsightRecordBulletin(new[] { hiddenLineNumber }));
			WaitUntil(() => scrolledTo.HasValue && !viewModel.IsFilterInProgress && viewModel.IsMenuEnabled);

			viewModel.InclusiveFilter.Should().Be($"{IncludeFilter}||@Flagged");
			viewModel.VisibleItems.Should().Contain(record => record.LineNumber == hiddenLineNumber, "the filter now shows flagged records");
			scrolledTo.Should().NotBeNull("Weevil goes to the insight's record");
			viewModel.Rows.GetRow(scrolledTo!.Value).LineNumber.Should().Be(hiddenLineNumber);
		}

		[TestMethod]
		[WorkItem(998)]
		public async Task GivenAnInsightRecordTheExcludeFilterHides_WhenGoingToIt_ThenWeevilSaysSoInsteadOfGoingToAnotherRecord()
		{
			// Going to an insight's record that stayed hidden went silently to the nearest record shown instead.
			var bulletinMediator = new BulletinMediator();
			var dialog = Substitute.For<IDialogBoxService>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog));
			await viewModel.OpenAsync(CopyLog("Excluded.log"));
			WaitUntil(() => viewModel.IsMenuEnabled);
			var hiddenLineNumber = viewModel.VisibleItems.First(record => !record.Content.Contains(IncludeFilter)).LineNumber;
			var exclusiveFilter = $"@Line={hiddenLineNumber}";

			viewModel.FilterManually(new object[] { IncludeFilter, exclusiveFilter });
			await viewModel.WhenIdle();

			var wasScrolled = false;
			viewModel.ScrollRequested += (_, _) => wasScrolled = true;
			bulletinMediator.Post(new NavigateToInsightRecordBulletin(new[] { hiddenLineNumber }));
			WaitUntil(() => dialog.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IDialogBoxService.ShowMessage)) && viewModel.IsMenuEnabled);

			dialog.Received(1).ShowMessage(Arg.Is<string>(message => message.Contains("exclude filter")), "Not Found");
			wasScrolled.Should().BeFalse("Weevil does not go to a record that isn't the insight's");
		}

		/// <remarks>Regression: #1011. A failed open unsubscribed the open log from its filter history changes.</remarks>
		[TestMethod]
		public async Task GivenOpeningAnotherLogFailed_WhenFilteringTheOpenLog_ThenIncludeHistoryShowsTheFilter()
		{
			var dialogBox = Substitute.For<IDialogBoxService>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialogBox));

			await viewModel.OpenAsync(CopyLog("A.log"));
			await viewModel.OpenAsync(CopyLogWithIncompatibleSidecar("B.log"));
			dialogBox.Received(1).ShowMessage(Arg.Any<string>(), Arg.Any<string>());

			viewModel.FilterManually(new object[] { IncludeFilter, string.Empty });
			await viewModel.WhenIdle();

			viewModel.InclusiveFilterHistory.Should().Equal(IncludeFilter);
		}

		/// <remarks>Regression: #1011. Reload re-applied the filter to an empty history, and the GUI added the filter a second time.</remarks>
		[TestMethod]
		public async Task GivenFilteredLogWithoutSidecar_WhenReloading_ThenIncludeHistoryShowsTheFilterOnce()
		{
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, Substitute.For<IDialogBoxService>()));

			await viewModel.OpenAsync(CopyLog("A.log"));
			viewModel.FilterManually(new object[] { IncludeFilter, string.Empty });
			await viewModel.WhenIdle();

			viewModel.Reload();
			await viewModel.WhenIdle();

			// OpenAsync posts this once, and Reload posts it only after the engine has reloaded.
			bulletinMediator.Received(2).Post(Arg.Any<SourceFileOpenedBulletin>());

			viewModel.InclusiveFilterHistory.Should().Equal(IncludeFilter);
		}

		[TestMethod]
		[WorkItem(1307)]
		public async Task GivenALogWhoseSidecarCannotBeWritten_WhenReloaded_ThenItIsReloaded()
		{
			// Reload saved the sidecar first, and failed where it couldn't, as in a read-only folder.
			var logPath = CopyLogWhereTheSidecarCannotBeWritten(out var folder);
			var dialogBox = Substitute.For<IDialogBoxService>();
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialogBox));

			try
			{
				await viewModel.OpenAsync(logPath);
				WaitUntil(() => viewModel.CanOpenLogFile);

				viewModel.Rows.GetRow(0).IsPinned = true;

				viewModel.Reload();
				await viewModel.WhenIdle();

				// Reload shows a message when it fails. It never asks: it keeps the changes in memory (#1107).
				dialogBox.DidNotReceive().ShowMessage(Arg.Any<string>(), Arg.Any<string>());
				dialogBox.DidNotReceive().ShowQuestion(Arg.Any<string>(), Arg.Any<string>());
				viewModel.IsFilterToolboxEnabled.Should().BeTrue("Reload has finished");
				viewModel.Rows.GetRow(0).IsPinned.Should().BeTrue("Reload keeps the pin");
			}
			finally
			{
				// The log's lock is in the folder.
				CloseViewModels();
				System.IO.Directory.Delete(folder, recursive: true);
			}
		}

		[TestMethod]
		[WorkItem(1307)]
		public async Task GivenALogWhoseSidecarCannotBeWrittenAndNoChange_WhenAnotherLogIsOpened_ThenItOpensWithoutAsking()
		{
			// Opening another log saved the open log's sidecar first, and failed where it couldn't.
			var logPath = CopyLogWhereTheSidecarCannotBeWritten(out var folder);
			var dialogBox = Substitute.For<IDialogBoxService>();
			var openedPaths = new List<string>();
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			bulletinMediator.Post(Arg.Do<SourceFileOpenedBulletin>(bulletin => openedPaths.Add(bulletin.SourceFilePath)));
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialogBox));

			try
			{
				await viewModel.OpenAsync(logPath);
				WaitUntil(() => viewModel.CanOpenLogFile);

				await viewModel.OpenAsync(CopyLog("Other.log"));
				WaitUntil(() => viewModel.CanOpenLogFile);

				openedPaths.Select(Path.GetFileName).Should().Equal("Application.log", "Other.log");
				dialogBox.DidNotReceive().ShowMessage(Arg.Any<string>(), Arg.Any<string>());
				dialogBox.DidNotReceive().ShowQuestion(Arg.Any<string>(), Arg.Any<string>());
			}
			finally
			{
				// The log's lock is in the folder.
				CloseViewModels();
				System.IO.Directory.Delete(folder, recursive: true);
			}
		}

		[TestMethod]
		[WorkItem(1307)]
		public async Task GivenALogWhoseSidecarCannotBeWrittenAndNoChange_WhenWeevilExits_ThenItClosesWithoutAsking()
		{
			// Exit saved the sidecar first, and threw where it couldn't.
			var logPath = CopyLogWhereTheSidecarCannotBeWritten(out var folder);
			var dialogBox = Substitute.For<IDialogBoxService>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialogBox));

			try
			{
				await viewModel.OpenAsync(logPath);
				WaitUntil(() => viewModel.CanOpenLogFile);

				viewModel.TryExit().Should().BeTrue("Weevil closes");
				dialogBox.DidNotReceive().ShowMessage(Arg.Any<string>(), Arg.Any<string>());
				dialogBox.DidNotReceive().ShowQuestion(Arg.Any<string>(), Arg.Any<string>());
			}
			finally
			{
				// The log's lock is in the folder.
				CloseViewModels();
				System.IO.Directory.Delete(folder, recursive: true);
			}
		}

		[TestMethod]
		[WorkItem(1307)]
		[DataRow(false)]
		[DataRow(true)]
		public async Task GivenAnUnsavedPinWhereTheSidecarCannotBeWritten_WhenAnotherLogIsOpened_ThenTheAnalystChoosesToStayOrGoOn(bool isContinuing)
		{
			// The pin can't be saved, so opening another log asks first: No keeps the log open, so the folder can be made writable and the log saved.
			var logPath = CopyLogWhereTheSidecarCannotBeWritten(out var folder);
			var dialogBox = Substitute.For<IDialogBoxService>();
			dialogBox.ShowQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(isContinuing);
			var openedPaths = new List<string>();
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			bulletinMediator.Post(Arg.Do<SourceFileOpenedBulletin>(bulletin => openedPaths.Add(bulletin.SourceFilePath)));
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialogBox));

			try
			{
				await viewModel.OpenAsync(logPath);
				WaitUntil(() => viewModel.CanOpenLogFile);
				viewModel.Rows.GetRow(0).IsPinned = true;

				await viewModel.OpenAsync(CopyLog("Other.log"));
				WaitUntil(() => viewModel.CanOpenLogFile);

				dialogBox.Received(1).ShowQuestion(Arg.Is<string>(message => message.Contains("Application.log")), Arg.Any<string>());
				openedPaths.Select(Path.GetFileName).Should().Equal(isContinuing
					? new[] { "Application.log", "Other.log" }
					: new[] { "Application.log" });
				viewModel.IsLogFileOpen.Should().BeTrue();
				viewModel.IsFilterToolboxEnabled.Should().BeTrue("the analyst keeps filtering the log shown");
			}
			finally
			{
				// The log's lock is in the folder.
				CloseViewModels();
				System.IO.Directory.Delete(folder, recursive: true);
			}
		}

		[TestMethod]
		[WorkItem(1307)]
		public async Task GivenAnUnsavedPinWhereTheSidecarCannotBeWritten_WhenWeevilExitsAndTheAnalystStays_ThenItDoesNotClose()
		{
			// The pin can't be saved, so closing asks first: No keeps Weevil open.
			var logPath = CopyLogWhereTheSidecarCannotBeWritten(out var folder);
			var dialogBox = Substitute.For<IDialogBoxService>();
			dialogBox.ShowQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialogBox));

			try
			{
				await viewModel.OpenAsync(logPath);
				WaitUntil(() => viewModel.CanOpenLogFile);
				viewModel.Rows.GetRow(0).IsPinned = true;

				viewModel.TryExit().Should().BeFalse("the analyst chose to stay");
				dialogBox.Received(1).ShowQuestion(Arg.Any<string>(), Arg.Any<string>());
			}
			finally
			{
				// The log's lock is in the folder.
				CloseViewModels();
				System.IO.Directory.Delete(folder, recursive: true);
			}
		}

		[TestMethod]
		[WorkItem(1126)]
		public async Task GivenAnOpenLog_WhenReloaded_ThenThePreviousRecordsHaveBeenFreed()
		{
			// The grid's rows kept the previous records while Reload read the log again, so memory held two copies of the log.
			// Records are created when they are read (#1140), so the test watches the list of records shown, which holds the log's records.
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), Substitute.For<IDialogBoxService>()));
			await viewModel.OpenAsync(CopyLog("A.log"));
			WaitUntil(() => viewModel.CanOpenLogFile);
			WeakReference previousRecords = TrackRecordsShown(viewModel);

			viewModel.Reload();
			await viewModel.WhenIdle();

			previousRecords.IsAlive.Should().BeFalse("Reload drops the rows and the engine's records before it reads the log, and opening collects them");
		}

		[TestMethod]
		public async Task GivenAnOpenLog_WhenAnotherLogIsOpened_ThenTheDashboardIsClosed()
		{
			// The dashboard reads the engine it was shown with, so it would keep the previous log in memory (#1139).
			var dialogBox = Substitute.For<IDialogBoxService>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialogBox));
			await viewModel.OpenAsync(CopyLog("A.log"));
			dialogBox.ClearReceivedCalls();

			await viewModel.OpenAsync(CopyLog("B.log"));

			dialogBox.Received(1).CloseDashboard();
		}

		[TestMethod]
		[WorkItem(1116)]
		public async Task GivenALogWithRemarks_WhenAnotherLogIsOpened_ThenTheRemarksShownAreRefreshed()
		{
			// Opening a log never told the Remarks box to refresh, so it kept showing the previous log's remarks.
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), Substitute.For<IDialogBoxService>()));
			await viewModel.OpenAsync(CopyLog("A.log"));
			viewModel.SourceFileRemarks = "Remarks about A";
			var changedProperties = new List<string>();
			viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

			await viewModel.OpenAsync(CopyLog("B.log"));

			changedProperties.Should().Contain(nameof(FilterViewModel.SourceFileRemarks));
			viewModel.SourceFileRemarks.Should().BeEmpty();
		}

		[TestMethod]
		[WorkItem(1156)]
		public async Task GivenAZip_WhenNoFileInItIsChosen_ThenNothingOpensAndTheZipIsKept()
		{
			// Cancelling the list of the zip's files opened the zip itself as a log, then deleted the zip.
			var dialogBox = Substitute.For<IDialogBoxService>();
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialogBox));
			var zipPath = CreateZip("Logs.zip", "A.log");

			// The substitute answers false: the analyst cancelled the Open As dialog shown in the zip.
			await viewModel.ShowOpenAsAsync(zipPath);

			System.IO.File.Exists(zipPath).Should().BeTrue("Weevil never deletes the zip a log comes from");
			viewModel.IsLogFileOpen.Should().BeFalse("no file was chosen");
		}

		[TestMethod]
		public async Task GivenAZip_WhenAFileInItIsChosen_ThenItsExtractedCopyOpensAndTheZipIsKept()
		{
			ZippedLogViewModel zipped = CreateViewModelForZip();
			var zipPath = CreateZip("Logs.zip", "A.log");

			try
			{
				await zipped.ViewModel.ShowOpenAsAsync(zipPath);

				zipped.ViewModel.IsLogFileOpen.Should().BeTrue("the chosen file is opened");
				zipped.OpenedPaths.Should().ContainSingle().Which.Should().Be(Path.Combine(zipPath, "A.log"), "the log is named by its zip, read like a folder (#1312)");
				var copy = ExtractedCopy("Logs.zip", "A.log");
				System.IO.File.Exists(copy).Should().BeTrue("Weevil reads an extracted copy, in a folder of its own kept between opens (#1253)");
				System.IO.File.Exists(Path.Combine(_folder, "A.log")).Should().BeFalse("Weevil never extracts a file beside the zip");
				System.IO.File.Exists(zipPath).Should().BeTrue("Weevil never deletes the zip a log comes from");
			}
			finally
			{
				zipped.ViewModel.Exit();
			}
		}

		[TestMethod]
		[WorkItem(1005)]
		public async Task GivenALogOpenedFromAZip_WhenReloaded_ThenItsRecordsAreBack()
		{
			// Weevil deleted the extracted copy right after opening it, so Reload had nothing to read.
			ZippedLogViewModel zipped = CreateViewModelForZip();

			try
			{
				await zipped.ViewModel.ShowOpenAsAsync(CreateZip("Logs.zip", "A.log"));
				WaitUntil(() => zipped.ViewModel.CanOpenLogFile);

				zipped.ViewModel.Reload();
				await zipped.ViewModel.WhenIdle();

				zipped.DialogBox.DidNotReceive().ShowMessage(Arg.Any<string>(), Arg.Any<string>());
				zipped.ViewModel.VisibleItems.Should().NotBeNullOrEmpty("Reload reads the log again from its extracted copy");
			}
			finally
			{
				zipped.ViewModel.Exit();
			}
		}

		[TestMethod]
		public async Task GivenAPinOnALogOpenedFromAZip_WhenTheSameFileIsOpenedFromTheZipAgain_ThenThePinIsBack()
		{
			// Weevil extracted a zipped log to a new temporary folder each time, and saved nothing beside it, so the analyst's work was lost (#1253).
			ZippedLogViewModel zipped = CreateViewModelForZip();
			var zipPath = CreateZip("Logs.zip", "A.log");

			try
			{
				await zipped.ViewModel.ShowOpenAsAsync(zipPath);
				WaitUntil(() => zipped.ViewModel.CanOpenLogFile);
				zipped.ViewModel.Rows.GetRow(0).IsPinned = true;

				// Opening another log saves the open one's sidecar first.
				await zipped.ViewModel.ShowOpenAsAsync(CopyLog("B.log"));
				WaitUntil(() => zipped.ViewModel.CanOpenLogFile);
				await zipped.ViewModel.ShowOpenAsAsync(zipPath);
				WaitUntil(() => zipped.ViewModel.CanOpenLogFile);

				zipped.ViewModel.Rows.GetRow(0).IsPinned.Should().BeTrue("the sidecar kept beside the copy is read again");
				System.IO.File.Exists($"{ExtractedCopy("Logs.zip", "A.log")}.idx").Should().BeTrue("the saved index is kept beside it, for the reopen to read");
			}
			finally
			{
				zipped.ViewModel.Exit();
			}
		}

		[TestMethod]
		public async Task GivenALogOpenedFromAZip_WhenTheSameFileIsChosenAgainAndOpenAsIsCancelled_ThenTheOpenLogIsStillRead()
		{
			// The same file of the same zip is extracted to the same copy, which the open log reads: cancelling deleted it (found in review of #1253).
			ZippedLogViewModel zipped = CreateViewModelForZip();
			var zipPath = CreateZip("Logs.zip", "A.log");

			try
			{
				await zipped.ViewModel.ShowOpenAsAsync(zipPath);
				WaitUntil(() => zipped.ViewModel.CanOpenLogFile);
				zipped.DialogBox.TryShowOpenAs(Arg.Any<string>(), Arg.Any<Action<string>>(), out Arg.Any<OpenAsChoice>()).Returns(false);

				await zipped.ViewModel.ShowOpenAsAsync(zipPath);
				zipped.ViewModel.Reload();
				await zipped.ViewModel.WhenIdle();

				System.IO.File.Exists(ExtractedCopy("Logs.zip", "A.log")).Should().BeTrue("the open log is read from that copy");
				zipped.DialogBox.DidNotReceive().ShowMessage(Arg.Any<string>(), Arg.Any<string>());
				zipped.ViewModel.VisibleItems.Should().NotBeNullOrEmpty("Reload reads the log again from its copy");
			}
			finally
			{
				zipped.ViewModel.Exit();
			}
		}

		[TestMethod]
		public async Task GivenALogOpenedFromAZip_WhenTheSelectedRecordsAreSaved_ThenTheFileIsSavedInTheFolderKeptForIt()
		{
			// The engine names a zipped log by its zip, which is no folder to write to: exports go beside the copy it reads (#1253, #1312).
			ZippedLogViewModel zipped = CreateViewModelForZip();

			try
			{
				await zipped.ViewModel.ShowOpenAsAsync(CreateZip("Logs.zip", "A.log"));
				WaitUntil(() => zipped.ViewModel.CanOpenLogFile);
				zipped.ViewModel.PressKey(Key.A, ModifierKeys.Control, pageSize: 10, isOnRow: true);

				zipped.ViewModel.SaveSelectedAsRawCommand.Execute(null);
				await zipped.ViewModel.BackgroundTask;

				System.IO.File.Exists(Path.Combine(Path.GetDirectoryName(ExtractedCopy("Logs.zip", "A.log")), "SelectedRecords.log")).Should().BeTrue();
				System.IO.File.Exists(Path.Combine(_folder, "SelectedRecords.log")).Should().BeFalse("Weevil writes nothing beside the zip");
			}
			finally
			{
				zipped.ViewModel.Exit();
			}
		}

		[TestMethod]
		public async Task GivenAPinOnALogOpenedFromAZip_WhenALogOfTheSameNameIsOpenedFromAnotherZip_ThenItOpensWithoutThePin()
		{
			ZippedLogViewModel zipped = CreateViewModelForZip();

			try
			{
				await zipped.ViewModel.ShowOpenAsAsync(CreateZip("Logs.zip", "A.log"));
				WaitUntil(() => zipped.ViewModel.CanOpenLogFile);
				zipped.ViewModel.Rows.GetRow(0).IsPinned = true;

				await zipped.ViewModel.ShowOpenAsAsync(CreateZip("Others.zip", "A.log"));
				WaitUntil(() => zipped.ViewModel.CanOpenLogFile);

				zipped.OpenedPaths[1].Should().Be(Path.Combine(_folder, "Others.zip", "A.log"), "the log is named by its own zip (#1312)");
				ExtractedCopy("Others.zip", "A.log").Should().NotBe(ExtractedCopy("Logs.zip", "A.log"), "another zip's file is extracted to a folder of its own");
				zipped.ViewModel.Rows.GetRow(0).IsPinned.Should().BeFalse("it is another log, whatever its name");
			}
			finally
			{
				zipped.ViewModel.Exit();
			}
		}

		[TestMethod]
		public async Task GivenALogOpenedFromAZip_WhenAnotherZipIsCancelled_ThenItsExtractedCopyIsKept()
		{
			ZippedLogViewModel zipped = CreateViewModelForZip();

			try
			{
				await zipped.ViewModel.ShowOpenAsAsync(CreateZip("Logs.zip", "A.log"));
				var extractedCopy = ExtractedCopy("Logs.zip", "A.log");
				zipped.DialogBox.TryShowOpenAs(Arg.Any<string>(), Arg.Any<Action<string>>(), out Arg.Any<OpenAsChoice>()).Returns(false);

				await zipped.ViewModel.ShowOpenAsAsync(CreateZip("Others.zip", "B.log"));

				System.IO.File.Exists(extractedCopy).Should().BeTrue("the zipped log is still the open one");
			}
			finally
			{
				zipped.ViewModel.Exit();
			}
		}

		[TestMethod]
		public async Task GivenALogOpenedFromAZip_WhenAnotherLogOpens_ThenItsExtractedCopyIsDeleted()
		{
			ZippedLogViewModel zipped = CreateViewModelForZip();
			await zipped.ViewModel.ShowOpenAsAsync(CreateZip("Logs.zip", "A.log"));
			var extractedCopy = ExtractedCopy("Logs.zip", "A.log");

			try
			{
				await zipped.ViewModel.ShowOpenAsAsync(CopyLog("B.log"));

				System.IO.File.Exists(extractedCopy).Should().BeFalse("nothing reads the previous log's copy any more");
				System.IO.File.Exists($"{extractedCopy}.idx").Should().BeTrue("the log's saved index stays for its next open (#1253)");
			}
			finally
			{
				DeleteExtractedFolder(extractedCopy);
			}
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task GivenALogOpenedFromAZip_WhenAnotherLogFailsToOpen_ThenItsExtractedCopyIsKept(bool isOtherLogZipped)
		{
			ZippedLogViewModel zipped = CreateViewModelForZip();

			try
			{
				await zipped.ViewModel.ShowOpenAsAsync(CreateZip("Logs.zip", "A.log"));
				var extractedCopy = ExtractedCopy("Logs.zip", "A.log");

				await zipped.ViewModel.ShowOpenAsAsync(isOtherLogZipped
					? CreateZipWithIncompatibleSidecar("Others.zip", "A.log")
					: CopyLogWithIncompatibleSidecar("B.log"));

				zipped.OpenedPaths.Should().ContainSingle("the other log fails to open");
				System.IO.File.Exists(extractedCopy).Should().BeTrue("the zipped log is still the open one");
			}
			finally
			{
				zipped.ViewModel.Exit();
			}
		}

		[TestMethod]
		public async Task GivenALogOpenedFromAZip_WhenWeevilExits_ThenItsExtractedCopyIsDeleted()
		{
			ZippedLogViewModel zipped = CreateViewModelForZip();
			await zipped.ViewModel.ShowOpenAsAsync(CreateZip("Logs.zip", "A.log"));
			var extractedCopy = ExtractedCopy("Logs.zip", "A.log");

			try
			{
				System.IO.File.Exists(extractedCopy).Should().BeTrue("the copy stays while its log is open");

				zipped.ViewModel.Exit();

				System.IO.File.Exists(extractedCopy).Should().BeFalse("Weevil deletes the log it extracted when it exits");
				System.IO.File.Exists($"{extractedCopy}.idx").Should().BeTrue("the log's saved index stays for its next open (#1253)");
			}
			finally
			{
				DeleteExtractedFolder(extractedCopy);
			}
		}

		[TestMethod]
		public async Task GivenAnExportBesideAZippedLog_WhenAnotherLogOpens_ThenTheExportIsKept()
		{
			ZippedLogViewModel zipped = CreateViewModelForZip();

			// The zip already holds a file by the export's name, as when a folder holding an earlier export was zipped.
			await zipped.ViewModel.ShowOpenAsAsync(CreateZip("Logs.zip", "A.log", "SelectedRecords.log"));
			var extractedCopy = ExtractedCopy("Logs.zip", "A.log");
			var export = Path.Combine(Path.GetDirectoryName(extractedCopy), "SelectedRecords.log");
			System.IO.File.WriteAllText(export, "Records the analyst saved beside the open log");

			try
			{
				await zipped.ViewModel.ShowOpenAsAsync(CopyLog("B.log"));

				System.IO.File.Exists(export).Should().BeTrue("only what Weevil extracted is deleted");
				System.IO.File.Exists(extractedCopy).Should().BeFalse("nothing reads the previous log's copy any more");
			}
			finally
			{
				DeleteExtractedFolder(extractedCopy);
			}
		}

		[TestMethod]
		[WorkItem(1071)]
		public async Task GivenABusyUiThread_WhenRecordsAreCleared_ThenClearRunsInTheBackground()
		{
			// Clear rebuilt the engine on the UI thread, which froze the window for seconds on a large log.
			using var uiDispatcher = new PausableUiDispatcher();
			var viewModel = Track(new FilterViewModel(uiDispatcher, Substitute.For<IBulletinMediator>(), Substitute.For<IDialogBoxService>()));
			await viewModel.OpenAsync(CopyLog("ClearRecords.log"));
			WaitUntil(() => viewModel.IsMenuEnabled);
			var recordCount = viewModel.VisibleItems.Count;
			viewModel.PressRow(viewModel.Rows.GetRow(10), isRightButton: false, ModifierKeys.None);

			uiDispatcher.Pause();
			Task clear = viewModel.ClearRecords(ClearOperation.BeforeSelected);

			try
			{
				viewModel.IsProcessingLongOperation.Should().BeTrue("the progress bar a filter shows is shown while Clear runs");
				clear.IsCompleted.Should().BeFalse("Clear needs the UI thread only briefly, to drop the rows and then show its results");
			}
			finally
			{
				uiDispatcher.Resume();
				await clear;
			}

			// Clear applies the filter again, and has begun it by the time it returns.
			await viewModel.WhenIdle();
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

		[TestMethod]
		public async Task GivenMoreRecordsSelectedThanGraphAllows_WhenGraphed_ThenAMessageSaysSoAndNoGraphIsShown()
		{
			// A graph keeps its records for as long as its window is open (decided 2026-10-30).
			var dialogBox = Substitute.For<IDialogBoxService>();
			FilterViewModel viewModel = await OpenWithMoreRecordsSelectedThanTheClipboardAllows(dialogBox);

			viewModel.GraphDataCommand.Execute(null);

			dialogBox.Received(1).ShowMessage(Arg.Is<string>(message => message.Contains("100,000")), "Graph");
			dialogBox.DidNotReceiveWithAnyArgs().ShowGraph(default, default, default);
		}

		private async Task<FilterViewModel> OpenWithMoreRecordsSelectedThanTheClipboardAllows(IDialogBoxService dialogBox)
		{
			var logPath = Path.Combine(_folder, "Large.log");
			System.IO.File.WriteAllLines(logPath, Enumerable.Range(1, 100_001).Select(n => $"Record {n}"));
			var viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>(), dialogBox));
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
			await viewModel.WhenIdle();

			viewModel.IsMenuEnabled.Should().BeTrue("the log file should be open before the test continues");
		}

		/// <summary>
		/// Watches the list of records shown without keeping it alive: a Debug build keeps a test method's locals until it ends.
		/// It tracks resurrection, so a list that a finalizer still holds counts as alive.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static WeakReference TrackRecordsShown(FilterViewModel viewModel)
		{
			// A list that was never shown would look freed.
			viewModel.VisibleItems.Should().NotBeNullOrEmpty("the open log's records are shown");

			return new WeakReference(viewModel.VisibleItems, trackResurrection: true);
		}

		/// <summary>
		/// Makes the Open As dialog answer with <paramref name="choice"/>, as if the user found a log and chose how to open it.
		/// </summary>
		private static void ChooseInOpenAs(IDialogBoxService dialogBox, OpenAsChoice choice)
		{
			dialogBox.TryShowOpenAs(Arg.Any<string>(), Arg.Any<Action<string>>(), out Arg.Any<OpenAsChoice>()).Returns(x =>
			{
				x[2] = choice;
				return true;
			});
		}

		/// <summary>
		/// Makes the Open As dialog open what it is shown with, in <paramref name="storageMode"/>, as the analyst does by pressing Open:
		/// a log, or the file <paramref name="fileInZip"/> of a zip, which the dialog extracts with its sidecar and index to a folder of its own (#1253).
		/// </summary>
		/// <remarks>
		/// Like the dialog, it first checks that no other Weevil has the log open (#1254). The dialog then shows why, and the analyst cancels.
		/// </remarks>
		private void OpenWhatOpenAsShows(IDialogBoxService dialogBox, StorageMode storageMode, string fileInZip = "A.log")
		{
			dialogBox.TryShowOpenAs(Arg.Any<string>(), Arg.Any<Action<string>>(), out Arg.Any<OpenAsChoice>()).Returns(x =>
			{
				var location = (string)x[0];
				var checkBeforeOpening = (Action<string>)x[1];

				try
				{
					if (!location.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
					{
						checkBeforeOpening(location);
						x[2] = new OpenAsChoice(location, ExtractedFolder: null, storageMode, Advice: null);
						return true;
					}

					var file = BlueDotBrigade.Weevil.Windows.Dialogs.Storage.Location.ForArchive(location, fileInZip);
					using var reader = new BlueDotBrigade.Weevil.Windows.Dialogs.Storage.ContainerReader();
					var destination = Path.Combine(_copiesFolder, reader.GetCopyKey(file));
					checkBeforeOpening(Path.Combine(destination, fileInZip));

					x[2] = new OpenAsChoice(reader.Extract(file, ExtractedLog.CompanionSuffixes, destination), destination, storageMode, Advice: null, DisplayPath: file.ToString());
					return true;
				}
				catch (IOException)
				{
					x[2] = null;
					return false;
				}
			});
		}

		/// <summary>
		/// Opens the log on disk, and waits until the table of contents and the insights have read it, so nothing else reads it afterwards.
		/// </summary>
		private static async Task OpenOnDiskAsync(FilterViewModel viewModel, string logPath)
		{
			await viewModel.OpenAsync(logPath, StorageMode.Disk);
			WaitUntil(() => viewModel.IsMenuEnabled && viewModel.AreInsightsReady);

			viewModel.AreInsightsReady.Should().BeTrue("the log should be open before the test continues");
		}

		private string CopyLog(string fileName)
		{
			var logPath = Path.Combine(_folder, fileName);
			System.IO.File.Copy(new Daten().AsFilePath(From.GlobalDefault), logPath);

			return logPath;
		}

		/// <summary>
		/// Copies the test log to a folder of its own, with a folder where its sidecar would go: Weevil finds no sidecar to load, and can't write one,
		/// even as an administrator, as in a read-only folder.
		/// </summary>
		/// <remarks>
		/// Not under the temporary folder, where Weevil saves no sidecar, so the tests would pass without testing anything.
		/// </remarks>
		private static string CopyLogWhereTheSidecarCannotBeWritten(out string folder)
		{
			folder = Path.Combine(AppContext.BaseDirectory, nameof(FilterViewModelTests), Guid.NewGuid().ToString("N"));
			System.IO.Directory.CreateDirectory(folder);

			var logPath = Path.Combine(folder, "Application.log");
			System.IO.File.Copy(new Daten().AsFilePath(From.GlobalDefault), logPath);
			System.IO.Directory.CreateDirectory($"{logPath}.xml");

			FileHelper.IsInTemporaryDirectory(logPath).Should().BeFalse("Weevil saves no sidecar under the temporary folder");

			return logPath;
		}

		/// <summary>
		/// Creates a view model that opens <c>A.log</c> whenever the Open As dialog is shown in a zip, and collects the path of each log it opens.
		/// </summary>
		private ZippedLogViewModel CreateViewModelForZip()
		{
			var dialogBox = Substitute.For<IDialogBoxService>();
			OpenWhatOpenAsShows(dialogBox, StorageMode.Ram);
			var openedPaths = new List<string>();
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			bulletinMediator.Post(Arg.Do<SourceFileOpenedBulletin>(bulletin => openedPaths.Add(bulletin.SourceFilePath)));

			var viewModel = Track(new FilterViewModel(this.UiDispatcher, bulletinMediator, dialogBox));

			return new ZippedLogViewModel(viewModel, dialogBox, openedPaths);
		}

		/// <summary>
		/// Creates a zip whose entries are copies of the test log, or folders when a name ends with a slash.
		/// No file is left beside the zip.
		/// </summary>
		private string CreateZip(string zipName, params string[] entryNames)
		{
			var zipPath = Path.Combine(_folder, zipName);

			using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
			{
				foreach (var entryName in entryNames)
				{
					if (entryName.EndsWith('/'))
					{
						archive.CreateEntry(entryName);
					}
					else
					{
						archive.CreateEntryFromFile(new Daten().AsFilePath(From.GlobalDefault), entryName);
					}
				}
			}

			return zipPath;
		}

		/// <summary>
		/// Creates a zip holding a copy of the test log and a sidecar from a newer version of Weevil, which stops the log opening.
		/// </summary>
		private string CreateZipWithIncompatibleSidecar(string zipName, string logName)
		{
			var logPath = CopyLogWithIncompatibleSidecar(logName);
			var zipPath = Path.Combine(_folder, zipName);

			using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
			{
				archive.CreateEntryFromFile(logPath, logName);
				archive.CreateEntryFromFile($"{logPath}.xml", $"{logName}.xml");
			}

			return zipPath;
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

		/// <summary>
		/// Deletes the folder a zip was extracted to, in case the test failed before Weevil deleted it.
		/// </summary>
		private void DeleteExtractedFolder(string extractedFilePath)
		{
			var extractedFolder = Path.GetDirectoryName(extractedFilePath);

			if (!string.IsNullOrEmpty(extractedFolder) && extractedFolder != _folder && System.IO.Directory.Exists(extractedFolder))
			{
				System.IO.Directory.Delete(extractedFolder, recursive: true);
			}
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
		/// The copy Weevil extracts a file of a zip in the test's folder to: the folder named by the file's key, in the test's copies folder (#1253).
		/// </summary>
		private string ExtractedCopy(string zipName, string entryName)
		{
			using var reader = new BlueDotBrigade.Weevil.Windows.Dialogs.Storage.ContainerReader();
			var key = reader.GetCopyKey(BlueDotBrigade.Weevil.Windows.Dialogs.Storage.Location.ForArchive(Path.Combine(_folder, zipName), entryName));

			return Path.Combine(_copiesFolder, key, entryName);
		}

		private sealed record ZippedLogViewModel(FilterViewModel ViewModel, IDialogBoxService DialogBox, List<string> OpenedPaths);

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
