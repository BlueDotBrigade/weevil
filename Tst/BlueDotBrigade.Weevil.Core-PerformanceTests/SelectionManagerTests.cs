namespace BlueDotBrigade.Weevil
{
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Analysis;
	using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

	/// <summary>
	/// Checks that saving records reads a log on disk once, rather than once per record.
	/// </summary>
	[TestClass]
	public class SelectionManagerTests
	{
		// Generous, so a slow build machine does not fail the test. Measured on 2026-11-10 in a Debug build on 4 cores, with the log in the
		// operating system's cache: saving took 3.0 s through one content buffer, and 80 s reading one record at a time (#1027).
		private static readonly TimeSpan MaximumDuration = TimeSpan.FromSeconds(30);

		private string _folder = string.Empty;

		public TestContext TestContext { get; set; } = null!;

		[TestInitialize]
		public void Setup()
		{
			_folder = Directory.CreateTempSubdirectory("Weevil.PerformanceTests.").FullName;
		}

		[TestCleanup]
		public void Cleanup()
		{
			Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		[DoNotParallelize]
		public void GivenADroidLogOpenedOnDisk_WhenEveryRecordIsSaved_ThenTheRecordsAreSavedQuickly()
		{
			var logPath = DroidLog.GetPath(DroidLog.Utf8);
			IEngine engine = Engine.UsingPath(logPath).UsingStorage(StorageMode.Disk).Open();
			var destinationPath = Path.Combine(_folder, "Saved.log");

			var stopwatch = Stopwatch.StartNew();
			engine.Selector.SaveSelection(destinationPath, FileFormatType.Raw);
			stopwatch.Stop();

			TestContext.WriteLine($"Saved {engine.Count:N0} records in {stopwatch.Elapsed.TotalSeconds:N2} s.");
			new FileInfo(destinationPath).Length.Should().BeGreaterThan(new FileInfo(logPath).Length * 9 / 10, "every record is saved");
			stopwatch.Elapsed.Should().BeLessThan(MaximumDuration);
		}
	}
}
