namespace BlueDotBrigade.Weevil.Cli.Analysis
{
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text.Json;
	using IOutputWriter = BlueDotBrigade.Weevil.IO.IOutputWriter;
	using OutputWriterContext = BlueDotBrigade.Weevil.IO.OutputWriterContext;
	using JsonFormatter = BlueDotBrigade.Weevil.IO.JsonFormatter;
	using OutputFormat = BlueDotBrigade.Weevil.IO.OutputFormat;
	using OutputOptions = BlueDotBrigade.Weevil.Cli.IO.OutputOptions;
	using PlainTextFormatter = BlueDotBrigade.Weevil.IO.PlainTextFormatter;

	[TestClass]
	[DoNotParallelize]
	public class InsightCommandsTests
	{
		private readonly LinesWritten _output = new LinesWritten();
		private string _folder = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			// Weevil saves a sidecar beside the log, so each test opens its own copy.
			_folder = Directory.CreateTempSubdirectory("WeevilCli.").FullName;
			OutputWriterContext.Configure(new PlainTextFormatter(), _output);
		}

		[TestCleanup]
		public void Cleanup()
		{
			// Later tests write nowhere.
			OutputWriterContext.Configure(new PlainTextFormatter(), new LinesWritten());
			Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		public void GivenALog_WhenInsightIsRunVerbose_ThenItWritesTheLogsMetricsThenEveryInsight()
		{
			// The command now writes from the report model, and its output is unchanged (#837).
			var logPath = Path.Combine(_folder, "Application.log");
			File.Copy(new Daten().AsFilePath("Application.log"), logPath);

			new InsightCommands(new OutputOptions(OutputFormat.PlainText)).Insight(logPath, verbose: true);

			_output.Lines.Should().ContainInOrder("METRICS", "Total Records\t9", $"File Size\t{new FileInfo(logPath).Length}", "INSIGHTS");
			_output.Lines.Should().Contain(line => line.StartsWith("Errors\t"), "the filter's metrics follow the log's");
			_output.Lines.Should().NotContain("No noteworthy insight was found.", "verbose shows every insight");
		}

		[TestMethod]
		public void GivenALog_WhenInsightIsRun_ThenItWritesOnlyTheInsightsThatNeedAttention()
		{
			// Without --verbose, the command shows no metrics, and filters the report's insights by their flag (#837).
			var logPath = Path.Combine(_folder, "Application.log");
			File.Copy(new Daten().AsFilePath("Application.log"), logPath);
			var expected = ExpectedInsightLines(logPath);

			new InsightCommands(new OutputOptions(OutputFormat.PlainText)).Insight(logPath);

			_output.Lines.Should().NotContain("METRICS");
			_output.Lines.Should().Equal(new[] { "INSIGHTS" }.Concat(expected));
		}

		[TestMethod]
		public void GivenALog_WhenInsightIsRunAsJson_ThenItWritesTheWholeReportAsOneJsonDocument()
		{
			// The JSON is rendered from the report, not run through the formatter, and holds every insight without --verbose (#837).
			var logPath = Path.Combine(_folder, "Application.log");
			File.Copy(new Daten().AsFilePath("Application.log"), logPath);
			var insightCount = Engine.UsingPath(logPath).Open().Analyzer.GetInsights().Length;
			OutputWriterContext.Configure(new JsonFormatter(), _output);

			new InsightCommands(new OutputOptions(OutputFormat.Json)).Insight(logPath);

			using var report = JsonDocument.Parse(_output.Lines.Single());
			report.RootElement.GetProperty("schemaVersion").GetInt32().Should().Be(1);
			report.RootElement.GetProperty("sourceFileName").GetString().Should().Be("Application.log");
			report.RootElement.GetProperty("recordCount").GetInt32().Should().Be(9);
			report.RootElement.GetProperty("insights").GetArrayLength().Should().Be(insightCount);
		}

		/// <summary>
		/// The lines the command writes for the insights that need attention, worked out from the engine itself.
		/// </summary>
		private static string[] ExpectedInsightLines(string logPath)
		{
			var lines = Engine.UsingPath(logPath).Open().Analyzer.GetInsights()
				.Where(insight => insight.IsAttentionRequired)
				.Select(insight => $"{insight.Title}\t{insight.MetricValue} {insight.MetricUnit}\t{insight.Details}")
				.ToArray();

			return lines.Length == 0 ? new[] { "No noteworthy insight was found." } : lines;
		}

		private sealed class LinesWritten : IOutputWriter
		{
			public List<string> Lines { get; } = new List<string>();

			public void WriteLine(string message) => this.Lines.Add(message);
		}
	}
}
