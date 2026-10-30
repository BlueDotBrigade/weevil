namespace BlueDotBrigade.Weevil
{
	/// <summary>
	/// Checks that opening a log does not take noticeably more memory than it does today.
	/// </summary>
	/// <remarks>
	/// A sanity check that runs in CI on a small log. The requirements' full-size targets are measured on the reference machine.
	/// It counts managed memory, which is almost all of the working set the requirements limit.
	/// </remarks>
	[TestClass]
	public class EngineTests
	{
		private const int RecordCount = 200_000;

		// Measured on 2026-10-29: 205 bytes per record once records held the log's bytes instead of a string (#1133), down from 348 (#1100) and 397.
		// #1086 needs about 157 (1.2 GB per GB of log), which #988 delivers: lower the budget as its steps land.
		private const int MaximumBytesPerRecord = 250;

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
		[WorkItem(1086)]
		[DoNotParallelize]
		public void GivenAReferenceLog_WhenOpened_ThenTheMemoryKeptPerRecordStaysWithinBudget()
		{
			// Opened once first, so the memory of loading Weevil itself is not counted.
			_ = Engine.UsingPath(ReferenceLog.Create(_folder, 10)).Open();
			var logPath = ReferenceLog.Create(_folder, RecordCount);
			var before = GC.GetTotalMemory(forceFullCollection: true);

			IEngine engine = Engine.UsingPath(logPath).Open();

			var bytesPerRecord = (GC.GetTotalMemory(forceFullCollection: true) - before) / (double)engine.Count;
			TestContext.WriteLine($"Records: {engine.Count:N0}. Managed memory kept: {bytesPerRecord:N0} bytes per record.");
			GC.KeepAlive(engine);
			bytesPerRecord.Should().BeLessThanOrEqualTo(MaximumBytesPerRecord, "opening a log should not keep noticeably more memory per record than it did");
		}
	}
}
