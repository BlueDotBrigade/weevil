namespace BlueDotBrigade.Weevil.Windows.Dialogs
{
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;

	/// <summary>
	/// What the file browser shows as it navigates, with a substitute reader, so no folder needs to exist.
	/// </summary>
	[TestClass]
	public class FileBrowserModelTests
	{
		private static readonly Location Logs = Location.ForFileSystem(Path.Combine(Path.GetTempPath(), "Logs"));
		private static readonly Location Archive = Logs.Combine("Archive", entersContainer: false);

		private IContainerReader _reader = null!;
		private FileBrowserModel _model = null!;

		[TestInitialize]
		public void Setup()
		{
			_reader = Substitute.For<IContainerReader>();
			_reader.List(Arg.Any<Location>()).Returns(call => new ListingResult(call.Arg<Location>(), Array.Empty<BrowserItem>()));
			_reader.List(Logs).Returns(new ListingResult(Logs, new[]
			{
				File(Logs, "b.log"),
				File(Logs, "App.log.xml"),
				new BrowserItem(Archive, "Archive", ItemKind.Folder),
				File(Logs, "a.log"),
				new BrowserItem(Logs.Combine("Bundle.zip", entersContainer: false), "Bundle.zip", ItemKind.Container),
			}));

			_model = new FileBrowserModel(_reader);
		}

		[TestMethod]
		public async Task GivenAFolder_WhenNavigatedTo_ThenFoldersComeFirstThenZipsAndFilesByName()
		{
			await _model.NavigateAsync(Logs);

			Names().Should().Equal("Archive", "a.log", "App.log.xml", "b.log", "Bundle.zip");
		}

		[TestMethod]
		public async Task GivenAFolder_WhenNavigatedTo_ThenNothingIsSelected()
		{
			// So entering a folder does not make the host read its first file.
			await _model.NavigateAsync(Logs);

			_model.SelectedItem.Should().BeNull();
		}

		[TestMethod]
		public async Task GivenANameFilter_WhenApplied_ThenOnlyItemsWhoseNameContainsItRemain()
		{
			await _model.NavigateAsync(Logs);

			_model.NameFilter = "LOG";

			Names().Should().Equal("a.log", "App.log.xml", "b.log");
		}

		[TestMethod]
		public async Task GivenAFileType_WhenChosen_ThenOtherFilesAreHiddenButFoldersAndZipsRemain()
		{
			var logFiles = new FileTypeFilter("Log files", new[] { "*.log" }, new[] { "*.xml" });
			_model.FileTypes = new[] { logFiles, FileTypeFilter.AllFiles };
			await _model.NavigateAsync(Logs);

			Names().Should().Equal("Archive", "a.log", "b.log", "Bundle.zip");
		}

		[TestMethod]
		public async Task GivenASubfolder_WhenGoingUp_ThenTheSubfolderIsSelectedInItsParent()
		{
			await _model.NavigateAsync(Archive);

			await _model.GoUpAsync();

			_model.CurrentLocation.Should().Be(Logs);
			_model.SelectedItem!.Name.Should().Be("Archive");
		}

		[TestMethod]
		public async Task GivenAPlaceThatCannotBeListed_WhenNavigatedTo_ThenItsErrorIsShown()
		{
			_reader.List(Archive).Returns(new ListingResult(Archive, "Access is denied."));

			await _model.NavigateAsync(Archive);

			_model.HasError.Should().BeTrue();
			_model.Error.Should().Be("Access is denied.");
		}

		[TestMethod]
		public async Task GivenASlowListing_WhenANewerNavigationFinishesFirst_ThenTheSlowListingIsDiscarded()
		{
			// A folder on an unreachable share cannot be cancelled, so its late result must not replace the newer one.
			using var release = new ManualResetEventSlim();
			Location slow = Location.ForFileSystem(Path.Combine(Path.GetTempPath(), "Slow"));
			_reader.List(slow).Returns(_ =>
			{
				release.Wait(TimeSpan.FromSeconds(10));
				return new ListingResult(slow, new[] { File(slow, "late.log") });
			});

			Task slowNavigation = _model.NavigateAsync(slow);
			await _model.NavigateAsync(Logs);
			release.Set();
			await slowNavigation;

			_model.CurrentLocation.Should().Be(Logs);
			Names().Should().NotContain("late.log");
		}

		[TestMethod]
		public async Task GivenTwoPlacesVisited_WhenGoingBackThenForward_ThenEachIsShownAgain()
		{
			await _model.NavigateAsync(Logs);
			await _model.NavigateAsync(Archive);

			await _model.GoBackAsync();
			_model.CurrentLocation.Should().Be(Logs);

			await _model.GoForwardAsync();
			_model.CurrentLocation.Should().Be(Archive);
		}

		[TestMethod]
		public async Task GivenPlacesVisited_WhenRecentFoldersAreShown_ThenTheNewestIsFirstAndThisPcIsLeftOut()
		{
			await _model.NavigateAsync(Logs);
			await _model.NavigateAsync(Location.Computer);
			await _model.NavigateAsync(Archive);

			_model.RecentFolders.Select(place => place.Location).Should().Equal(Archive, Logs);
		}

		[TestMethod]
		public async Task GivenManyPlacesVisited_WhenRecentFoldersAreShown_ThenOnlyTheLastEightAreKept()
		{
			for (var index = 0; index < 10; index++)
			{
				await _model.NavigateAsync(Logs.Combine($"Folder{index}", entersContainer: false));
			}

			_model.RecentFolders.Should().HaveCount(8);
			_model.RecentFolders[0].Name.Should().Be("Folder9");
		}

		[TestMethod]
		public async Task GivenAFile_WhenActivated_ThenItIsReportedAndTheBrowserStaysWhereItIs()
		{
			await _model.NavigateAsync(Logs);
			BrowserItem? activated = null;
			_model.FileActivated += (_, item) => activated = item;

			await _model.ActivateAsync(_model.VisibleItems.Single(item => item.Name == "a.log"));

			activated!.Name.Should().Be("a.log");
			_model.CurrentLocation.Should().Be(Logs);
		}

		[TestMethod]
		public async Task GivenATypedPathToAFile_WhenGoneTo_ThenTheFileIsSelectedInItsFolderAndNotOpened()
		{
			var folder = Directory.CreateTempSubdirectory("Weevil.FileBrowserModelTests.").FullName;

			try
			{
				var logPath = Path.Combine(folder, "Typed.log");
				System.IO.File.WriteAllText(logPath, "line 1\n");
				Location folderLocation = Location.ForFileSystem(folder);
				_reader.GetKind(Arg.Any<Location>()).Returns(ItemKind.File);
				_reader.List(folderLocation).Returns(new ListingResult(folderLocation, new[] { File(folderLocation, "Typed.log") }));
				var isActivated = false;
				_model.FileActivated += (_, _) => isActivated = true;

				_model.PathText = logPath;
				await _model.GoToPathTextAsync();

				_model.CurrentLocation.Should().Be(folderLocation);
				_model.SelectedItem!.Name.Should().Be("Typed.log");
				isActivated.Should().BeFalse();
			}
			finally
			{
				Directory.Delete(folder, recursive: true);
			}
		}

		private IEnumerable<string> Names() => _model.VisibleItems.Select(item => item.Name);

		private static BrowserItem File(Location folder, string name) => new BrowserItem(folder.Combine(name, entersContainer: false), name, ItemKind.File, size: 10);
	}
}
