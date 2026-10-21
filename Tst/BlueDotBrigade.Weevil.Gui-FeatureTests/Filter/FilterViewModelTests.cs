namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Gui.IO;

	[TestClass]
	public class FilterViewModelTests : UiTestBase
	{
		private const string IncludeFilter = "Directives";

		private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

		private string _folder = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			// Weevil does not save sidecars in a subfolder of the temp folder,
			// ... so each test starts with an empty filter history that Reload cannot restore.
			_folder = Directory.CreateTempSubdirectory("WeevilGui.").FullName;
		}

		[TestCleanup]
		public void Cleanup()
		{
			Directory.Delete(_folder, recursive: true);
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

		private string CopyLog(string fileName)
		{
			var logPath = Path.Combine(_folder, fileName);
			File.Copy(new Daten().AsFilePath(From.GlobalDefault), logPath);

			return logPath;
		}

		private string CopyLogWithIncompatibleSidecar(string fileName)
		{
			var logPath = CopyLog(fileName);

			// A sidecar from a newer version of Weevil: opening the log throws NotSupportedException.
			File.WriteAllText(
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
	}
}
