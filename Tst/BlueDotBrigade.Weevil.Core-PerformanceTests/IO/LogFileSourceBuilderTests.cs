namespace BlueDotBrigade.Weevil.IO
{
	using System.Diagnostics;

	/// <summary>
	/// Checks that the block checksums disk mode computes while a log opens add little to the time it takes (#1025).
	/// </summary>
	[TestClass]
	public class LogFileSourceBuilderTests
	{
		// About the size of the pieces Weevil's parser reads (LogLineReader).
		private const int PieceSize = 64 * 1024;

		// Measured on 2026-11-08: 0.14 s per GB, or 2.7 s for a 20 GB log. Opening the 1 GB UTF-8 droid log takes about 15 s in CI.
		// The budget is about ten times the measurement, for shared CI machines, and still about a tenth of the time to open the log.
		private const double MaximumSecondsPerGigabyte = 1.5;

		public TestContext TestContext { get; set; } = null!;

		[TestMethod]
		[DoNotParallelize]
		public void GivenADroidLog_WhenItsBlockChecksumsAreComputed_ThenTheyAddLittleToTheTimeToOpenIt()
		{
			var logPath = DroidLog.GetPath(DroidLog.Utf8);
			var builder = new LogFileSourceBuilder(logPath, 0, new FileInfo(logPath).Length);
			var piece = new byte[PieceSize];
			var checksumming = new Stopwatch();

			// Only the checksums are timed: the parser reads the bytes anyway.
			using (FileStream log = System.IO.File.OpenRead(logPath))
			{
				int read;
				while ((read = log.Read(piece)) > 0)
				{
					checksumming.Start();
					builder.Append(piece.AsSpan(0, read));
					checksumming.Stop();
				}
			}

			LogFileSource source = builder.Build();

			var gigabytes = source.EndOffset / 1_000_000_000.0;
			var secondsPerGigabyte = checksumming.Elapsed.TotalSeconds / gigabytes;
			TestContext.WriteLine($"Checksums of {gigabytes:N2} GB computed in {checksumming.Elapsed.TotalSeconds:N2} s: {secondsPerGigabyte:N3} s per GB, or {20 * secondsPerGigabyte:N1} s for a 20 GB log.");
			secondsPerGigabyte.Should().BeLessThanOrEqualTo(MaximumSecondsPerGigabyte, "computing the checksums should add little to the time a log takes to open");
		}
	}
}
