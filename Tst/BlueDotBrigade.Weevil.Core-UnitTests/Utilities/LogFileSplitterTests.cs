namespace BlueDotBrigade.Weevil.Utilities
{
	using System;
	using System.IO;
	using System.Threading;

	[TestClass]
	public class LogFileSplitterTests
	{
		private string _folder = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			// The split files are written beside the log, so each test opens its own copy.
			_folder = System.IO.Directory.CreateTempSubdirectory("WeevilSplit.").FullName;
		}

		[TestCleanup]
		public void Cleanup()
		{
			System.IO.Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		public void GivenAnAbortedSplit_WhenRun_ThenItStopsWithoutWritingAFile()
		{
			// Abort (Escape) stops a split started from the GUI (#1028).
			var logPath = Path.Combine(_folder, "SampleData.log");
			System.IO.File.Copy(new Daten().AsFilePath("SampleData.log"), logPath);
			IEngine engine = Engine.UsingPath(logPath).Open();
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();

			FluentActions.Invoking(() => new LogFileSplitter(logPath).Run(engine.Filter.Results, cancellation.Token))
				.Should().Throw<OperationCanceledException>();

			System.IO.Directory.GetFiles(_folder, "SampleData-*.log").Should().BeEmpty();
		}

		[TestMethod]
		public void GivenASplit_WhenRunToTheEnd_ThenItWritesTheRecords()
		{
			var logPath = Path.Combine(_folder, "SampleData.log");
			System.IO.File.Copy(new Daten().AsFilePath("SampleData.log"), logPath);
			IEngine engine = Engine.UsingPath(logPath).Open();

			new LogFileSplitter(logPath).Run(engine.Filter.Results, CancellationToken.None);

			System.IO.Directory.GetFiles(_folder, "SampleData-*.log").Should().NotBeEmpty();
		}
	}
}
