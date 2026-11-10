namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// Checks that an analyzer reads a log on disk once, rather than once per record.
	/// </summary>
	[TestClass]
	public class AnalysisManagerTests
	{
		// Generous, so a slow build machine does not fail the test. Measured on 2026-11-10 in a Debug build on 4 cores, with the log in the
		// operating system's cache: the analysis took 4.4 s through one content buffer, and 83 s reading one record at a time (#1027).
		private static readonly TimeSpan MaximumDuration = TimeSpan.FromSeconds(30);

		public TestContext TestContext { get; set; } = null!;

		[TestMethod]
		[DoNotParallelize]
		public void GivenADroidLogOpenedOnDisk_WhenEveryRecordIsSearchedForData_ThenTheAnalysisEndsQuickly()
		{
			IEngine engine = StorageModes.UsingPath(DroidLog.GetPath(DroidLog.Utf8), StorageMode.Disk).Open();
			var dialog = new ExpressionDialog(@"\[Security\] (?<Threat>.*) detected");

			var stopwatch = Stopwatch.StartNew();
			Results results = engine.Analyzer.Analyze(AnalysisType.DetectData, dialog);
			stopwatch.Stop();

			TestContext.WriteLine($"Analyzed {engine.Count:N0} records in {stopwatch.Elapsed.TotalSeconds:N2} s, and flagged {results.FlaggedRecords:N0}.");
			results.FlaggedRecords.Should().Be(34);
			stopwatch.Elapsed.Should().BeLessThan(MaximumDuration);
		}

		/// <summary>
		/// Answers an analyzer's request for a regular expression, as the user would.
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
