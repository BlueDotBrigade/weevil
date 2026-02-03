namespace BlueDotBrigade.Weevil.Configuration.Sidecar
{
	using System;
	using System.Collections.Immutable;
	using System.IO;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Navigation;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SidecarManagerTests
	{
		[TestMethod]
		public void Save_WhenInTemporaryDirectory_SkipsSaveOperation()
		{
			// Arrange - Use temp directory (from compressed archive scenario)
			var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "test.log");
			var sidecarManager = new SidecarManager(tempPath);
			var sidecarData = new SidecarData
			{
				Records = ImmutableArray<IRecord>.Empty,
				Context = new ContextDictionary(),
				FilterTraits = null,
				SourceFileRemarks = string.Empty,
				TableOfContents = null,
				Regions = ImmutableArray<Region>.Empty,
				Bookmarks = ImmutableArray<Bookmark>.Empty
			};

			// Act - should skip save and not throw exception
			sidecarManager.Save(sidecarData, false);

			// Assert - test passes if no exception is thrown and no file is created
			Assert.IsFalse(System.IO.File.Exists($"{tempPath}.xml"), "Sidecar should not be created in temp directory");
		}

		[TestMethod]
		public void Save_WhenNotInTemporaryDirectory_SavesMetadata()
		{
			// Arrange - Use a non-temp directory
			var currentDir = Directory.GetCurrentDirectory();
			var tempFile = Path.Combine(currentDir, $"test_{Guid.NewGuid()}.log");
			System.IO.File.WriteAllText(tempFile, "test content");

			try
			{
				var sidecarManager = new SidecarManager(tempFile);
				var sidecarData = new SidecarData
				{
					Records = ImmutableArray<IRecord>.Empty,
					Context = new ContextDictionary(),
					FilterTraits = null,
					SourceFileRemarks = "Test remarks",
					TableOfContents = null,
					Regions = ImmutableArray<Region>.Empty,
					Bookmarks = ImmutableArray<Bookmark>.Empty
				};

				// Act
				sidecarManager.Save(sidecarData, true);

				// Assert
				var sidecarPath = $"{tempFile}.xml";
				Assert.IsTrue(System.IO.File.Exists(sidecarPath), "Sidecar file should be created in non-temp directory");
			}
			finally
			{
				// Cleanup
				try
				{
					System.IO.File.Delete(tempFile);
					System.IO.File.Delete($"{tempFile}.xml");
					System.IO.File.Delete($"{tempFile}.xml~");
				}
				catch
				{
					// Ignore cleanup errors
				}
			}
		}
	}
}
