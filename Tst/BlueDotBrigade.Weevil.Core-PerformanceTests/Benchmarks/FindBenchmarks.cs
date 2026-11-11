namespace BlueDotBrigade.Weevil
{
	using BenchmarkDotNet.Attributes;
	using BlueDotBrigade.Weevil.Navigation;

	/// <summary>
	/// Find (F3) from the first record to text that only the last one holds, so it reads every record (#1177).
	/// </summary>
	[MemoryDiagnoser]
	public class FindBenchmarks
	{
		private IEngine _engine = null!;
		private string _lastRecordText = string.Empty;

		[Params(100_000, 1_000_000)]
		public int Records { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_engine = BenchmarkLogs.Open(this.Records);
			_lastRecordText = BenchmarkLogs.LastRecordText(this.Records);
		}

		[Benchmark]
		public int NextContent()
		{
			_engine.Navigate.GoTo(1, RecordSearchType.ExactMatch);

			return _engine.Navigate.NextContent(_lastRecordText, isCaseSensitive: true).LineNumber;
		}
	}
}
