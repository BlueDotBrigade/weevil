namespace BlueDotBrigade.Weevil
{
	using BenchmarkDotNet.Attributes;
	using BlueDotBrigade.Weevil.Analysis;

	/// <summary>
	/// An analyzer that reads every record's content: Detect Data, with an expression that few records match (#1177).
	/// </summary>
	[MemoryDiagnoser]
	public class AnalyzerBenchmarks
	{
		private IEngine _engine = null!;

		[Params(100_000, 1_000_000)]
		public int Records { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_engine = BenchmarkLogs.Open(this.Records);
		}

		/// <summary>
		/// Detect Data adds a comment to each record it flags, so each run starts without them.
		/// </summary>
		[IterationSetup]
		public void RemoveComments()
		{
			_engine.Analyzer.RemoveComments(clearAll: true);
		}

		[Benchmark]
		public int DetectData()
		{
			return _engine.Analyzer.Analyze(AnalysisType.DetectData, new ExpressionDialog(@"sequence=(?<Sequence>\d*7777)\b")).FlaggedRecords;
		}
	}
}
