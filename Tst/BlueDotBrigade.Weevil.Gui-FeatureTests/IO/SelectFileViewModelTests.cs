namespace BlueDotBrigade.Weevil.Gui.IO
{
	// Regression: Issue #478 - Compressed file selector does not list the files contained within the provided *.zip
	[TestClass]
	public class SelectFileViewModelTests
	{
		[TestMethod]
		public void GivenFileSelected_WhenOkExecuted_ThenSelectedFilenameIsRetained()
		{
			var closeRequested = false;
			var viewModel = new SelectFileViewModel(new[] { "application.log", "system.log", "readme.txt" });
			viewModel.SelectedFilename = "application.log";
			viewModel.CloseRequested += (sender, args) => closeRequested = true;

			viewModel.OkCommand.Execute();

			viewModel.SelectedFilename.Should().Be("application.log");
			closeRequested.Should().BeTrue();
		}

		[TestMethod]
		public void GivenFileSelected_WhenCancelExecuted_ThenSelectedFilenameIsNull()
		{
			var closeRequested = false;
			var viewModel = new SelectFileViewModel(new[] { "application.log", "system.log", "readme.txt" });
			viewModel.SelectedFilename = "application.log";
			viewModel.CloseRequested += (sender, args) => closeRequested = true;

			viewModel.CancelCommand.Execute();

			viewModel.SelectedFilename.Should().BeNull();
			closeRequested.Should().BeTrue();
		}
	}
}
