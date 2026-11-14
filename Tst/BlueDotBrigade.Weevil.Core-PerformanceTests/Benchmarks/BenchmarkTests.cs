namespace BlueDotBrigade.Weevil
{
	using BenchmarkDotNet.Configs;
	using BenchmarkDotNet.Exporters;
	using BenchmarkDotNet.Jobs;
	using BenchmarkDotNet.Loggers;
	using BenchmarkDotNet.Reports;
	using BenchmarkDotNet.Running;

	/// <summary>
	/// Runs the BenchmarkDotNet benchmarks in this folder, one test per class, and writes each class's results to a Markdown file (#1177).
	/// </summary>
	/// <remarks>
	/// The tests carry the <c>Benchmark</c> category, so a test run includes them only when asked to:
	/// <code>dotnet test Tst/BlueDotBrigade.Weevil.Core-PerformanceTests -c Release -p:Platform=x64 --filter TestCategory=Benchmark</code>
	/// CI excludes the category, and the Benchmarks workflow runs it on demand. In a Debug build each test is inconclusive and measures nothing, because BenchmarkDotNet's times mean nothing without the optimizer.
	///
	/// The results stay in <see cref="ArtifactsPath"/>: a <c>*-report-github.md</c> file per class, under <c>results</c>, and a log per class, named after the class and the time it started.
	/// Each test also attaches its report to its result, and writes the table to its output.
	///
	/// The output can stop, or not appear, in these cases. Doc/Guides/Testing/Testing.md says what to do about each:
	/// <list type="bullet">
	///   <item>The test runner shows a test's output only once the test ends, so a run of several minutes shows no progress. BenchmarkDotNet writes the class's log, in <see cref="ArtifactsPath"/>, as it goes.</item>
	///   <item>A timeout, from the test runner or from the workflow, stops the run. The class being measured then writes no report; the classes already measured keep theirs.</item>
	///   <item>BenchmarkDotNet's documentation supports console applications only. It works from MSTest with BenchmarkDotNet 0.15.8, but a later version may stop working from a test runner.</item>
	/// </list>
	/// </remarks>
	[TestClass]
	[TestCategory("Benchmark")]
	[DoNotParallelize]
	public class BenchmarkTests
	{
		/// <summary>
		/// Where BenchmarkDotNet writes its log and the reports: beside the test assembly, in a folder that Git ignores.
		/// </summary>
		private static readonly string ArtifactsPath = Path.Combine(AppContext.BaseDirectory, "BenchmarkDotNet.Artifacts");

		public TestContext TestContext { get; set; } = null!;

		[TestMethod]
		public void GivenOpenBenchmarks_WhenRun_ThenReportIsWritten() => Run<OpenBenchmarks>();

		[TestMethod]
		public void GivenReopenBenchmarks_WhenRun_ThenReportIsWritten() => Run<ReopenBenchmarks>();

		[TestMethod]
		public void GivenFilterBenchmarks_WhenRun_ThenReportIsWritten() => Run<FilterBenchmarks>();

		[TestMethod]
		public void GivenFindBenchmarks_WhenRun_ThenReportIsWritten() => Run<FindBenchmarks>();

		[TestMethod]
		public void GivenExportBenchmarks_WhenRun_ThenReportIsWritten() => Run<ExportBenchmarks>();

		[TestMethod]
		public void GivenAnalyzerBenchmarks_WhenRun_ThenReportIsWritten() => Run<AnalyzerBenchmarks>();

		[TestMethod]
		public void GivenScreenRowsBenchmarks_WhenRun_ThenReportIsWritten() => Run<ScreenRowsBenchmarks>();

		private void Run<TBenchmarks>()
		{
#if DEBUG
			Assert.Inconclusive("Benchmarks measure only a Release build: dotnet test Tst/BlueDotBrigade.Weevil.Core-PerformanceTests -c Release -p:Platform=x64 --filter TestCategory=Benchmark");
#endif
			Summary summary = BenchmarkRunner.Run<TBenchmarks>(CreateConfig());

			// Written here, rather than by an exporter in the configuration, so the test knows the file's path.
			var reportPaths = MarkdownExporter.GitHub.ExportToFiles(summary, NullLogger.Instance).ToList();

			foreach (var reportPath in reportPaths)
			{
				this.TestContext.AddResultFile(reportPath);
				this.TestContext.WriteLine(File.ReadAllText(reportPath));
			}

			summary.HasCriticalValidationErrors.Should().BeFalse("BenchmarkDotNet must accept the benchmarks; its log says why it did not");
			summary.Reports.Should().OnlyContain(report => report.Success, "every benchmark must run to the end; BenchmarkDotNet's log says which failed");
			reportPaths.Should().ContainSingle(path => File.Exists(path), $"the results are written to {summary.ResultsDirectoryPath}");
		}

		/// <summary>
		/// BenchmarkDotNet's defaults, without its CSV, HTML and Markdown reports: <see cref="Run{TBenchmarks}"/> writes the Markdown one.
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		///   <item>
		///   BenchmarkDotNet refuses an assembly that references an unoptimized one, and DatenLokator, which only the tests use, ships unoptimized (#1180).
		///   So its check is turned off, and the <c>#if DEBUG</c> in <see cref="Run{TBenchmarks}"/> takes its place: this project, Core and Common are built in Release.
		///   </item>
		///   <item>The environment variable <c>WEEVIL_BENCHMARK_JOB=Short</c> picks BenchmarkDotNet's short job, for a quick look. Otherwise the default job measures more precisely, and takes longer.</item>
		/// </list>
		/// </remarks>
		private static IConfig CreateConfig()
		{
			IConfig defaults = DefaultConfig.Instance;

			ManualConfig config = ManualConfig.CreateEmpty()
				.AddColumnProvider(defaults.GetColumnProviders().ToArray())
				.AddLogger(defaults.GetLoggers().ToArray())
				.AddAnalyser(defaults.GetAnalysers().ToArray())
				.AddValidator(defaults.GetValidators().ToArray())
				.WithArtifactsPath(ArtifactsPath)
				.WithOptions(ConfigOptions.DisableOptimizationsValidator);

			if (string.Equals(Environment.GetEnvironmentVariable("WEEVIL_BENCHMARK_JOB"), "Short", StringComparison.OrdinalIgnoreCase))
			{
				config.AddJob(Job.ShortRun);
			}

			return config;
		}
	}
}
