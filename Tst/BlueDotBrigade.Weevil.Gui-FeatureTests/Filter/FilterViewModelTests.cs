namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Gui.Threading;

	[TestClass]
	public class FilterViewModelTests : UiTestBase
	{
		private string _folder = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			// Weevil does not save sidecars in the temp folder, so the logs are copied next to the tests.
			_folder = Path.Combine(AppContext.BaseDirectory, "FilterViewModelTests", Guid.NewGuid().ToString());
			Directory.CreateDirectory(_folder);
		}

		[TestCleanup]
		public void Cleanup()
		{
			Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		[WorkItem(1123)]
		public async Task GivenTheLastNavigationWentToBookmark1_WhenGoingToBookmark1Again_ThenTheListIsToldToShowItAgain()
		{
			// Ctrl+1 did nothing when the last navigation had gone to the same row, because the index the list scrolls to did not change.
			const int lineNumber = 10;
			var viewModel = new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>());
			await OpenAsync(viewModel, CopyLogWithBookmark1(lineNumber));
			viewModel.GoToBookmark1Command.Execute(null);
			viewModel.ActiveRecordIndex.Should().Be(lineNumber - 1);
			var indexesSentToList = new List<int>();
			((INotifyPropertyChanged)viewModel).PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == nameof(FilterViewModel.ActiveRecordIndex))
				{
					indexesSentToList.Add(viewModel.ActiveRecordIndex);
				}
			};

			viewModel.GoToBookmark1Command.Execute(null);

			// The list scrolls only when the index changes, so it must change before it returns to the same row.
			indexesSentToList.Should().Contain(index => index != lineNumber - 1);
			indexesSentToList.Should().EndWith(lineNumber - 1);
		}

		[TestMethod]
		[WorkItem(1124)]
		public async Task GivenWeevilHasJustStarted_WhenALogWithRemarksIsOpened_ThenTheRemarksShownAreRefreshed()
		{
			// Opening a log never told the Remarks box to refresh, so it stayed empty although the sidecar held remarks.
			var viewModel = new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>());
			List<string> remarksSentToBox = RecordRemarksSentToBox(viewModel);

			await OpenAsync(viewModel, CopyLogWithSidecar("A.log"));

			remarksSentToBox.Should().EndWith("Why did the droid reboot?");
		}

		[TestMethod]
		[WorkItem(1124)]
		public async Task GivenALogWithRemarks_WhenAnotherLogIsOpened_ThenTheRemarksShownAreRefreshed()
		{
			// Opening a log never told the Remarks box to refresh, so it kept showing the previous log's remarks.
			var viewModel = new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>());
			await OpenAsync(viewModel, CopyLog("A.log"));
			viewModel.SourceFileRemarks = "Remarks about A";
			List<string> remarksSentToBox = RecordRemarksSentToBox(viewModel);

			await OpenAsync(viewModel, CopyLog("B.log"));

			remarksSentToBox.Should().EndWith(string.Empty);
		}

		private static async Task OpenAsync(FilterViewModel viewModel, string logPath)
		{
			await viewModel.OpenAsync(logPath);

			var stopwatch = Stopwatch.StartNew();
			while (!viewModel.IsLogFileOpen && stopwatch.Elapsed < TimeSpan.FromSeconds(5))
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(100));
			}

			viewModel.IsLogFileOpen.Should().BeTrue("the log should be open before the test continues");
		}

		/// <summary>
		/// Records the remarks the Remarks box would read each time it is told they changed.
		/// </summary>
		private static List<string> RecordRemarksSentToBox(FilterViewModel viewModel)
		{
			var remarksSentToBox = new List<string>();
			((INotifyPropertyChanged)viewModel).PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == nameof(FilterViewModel.SourceFileRemarks))
				{
					remarksSentToBox.Add(viewModel.SourceFileRemarks);
				}
			};

			return remarksSentToBox;
		}

		private string CopyLog(string fileName)
		{
			var logPath = Path.Combine(_folder, fileName);
			File.Copy(new Daten().AsFilePath(From.GlobalDefault), logPath);

			return logPath;
		}

		private string CopyLogWithSidecar(string fileName)
		{
			var logPath = CopyLog(fileName);
			File.Copy($"{new Daten().AsFilePath(From.GlobalDefault)}.xml", $"{logPath}.xml");

			return logPath;
		}

		/// <summary>
		/// Copies the log and saves a sidecar that holds bookmark 1, as a previous session would.
		/// </summary>
		private string CopyLogWithBookmark1(int lineNumber)
		{
			var logPath = CopyLog("Bookmarked.log");

			IEngine engine = Engine.UsingPath(logPath).Open();
			engine.Bookmarks.Create(1, "Bookmark 1", lineNumber);
			engine.Save();

			return logPath;
		}
	}
}
