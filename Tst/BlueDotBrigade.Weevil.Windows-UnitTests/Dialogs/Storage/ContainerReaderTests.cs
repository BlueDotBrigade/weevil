namespace BlueDotBrigade.Weevil.Windows.Dialogs.Storage
{
	using System.IO.Compression;
	using System.Text;

	[TestClass]
	public class ContainerReaderTests
	{
		private static readonly string[] Companions = { ".xml", ".idx" };

		private string _root = string.Empty;
		private string _logs = string.Empty;
		private string _zipPath = string.Empty;
		private ContainerReader _reader = null!;

		/// <summary>
		/// A folder of logs, with a sidecar and an index for one of them, a rolled log, and a zip of logs that holds a zip of its own.
		/// </summary>
		[TestInitialize]
		public void Setup()
		{
			_root = Directory.CreateTempSubdirectory("Weevil.ContainerReaderTests.").FullName;
			_logs = Directory.CreateDirectory(Path.Combine(_root, "Logs")).FullName;
			File.WriteAllText(Path.Combine(_logs, "App.log"), "line 1\n");
			File.WriteAllText(Path.Combine(_logs, "App.log.xml"), "<sidecar />");
			File.WriteAllText(Path.Combine(_logs, "App.log.idx"), "index");
			File.WriteAllText(Path.Combine(_logs, "App.log.1"), "rolled\n");
			Directory.CreateDirectory(Path.Combine(_logs, "Archive"));
			File.WriteAllText(Path.Combine(_logs, "Broken.zip"), "not a zip");

			byte[] innerZip;
			using (var memory = new MemoryStream())
			{
				using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
				{
					Add(zip, "Old.log", "old log\n");
					Add(zip, "Old.log.xml", "<sidecar />");
				}

				innerZip = memory.ToArray();
			}

			_zipPath = Path.Combine(_logs, "Bundle.zip");
			using (ZipArchive zip = ZipFile.Open(_zipPath, ZipArchiveMode.Create))
			{
				Add(zip, "2026/");
				Add(zip, "2026/Server.log", "server log\n");
				Add(zip, "2026/Server.log.xml", "<sidecar />");
				Add(zip, "2026/Server.log.idx", "idx");
				Add(zip, @"Windows\Client.log", "client log\n");
				Add(zip, "Nested/Inner.zip", innerZip);
				Add(zip, "Readme.txt", "readme");
			}

			_reader = new ContainerReader();
		}

		[TestCleanup]
		public void Cleanup()
		{
			_reader.Dispose();
			Directory.Delete(_root, recursive: true);
		}

		[TestMethod]
		public void GivenAFolder_WhenListed_ThenItsFilesFoldersAndZipsAreListed()
		{
			ListingResult listing = _reader.List(Location.ForFileSystem(_logs));

			listing.Error.Should().BeNull();
			Names(listing).Should().BeEquivalentTo(
				"App.log:File", "App.log.1:File", "App.log.idx:File", "App.log.xml:File", "Archive:Folder", "Broken.zip:Container", "Bundle.zip:Container");
		}

		[TestMethod]
		public void GivenAMissingFolder_WhenListed_ThenTheErrorSaysSo()
		{
			_reader.List(Location.ForFileSystem(Path.Combine(_root, "Missing"))).Error.Should().Be("This folder does not exist.");
		}

		[TestMethod]
		public void GivenAZip_WhenListed_ThenItsFoldersAreListedIncludingThoseKnownOnlyFromPaths()
		{
			ListingResult listing = _reader.List(Location.ForArchive(_zipPath));

			listing.Error.Should().BeNull();
			Names(listing).Should().BeEquivalentTo("2026:Folder", "Nested:Folder", "Readme.txt:File", "Windows:Folder");
		}

		[TestMethod]
		public void GivenAPathWrittenWithBackslashesInAZip_WhenItsFolderIsListed_ThenItHoldsTheFile()
		{
			Names(_reader.List(Location.ForArchive(_zipPath, "Windows"))).Should().Equal("Client.log:File");
		}

		[TestMethod]
		public void GivenAZipInAZip_WhenListed_ThenItIsAContainerWhoseFilesAreListed()
		{
			BrowserItem inner = _reader.List(Location.ForArchive(_zipPath, "Nested")).Items.Single();

			inner.Kind.Should().Be(ItemKind.Container);
			Names(_reader.List(inner.Open)).Should().BeEquivalentTo("Old.log:File", "Old.log.xml:File");
		}

		[TestMethod]
		public void GivenADamagedZip_WhenListed_ThenTheErrorSaysSo()
		{
			_reader.List(Location.ForArchive(Path.Combine(_logs, "Broken.zip"))).Error.Should().Be("This is not a zip, or it is damaged.");
		}

		[TestMethod]
		public void GivenAFolderMissingFromAZip_WhenListed_ThenTheErrorSaysSo()
		{
			_reader.List(Location.ForArchive(_zipPath, "NoSuchFolder")).Error.Should().Be("This folder is not in the zip.");
		}

		[TestMethod]
		public void GivenTypedPaths_WhenTheirKindIsAsked_ThenAFileAFolderInAZipAndAZipAreTold()
		{
			_reader.GetKind(Location.ForArchive(_zipPath, "Nested/Inner.zip", "Old.log")).Should().Be(ItemKind.File);
			_reader.GetKind(Location.ForArchive(_zipPath, "Nested")).Should().Be(ItemKind.Folder);
			_reader.GetKind(Location.ForArchive(_zipPath)).Should().Be(ItemKind.Container);
			_reader.GetKind(Location.ForFileSystem(Path.Combine(_root, "Nowhere"))).Should().BeNull();
		}

		[TestMethod]
		public void GivenALogInAZip_WhenItsSidecarIsLookedFor_ThenOnlyAnExistingOneIsFound()
		{
			Location log = Location.ForArchive(_zipPath, "2026/Server.log");

			_reader.Exists(ContainerReader.GetSibling(log, "Server.log.xml")).Should().BeTrue();
			_reader.Exists(ContainerReader.GetSibling(log, "Server.log.missing")).Should().BeFalse();
		}

		[TestMethod]
		public void GivenAFileInAZipInAZip_WhenRead_ThenItsContentIsReturned()
		{
			using Stream stream = _reader.OpenRead(Location.ForArchive(_zipPath, "Nested/Inner.zip", "Old.log"));
			using var text = new StreamReader(stream);

			text.ReadToEnd().Should().Be("old log\n");
		}

		[TestMethod]
		public void GivenALogOnDisk_WhenExtracted_ThenItIsOpenedWhereItIs()
		{
			var destination = Path.Combine(_root, "Unused");

			_reader.Extract(Location.ForFileSystem(Path.Combine(_logs, "App.log")), Companions, destination).Should().Be(Path.Combine(_logs, "App.log"));
			Directory.Exists(destination).Should().BeFalse();
		}

		[TestMethod]
		public void GivenALogInAZip_WhenExtracted_ThenItsSidecarAndIndexComeWithIt()
		{
			var extracted = _reader.Extract(Location.ForArchive(_zipPath, "2026/Server.log"), Companions, Path.Combine(_root, "Extracted"));

			File.ReadAllText(extracted).Should().Be("server log\n");
			File.Exists($"{extracted}.xml").Should().BeTrue();
			File.ReadAllText($"{extracted}.idx").Should().Be("idx");
		}

		[TestMethod]
		public void GivenALogInAZipInAZip_WhenExtracted_ThenOnlyTheCompanionsItHasComeWithIt()
		{
			var extracted = _reader.Extract(Location.ForArchive(_zipPath, "Nested/Inner.zip", "Old.log"), Companions, Path.Combine(_root, "Extracted"));

			File.ReadAllText(extracted).Should().Be("old log\n");
			File.Exists($"{extracted}.xml").Should().BeTrue();
			File.Exists($"{extracted}.idx").Should().BeFalse();
		}

		[TestMethod]
		public void GivenALogInAZipExtractedBefore_WhenExtractedAgain_ThenTheSidecarKeptBesideItIsNotReplaced()
		{
			// The kept sidecar holds the comments and pins added since the log was last opened from the zip (#1253).
			Location log = Location.ForArchive(_zipPath, "2026/Server.log");
			var destination = Path.Combine(_root, "Kept");
			var extracted = _reader.Extract(log, Companions, destination);
			File.WriteAllText($"{extracted}.xml", "<sidecar comments=\"kept\" />");
			File.Delete(extracted);

			_reader.Extract(log, Companions, destination).Should().Be(extracted);

			File.ReadAllText(extracted).Should().Be("server log\n", "the log itself is extracted again");
			File.ReadAllText($"{extracted}.xml").Should().Be("<sidecar comments=\"kept\" />");
		}

		[TestMethod]
		public void GivenALogInAZip_WhenItsCopyKeyIsAskedTwice_ThenItIsTheSameShortName()
		{
			// The same file of the same zip finds the folder its sidecar and index were kept in (#1253).
			Location log = Location.ForArchive(_zipPath, "2026/Server.log");

			var key = _reader.GetCopyKey(log);

			key.Should().MatchRegex("^[0-9a-f]{16}$", "a short folder name keeps paths well inside MAX_PATH");
			new ContainerReader().GetCopyKey(Location.ForArchive(_zipPath.ToUpperInvariant(), "2026/SERVER.LOG")).Should().Be(key, "Windows ignores case in paths");
		}

		[TestMethod]
		public void GivenAnotherFileOrZip_WhenItsCopyKeyIsAsked_ThenItDiffersFromTheFirst()
		{
			var key = _reader.GetCopyKey(Location.ForArchive(_zipPath, "2026/Server.log"));

			// The same name with other content, in a zip of the same path, is another log: its CRC-32 and size differ.
			using (ZipArchive zip = ZipFile.Open(_zipPath, ZipArchiveMode.Update))
			{
				zip.GetEntry("2026/Server.log")!.Delete();
				Add(zip, "2026/Server.log", "a newer server log\n");
			}

			var copy = Path.Combine(_logs, "Copy.zip");
			File.Copy(_zipPath, copy);

			new ContainerReader().GetCopyKey(Location.ForArchive(_zipPath, "2026/Server.log")).Should().NotBe(key, "the entry's content changed");
			new ContainerReader().GetCopyKey(Location.ForArchive(copy, "2026/Server.log")).Should().NotBe(key, "it is in another zip");
		}

		[TestMethod]
		public void GivenTheSameFileInTwoZipsInAZip_WhenTheirCopyKeysAreAsked_ThenTheyDiffer()
		{
			// The file's name, CRC-32 and size are the same: only the name of the zip it is in tells them apart.
			using (ZipArchive zip = ZipFile.Open(_zipPath, ZipArchiveMode.Update))
			{
				using var inner = new MemoryStream();
				using (Stream entry = zip.GetEntry("Nested/Inner.zip")!.Open())
				{
					entry.CopyTo(inner);
				}

				Add(zip, "Copy/Inner.zip", inner.ToArray());
			}

			using var reader = new ContainerReader();

			reader.GetCopyKey(Location.ForArchive(_zipPath, "Copy/Inner.zip", "Old.log"))
				.Should().NotBe(reader.GetCopyKey(Location.ForArchive(_zipPath, "Nested/Inner.zip", "Old.log")));
		}

		[TestMethod]
		public void GivenAFolderKeptFromAnEarlierOpen_WhenAnExtractionIntoItFails_ThenWhatWasKeptStays()
		{
			// A failed copy into a kept folder removes only what it copied: the kept sidecar holds the analyst's work (#1253).
			var destination = Directory.CreateDirectory(Path.Combine(_root, "Kept")).FullName;
			var keptSidecar = Path.Combine(destination, "App.log.xml");
			File.WriteAllText(keptSidecar, "<sidecar comments=\"kept\" />");

			Action extract = () => _reader.Extract(Location.ForArchive(Path.Combine(_logs, "Broken.zip"), "App.log"), Companions, destination);

			extract.Should().Throw<InvalidDataException>();
			File.ReadAllText(keptSidecar).Should().Be("<sidecar comments=\"kept\" />");
		}

		[TestMethod]
		public void GivenACopyOfTheSameFileKept_WhenExtractedAgain_ThenTheCopyIsUsedAsItIs()
		{
			// The copy may be the log open in Weevil, which must not be rewritten (found in review of #1253).
			Location log = Location.ForArchive(_zipPath, "2026/Server.log");
			var destination = Path.Combine(_root, "Kept");
			var extracted = _reader.Extract(log, Companions, destination);
			var written = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			File.SetLastWriteTimeUtc(extracted, written);

			_reader.Extract(log, Companions, destination);

			File.GetLastWriteTimeUtc(extracted).Should().Be(written, "a copy of the entry's size is the same file");
		}

		[TestMethod]
		[WorkItem(1251)]
		public void GivenAnExtractionFromAZip_WhenCancelled_ThenNothingIsLeftInTheDestination()
		{
			// Closing the Open As dialog left the copy running, and the copy in the temporary folder.
			var destination = Path.Combine(_root, "Extracted");
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();

			Action extract = () => _reader.Extract(Location.ForArchive(_zipPath, "2026/Server.log"), Companions, destination, cancellation.Token);

			extract.Should().Throw<OperationCanceledException>();
			Directory.Exists(destination).Should().BeFalse();
		}

		[TestMethod]
		[WorkItem(1251)]
		public void GivenADamagedZip_WhenALogIsExtracted_ThenNothingIsLeftInTheDestination()
		{
			// A failed extraction left its folder in the temporary folder.
			var destination = Path.Combine(_root, "Extracted");

			Action extract = () => _reader.Extract(Location.ForArchive(Path.Combine(_logs, "Broken.zip"), "App.log"), Companions, destination);

			extract.Should().Throw<InvalidDataException>();
			Directory.Exists(destination).Should().BeFalse();
		}

		[TestMethod]
		[WorkItem(1251)]
		public void GivenADisposedReader_WhenAZipInAZipIsListed_ThenAnErrorIsReturned()
		{
			// A listing that was still running when the dialog closed made a new copy of the inner zip, which nothing deleted.
			Location innerZip = _reader.List(Location.ForArchive(_zipPath, "Nested")).Items.Single().Open;
			_reader.Dispose();

			ListingResult listing = _reader.List(innerZip);

			listing.Error.Should().NotBeNull();
			listing.Items.Should().BeEmpty();
		}

		private static IEnumerable<string> Names(ListingResult listing) => listing.Items.Select(item => $"{item.Name}:{item.Kind}");

		private static void Add(ZipArchive zip, string name, string content) => Add(zip, name, Encoding.UTF8.GetBytes(content));

		private static void Add(ZipArchive zip, string name, byte[]? content = null)
		{
			ZipArchiveEntry entry = zip.CreateEntry(name);

			if (content is not null)
			{
				using Stream stream = entry.Open();
				stream.Write(content);
			}
		}
	}
}
