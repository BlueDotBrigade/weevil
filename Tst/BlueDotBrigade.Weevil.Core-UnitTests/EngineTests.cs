namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Navigation;

	[TestClass]
	public class EngineTests
	{
		private string _folder = string.Empty;

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
		public void GivenSavedBookmarksRegionsAndRemarks_WhenTheLogIsOpenedAgain_ThenTheyAreReadFromTheSidecar()
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
				engine.Save();

				IEngine reopened = Engine.UsingPath(logPath).Open();

				reopened.Bookmarks.TryGetBookmarkName(10, out var bookmarkName).Should().BeTrue();
				bookmarkName.Should().Be("Start");
				reopened.Regions.TryGetRegionName(25, out var regionName).Should().BeTrue();
				regionName.Should().Be("Startup");
				reopened.SourceFileRemarks.Should().Be("Reviewed");
			}
			finally
			{
				Directory.Delete(folder, recursive: true);
			}
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
		public void GivenALog_WhenOpened_ThenTheApplicationLogRecordsHowLongItTookAndTheMemoryUsed()
		{
			using var log = CapturingLogWriter.Register();

			IEngine engine = OpenCopy();

			IDictionary<string, object> entry = log.Last("File loading complete.");
			entry["RecordCount"].Should().Be(engine.Count);
			((double)entry["OpeningDuration"]).Should().BePositive();
			((long)entry["WorkingSet"]).Should().BePositive();
			((long)entry["ManagedMemory"]).Should().BePositive();
		}

		[TestMethod]
		[WorkItem(1105)]
		[DoNotParallelize]
		public void GivenFilteredResults_WhenASearchFindsARecord_ThenTheApplicationLogRecordsHowManyResultsItSearched()
		{
			IEngine engine = OpenCopy();
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));
			using var log = CapturingLogWriter.Register();

			engine.Navigate.PreviousContent("Id=150", isCaseSensitive: true);

			IDictionary<string, object> entry = log.Last("Search complete.");
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

		private IEngine OpenCopy()
		{
			var logPath = Path.Combine(_folder, "SampleData.log");
			File.Copy(new Daten().AsFilePath("SampleData.log"), logPath);

			return Engine.UsingPath(logPath).Open();
		}

		/// <summary>
		/// Captures what is written to the application's log, until disposed.
		/// </summary>
		private sealed class CapturingLogWriter : ILogWriter, IDisposable
		{
			private readonly ILogWriter _original = Log.Default;
			private readonly ConcurrentQueue<(string Message, IDictionary<string, object> Metadata)> _entries = new();

			public static CapturingLogWriter Register()
			{
				var writer = new CapturingLogWriter();
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

			public void Dispose() => Log.Register(_original);

			public void Write(string message) => Write(LogSeverityType.Debug, message, null);

			public void Write(string message, IEnumerable<KeyValuePair<string, object>> metadata) => Write(LogSeverityType.Debug, message, metadata);

			public void Write(LogSeverityType severity, string message) => Write(severity, message, null);

			public void Write(LogSeverityType severity, string message, IEnumerable<KeyValuePair<string, object>> metadata)
			{
				_entries.Enqueue((message, (metadata ?? []).ToDictionary(pair => pair.Key, pair => pair.Value)));
			}

			public void Write(LogSeverityType severity, Exception exception) => Write(severity, exception.Message, null);

			public void Write(LogSeverityType severity, Exception exception, string message) => Write(severity, message, null);

			public void Write(LogSeverityType severity, Exception exception, string message, IEnumerable<KeyValuePair<string, object>> metadata) => Write(severity, message, metadata);
		}
	}
}
