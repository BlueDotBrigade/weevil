namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Gui.IO;
	using BlueDotBrigade.Weevil.IO;

	[TestClass]
	public class FilterViewModelTests : UiTestBase
	{
		// Regression: Issue #996 - running an analyzer froze the window until it finished.
		[TestMethod]
		public async Task GivenOpenLog_WhenAnalyzing_ThenAnalyzerRunsInBackgroundWithMenusDisabled()
		{
			var callingThread = Environment.CurrentManagedThreadId;
			var promptThread = callingThread;
			var wereMenusEnabledDuringAnalysis = true;
			FilterViewModel viewModel = null!;

			var dialog = Substitute.For<IDialogBoxService>();
			((IUserDialog)dialog).TryGetExpressions(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>()).Returns(x =>
			{
				promptThread = Environment.CurrentManagedThreadId;
				wereMenusEnabledDuringAnalysis = viewModel.IsMenuEnabled;
				x[2] = @"Cpu1=(?<Cpu1>\d+\.\d)%";
				return true;
			});

			var bulletinMediator = Substitute.For<IBulletinMediator>();
			viewModel = new FilterViewModel(this.UiDispatcher, bulletinMediator, dialog);
			await OpenAsync(viewModel);

			await viewModel.Analyze(AnalysisType.DetectData);

			promptThread.Should().NotBe(callingThread, "the analyzer runs on a background thread");
			wereMenusEnabledDuringAnalysis.Should().BeFalse("the menus are disabled while the analyzer runs");
			viewModel.IsMenuEnabled.Should().BeTrue("the menus are enabled again once the analyzer finishes");
			bulletinMediator.Received(1).Post(Arg.Is<AnalysisCompleteBulletin>(bulletin => bulletin.FlaggedRecords > 0));
		}

		private static async Task OpenAsync(FilterViewModel viewModel)
		{
			await viewModel.OpenAsync(new Daten().AsFilePath(From.GlobalDefault));

			var stopwatch = Stopwatch.StartNew();
			while (!viewModel.IsMenuEnabled && stopwatch.Elapsed < TimeSpan.FromSeconds(5))
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(100));
			}

			viewModel.IsMenuEnabled.Should().BeTrue("the log file should be open before the test continues");
		}
	}
}
