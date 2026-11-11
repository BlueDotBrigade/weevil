namespace BlueDotBrigade.Weevil
{
	using BenchmarkDotNet.Attributes;
	using BlueDotBrigade.Weevil.Analysis;

	/// <summary>
	/// Saving every record of a log in RAM, as the GUI's Save and the CLI do (#1177).
	/// </summary>
	[MemoryDiagnoser]
	public class ExportBenchmarks
	{
		private IEngine _engine = null!;
		private string _destinationPath = string.Empty;

		[Params(100_000, 1_000_000)]
		public int Records { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			var logPath = BenchmarkLogs.GetPath(this.Records);
			_engine = Engine.UsingPath(logPath).Open();
			_destinationPath = Path.ChangeExtension(logPath, ".Saved.log");
		}

		[GlobalCleanup]
		public void Cleanup()
		{
			File.Delete(_destinationPath);
		}

		[Benchmark]
		public void Raw()
		{
			_engine.Selector.SaveSelection(_destinationPath, FileFormatType.Raw);
		}

		[Benchmark]
		public void Tsv()
		{
			_engine.Selector.SaveSelection(_destinationPath, FileFormatType.Tsv);
		}
	}
}
