namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.IO;
	using System.IO.Compression;
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;
	using BlueDotBrigade.Weevil.Windows.IO;

	/// <summary>
	/// What the Open As dialog reads about the log selected, without opening or extracting it (#1216).
	/// </summary>
	[TestClass]
	public class OpenAsLookupTests
	{
		private string _folder = string.Empty;

		[TestInitialize]
		public void CreateFolder()
		{
			_folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(OpenAsLookupTests), Guid.NewGuid().ToString("N"))).FullName;
		}

		[TestCleanup]
		public void DeleteFolder()
		{
			Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		public void GivenALogInAZip_WhenItsPluginSuppliesOptions_ThenTheyAreFound()
		{
			var zip = CreateZip("Logs.zip", "Logs/App.log");
			var provider = new FakeProvider(new PluginFactory().Create(zip).Name);
			using var reader = new ContainerReader();

			IOpenAsOptionsProvider? found = OpenAsLookup.FindOptionsProvider(new PluginFactory(), new[] { provider }, reader, InZip(zip, "Logs/App.log"));

			found.Should().BeSameAs(provider);
		}

		[TestMethod]
		public void GivenAZipEntryWhoseNameHasADrive_WhenSelected_ThenNothingIsWrittenOutsideTheTemporaryFolder()
		{
			// A name such as C:evil.log would have put a copy of it, or of its sidecar, in the current folder of drive C.
			var zip = CreateZip("Evil.zip", "C:evil.log", "C:evil.log.xml");
			var provider = new FakeProvider(new PluginFactory().Create(zip).Name);
			using var reader = new ContainerReader();
			BrowserItem evil = InZip(zip, "C:evil.log");

			OpenAsLookup.FindOptionsProvider(new PluginFactory(), new[] { provider }, reader, evil).Should().BeNull();
			OpenAsLookup.ReadParts(reader, _folder, evil).HasSidecar.Should().BeFalse();

			File.Exists("C:evil.log").Should().BeFalse();
			File.Exists("C:evil.log.xml").Should().BeFalse();
		}

		[TestMethod]
		public void GivenAZipEntryNamedDotDot_WhenSelected_ThenItIsRefused()
		{
			// Refused on every system, as extracting it is: it names the folder above.
			var zip = CreateZip("Up.zip", "Logs/..");
			var provider = new FakeProvider(new PluginFactory().Create(zip).Name);
			using var reader = new ContainerReader();
			BrowserItem up = InZip(zip, "Logs/..");

			OpenAsLookup.FindOptionsProvider(new PluginFactory(), new[] { provider }, reader, up).Should().BeNull();
			OpenAsLookup.ReadParts(reader, _folder, up).HasSidecar.Should().BeFalse();
		}

		[TestMethod]
		public void GivenALogInAZipOpenedBefore_WhenSelected_ThenTheKeptSidecarsPartsAreListed()
		{
			// The sidecar kept beside the copy of an earlier open holds the analyst's regions (#1253), so it wins over the one in the zip.
			var zip = CreateZip("Logs.zip", ("App.log", "2026-11-22 10:00:00 Info A record."), ("App.log.xml", Sidecar("InTheZip")));
			using var reader = new ContainerReader();
			BrowserItem log = InZip(zip, "App.log");
			var kept = Directory.CreateDirectory(Path.Combine(_folder, reader.GetCopyKey(log.Location))).FullName;
			File.WriteAllText(Path.Combine(kept, "App.log.xml"), Sidecar("Kept"));

			SidecarParts parts = OpenAsLookup.ReadParts(reader, _folder, log);

			parts.HasSidecar.Should().BeTrue();
			parts.Parts.Should().ContainSingle().Which.RegionName.Should().Be("Kept");
		}

		[TestMethod]
		public void GivenALogInAZipNeverOpened_WhenSelected_ThenTheZipsSidecarIsReadWithoutLeavingACopy()
		{
			var zip = CreateZip("Logs.zip", ("App.log", "2026-11-22 10:00:00 Info A record."), ("App.log.xml", Sidecar("InTheZip")));
			using var reader = new ContainerReader();
			var copiesBefore = Directory.GetDirectories(Path.GetTempPath(), "Weevil.OpenAs.*").Length;

			SidecarParts parts = OpenAsLookup.ReadParts(reader, _folder, InZip(zip, "App.log"));

			parts.Parts.Should().ContainSingle().Which.RegionName.Should().Be("InTheZip");
			Directory.GetDirectories(Path.GetTempPath(), "Weevil.OpenAs.*").Should().HaveCount(copiesBefore, "the temporary copy of the sidecar is deleted");
		}

		[TestMethod]
		public void GivenALogStoredOnlineOnly_WhenSelected_ThenItIsNotRead()
		{
			// Reading any of it would download it from OneDrive. The file isn't there, so reading it would throw.
			var provider = new FakeProvider("Any plugin");
			var cloudLog = new BrowserItem(Location.ForFileSystem(Path.Combine(_folder, "Cloud.log")), "Cloud.log", ItemKind.File, size: 1_000, isOnlineOnly: true);
			using var reader = new ContainerReader();

			OpenAsLookup.FindOptionsProvider(new PluginFactory(), new[] { provider }, reader, cloudLog).Should().BeNull();
		}

		private string CreateZip(string name, params string[] entries)
		{
			return CreateZip(name, Array.ConvertAll(entries, entry => (entry, "2026-11-22 10:00:00 Info A record.")));
		}

		private string CreateZip(string name, params (string Entry, string Content)[] entries)
		{
			var path = Path.Combine(_folder, name);

			using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
			{
				foreach ((var entry, var content) in entries)
				{
					using var writer = new StreamWriter(archive.CreateEntry(entry).Open());
					writer.Write(content);
				}
			}

			return path;
		}

		/// <summary>
		/// A sidecar that lists one region.
		/// </summary>
		private static string Sidecar(string regionName)
		{
			return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"<WeevilSidecar xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/BlueDotBrigade.Weevil.Configuration.Sidecar.v2\">" +
				$"<CommonData><Regions><Region><Name>{regionName}</Name><Minimum><ByteOffset>-1</ByteOffset><LineNumber>1</LineNumber></Minimum>" +
				"<Maximum><ByteOffset>-1</ByteOffset><LineNumber>1</LineNumber></Maximum></Region></Regions><Context/><FilterHistory><ExclusiveFilters/><InclusiveFilters/></FilterHistory><Records/>" +
				"<TableOfContents></TableOfContents><UserRemarks/></CommonData>" +
				"<Header><SavedAt>2026-11-22T00:00:00</SavedAt><SchemaVersion xmlns:d3p1=\"http://schemas.datacontract.org/2004/07/System\">" +
				"<d3p1:_Build>0</d3p1:_Build><d3p1:_Major>4</d3p1:_Major><d3p1:_Minor>2</d3p1:_Minor><d3p1:_Revision>-1</d3p1:_Revision></SchemaVersion></Header>" +
				"<PluginData i:nil=\"true\" /></WeevilSidecar>";
		}

		private static BrowserItem InZip(string zip, string entry)
		{
			return new BrowserItem(Location.ForArchive(zip, entry), Path.GetFileName(entry.Replace('/', Path.DirectorySeparatorChar)), ItemKind.File, size: 100);
		}

		private sealed class FakeProvider : IOpenAsOptionsProvider
		{
			public FakeProvider(string pluginName)
			{
				this.PluginName = pluginName;
			}

			public string PluginName { get; }

			public IOpenAsOptions CreateOpenAsOptions() => throw new NotSupportedException();

			public object CreateOpenAsView(IOpenAsOptions options) => throw new NotSupportedException();
		}
	}
}
