namespace BlueDotBrigade.Weevil.Cli.Analysis
{
	using System.Collections.Generic;
	using System.IO;
	using IOutputWriter = BlueDotBrigade.Weevil.IO.IOutputWriter;
	using OutputWriterContext = BlueDotBrigade.Weevil.IO.OutputWriterContext;
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
			// Later tests write nowhere, as before this test.
			OutputWriterContext.Configure(new PlainTextFormatter(), new LinesWritten());
			Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		public void GivenALog_WhenInsightIsRunVerbose_ThenItWritesTheLogsMetricsThenEveryInsight()
		{
			// The command now writes from the report model, and its output is unchanged (#837).
			var logPath = Path.Combine(_folder, "Application.log");
			File.Copy(new Daten().AsFilePath("Application.log"), logPath);

			new InsightCommands().Insight(logPath, verbose: true);

			_output.Lines.Should().ContainInOrder("METRICS", "Total Records\t9", $"File Size\t{new FileInfo(logPath).Length}", "INSIGHTS");
			_output.Lines.Should().Contain(line => line.StartsWith("Errors\t"), "the filter's metrics follow the log's");
			_output.Lines.Should().NotContain("No noteworthy insight was found.", "verbose shows every insight");
		}

		private sealed class LinesWritten : IOutputWriter
		{
			public List<string> Lines { get; } = new List<string>();

			public void WriteLine(string message) => this.Lines.Add(message);
		}
	}
}
