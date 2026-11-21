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

		[TestMethod]
		public async Task GivenALogWithSectionsAndRegions_WhenOneIsPicked_ThenThatPartIsOpened()
		{
			// The analyst opens one section or one region, listed from the log's sidecar (#1212, #1213).
			var options = new OpenAsOptionsViewModel(Advise, ReadParts);
			using var monitor = options.Monitor();
			await options.SelectAsync(LargeLog);
			monitor.Should().RaisePropertyChangeFor(o => o.HasSections, "the view enables the section list only when it is told");
			monitor.Should().RaisePropertyChangeFor(o => o.HasRegions);
			options.Part.IsWhole.Should().BeTrue("the whole log is opened unless another part is picked");
			options.Sections.Should().Equal("Startup (lines 1 – 119)", "Shutdown (from line 300)");

			options.ChosenSectionIndex = 1;
			options.Part.SectionName.Should().Be("Shutdown");

			// The first item of a list is picked like any other: nothing is picked in advance, which would make picking it do nothing.
			options.ChosenRegionIndex = 0;
			options.Part.RegionName.Should().Be("Docking", "one part is opened at a time");
			options.CanOpen.Should().BeTrue();
		}

		[TestMethod]
		public async Task GivenARegionPicked_WhenAnotherLogIsSelected_ThenTheWholeLogIsOpened()
		{
			// A region belongs to its log, so it is not kept for the next one.
			var options = new OpenAsOptionsViewModel(Advise, ReadParts);
			await options.SelectAsync(LargeLog);
			options.ChosenPart = PartKind.Region;
			options.Part.RegionName.Should().Be("Docking", "choosing Region picks the first region");
			options.ChosenRegionIndex.Should().Be(0, "the list shows the region picked");

			await options.SelectAsync(SmallLog);

			options.ChosenPart.Should().Be(PartKind.Whole);
			options.HasRegions.Should().BeFalse();
			options.Part.IsWhole.Should().BeTrue();
		}

		[TestMethod]
		public async Task GivenLinesTyped_WhenTheyAreNotARange_ThenOpenIsDisabledAndSaysWhy()
		{
			// For lines, the analyst types a first and last line. An invalid range disables Open and says why (#1214).
			var options = new OpenAsOptionsViewModel(Advise);
			await options.SelectAsync(SmallLog);

			options.FirstLine = "40";
			options.LastLine = "10";

			options.ChosenPart.Should().Be(PartKind.Lines, "typing a line picks Lines");
			options.CanOpen.Should().BeFalse();
			options.PartError.Should().Contain("before the first");

			options.LastLine = "x";
			options.CanOpen.Should().BeFalse();
			options.PartError.Should().Contain("whole number");
		}

		[TestMethod]
		public async Task GivenLinesTyped_WhenTheyAreARange_ThenThoseLinesAreOpened()
		{
			var options = new OpenAsOptionsViewModel(Advise);
			await options.SelectAsync(SmallLog);

			options.FirstLine = "10";
			options.LastLine = "40";
			options.Part.Lines.Should().Be(new Range(10, 40));
			options.CanOpen.Should().BeTrue();
			options.PartError.Should().BeEmpty();

			options.LastLine = string.Empty;
			options.Part.IsToEndOfLog.Should().BeTrue("an empty last line is the end of the log");
			options.Part.Lines.Start.Value.Should().Be(10);
		}

		[TestMethod]
		public async Task GivenLinesTyped_WhenAnotherLogIsSelected_ThenTheWholeLogIsChosenAndTheLinesCleared()
		{
			// As in the proof of concept (#1209).
			var options = new OpenAsOptionsViewModel(Advise);
			await options.SelectAsync(SmallLog);
			options.FirstLine = "10";

			await options.SelectAsync(LargeLog);

			options.ChosenPart.Should().Be(PartKind.Whole);
			options.Part.IsWhole.Should().BeTrue();
			options.FirstLine.Should().BeEmpty("the lines typed were for the other log");
		}

		[TestMethod]
		public async Task GivenLinesTyped_WhenTheBrowserSelectsTheSameLogAgain_ThenTheLinesAreKept()
		{
			// Refreshing, sorting or filtering the browser's list selects nothing, then the same log again, as a new item.
			var options = new OpenAsOptionsViewModel(Advise);
			await options.SelectAsync(SmallLog);
			options.FirstLine = "10";
			options.LastLine = "40";

			await options.SelectAsync(null);
			await options.SelectAsync(Log("Small.log"));

			options.ChosenPart.Should().Be(PartKind.Lines);
			options.Part.Lines.Should().Be(new Range(10, 40));
		}

		[TestMethod]
		public async Task GivenLogsWithAndWithoutASidecar_WhenSelected_ThenTheDialogSaysWhatTheirSidecarsHold()
		{
			// The analyst knows why no section or region can be picked (#1209).
			var options = new OpenAsOptionsViewModel(Advise, ReadParts);

			await options.SelectAsync(LargeLog);
			options.PartsStatus.Should().Be("Its sidecar lists 2 sections and 1 named region.");

			await options.SelectAsync(SmallLog);
			options.PartsStatus.Should().Be("No sidecar, so no sections or regions.");
		}

		[TestMethod]
		public async Task GivenSidecarsThatListNothingOrCantBeRead_WhenSelected_ThenTheDialogSaysSo()
		{
			// Regions marked without names can't be opened by name, so they aren't counted.
			var options = new OpenAsOptionsViewModel(Advise, file => file == SmallLog
				? new SidecarParts(true, Array.Empty<LogPart>())
				: throw new NotSupportedException("This file's sidecar is not compatible with this version of the application."));

			await options.SelectAsync(SmallLog);
			options.PartsStatus.Should().Be("Its sidecar lists no named sections or regions.");

			await options.SelectAsync(LargeLog);
			options.PartsStatus.Should().StartWith("Its sidecar can't be read");
			options.CanOpen.Should().BeTrue("the whole log can still be opened");
		}

		[TestMethod]
		public async Task GivenAnOnlineOnlyLog_WhenSelected_ThenItsSidecarIsNotRead()
		{
			// Reading an online-only file downloads it from OneDrive, so its sidecar is not read just because it is selected (#1209).
			var onlineOnlyLog = new BrowserItem(Location.ForFileSystem(Path.Combine(Path.GetTempPath(), "Cloud.log")), "Cloud.log", ItemKind.File, size: 1_000, isOnlineOnly: true);
			var options = new OpenAsOptionsViewModel(Advise, _ => throw new InvalidOperationException("The sidecar of an online-only log was read."));

			await options.SelectAsync(onlineOnlyLog);

			options.PartsStatus.Should().Contain("online only");
			options.HasSections.Should().BeFalse();
			options.CanOpen.Should().BeTrue("the whole log can still be opened");
		}

		/// <summary>
		/// The large log's sidecar lists two level-1 sections and one region; the small log has no sidecar.
		/// </summary>
		private static SidecarParts ReadParts(BrowserItem file)
		{
			return file == LargeLog
				? new SidecarParts(true, new[]
				{
					LogPart.ForSection("Startup", new Range(1, 119)),
					LogPart.ForSection("Shutdown", new Range(300, int.MaxValue)),
					LogPart.ForRegion("Docking", new Range(10, 40)),
				})
				: SidecarParts.NoSidecar;
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
