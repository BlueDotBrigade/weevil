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
		public void GivenALogInAZip_WhenItsPluginSuppliesOptions_ThenTheyAreFoundWithoutExtractingTheLog()
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
			var path = Path.Combine(_folder, name);

			using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
			{
				foreach (var entry in entries)
				{
					using var writer = new StreamWriter(archive.CreateEntry(entry).Open());
					writer.Write("2026-11-22 10:00:00 Info A record.");
				}
			}

			return path;
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
