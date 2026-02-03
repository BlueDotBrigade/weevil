namespace BlueDotBrigade.Weevil.Integration
{
	using System;
	using System.IO;
	using System.IO.Compression;
	using BlueDotBrigade.Weevil;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	/// <summary>
	/// Integration test to verify that opening a log from a zip file and saving metadata
	/// does not throw an exception when the temporary directory is deleted.
	/// </summary>
	[TestClass]
	public class CompressedFileIntegrationTests
	{
		[TestMethod]
		public void OpenLogFromZip_SaveMetadata_SkipsSaveInTempDirectory()
		{
			// Arrange - Create a test log file in a zip
			var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDir);
			
			var logDir = Path.Combine(tempDir, "logs");
			Directory.CreateDirectory(logDir);
			
			var logFilePath = Path.Combine(logDir, "test.log");
			System.IO.File.WriteAllLines(logFilePath, new[]
			{
				"2024-01-01 10:00:00 [INFO] Test log entry 1",
				"2024-01-01 10:00:01 [INFO] Test log entry 2",
				"2024-01-01 10:00:02 [ERROR] Test error entry 3",
				"2024-01-01 10:00:03 [INFO] Test log entry 4",
				"2024-01-01 10:00:04 [INFO] Test log entry 5"
			});

			var zipFilePath = Path.Combine(tempDir, "test.zip");
			ZipFile.CreateFromDirectory(logDir, zipFilePath, CompressionLevel.Optimal, false);

			// Simulate extracting to a temp directory (like Weevil does)
			var extractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			ZipFile.ExtractToDirectory(zipFilePath, extractDir);
			var extractedLogPath = Path.Combine(extractDir, "test.log");

			try
			{
				// Act - Open the log file and add a comment
				var engine = Engine
					.UsingPath(extractedLogPath)
					.Open();

				// Add a comment to the first record
				engine.Records[0].Metadata.Comment = "Test comment added";

				// Try to save - this should NOT throw an exception
				// and should skip the save since it's in a temp directory
				engine.Save();

				// Assert - no sidecar should be created in temp directory
				var sidecarPath = $"{extractedLogPath}.xml";
				Assert.IsFalse(System.IO.File.Exists(sidecarPath), "Sidecar should not be created in temp directory");
			}
			finally
			{
				// Cleanup
				try
				{
					if (Directory.Exists(tempDir))
					{
						Directory.Delete(tempDir, true);
					}
					if (Directory.Exists(extractDir))
					{
						Directory.Delete(extractDir, true);
					}
				}
				catch
				{
					// Ignore cleanup errors
				}
			}
		}

		[TestMethod]
		public void OpenLogFromNonTempDirectory_SuccessfulSave_CreatesMetadata()
		{
			// Arrange - Create a test log file in current directory (not temp)
			var currentDir = Directory.GetCurrentDirectory();
			var testDir = Path.Combine(currentDir, $"test_{Guid.NewGuid()}");
			Directory.CreateDirectory(testDir);
			
			var logFilePath = Path.Combine(testDir, "test.log");
			System.IO.File.WriteAllLines(logFilePath, new[]
			{
				"2024-01-01 10:00:00 [INFO] Test log entry 1",
				"2024-01-01 10:00:01 [INFO] Test log entry 2",
				"2024-01-01 10:00:02 [ERROR] Test error entry 3"
			});

			try
			{
				// Act - Open the log file and add a comment
				var engine = Engine
					.UsingPath(logFilePath)
					.Open();

				// Add a comment to the first record
				engine.Records[0].Metadata.Comment = "Test comment";

				// Save - this should succeed since it's not in temp directory
				engine.Save();

				// Assert
				var sidecarPath = $"{logFilePath}.xml";
				Assert.IsTrue(System.IO.File.Exists(sidecarPath), "Sidecar file should be created in non-temp directory");

				// Verify the comment was saved
				var engine2 = Engine
					.UsingPath(logFilePath)
					.Open();
				
				Assert.AreEqual("Test comment", engine2.Records[0].Metadata.Comment, "Comment should be loaded from sidecar");
			}
			finally
			{
				// Cleanup
				try
				{
					if (Directory.Exists(testDir))
					{
						Directory.Delete(testDir, true);
					}
				}
				catch
				{
					// Ignore cleanup errors
				}
			}
		}
	}
}
