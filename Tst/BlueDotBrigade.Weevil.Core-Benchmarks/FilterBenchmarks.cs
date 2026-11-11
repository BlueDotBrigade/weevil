namespace BlueDotBrigade.Weevil
{
	using BenchmarkDotNet.Attributes;
	using BlueDotBrigade.Weevil.Filter;

	/// <summary>
	/// Filtering a log in RAM, with expressions that cost little and much per record (#1177).
	/// </summary>
	/// <remarks>
	/// The small log fits in one 16 MB content chunk, so it shows whether a filter still keeps every core busy (#1027).
	/// </remarks>
	[MemoryDiagnoser]
	public class FilterBenchmarks
	{
		private IEngine _engine = null!;

		[Params(100_000, 1_000_000)]
		public int Records { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_engine = Engine.UsingPath(BenchmarkLogs.GetPath(this.Records)).Open();
		}

		[Benchmark]
		public int PlainText()
		{
			return Apply(FilterType.PlainText, "timeout");
		}

		[Benchmark]
		public int RegularExpression()
		{
			return Apply(FilterType.RegularExpression, @"retry.*connection");
		}

		/// <summary>
		/// A back-reference makes each record cost far more than reading it, so the time shows how well the cores share the work.
		/// </summary>
		[Benchmark]
		public int HeavyRegularExpression()
		{
			return Apply(FilterType.RegularExpression, @"\b(\w+)\b.*\b\1\b.*ZZZ");
		}

		private int Apply(FilterType filterType, string expression)
		{
			_engine.Filter.Apply(filterType, new FilterCriteria(expression));

			return _engine.Filter.Results.Count;
		}
	}
}
