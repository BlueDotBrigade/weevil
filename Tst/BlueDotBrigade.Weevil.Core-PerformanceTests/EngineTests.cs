namespace BlueDotBrigade.Weevil
{
	using System.Diagnostics;
	using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

	/// <summary>
	/// Checks that opening a log does not take noticeably more memory than it does today, and that Clear frees the memory of the records it clears.
	/// </summary>
	/// <remarks>
	/// Sanity checks that run in CI on logs smaller than the reference log. The requirements' full-size targets are measured on the reference machine.
	/// They count managed memory, which is almost all of the working set the requirements limit.
	/// </remarks>
	[TestClass]
	public class EngineTests
	{
		private const int RecordCount = 200_000;

		// Measured on 2026-11-07: 153 bytes per record once elapsed times were calculated when read (#1150), down from 161 (#1140),
		// 205 (#1133), 348 (#1100) and 397. #1086 needs about 157 (1.2 GB per GB of log). Keeping elapsed times per line again would be 161.
		private const int MaximumBytesPerRecord = 160;

		// Measured on 2026-11-10: 22 to 24 bytes per record on disk, of which the record table is 21, against 153 in RAM.
		// #1087's estimate was 18, or 137 MB per GB of the reference log: 22 is about 170 MB per GB. The droid log's budget below is the tighter one.
		private const int MaximumBytesPerRecordOnDisk = 27;

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
			var bytesPerRecord = MeasureBytesPerRecord(StorageMode.Ram);

			bytesPerRecord.Should().BeLessThanOrEqualTo(MaximumBytesPerRecord, "opening a log should not keep noticeably more memory per record than it did");
		}

		[TestMethod]
		[WorkItem(1087)]
		[DoNotParallelize]
		public void GivenAReferenceLog_WhenOpenedOnDisk_ThenTheMemoryKeptPerRecordStaysWithinBudget()
		{
			var bytesPerRecord = MeasureBytesPerRecord(StorageMode.Disk);

			bytesPerRecord.Should().BeLessThanOrEqualTo(MaximumBytesPerRecordOnDisk, "a log on disk keeps its record table in memory, and none of its content");
		}

		// Measured on 2026-11-07, once elapsed times were calculated when read (#1150): 1.22 GB per GB of the UTF-8 log (123 bytes per record),
		// and 1.11 per GB of the UTF-16 log (224), whose records hold twice the bytes. Before #1140: 1.73 and 1.36. #1086 needs 1.2.
		[TestMethod]
		[WorkItem(1086)]
		[DoNotParallelize]
		[DataRow(DroidLog.Utf8, 1.35)]
		[DataRow(DroidLog.Utf16LittleEndian, 1.25)]
		public void GivenADroidLog_WhenOpened_ThenEveryRecordIsKeptWithinTheMemoryBudget(string logName, double maximumMemoryPerByteOfLog)
		{
			var memoryPerByteOfLog = MeasureMemoryPerByteOfLog(logName, StorageMode.Ram);

			memoryPerByteOfLog.Should().BeLessThanOrEqualTo(maximumMemoryPerByteOfLog, "opening a log should not keep noticeably more memory than it did");
		}

		// Measured on 2026-11-10: 0.22 GB per GB of the UTF-8 log (22 bytes per record), against 1.22 in RAM.
		// The UTF-16 log keeps the same 22 bytes per record, 0.11 GB per GB, so it is not opened here as well.
		[TestMethod]
		[WorkItem(1087)]
		[DoNotParallelize]
		[DataRow(DroidLog.Utf8, 0.25)]
		public void GivenADroidLog_WhenOpenedOnDisk_ThenOnlyItsRecordTableIsKeptWithinTheMemoryBudget(string logName, double maximumMemoryPerByteOfLog)
		{
			var memoryPerByteOfLog = MeasureMemoryPerByteOfLog(logName, StorageMode.Disk);

			memoryPerByteOfLog.Should().BeLessThanOrEqualTo(maximumMemoryPerByteOfLog, "a log on disk keeps its record table in memory, and none of its content");
		}

		[TestMethod]
		[DoNotParallelize]
		public void GivenAReferenceLog_WhenAllButOnePercentOfItIsCleared_ThenTheContentChunksOnlyTheClearedRecordsWereReadFromAreFreed()
		{
			// Opened once first, so the memory of loading Weevil itself is not counted.
			_ = Engine.UsingPath(ReferenceLog.Create(_folder, 10)).UsingStorage(StorageMode.Ram).Open();
			var before = GC.GetTotalMemory(forceFullCollection: true);

			// In RAM, where the log's content is held in chunks of about 16 MB.
			IEngine engine = Engine.UsingPath(ReferenceLog.Create(_folder, RecordCount)).UsingStorage(StorageMode.Ram).Open();
			var memoryWhenOpened = GC.GetTotalMemory(forceFullCollection: true) - before;

			var keptCount = RecordCount / 100;
			SelectFirstRecords(engine, keptCount);
			engine.Clear(ClearOperation.Unselected);

			// The records kept still hold the chunk they were read from: only whole chunks are freed (#1022).
			var memoryAfterClear = GC.GetTotalMemory(forceFullCollection: true) - before;
			TestContext.WriteLine($"Managed memory kept: {memoryWhenOpened / 1_000_000.0:N1} MB when opened, {memoryAfterClear / 1_000_000.0:N1} MB once {keptCount:N0} of {RecordCount:N0} records are kept.");

			engine.Count.Should().Be(keptCount);
			memoryAfterClear.Should().BeLessThan(memoryWhenOpened * 3 / 4, "the content chunks only the records cleared were read from are freed");
			GC.KeepAlive(engine);
		}

		/// <summary>
		/// Selects the first records, in a method of its own, so that this test's own method holds none of the records Clear frees,
		/// even in a Debug build, which keeps a method's temporaries until it returns.
		/// </summary>
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		private static void SelectFirstRecords(IEngine engine, int count)
		{
			engine.Selector.SelectRange(engine.Records.GetLineNumber(0), engine.Records.GetLineNumber(count - 1));
		}

		/// <summary>
		/// Opens the reference log, and returns the managed memory the engine keeps per record.
		/// </summary>
		private double MeasureBytesPerRecord(StorageMode storageMode)
		{
			// Opened once first, so the memory of loading Weevil itself is not counted.
			_ = Engine.UsingPath(ReferenceLog.Create(_folder, 10)).UsingStorage(storageMode).Open();

			(IEngine engine, var memoryKept, _) = OpenAndMeasure(ReferenceLog.Create(_folder, RecordCount), storageMode);

			var bytesPerRecord = memoryKept / (double)engine.Count;
			TestContext.WriteLine($"Records: {engine.Count:N0}. Storage: {storageMode}. Managed memory kept: {bytesPerRecord:N1} bytes per record.");

			return bytesPerRecord;
		}

		/// <summary>
		/// Opens a droid log, and returns the managed memory the engine keeps per byte of the log.
		/// </summary>
		private double MeasureMemoryPerByteOfLog(string logName, StorageMode storageMode)
		{
			var logPath = DroidLog.GetPath(logName);

			// Its first records are opened first, so the memory of loading Weevil itself is not counted.
			_ = Engine.UsingPath(logPath).UsingStorage(storageMode).UsingLimit(10).Open();

			(IEngine engine, var memoryKept, TimeSpan duration) = OpenAndMeasure(logPath, storageMode);

			var memoryPerByteOfLog = memoryKept / (double)new FileInfo(logPath).Length;
			TestContext.WriteLine($"Opened in {duration.TotalSeconds:N1} s. Storage: {storageMode}. Managed memory kept: {memoryPerByteOfLog:N3} GB per GB of log, {memoryKept / (double)engine.Count:N1} bytes per record.");
			engine.Count.Should().Be(DroidLog.RecordCount);

			return memoryPerByteOfLog;
		}

		/// <summary>
		/// Opens the log, and returns the managed memory the engine keeps and how long opening took.
		/// </summary>
		private static (IEngine Engine, long MemoryKept, TimeSpan Duration) OpenAndMeasure(string logPath, StorageMode storageMode)
		{
			var before = GC.GetTotalMemory(forceFullCollection: true);
			var stopwatch = Stopwatch.StartNew();

			IEngine engine = Engine.UsingPath(logPath).UsingStorage(storageMode).Open();

			stopwatch.Stop();
			var memoryKept = GC.GetTotalMemory(forceFullCollection: true) - before;
			GC.KeepAlive(engine);

			return (engine, memoryKept, stopwatch.Elapsed);
		}
	}
}
