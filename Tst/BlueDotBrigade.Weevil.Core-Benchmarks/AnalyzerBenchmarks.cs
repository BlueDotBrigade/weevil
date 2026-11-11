namespace BlueDotBrigade.Weevil
{
	using BenchmarkDotNet.Attributes;
	using BlueDotBrigade.Weevil.Analysis;
	using IUserDialog = BlueDotBrigade.Weevil.IO.IUserDialog;

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
			_engine = Engine.UsingPath(BenchmarkLogs.GetPath(this.Records)).Open();
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

		/// <summary>
		/// Answers the analyzer's request for a regular expression, as the user would.
		/// </summary>
		private sealed class ExpressionDialog : IUserDialog
		{
			private readonly string _regularExpression;

			public ExpressionDialog(string regularExpression)
			{
				_regularExpression = regularExpression;
			}

			public bool TryGetExpressions(string defaultRegex, string recordsDescription, out string regularExpression)
			{
				regularExpression = _regularExpression;
				return true;
			}

			public string ShowUserPrompt(string title, string prompt) => throw new NotSupportedException();

			public string ShowUserPrompt(string title, string prompt, string defaultValue) => throw new NotSupportedException();

			public bool TryShowFind(string defaultValue, out bool isCaseSensitive, out bool findNext, out bool useRegex, out string findText, out bool searchElapsedTime, out int? minElapsedMs, out int? maxElapsedMs, out bool searchComments) => throw new NotSupportedException();

			public bool TryGetThreshold(string defaultRegex, string recordsDescription, string defaultThreshold, string defaultComparison, out string regularExpression, out string threshold, out string comparison) => throw new NotSupportedException();
		}
	}
}
