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
