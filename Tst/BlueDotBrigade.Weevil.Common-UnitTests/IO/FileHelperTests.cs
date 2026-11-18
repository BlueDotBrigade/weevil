namespace BlueDotBrigade.Weevil.IO
{
	using System.IO;
	using System.Linq;

	[TestClass]
	public class FileHelperTests
	{
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
