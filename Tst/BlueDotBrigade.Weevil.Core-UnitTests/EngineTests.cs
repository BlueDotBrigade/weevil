namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Navigation;

	[TestClass]
	[DoNotParallelize]
	public class EngineTests
	{
		private ILogWriter _originalWriter;
		private CapturingLogWriter _log;

		[TestInitialize]
		public void Setup()
		{
			_originalWriter = Log.Default;
			_log = new CapturingLogWriter();
			Log.Register(_log);
		}

		[TestCleanup]
		public void Cleanup()
		{
			Log.Register(_originalWriter);
		}

		[TestMethod]
		[WorkItem(1118)]
		public void GivenALog_WhenOpened_ThenTheApplicationLogRecordsHowLongItTookAndTheMemoryUsed()
		{
			IEngine engine = Engine.UsingPath(new Daten().AsFilePath("SampleData.log")).Open();

			IDictionary<string, object> entry = _log.Entries.Last(e => e.Message == "File loading complete.").Metadata;
			entry["RecordCount"].Should().Be(engine.Count);
			((double)entry["OpeningDuration"]).Should().BePositive();
			((long)entry["WorkingSet"]).Should().BePositive();
			((long)entry["ManagedMemory"]).Should().BePositive();
		}

		[TestMethod]
		[WorkItem(1118)]
		public void GivenFilteredResults_WhenASearchFindsARecord_ThenTheApplicationLogRecordsHowManyResultsItSearched()
		{
			IEngine engine = Engine.UsingPath(new Daten().AsFilePath("SampleData.log")).Open();
			engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Id=1"));

			engine.Navigate.PreviousContent("Id=150", isCaseSensitive: true);

			var (severity, _, entry) = _log.Entries.Last(e => e.Message == "Search complete.");
			severity.Should().Be(LogSeverityType.Information);
			entry["Search"].Should().Be(nameof(INavigate.PreviousContent));
			entry["WasFound"].Should().Be(true);
			entry["Records"].Should().Be(engine.Filter.Results.Length).And.NotBe(engine.Count);
		}

		[TestMethod]
		[WorkItem(1118)]
		public void GivenASearchThatFindsNothing_WhenItFinishes_ThenTheApplicationLogRecordsHowLongItTookWithoutTheText()
		{
			const string text = "Text that no record holds";
			IEngine engine = Engine.UsingPath(new Daten().AsFilePath("SampleData.log")).Open();

			Action search = () => engine.Navigate.NextContent(text, isCaseSensitive: true);

			search.Should().Throw<RecordNotFoundException>();
			IDictionary<string, object> entry = _log.Entries.Last(e => e.Message == "Search complete.").Metadata;
			entry["Search"].Should().Be(nameof(INavigate.NextContent));
			entry["WasFound"].Should().Be(false);
			entry["Records"].Should().Be(engine.Filter.Results.Length);
			((double)entry["Duration"]).Should().BeGreaterThanOrEqualTo(0);
			entry.Values.Should().NotContain(text);
		}

		private sealed class CapturingLogWriter : ILogWriter
		{
			private readonly ConcurrentQueue<(LogSeverityType Severity, string Message, IDictionary<string, object> Metadata)> _entries = new();

			public IEnumerable<(LogSeverityType Severity, string Message, IDictionary<string, object> Metadata)> Entries => _entries;

			public void Write(string message) => Write(LogSeverityType.Debug, message, null);

			public void Write(string message, IEnumerable<KeyValuePair<string, object>> metadata) => Write(LogSeverityType.Debug, message, metadata);

			public void Write(LogSeverityType severity, string message) => Write(severity, message, null);

			public void Write(LogSeverityType severity, string message, IEnumerable<KeyValuePair<string, object>> metadata)
			{
				_entries.Enqueue((severity, message, (metadata ?? []).ToDictionary(pair => pair.Key, pair => pair.Value)));
			}

			public void Write(LogSeverityType severity, Exception exception) => Write(severity, exception.Message, null);

			public void Write(LogSeverityType severity, Exception exception, string message) => Write(severity, message, null);

			public void Write(LogSeverityType severity, Exception exception, string message, IEnumerable<KeyValuePair<string, object>> metadata) => Write(severity, message, metadata);
		}
	}
}
