namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;

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

		private IEngine OpenCopy()
		{
			var logPath = Path.Combine(_folder, "SampleData.log");
			File.Copy(new Daten().AsFilePath("SampleData.log"), logPath);

			return Engine.UsingPath(logPath).Open();
		}
	}
}
