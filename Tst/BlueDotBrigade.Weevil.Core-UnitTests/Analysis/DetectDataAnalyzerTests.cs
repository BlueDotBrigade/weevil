namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Threading;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.TestTools.Data;
	using NSubstitute;

	[TestClass]
	public class DetectDataAnalyzerTests
	{
		[TestMethod]
		public void GivenAnAnalysisAbortedWhenItFlagsARecord_WhenItGoesOn_ThenItStopsAtTheNextRecordAndKeepsTheFlag()
		{
			// Abort cancels the analysis, which stops within one record, and keeps what it changed so far (#1027).
			var records = R.Create()
				.WithContent("Value=1")
				.WithContent("Value=2")
				.WithContent("Value=3")
				.GetRecords();
			var analyzer = new DetectDataAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+)");
			using var abort = new CancellationTokenSource();
			IRecordState state = Substitute.For<IRecordState>();
			state.When(x => x.SetFlagged(Arg.Any<IRecord>(), true)).Do(_ => abort.Cancel());

			Action analyze = () => analyzer.Analyze(records, state, string.Empty, userDialog, canUpdateMetadata: true, abort.Token);

			analyze.Should().Throw<OperationCanceledException>();
			state.Received(1).SetFlagged(Arg.Any<IRecord>(), true);
		}

		[TestMethod]
		public void GivenIntegerAndDecimalMatches_WhenAnalyzeRuns_ThenMatchingRecordsAreFlagged()
		{
			var records = R.Create()
				.WithContent("Value=2")
				.WithContent("Value=2.5")
				.WithContent("Value=Done")
				.GetRecords();

			var analyzer = new DetectDataAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+(?:\.\d+)?)");

			var state = new RecordState();

			Results results = analyzer.Analyze(records, state, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			state.IsFlagged(records[0]).Should().BeTrue();
			state.IsFlagged(records[1]).Should().BeTrue();
			state.IsFlagged(records[2]).Should().BeFalse();
			state.GetComment(records[0]).Should().Contain("Value: 2");
			state.GetComment(records[1]).Should().Contain("Value: 2.5");
		}

		[TestMethod]
		[WorkItem(923)]
		public void GivenKeepAllRecords_WhenAnalyzeRunsWithNamedGroupRegex_ThenMatchingRecordsAreFlagged()
		{
			// An analyzer ignored its regular expression unless the active filter was a regular expression.
			var records = R.Create()
				.WithContent("Value=2")
				.WithContent("Value=2.5")
				.WithContent("Value=Done")
				.GetRecords();

			var analyzer = new DetectDataAnalyzer(FilterStrategy.KeepAllRecords);
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+(?:\.\d+)?)");

			var state = new RecordState();

			Results results = analyzer.Analyze(records, state, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			state.IsFlagged(records[0]).Should().BeTrue();
			state.IsFlagged(records[1]).Should().BeTrue();
			state.IsFlagged(records[2]).Should().BeFalse();
		}

		[TestMethod]
		[WorkItem(923)]
		public void GivenPlainTextFilter_WhenAnalyzeRunsWithNamedGroupRegex_ThenMatchingRecordsAreFlagged()
		{
			// An analyzer ignored its regular expression unless the active filter was a regular expression.
			var records = R.Create()
				.WithContent("Value=2")
				.WithContent("Value=2.5")
				.WithContent("Value=Done")
				.GetRecords();

			var analyzer = new DetectDataAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy(FilterType.PlainText));
			var userDialog = RecordAnalyzerTestContext.CreateDialog(@"Value=(?<Value>\d+(?:\.\d+)?)");

			var state = new RecordState();

			Results results = analyzer.Analyze(records, state, string.Empty, userDialog, canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(2);
			state.IsFlagged(records[0]).Should().BeTrue();
			state.IsFlagged(records[1]).Should().BeTrue();
			state.IsFlagged(records[2]).Should().BeFalse();
		}
	}
}
