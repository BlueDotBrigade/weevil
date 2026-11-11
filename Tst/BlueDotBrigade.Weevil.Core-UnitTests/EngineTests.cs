namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Navigation;
	using NSubstitute;
	using IUserDialog = BlueDotBrigade.Weevil.IO.IUserDialog;
	using LogFileChangedException = BlueDotBrigade.Weevil.IO.LogFileChangedException;
	using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

	[TestClass]
	public class EngineTests
	{
		private string _folder = string.Empty;

		public TestContext TestContext { get; set; } = null!;

		[TestInitialize]
		public void Setup()
		{
			// Clear saves the sidecar, so each test opens its own copy of the log.
			_folder = Directory.CreateTempSubdirectory("WeevilEngine.").FullName;
		}

		[TestCleanup]
		public void Cleanup()
		{
			Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		public void GivenSubscribedToFilterResults_WhenFilteringAfterClear_ThenEngineRaisesTheEvent()
		{
			IEngine engine = OpenCopy();
			var results = new List<int>();
			engine.FilterResultsChanged += (sender, e) => results.Add(e.Records.Length);

			engine.Selector.Select(lineNumber: 101);
			engine.Clear(ClearOperation.BeforeSelected);
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));

			results.Should().ContainSingle()
				.Which.Should().Be(engine.Filter.Results.Length);
		}

		[TestMethod]
		public void GivenSubscribedToTheFiltersOwnEvent_WhenFilteringAfterClear_ThenTheFilterStillRaisesIt()
		{
			// Clear keeps the engine's filter (#1018), so a subscription to the filter itself survives.
			IEngine engine = OpenCopy();
			IFilter filter = engine.Filter;
			var results = new List<int>();
			filter.ResultsChanged += (sender, e) => results.Add(e.Records.Length);

			engine.Selector.Select(lineNumber: 101);
			engine.Clear(ClearOperation.BeforeSelected);
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));

			engine.Filter.Should().BeSameAs(filter);
			results.Should().EndWith(engine.Filter.Results.Length);
		}

		[TestMethod]
		public void GivenSubscribedToTheFiltersOwnEvent_WhenClearKeepsASelectedRecord_ThenTheSelectionAlreadyMatchesTheResults()
		{
			// Listeners read the selection that matches the new results (#1015), when Clear resets the filter too.
			IEngine engine = OpenCopy();
			engine.Selector.Select(lineNumber: 101);
			var selectedWhenNotified = new List<int>();
			engine.Filter.ResultsChanged += (sender, e) =>
				selectedWhenNotified.AddRange(engine.Selector.GetSelected().Select(record => record.LineNumber));

			engine.Clear(ClearOperation.BeforeSelected);

			selectedWhenNotified.Should().Equal(101);
		}

		[TestMethod]
		public void GivenAFilteredSelectionAcrossAGapBetweenRegions_WhenClearedBeyondRegions_ThenOnlyTheSelectedRecordsKeptStaySelected()
		{
			// Lines 7 and 9 are cleared, and lines 4 and 6, which the filter hid, were never selected.
			IEngine engine = OpenCopy();
			engine.Regions.CreateFromSelection("First", new[] { 3, 6 });
			engine.Regions.CreateFromSelection("Second", new[] { 11, 12 });
			engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(@"Id=0(03|05|07|09|11)$"));
			engine.Selector.SelectAll();

			engine.Clear(ClearOperation.BeyondRegions);

			engine.Selector.GetSelected().Select(record => record.LineNumber).Should().Equal(3, 5, 11);
		}

		[TestMethod]
		public void GivenRecordsSelectedTogetherAndApart_WhenTheRecordsBeforeThemAreCleared_ThenTheyStaySelected()
		{
			IEngine engine = OpenCopy();
			engine.Selector.SelectOnly(5, 6).Select(lineNumber: 8);

			engine.Clear(ClearOperation.BeforeSelected);

			engine.Selector.GetSelected().Select(record => record.LineNumber).Should().Equal(5, 6, 8);
		}

		[TestMethod]
		public void GivenAnActiveRecordButNoSelectedRecord_WhenCleared_ThenNavigationStartsFromTheTop()
		{
			// A cleared engine starts as a new one did, with no active record.
			IEngine engine = OpenCopy();
			engine.RecordState.SetPinned(engine.Records.First(record => record.LineNumber == 2), true);
			engine.RecordState.SetPinned(engine.Records.First(record => record.LineNumber == 20), true);
			engine.Selector.Select(lineNumber: 10);
			engine.Selector.Unselect(lineNumber: 10);

			engine.Clear(ClearOperation.Selected);

			engine.Navigate.NextPin().LineNumber.Should().Be(2);
		}

		[TestMethod]
		public void GivenSubscribedToFilterResults_WhenReloading_ThenEngineRaisesTheEventForTheReappliedFilter()
		{
			IEngine engine = OpenCopy();
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));
			var results = new List<int>();
			engine.FilterResultsChanged += (sender, e) => results.Add(e.Records.Length);

			engine.Reload();

			results.Should().ContainSingle()
				.Which.Should().Be(engine.Filter.Results.Length);
		}

		[TestMethod]
		public void GivenSubscribedToFilterHistory_WhenFilteringAfterClear_ThenSenderIsTheCurrentIncludeHistory()
		{
			IEngine engine = OpenCopy();
			var senders = new List<object>();
			engine.FilterHistoryChanged += (sender, e) => senders.Add(sender);

			engine.Selector.Select(lineNumber: 101);
			engine.Clear(ClearOperation.BeforeSelected);
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));

			senders.Should().ContainSingle()
				.Which.Should().BeSameAs(engine.Filter.IncludeHistory);
		}

		[TestMethod]
		public void GivenSubscribedToFilterHistory_WhenFilteringAfterReload_ThenEngineRaisesTheEvent()
		{
			IEngine engine = OpenCopy();
			var values = new List<string>();
			engine.FilterHistoryChanged += (sender, e) => values.Add(e.Value);

			engine.Reload();
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=2"));

			values.Should().Equal("Id=2");
		}

		[TestMethod]
		public void GivenSubscribedToAnnotations_WhenARecordIsPinnedAfterClearAndReload_ThenEngineRaisesTheEvent()
		{
			IEngine engine = OpenCopy();
			IAnnotations annotations = engine.Annotations;
			var lines = new List<int>();
			annotations.Changed += (sender, e) => lines.Add(e.LineNumber);

			engine.Selector.Select(lineNumber: 101);
			engine.Clear(ClearOperation.BeforeSelected);
			engine.Reload();
			engine.RecordState.SetPinned(engine.Records[0], true);

			engine.Annotations.Should().BeSameAs(annotations);
			lines.Should().Equal(engine.Records[0].LineNumber);
			annotations.IsPinned(engine.Records[0].LineNumber).Should().BeTrue();
		}

		[TestMethod]
		[WorkItem(1108)]
		public void GivenUnsavedBookmarksRegionsAndRemarks_WhenReloaded_ThenTheyAreKept()
		{
			// Reload keeps the session's state in memory, as it keeps comments and pins, instead of reading it back from the sidecar.
			IEngine engine = OpenCopy();
			engine.Bookmarks.Create(1, "Start", 10);
			engine.Regions.CreateFromSelection("Startup", new[] { 20, 30 });
			engine.SourceFileRemarks = "Reviewed";

			engine.Reload();

			engine.Bookmarks.TryGetBookmarkName(10, out var bookmarkName).Should().BeTrue();
			bookmarkName.Should().Be("Start");
			engine.Regions.TryGetRegionName(25, out var regionName).Should().BeTrue();
			regionName.Should().Be("Startup");
			engine.SourceFileRemarks.Should().Be("Reviewed");
		}

		[TestMethod]
		[WorkItem(1126)]
		public void GivenAnOpenLog_WhenReloaded_ThenItsPreviousRecordsHaveBeenFreed()
		{
			// Reload kept the previous records while it read the log again, so memory held two copies of the log.
			// Records are created when they are read (#1140), so the test watches the record table they are created from.
			IEngine engine = OpenCopy();
			WeakReference previousTable = TrackRecordTable(engine);

			engine.Reload();

			previousTable.IsAlive.Should().BeFalse("Reload releases the previous records before it reads the log, and opening collects them");
		}

		[TestMethod]
		public void GivenAnOpenLog_WhenRecordsAreCleared_ThenTheRecordTableTheyWereReadFromHasBeenFreed()
		{
			// Clear keeps a record table of only the records it keeps, so the records it clears can be freed (#1140).
			IEngine engine = OpenCopy();
			WeakReference previousTable = TrackRecordTable(engine);
			engine.Selector.SelectOnly(10, 20);

			engine.Clear(ClearOperation.BeforeSelected);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			previousTable.IsAlive.Should().BeFalse("Clear keeps a record table of only the records it keeps");
			engine.Records.GetLineNumber(0).Should().Be(10);
		}

		[TestMethod]
		public void GivenInsightsThatAreKept_WhenTheLogIsReloaded_ThenTheRecordTheyReferToHasBeenFreed()
		{
			// The GUI keeps its insights across Reload; an insight that kept a record would keep the previous log's content (#1139).
			// The log's third timestamp is earlier than its second, so the temporal anomaly insight refers to line 2.
			// A kept record keeps the chunk of the log's content it is read from, so the test watches that chunk (#1140).
			IEngine engine = OpenCopy("OutOfOrderTimestamps.log");
			(IInsight[] insights, WeakReference referredChunk) = GetInsightsAndTrackTheirChunk(engine);

			engine.Reload();

			referredChunk.IsAlive.Should().BeFalse("insights keep line numbers, not records");
			GC.KeepAlive(insights);
		}

		[TestMethod]
		public void GivenAReloadThatCouldNotReadTheLog_WhenReloadedAgain_ThenTheLogIsBackWithItsCommentsAndFilter()
		{
			IEngine engine = OpenCopy();
			var logPath = engine.SourceFilePath;
			var recordCount = engine.Count;
			engine.RecordState.SetComment(engine.Records[0], "Checked");
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));
			var resultCount = engine.Filter.Results.Length;

			File.Move(logPath, $"{logPath}~");
			FluentActions.Invoking(engine.Reload).Should().Throw<IOException>();
			File.Move($"{logPath}~", logPath);

			engine.Reload();

			engine.Count.Should().Be(recordCount);
			engine.Filter.Results.Length.Should().Be(resultCount);
			engine.RecordState.GetComment(engine.Records[0]).Should().Be("Checked");
		}

		[TestMethod]
		public void GivenALogOpenOnDisk_WhenAReloadThatCouldNotReadTheLogIsRetried_ThenItStillReadsItsRecordsFromTheLog()
		{
			// Reload carries the mode, and so does the engine without records that a failed Reload leaves for the retry (#1025).
			IEngine engine = OpenCopy(storageMode: StorageMode.Disk);
			var logPath = engine.SourceFilePath;

			File.Move(logPath, $"{logPath}~");
			FluentActions.Invoking(engine.Reload).Should().Throw<IOException>();
			File.Move($"{logPath}~", logPath);
			engine.Reload();

			ReadsContentFromTheLog(engine).Should().BeTrue("Reload keeps disk mode");
		}

		[TestMethod]
		[DoNotParallelize]
		public void GivenALogOpenOnDiskThatChanged_WhenFiltered_ThenTheFilterThrowsThatTheLogChangedWithoutLoggingAnError()
		{
			// The filter's parallel workers each find the change, but the caller gets the read's exception, not an AggregateException,
			// and the change, which is expected in disk mode, is not logged as an unexpected error (#1025).
			IEngine engine = OpenCopy(storageMode: StorageMode.Disk);
			var bytes = File.ReadAllBytes(engine.SourceFilePath);
			File.WriteAllBytes(engine.SourceFilePath, bytes.AsSpan(0, bytes.Length / 2).ToArray());
			using var log = CapturingLogWriter.Register();

			FluentActions.Invoking(() => engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1")))
				.Should().ThrowExactly<LogFileChangedException>();

			log.Contains(LogSeverityType.Error).Should().BeFalse();
		}

		[TestMethod]
		public void GivenALogThatChangedWhileOpenOnDisk_WhenReloaded_ThenItKeepsItsCommentsPinsBookmarksFilterAndDiskMode()
		{
			IEngine engine = OpenCopy(storageMode: StorageMode.Disk);
			var logPath = engine.SourceFilePath;
			engine.RecordState.SetComment(engine.Records[0], "Checked");
			engine.RecordState.SetPinned(engine.Records[1], true);
			engine.Bookmarks.Create(1, "Start", 10);
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));

			// Rewritten with its first 150 lines, as a logger that starts over does.
			var lines = File.ReadAllLines(logPath).Take(150).ToArray();
			File.WriteAllLines(logPath, lines);
			FluentActions.Invoking(() => engine.Filter.ReApply()).Should().Throw<LogFileChangedException>();

			engine.Reload();

			engine.Count.Should().Be(150);
			engine.RecordState.GetComment(engine.Records[0]).Should().Be("Checked");
			engine.Annotations.IsPinned(engine.Records[1].LineNumber).Should().BeTrue();
			engine.Bookmarks.TryGetBookmarkName(10, out var bookmarkName).Should().BeTrue();
			bookmarkName.Should().Be("Start");
			engine.Filter.Criteria.Include.Should().Be("Id=1");
			ReadsContentFromTheLog(engine).Should().BeTrue("Reload keeps disk mode");
		}

		[TestMethod]
		[DoNotParallelize]
		public void GivenALogThatChangesWhileItIsReloadedOnDisk_WhenReloadedAgain_ThenTheFilterIsKept()
		{
			// The log changes after Reload reads it, before the filter is applied again: Reload ends like one that could not read the log,
			// and keeps the filter for the next one (#1025).
			IEngine engine = OpenCopy(storageMode: StorageMode.Disk);
			var logPath = engine.SourceFilePath;
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));
			var lines = File.ReadAllLines(logPath).Take(150).ToArray();

			using (CapturingLogWriter.Register(message =>
			{
				if (message == "File loading complete.")
				{
					File.WriteAllLines(logPath, lines);
				}
			}))
			{
				FluentActions.Invoking(engine.Reload).Should().Throw<IOException>();
			}

			engine.Count.Should().Be(0, "a Reload that could not read the log shows it without records");
			engine.Filter.Criteria.Include.Should().Be("Id=1");

			engine.Reload();

			engine.Count.Should().Be(150);
			engine.Filter.Criteria.Include.Should().Be("Id=1");
			engine.Filter.Results.Should().NotBeEmpty();
		}

		[TestMethod]
		public void GivenALogOpenOnDiskThatGrew_WhenFilteredAgain_ThenTheResultsAreUnchanged()
		{
			// A log that grows is normal: it is read as it was until it is reloaded (#1025).
			IEngine engine = OpenCopy(storageMode: StorageMode.Disk);
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));
			var results = engine.Filter.Results.Select(record => record.LineNumber).ToArray();

			File.AppendAllText(engine.SourceFilePath, "Info 1900-01-01 13:00:00.0000 248 Written after the log was opened. Id=1000\n");
			engine.Filter.ReApply();

			engine.Filter.Results.Select(record => record.LineNumber).Should().Equal(results);
		}

		[TestMethod]
		public void GivenALogOpenOnDisk_WhenRecordsAreCleared_ThenTheRecordsKeptStillReadTheLog()
		{
			IEngine engine = OpenCopy(storageMode: StorageMode.Disk);
			engine.Selector.Select(lineNumber: 101);

			engine.Clear(ClearOperation.BeforeSelected);

			ReadsContentFromTheLog(engine).Should().BeTrue("Clear keeps disk mode");
		}

		[TestMethod]
		public void GivenNoStorageMode_WhenALogIsOpened_ThenItOpensInRamUnlessTheTestsRunOnDisk()
		{
			// Tst/Disk.runsettings marks the disk run, so this also fails if that run silently opens its logs in RAM (#1025).
			var isDiskRun = TestContext.Properties.TryGetValue("StorageMode", out var runMode) && string.Equals(runMode as string, nameof(StorageMode.Disk), StringComparison.Ordinal);

			IEngine engine = OpenCopy();

			ReadsContentFromTheLog(engine).Should().Be(isDiskRun, "a log given no storage mode opens in RAM, except in the disk run");
		}

		[TestMethod]
		public void GivenASavedComment_WhenSavedAgainAfterAReloadThatCouldNotReadTheLog_ThenTheCommentIsKept()
		{
			// A log in the temporary folder is never saved, as if it came from an archive, so this copy is kept beside the tests.
			var folder = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, nameof(EngineTests), Guid.NewGuid().ToString("N"))).FullName;
			try
			{
				var logPath = Path.Combine(folder, "SampleData.log");
				File.Copy(new Daten().AsFilePath("SampleData.log"), logPath);
				IEngine engine = Engine.UsingPath(logPath).Open();
				engine.RecordState.SetComment(engine.Records[0], "Checked");
				engine.Save();

				File.Move(logPath, $"{logPath}~");
				FluentActions.Invoking(engine.Reload).Should().Throw<IOException>();
				File.Move($"{logPath}~", logPath);
				engine.Save();

				IEngine reopened = Engine.UsingPath(logPath).Open();

				reopened.RecordState.GetComment(reopened.Records[0]).Should().Be("Checked");
			}
			finally
			{
				Directory.Delete(folder, recursive: true);
			}
		}

		[TestMethod]
		[WorkItem(1109)]
		public void GivenFiltersApplied_WhenReloaded_ThenTheHistoryIsKeptWithoutSaving()
		{
			// Reload keeps the history in memory, as it keeps the rest of the session, instead of reading it back from the sidecar.
			IEngine engine = OpenCopy();
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=2"));
			IReadOnlyList<string> history = engine.Filter.IncludeHistory;

			engine.Reload();

			engine.Filter.IncludeHistory.Should().BeSameAs(history);
			history.Take(2).Should().Equal("Id=2", "Id=1");
		}

		[TestMethod]
		[WorkItem(1108)]
		[WorkItem(1109)]
		public void GivenSavedSessionState_WhenTheLogIsOpenedAgain_ThenItIsReadFromTheSidecar()
		{
			// A log in the temporary folder is never saved, as if it came from an archive, so this copy is kept beside the tests.
			var folder = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, nameof(EngineTests), Guid.NewGuid().ToString("N"))).FullName;
			try
			{
				var logPath = Path.Combine(folder, "SampleData.log");
				File.Copy(new Daten().AsFilePath("SampleData.log"), logPath);
				IEngine engine = Engine.UsingPath(logPath).Open();
				engine.Bookmarks.Create(1, "Start", 10);
				engine.Regions.CreateFromSelection("Startup", new[] { 20, 30 });
				engine.SourceFileRemarks = "Reviewed";
				engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1", "Id=99"));
				engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=2", "Id=98"));
				engine.Save();

				IEngine reopened = Engine.UsingPath(logPath).Open();

				reopened.Bookmarks.TryGetBookmarkName(10, out var bookmarkName).Should().BeTrue();
				bookmarkName.Should().Be("Start");
				reopened.Regions.TryGetRegionName(25, out var regionName).Should().BeTrue();
				regionName.Should().Be("Startup");
				reopened.SourceFileRemarks.Should().Be("Reviewed");
				reopened.Filter.IncludeHistory.Take(2).Should().Equal("Id=2", "Id=1");
				reopened.Filter.ExcludeHistory.Take(2).Should().Equal("Id=98", "Id=99");
			}
			finally
			{
				Directory.Delete(folder, recursive: true);
			}
		}

		[TestMethod]
		[DataRow(StorageMode.Ram)]
		[DataRow(StorageMode.Disk)]
		public void GivenClearedRecords_WhenReloaded_ThenTheyAreBackWithTheirContent(StorageMode storageMode)
		{
			// Reload reads the whole log again, so there is no separate command to restore cleared records (#1026).
			IEngine engine = OpenCopy(storageMode: storageMode);
			var recordCount = engine.Count;
			var firstContent = engine.Records[0].Content;
			engine.Selector.Select(lineNumber: 101);
			engine.Clear(ClearOperation.BeforeSelected);
			engine.Count.Should().BeLessThan(recordCount);

			engine.Reload();

			engine.Count.Should().Be(recordCount);
			engine.Records[0].Content.Should().Be(firstContent);
			ReadsContentFromTheLog(engine).Should().Be(storageMode == StorageMode.Disk, "Reload keeps the storage mode");
		}

		[TestMethod]
		public void GivenUnselectedRecordsWereCleared_WhenReloaded_ThenTheRestoredRecordsAreNotSelected()
		{
			IEngine engine = OpenCopy();
			engine.Selector.SelectOnly(1, 3).SelectRange(7, 9);
			engine.Clear(ClearOperation.Unselected);
			engine.Selector.SelectAll();

			engine.Reload();

			engine.Selector.Selected.Keys.Should().Equal(1, 2, 3, 7, 8, 9);
		}

		[TestMethod]
		public void GivenAClearedRecordAndALogThatShrank_WhenReloaded_ThenTheRestoredRecordIsNotSelected()
		{
			// One line fewer at the end made the remap count line 5 as selected.
			var logPath = Path.Combine(_folder, "SampleData.log");
			var lines = File.ReadAllLines(new Daten().AsFilePath("SampleData.log"));
			File.WriteAllLines(logPath, lines.Take(10));
			IEngine engine = Engine.UsingPath(logPath).Open();
			engine.Selector.SelectOnly(5, 5);
			engine.Clear(ClearOperation.Selected);
			engine.Selector.SelectAll();
			File.WriteAllLines(logPath, lines.Take(8));

			engine.Reload();

			engine.Selector.Selected.Keys.Should().Equal(1, 2, 3, 4, 6, 7, 8);
		}

		[TestMethod]
		[WorkItem(1105)]
		[DoNotParallelize]
		public void GivenALog_WhenOpened_ThenTheApplicationLogRecordsHowLongItTookTheMemoryUsedAndTheStorageMode()
		{
			using var log = CapturingLogWriter.Register();

			// On disk, which no run opens by default in RAM, so the mode recorded is this log's (#1025).
			IEngine engine = OpenCopy(storageMode: StorageMode.Disk);

			IDictionary<string, object> entry = log.Last("File loading complete.");
			entry["RecordCount"].Should().Be(engine.Count);
			((double)entry["OpeningDuration"]).Should().BePositive();
			((long)entry["WorkingSet"]).Should().BePositive();
			((long)entry["ManagedMemory"]).Should().BePositive();
			entry["StorageMode"].Should().Be(StorageMode.Disk);
		}

		[TestMethod]
		[WorkItem(1105)]
		[WorkItem(1120)]
		[DoNotParallelize]
		public void GivenFilteredResults_WhenASearchFindsARecord_ThenTheApplicationLogRecordsHowManyResultsItSearched()
		{
			IEngine engine = OpenCopy();
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));
			using var log = CapturingLogWriter.Register();

			engine.Navigate.PreviousContent("Id=150", isCaseSensitive: true);

			IDictionary<string, object> entry = log.Last("Search complete.");
			log.SeverityOfLast("Search complete.").Should().Be(LogSeverityType.Information);
			entry["Search"].Should().Be(nameof(INavigate.PreviousContent));
			entry["WasFound"].Should().Be(true);
			entry["Records"].Should().Be(engine.Filter.Results.Length).And.NotBe(engine.Count);
		}

		[TestMethod]
		[WorkItem(1105)]
		[DoNotParallelize]
		public void GivenASearchThatFindsNothing_WhenItFinishes_ThenTheApplicationLogRecordsHowLongItTookWithoutTheText()
		{
			const string text = "Text that no record holds";
			IEngine engine = OpenCopy();
			using var log = CapturingLogWriter.Register();

			Action search = () => engine.Navigate.NextContent(text, isCaseSensitive: true);

			search.Should().Throw<RecordNotFoundException>();
			IDictionary<string, object> entry = log.Last("Search complete.");
			entry["Search"].Should().Be(nameof(INavigate.NextContent));
			entry["WasFound"].Should().Be(false);
			((double)entry["Duration"]).Should().BeGreaterThanOrEqualTo(0);
			entry.Values.Should().NotContain(text);
		}

		[TestMethod]
		public void GivenSelectedRecords_WhenSavingThemIsAborted_ThenNoFileIsLeft()
		{
			// Abort cancels the save, which deletes the file saved so far (#1027).
			IEngine engine = OpenCopy();
			var destinationPath = Path.Combine(_folder, "Selected.log");
			engine.Selector.SelectRange(2, 4);

			Action save = () => engine.Selector.SaveSelection(destinationPath, FileFormatType.Raw, new CancellationToken(canceled: true));

			save.Should().Throw<OperationCanceledException>();
			File.Exists(destinationPath).Should().BeFalse();
		}

		[TestMethod]
		public void GivenTextThatALaterRecordHolds_WhenSearchingForItIsAborted_ThenTheSearchStopsWithoutFindingIt()
		{
			// A search that was aborted is not mistaken for one that found nothing (#1027).
			IEngine engine = OpenCopy();
			engine.Selector.Select(lineNumber: 1);

			Action search = () => engine.Navigate.NextContent("Id=150", isCaseSensitive: true, useRegex: false, new CancellationToken(canceled: true));

			search.Should().Throw<OperationCanceledException>();
			engine.Navigate.NextContent("Id=150", isCaseSensitive: true).LineNumber.Should().Be(150, "the aborted search did not move the active record");
		}

		[TestMethod]
		public void GivenAnAnalyzer_WhenItIsAborted_ThenItStops()
		{
			// The engine passes Abort's token to the analyzer (#1027).
			IEngine engine = OpenCopy();
			IUserDialog dialog = Substitute.For<IUserDialog>();
			dialog
				.TryGetExpressions(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
				.Returns(callInfo =>
				{
					callInfo[2] = @"Id=(?<Id>\d+)";
					return true;
				});

			Action analyze = () => engine.Analyzer.Analyze(AnalysisType.DetectData, dialog, new CancellationToken(canceled: true));

			analyze.Should().Throw<OperationCanceledException>();
			engine.Analyzer.Analyze(AnalysisType.DetectData, dialog).FlaggedRecords.Should().BePositive("the analyzer reads the same records when it is not aborted");
		}

		[TestMethod]
		[DataRow(StorageMode.Ram)]
		[DataRow(StorageMode.Disk)]
		public void GivenSelectedRecords_WhenSavedInRawFormat_ThenEachRecordIsALineOfTheFile(StorageMode storageMode)
		{
			IEngine engine = OpenCopy(storageMode: storageMode);
			var destinationPath = Path.Combine(_folder, "Selected.log");
			engine.Selector.SelectRange(2, 4);

			engine.Selector.SaveSelection(destinationPath, FileFormatType.Raw);

			File.ReadAllLines(destinationPath).Should().Equal(ReadLogLines(2, 4));
		}

		[TestMethod]
		[DataRow(StorageMode.Ram)]
		[DataRow(StorageMode.Disk)]
		public void GivenSelectedRecords_WhenSavedInTsvFormat_ThenEachRecordIsARowEndingWithItsContent(StorageMode storageMode)
		{
			IEngine engine = OpenCopy(storageMode: storageMode);
			var destinationPath = Path.Combine(_folder, "Selected.tsv");
			engine.RecordState.SetPinned(engine.Records.First(record => record.LineNumber == 3), true);
			engine.Selector.SelectRange(2, 4);

			engine.Selector.SaveSelection(destinationPath, FileFormatType.Tsv);

			string[][] rows = File.ReadAllLines(destinationPath).Skip(1).Select(line => line.Split('\t', 7)).ToArray();
			rows.Select(row => row[0]).Should().Equal("2", "3", "4");
			rows.Select(row => row[2]).Should().Equal("False", "True", "False");
			rows.Select(row => row[6]).Should().Equal(ReadLogLines(2, 4));
		}

		/// <summary>
		/// Watches the engine's record table without keeping it alive: a Debug build keeps a test method's locals until it ends.
		/// It tracks resurrection, so a table that a finalizer still holds counts as alive.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static WeakReference TrackRecordTable(IEngine engine)
		{
			return new WeakReference(((TableRecordList)engine.Records).Table, trackResurrection: true);
		}

		/// <summary>
		/// Gets the insights, and watches the chunk that holds the content of the first line an insight refers to, the same way.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (IInsight[] Insights, WeakReference Chunk) GetInsightsAndTrackTheirChunk(IEngine engine)
		{
			IInsight[] insights = engine.Analyzer.GetInsights().ToArray();
			var lineNumber = insights.First(insight => insight.RelatedLineNumbers.Count > 0).RelatedLineNumbers[0];

			// Nothing has been cleared, so a record's position in the engine's records is its number in the table.
			var records = (TableRecordList)engine.Records;
			ContentChunk chunk = records.Table.GetChunk(records.IndexOfLineNumber(lineNumber));

			return (insights, new WeakReference(chunk, trackResurrection: true));
		}

		/// <summary>
		/// Returns lines <paramref name="first"/> to <paramref name="last"/> of the sample log, as the log holds them.
		/// </summary>
		private static string[] ReadLogLines(int first, int last)
		{
			return File.ReadAllLines(new Daten().AsFilePath("SampleData.log")).Skip(first - 1).Take(last - first + 1).ToArray();
		}

		/// <param name="storageMode">The storage mode, or <see langword="null"/> to give the builder none.</param>
		private IEngine OpenCopy(string fileName = "SampleData.log", StorageMode? storageMode = null)
		{
			var logPath = Path.Combine(_folder, fileName);
			File.Copy(new Daten().AsFilePath(fileName), logPath);

			return storageMode is { } mode
				? new EngineBuilder(logPath).UsingStorage(mode).Open()
				: Engine.UsingPath(logPath).Open();
		}

		/// <summary>
		/// Returns <see langword="true"/> when the engine reads its records' content back from the log, as disk mode does: RAM mode keeps it in memory.
		/// </summary>
		/// <remarks>
		/// Moves the log away while it reads a record, then puts it back: no handle is kept between reads, even on Windows.
		/// </remarks>
		private static bool ReadsContentFromTheLog(IEngine engine)
		{
			var logPath = engine.SourceFilePath;
			File.Move(logPath, $"{logPath}~");

			try
			{
				_ = engine.Records[0].Content;
				return false;
			}
			catch (IOException)
			{
				return true;
			}
			finally
			{
				File.Move($"{logPath}~", logPath);
			}
		}

		/// <summary>
		/// Captures what is written to the application's log, until disposed.
		/// </summary>
		private sealed class CapturingLogWriter : ILogWriter, IDisposable
		{
			private readonly ILogWriter _original = Log.Default;
			private readonly ConcurrentQueue<(LogSeverityType Severity, string Message, IDictionary<string, object> Metadata)> _entries = new();
			private Action<string> _onWrite;

			/// <param name="onWrite">Runs as each message is written, so a test can act at a point the application logs.</param>
			public static CapturingLogWriter Register(Action<string> onWrite = null)
			{
				var writer = new CapturingLogWriter { _onWrite = onWrite };
				Log.Register(writer);

				return writer;
			}

			/// <summary>
			/// Returns the metadata of the last entry with the given message.
			/// </summary>
			public IDictionary<string, object> Last(string message)
			{
				return _entries.Last(entry => entry.Message == message).Metadata;
			}

			/// <summary>
			/// Returns <see langword="true"/> when an entry of the given severity was written.
			/// </summary>
			public bool Contains(LogSeverityType severity)
			{
				return _entries.Any(entry => entry.Severity == severity);
			}

			/// <summary>
			/// Returns the severity of the last entry with the given message.
			/// </summary>
			public LogSeverityType SeverityOfLast(string message)
			{
				return _entries.Last(entry => entry.Message == message).Severity;
			}

			public void Dispose() => Log.Register(_original);

			public void Write(string message) => Write(LogSeverityType.Debug, message, null);

			public void Write(string message, IEnumerable<KeyValuePair<string, object>> metadata) => Write(LogSeverityType.Debug, message, metadata);

			public void Write(LogSeverityType severity, string message) => Write(severity, message, null);

			public void Write(LogSeverityType severity, string message, IEnumerable<KeyValuePair<string, object>> metadata)
			{
				_entries.Enqueue((severity, message, (metadata ?? []).ToDictionary(pair => pair.Key, pair => pair.Value)));
				_onWrite?.Invoke(message);
			}

			public void Write(LogSeverityType severity, Exception exception) => Write(severity, exception.Message, null);

			public void Write(LogSeverityType severity, Exception exception, string message) => Write(severity, message, null);

			public void Write(LogSeverityType severity, Exception exception, string message, IEnumerable<KeyValuePair<string, object>> metadata) => Write(severity, message, metadata);
		}
	}
}
