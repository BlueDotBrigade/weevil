namespace BlueDotBrigade.Weevil.Cli.Filter
{
	using System.IO;

	[TestClass]
	public class FilterCommandsTests
	{
		private string _folder = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			// The results file is written beside the log, so each test opens its own copy.
			_folder = Directory.CreateTempSubdirectory("WeevilCli.").FullName;
		}

		[TestCleanup]
		public void Cleanup()
		{
			Directory.Delete(_folder, recursive: true);
		}

		// Regression: Issue #1001
		[TestMethod]
		public void GivenIncludeFilter_WhenFiltering_ThenResultsFileHasOnlyMatchingRecords()
		{
			var logPath = CopyLog("Application.log");
			var lines = File.ReadAllLines(logPath);

			new FilterCommands().Filter(logPath, include: "Error", exclude: null);

			File.ReadAllLines(Path.Combine(_folder, "Application.Results.log"))
				.Should().Equal(lines[2], lines[4], lines[7]);
		}

		// Regression: Issue #1001
		[TestMethod]
		public void GivenIncludeAndExcludeFilters_WhenFiltering_ThenResultsFileHasOnlyMatchingRecords()
		{
			var logPath = CopyLog("Application.log");
			var lines = File.ReadAllLines(logPath);

			new FilterCommands().Filter(logPath, include: "database", exclude: "Unable");

			File.ReadAllLines(Path.Combine(_folder, "Application.Results.log"))
				.Should().Equal(lines[1], lines[5]);
		}

		private string CopyLog(string fileName)
		{
			var logPath = Path.Combine(_folder, fileName);
			File.Copy(new Daten().AsFilePath(fileName), logPath);
			return logPath;
		}
	}
}
