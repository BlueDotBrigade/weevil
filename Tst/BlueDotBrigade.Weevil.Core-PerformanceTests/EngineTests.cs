namespace BlueDotBrigade.Weevil
{
	using System.Diagnostics;

	/// <summary>
	/// Checks that opening a log does not take noticeably more memory than it does today.
	/// </summary>
	/// <remarks>
	/// Sanity checks that run in CI on logs smaller than the reference log. The requirements' full-size targets are measured on the reference machine.
	/// They count managed memory, which is almost all of the working set the requirements limit.
	/// </remarks>
	[TestClass]
	public class EngineTests
	{
		private const int RecordCount = 200_000;

		// Measured on 2026-11-02: 161 bytes per record once records were created when read (#1140), down from 205 (#1133), 348 (#1100) and 397.
		// #1086 needs about 157 (1.2 GB per GB of log); the last 4 bytes are revisited with #1023 (decided 2026-10-30).
		private const int MaximumBytesPerRecord = 175;

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

			(IEngine engine, var memoryKept, _) = OpenAndMeasure(ReferenceLog.Create(_folder, RecordCount));

			var bytesPerRecord = memoryKept / (double)engine.Count;
			TestContext.WriteLine($"Records: {engine.Count:N0}. Managed memory kept: {bytesPerRecord:N0} bytes per record.");
			bytesPerRecord.Should().BeLessThanOrEqualTo(MaximumBytesPerRecord, "opening a log should not keep noticeably more memory per record than it did");
		}

		// Measured on 2026-11-02, once records were created when read (#1140): 1.30 GB per GB of the UTF-8 log (131 bytes per record),
		// and 1.15 per GB of the UTF-16 log (232), whose records hold twice the bytes. Before: 1.73 and 1.36. #1086 needs 1.2.
		[TestMethod]
		[WorkItem(1086)]
		[DoNotParallelize]
		[DataRow(DroidLog.Utf8, 1.6)]
		[DataRow(DroidLog.Utf16LittleEndian, 1.3)]
		public void GivenADroidLog_WhenOpened_ThenEveryRecordIsKeptWithinTheMemoryBudget(string logName, double maximumMemoryPerByteOfLog)
		{
			var logPath = DroidLog.GetPath(logName);

			// Its first records are opened first, so the memory of loading Weevil itself is not counted.
			_ = Engine.UsingPath(logPath).UsingLimit(10).Open();

			(IEngine engine, var memoryKept, TimeSpan duration) = OpenAndMeasure(logPath);

			var memoryPerByteOfLog = memoryKept / (double)new FileInfo(logPath).Length;
			TestContext.WriteLine($"Opened in {duration.TotalSeconds:N1} s. Managed memory kept: {memoryPerByteOfLog:N2} GB per GB of log, {memoryKept / (double)engine.Count:N0} bytes per record.");
			engine.Count.Should().Be(DroidLog.RecordCount);
			memoryPerByteOfLog.Should().BeLessThanOrEqualTo(maximumMemoryPerByteOfLog, "opening a log should not keep noticeably more memory than it did");
		}

		/// <summary>
		/// Opens the log, and returns the managed memory the engine keeps and how long opening took.
		/// </summary>
		private static (IEngine Engine, long MemoryKept, TimeSpan Duration) OpenAndMeasure(string logPath)
		{
			var before = GC.GetTotalMemory(forceFullCollection: true);
			var stopwatch = Stopwatch.StartNew();

			IEngine engine = Engine.UsingPath(logPath).Open();

			stopwatch.Stop();
			var memoryKept = GC.GetTotalMemory(forceFullCollection: true) - before;
			GC.KeepAlive(engine);

			return (engine, memoryKept, stopwatch.Elapsed);
		}
	}
}
