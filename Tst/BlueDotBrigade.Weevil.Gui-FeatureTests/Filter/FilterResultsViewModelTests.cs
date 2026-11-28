namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System.IO;
	using System.Threading.Tasks;

	[STATestClass]
	public class FilterResultsViewModelTests : UiTestBase
	{
		// Weevil saves no sidecar under the temp folder, so each test starts with no pins or filter history.
		private string _folder = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			_folder = Directory.CreateTempSubdirectory("WeevilGui.").FullName;
		}

		[TestCleanup]
		public void Cleanup()
		{
			// The log's lock is in the folder.
			CloseViewModels();

			Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		public async Task GivenAPinnedAndAnUnpinnedRecord_WhenFilteredByIsPinned_ThenOnlyThePinnedRecordIsVisible()
		{
			var logPath = Path.Combine(_folder, "Application.log");
			File.WriteAllLines(logPath, ["Unpinned record", "Pinned record"]);

			FilterViewModel viewModel = Track(new FilterViewModel(this.UiDispatcher, Substitute.For<IBulletinMediator>()));
			await viewModel.OpenAsync(logPath);
			await viewModel.WhenIdle();

			viewModel.Rows.GetRow(1).IsPinned = true;
			viewModel.FilterManually(new object[] { "@IsPinned", string.Empty });
			await viewModel.WhenIdle();

			viewModel.VisibleItems.Should().ContainSingle()
				.Which.Content.Should().Be("Pinned record");
		}
	}
}
