namespace BlueDotBrigade.Weevil
{
	using BenchmarkDotNet.Attributes;

	/// <summary>
	/// Opening a log in RAM: parsing it into the record table (#1177).
	/// </summary>
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
		}

		[Benchmark]
		public int Open()
		{
			return Engine.UsingPath(_logPath).Open().Count;
		}
	}
}
