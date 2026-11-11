namespace BlueDotBrigade.Weevil
{
	using BenchmarkDotNet.Attributes;

	/// <summary>
	/// Opening a log in RAM: parsing it into the record table, without a saved line index (#1177).
	/// </summary>
	/// <remarks>
	/// Weevil saves no line index in the temporary folder, so every open parses the whole log, as a user's first open of a log does, without then saving the index.
	/// </remarks>
	[MemoryDiagnoser]
	public class OpenBenchmarks
	{
		private string _logPath = string.Empty;

		[Params(100_000, 1_000_000)]
		public int Records { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_logPath = BenchmarkLogs.GetPath(this.Records);

			// Once, before measuring, so the log is known to hold every record.
			_ = BenchmarkLogs.Open(this.Records);
		}

		[Benchmark]
		public int Open()
		{
			return Engine.UsingPath(_logPath).Open().Count;
		}
	}
}
