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
		[WorkItem(1124)]
		public async Task GivenWeevilHasJustStarted_WhenALogWithRemarksIsOpened_ThenTheRemarksShownAreRefreshed()
		{
			// Opening a log never told the Remarks box to refresh, so it stayed empty although the sidecar held remarks.
			var viewModel = new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>());
			var changedProperties = new List<string>();
			((INotifyPropertyChanged)viewModel).PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

			await OpenAsync(viewModel, CopyLogWithSidecar("A.log"));

			changedProperties.Should().Contain(nameof(FilterViewModel.SourceFileRemarks));
			viewModel.SourceFileRemarks.Should().Be("Why did the droid reboot?");
		}

		[TestMethod]
		[WorkItem(1124)]
		public async Task GivenALogWithRemarks_WhenAnotherLogIsOpened_ThenTheRemarksShownAreRefreshed()
		{
			// Opening a log never told the Remarks box to refresh, so it kept showing the previous log's remarks.
			var viewModel = new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>());
			await OpenAsync(viewModel, CopyLog("A.log"));
			viewModel.SourceFileRemarks = "Remarks about A";
			var changedProperties = new List<string>();
			((INotifyPropertyChanged)viewModel).PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

			await OpenAsync(viewModel, CopyLog("B.log"));

			changedProperties.Should().Contain(nameof(FilterViewModel.SourceFileRemarks));
			viewModel.SourceFileRemarks.Should().BeEmpty();
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
	}
}
