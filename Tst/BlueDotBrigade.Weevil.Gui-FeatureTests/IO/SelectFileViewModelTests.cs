namespace BlueDotBrigade.Weevil.Gui.IO
{
	// Regression: Issue #478 - Compressed file selector does not list the files contained within the provided *.zip
	[TestClass]
	public class SelectFileViewModelTests
	{
		[TestMethod]
		public void GivenFilesInZip_WhenConstructed_ThenAllFilesAreListed()
		{
			var fileNames = new[]
			{
				"application.log",
				"system.log",
				"debug.log",
			};

			var viewModel = new SelectFileViewModel(fileNames);

			viewModel.FileNames.Should().BeEquivalentTo(fileNames);
		}

		[TestMethod]
		public void GivenFilesIncludingSidecar_WhenConstructed_ThenSidecarFileIsExcluded()
		{
			var fileNames = new[]
			{
				"application.log",
				"application.log.xml",
				"system.log",
			};

			var viewModel = new SelectFileViewModel(fileNames);

			viewModel.FileNames.Should().BeEquivalentTo(new[] { "application.log", "system.log" });
		}

		[TestMethod]
		public void GivenNoFileSelected_WhenCreated_ThenOkCommandIsDisabled()
		{
			var viewModel = new SelectFileViewModel(new[] { "application.log" });

			viewModel.OkCommand.CanExecute().Should().BeFalse();
		}

		[TestMethod]
		public void GivenFileSelected_WhenOkCommandChecked_ThenOkCommandIsEnabled()
		{
			var viewModel = new SelectFileViewModel(new[] { "application.log" });

			viewModel.SelectedFilename = "application.log";

			viewModel.OkCommand.CanExecute().Should().BeTrue();
		}

		[TestMethod]
		public void GivenFileSelected_WhenCancelExecuted_ThenSelectedFilenameIsNull()
		{
			var closeRequested = false;
			var viewModel = new SelectFileViewModel(new[] { "application.log" });
			viewModel.SelectedFilename = "application.log";
			viewModel.CloseRequested += (sender, args) => closeRequested = true;

			viewModel.CancelCommand.Execute();

			viewModel.SelectedFilename.Should().BeNull();
			closeRequested.Should().BeTrue();
		}
	}
}
