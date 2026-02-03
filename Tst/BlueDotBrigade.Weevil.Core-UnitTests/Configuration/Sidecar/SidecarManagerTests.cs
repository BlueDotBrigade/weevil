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
		public void Save_WhenDirectoryDoesNotExist_DoesNotThrowException()
		{
			// Arrange
			var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "test.log");
			var sidecarManager = new SidecarManager(nonExistentPath);
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

			// Act - should not throw exception
			sidecarManager.Save(sidecarData, false);

			// Assert - test passes if no exception is thrown
		}

		[TestMethod]
		public void Save_WhenDirectoryIsReadOnly_DoesNotThrowException()
		{
			// Arrange
			var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDir);
			var tempFile = Path.Combine(tempDir, "test.log");
			System.IO.File.WriteAllText(tempFile, "test content");

			try
			{
				// Make directory read-only if possible (works on Windows, limited on Linux)
				var dirInfo = new DirectoryInfo(tempDir);
				dirInfo.Attributes |= FileAttributes.ReadOnly;

				var sidecarManager = new SidecarManager(tempFile);
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

				// Act - should not throw exception even if directory is read-only
				sidecarManager.Save(sidecarData, false);

				// Assert - test passes if no exception is thrown
			}
			finally
			{
				// Cleanup
				try
				{
					var dirInfo = new DirectoryInfo(tempDir);
					dirInfo.Attributes &= ~FileAttributes.ReadOnly;
					Directory.Delete(tempDir, true);
				}
				catch
				{
					// Ignore cleanup errors
				}
			}
		}

		[TestMethod]
		public void Save_WhenSuccessful_SavesMetadata()
		{
			// Arrange
			var tempDir = Path.GetTempPath();
			var tempFile = Path.Combine(tempDir, $"test_{Guid.NewGuid()}.log");
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
				Assert.IsTrue(System.IO.File.Exists(sidecarPath), "Sidecar file should be created");
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
