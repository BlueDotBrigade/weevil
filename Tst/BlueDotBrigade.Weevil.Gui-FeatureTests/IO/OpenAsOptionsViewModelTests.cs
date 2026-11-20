namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;

	/// <summary>
	/// RAM or Disk in the Open As dialog, as the analyst selects one log after another (#1215).
	/// </summary>
	[TestClass]
	public class OpenAsOptionsViewModelTests
	{
		private static readonly StorageUnit OneGigabyte = new StorageUnit(StorageUnit.BytesPerGigabyte);

		private static readonly BrowserItem SmallLog = Log("Small.log");

		private static readonly BrowserItem LargeLog = Log("Large.log");

		[TestMethod]
		public async Task GivenALogSelected_WhenItsAdviceIsReady_ThenTheModeSuggestedIsChosen()
		{
			var options = new OpenAsOptionsViewModel(Advise);
			options.CanOpen.Should().BeFalse("no log is selected");

			await options.SelectAsync(LargeLog);

			options.ChosenMode.Should().Be(StorageMode.Disk, "the large log does not fit in the free memory");
			options.Suggestion.Should().Contain("Disk");
			options.CanOpen.Should().BeTrue();
		}

		[TestMethod]
		public async Task GivenAModePicked_WhenAnotherLogIsSelected_ThenTheModeIsKeptAndItsWarningShown()
		{
			// Once the analyst picks a mode, selecting another log does not change it (decision 7 of the #1157 plan).
			var options = new OpenAsOptionsViewModel(Advise);
			await options.SelectAsync(LargeLog);
			options.ChosenMode.Should().Be(StorageMode.Disk, "Disk is suggested for the large log");
			options.ChosenMode = StorageMode.Ram;

			await options.SelectAsync(SmallLog);
			await options.SelectAsync(LargeLog);

			options.ChosenMode.Should().Be(StorageMode.Ram);
			options.Suggestion.Should().Contain("Disk", "Weevil still says what it would suggest");
			options.Warning.Should().Contain("may become slow");
		}

		[TestMethod]
		public async Task GivenALogWhoseAdviceIsSlow_WhenAnotherLogIsSelectedMeanwhile_ThenTheSlowAdviceIsThrownAway()
		{
			// A log on a slow network drive must not overwrite the advice for the log selected after it.
			using var release = new ManualResetEventSlim();
			var options = new OpenAsOptionsViewModel(file =>
			{
				if (file == LargeLog)
				{
					release.Wait(TimeSpan.FromSeconds(10));
				}

				return Advise(file);
			});

			Task slowSelection = options.SelectAsync(LargeLog);
			await options.SelectAsync(SmallLog);
			release.Set();
			await slowSelection;

			options.Advice.FileName.Should().Be("Small.log");
			options.ChosenMode.Should().Be(StorageMode.Ram);
		}

		[TestMethod]
		public async Task GivenAFileWhoseAdviceFails_WhenSelected_ThenItCanStillBeOpenedInRam()
		{
			// Browsing must never stop Weevil: the open itself reports why a log cannot be read.
			var options = new OpenAsOptionsViewModel(_ => throw new InvalidOperationException("Unexpected."));

			await options.SelectAsync(SmallLog);

			options.CanOpen.Should().BeTrue();
			options.ChosenMode.Should().Be(StorageMode.Ram);
		}

		[TestMethod]
		public async Task GivenALogSelected_WhenAFolderIsSelected_ThenNoLogCanBeOpened()
		{
			var options = new OpenAsOptionsViewModel(Advise);
			await options.SelectAsync(SmallLog);

			await options.SelectAsync(new BrowserItem(Location.ForFileSystem(Path.GetTempPath()), "Logs", ItemKind.Folder));

			options.HasFile.Should().BeFalse();
			options.CanOpen.Should().BeFalse();
		}

		/// <summary>
		/// 8 GB is free: the small log needs 1 GB, the large one 10 GB.
		/// </summary>
		private static OpenAsAdvice Advise(BrowserItem file)
		{
			var estimate = file == LargeLog ? new StorageUnit(10 * StorageUnit.BytesPerGigabyte) : OneGigabyte;

			return new OpenAsAdvice(file.Name, OneGigabyte, estimate, new StorageUnit(8 * StorageUnit.BytesPerGigabyte), isOnNetworkDrive: false);
		}

		private static BrowserItem Log(string name)
		{
			return new BrowserItem(Location.ForFileSystem(Path.Combine(Path.GetTempPath(), name)), name, ItemKind.File, size: 1_000);
		}
	}
}
