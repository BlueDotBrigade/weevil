namespace BlueDotBrigade.Weevil.Filter
{
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Diagnostics;
	using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

	/// <summary>
	/// Checks that a filter finds the same records in each encoding, without becoming much slower.
	/// </summary>
	[TestClass]
	public class FilterManagerTests
	{
		// Generous, so a slow build machine does not fail the test: each filter took about 0.6 s on 4 cores in a Debug build (2026-10-30).
		// #1089 compares with Weevil 2.12.1, whose times on the reference machine are not measured yet.
		private static readonly TimeSpan MaximumDuration = TimeSpan.FromSeconds(10);

		public TestContext TestContext { get; set; } = null!;

		[TestMethod]
		[WorkItem(1089)]
		[DoNotParallelize]
		[DataRow(DroidLog.Utf8, FilterType.PlainText, "Blaster", 23)]
		[DataRow(DroidLog.Utf16LittleEndian, FilterType.PlainText, "Blaster", 23)]
		[DataRow(DroidLog.Utf8, FilterType.RegularExpression, @"\[Security\].*detected", 34)]
		[DataRow(DroidLog.Utf16LittleEndian, FilterType.RegularExpression, @"\[Security\].*detected", 34)]
		public void GivenADroidLog_WhenFiltered_ThenTheMatchingRecordsAreFoundQuickly(string logName, FilterType filterType, string expression, int matchingRecords)
		{
			IEngine engine = Engine.UsingPath(DroidLog.GetPath(logName)).Open();

			var stopwatch = Stopwatch.StartNew();
			engine.Filter.Apply(filterType, new FilterCriteria(expression));
			stopwatch.Stop();

			this.TestContext.WriteLine($"Filtered {engine.Count:N0} records in {stopwatch.Elapsed.TotalSeconds:N2} s.");
			engine.Filter.Results.Should().HaveCount(matchingRecords);
			stopwatch.Elapsed.Should().BeLessThan(MaximumDuration);
		}

		[TestMethod]
		[WorkItem(1269)]
		[DoNotParallelize]
		public void GivenADroidLogOpenedOnDisk_WhenFiltered_ThenItAllocatesAboutOneChunkPerCoreMoreThanInRam()
		{
			// Each of the filter's many worker states allocated its own 16 MB buffer, so one filter on disk allocated about half the log's size again.
			var logPath = DroidLog.GetPath(DroidLog.Utf8);
			var inRam = MeasureFilterAllocations(Engine.UsingPath(logPath).UsingStorage(StorageMode.Ram).Open());
			var onDisk = MeasureFilterAllocations(Engine.UsingPath(logPath).UsingStorage(StorageMode.Disk).Open());

			// A buffer per core, of a 16 MB chunk and a 1/16 margin, plus one buffer of margin.
			// The old code allocated about 1 GB on this log, so on more than about 48 cores the budget no longer tells them apart.
			const long ChunkBytes = 17L * 1024 * 1024;
			var budget = (Environment.ProcessorCount + 1) * ChunkBytes;
			this.TestContext.WriteLine($"Allocated {inRam / 1_048_576.0:N0} MB in RAM and {onDisk / 1_048_576.0:N0} MB on disk; the budget for the difference is {budget / 1_048_576.0:N0} MB.");

			(onDisk - inRam).Should().BeLessThan(budget);
		}

		[TestMethod]
		[DoNotParallelize]
		[DataRow(DroidLog.Utf8)]
		[DataRow(DroidLog.Utf16LittleEndian)]
		public void GivenADroidLogOpenedOnDisk_WhenFiltered_ThenEachChunkOfTheLogIsReadOnce(string logName)
		{
			// Before #1027, the filter's workers shared chunks, so a filter read the log 1.3 to 2.1 times on 4 cores, and more on more cores.
			var logPath = DroidLog.GetPath(logName);

			if (Environment.ProcessorCount > new FileInfo(logPath).Length / (16 * 1024 * 1024))
			{
				Assert.Inconclusive("With more cores than 16 MB chunks, each chunk is split between workers, and read by each of them.");
			}

			IEngine engine = Engine.UsingPath(logPath).UsingStorage(StorageMode.Disk).Open();
			var writer = new FilterCompletedWriter();
			ILogWriter original = Log.Default;
			Log.Register(writer);

			try
			{
				engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(@"\[Security\].*detected"));
			}
			finally
			{
				Log.Register(original);
			}

			var logLength = new FileInfo(logPath).Length;
			this.TestContext.WriteLine($"Read {writer.BytesRead / (double)logLength:N3} times the log's {logLength:N0} bytes.");

			// Each chunk's first record reads its own 64 KB block first: 0.4% of a 16 MB chunk. Only the byte order mark is not read.
			writer.BytesRead.Should().BeInRange(logLength - 4, logLength + (logLength / 50), "each chunk of the log is read once");
		}

		/// <summary>
		/// Returns the bytes one filter allocates, after a first filter has warmed it up.
		/// </summary>
		private static long MeasureFilterAllocations(IEngine engine)
		{
			var criteria = new FilterCriteria("Blaster");
			engine.Filter.Apply(FilterType.PlainText, criteria);

			var before = GC.GetTotalAllocatedBytes(precise: true);
			engine.Filter.Apply(FilterType.PlainText, criteria);

			return GC.GetTotalAllocatedBytes(precise: true) - before;
		}

		/// <summary>
		/// Keeps the bytes read that the last "Filtering complete." entry logged.
		/// </summary>
		private sealed class FilterCompletedWriter : ILogWriter
		{
			public long BytesRead { get; private set; } = -1;

			public void Write(LogSeverityType severity, string message, IEnumerable<KeyValuePair<string, object>> metadata)
			{
				if (message == "Filtering complete.")
				{
					this.BytesRead = (long)metadata.Single(entry => entry.Key == "BytesRead").Value;
				}
			}

			public void Write(string message)
			{
			}

			public void Write(string message, IEnumerable<KeyValuePair<string, object>> metadata)
			{
			}

			public void Write(LogSeverityType severity, string message)
			{
			}

			public void Write(LogSeverityType severity, Exception exception)
			{
			}

			public void Write(LogSeverityType severity, Exception exception, string message)
			{
			}

			public void Write(LogSeverityType severity, Exception exception, string message, IEnumerable<KeyValuePair<string, object>> metadata)
			{
			}
		}
	}
}
