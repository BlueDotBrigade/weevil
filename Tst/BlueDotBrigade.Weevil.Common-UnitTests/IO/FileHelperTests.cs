namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;
	using System.Linq;

	[TestClass]
	public class FileHelperTests
	{
		[TestMethod]
		public void GivenAFailureToWrite_WhenClassified_ThenOnlyAPlaceThatCanNeverBeWrittenIsNeverWritable()
		{
			// Clear, Reload, opening another log and Exit carry on only when no later save could succeed either (#1246, #1307).
			FileHelper.IsNeverWritable(new UnauthorizedAccessException()).Should().BeTrue("a read-only folder or file throws it");
			FileHelper.IsNeverWritable(new IOException("The media is write protected.", unchecked((int)0x80070013))).Should().BeTrue("a read-only drive throws it");

			FileHelper.IsNeverWritable(new IOException("The file is in use.", unchecked((int)0x80070020))).Should().BeFalse("a lock may pass");
			FileHelper.IsNeverWritable(new IOException("There is not enough space on the disk.", unchecked((int)0x80070070))).Should().BeFalse("a full disk may pass");
		}

		[TestMethod]
		[WorkItem(1247)]
		public void GivenALogThatWeevilIsReading_WhenAnotherApplicationRenamesIt_ThenTheRenameSucceedsAndTheReadFinishes()
		{
			// Weevil stopped the application that writes a log from rolling it, for as long as Weevil parsed the log.
			// Only Windows enforces file sharing, so only there does this fail without the fix.
			var folder = System.IO.Directory.CreateTempSubdirectory("Weevil.FileHelperTests.").FullName;
			var logPath = Path.Combine(folder, "App.log");
			System.IO.File.WriteAllLines(logPath, Enumerable.Range(1, 10_000).Select(i => $"Line {i}"));

			try
			{
				var linesRead = 0;

				using (FileStream log = FileHelper.Open(logPath))
				using (var reader = new StreamReader(log))
				{
					reader.ReadLine().Should().Be("Line 1");
					linesRead++;

					System.IO.File.Move(logPath, $"{logPath}.1");

					while (reader.ReadLine() is not null)
					{
						linesRead++;
					}
				}

				linesRead.Should().Be(10_000, "the read finishes with the data it started with");
				System.IO.File.Exists($"{logPath}.1").Should().BeTrue();
			}
			finally
			{
				System.IO.Directory.Delete(folder, recursive: true);
			}
		}
	}
}
