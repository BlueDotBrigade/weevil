namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using BlueDotBrigade.Weevil.Analysis.Timeline;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.TestTools.Data;
	using NSubstitute;

	/// <summary>
	/// What every analyzer that reads values with regular expressions shares: the dialog, and reading rows without creating records.
	/// </summary>
	[TestClass]
	public class RegularExpressionAnalyzerTests
	{
		private const string ValueRegex = @"Value=(?<Value>\d+)";

		private static IRecordAnalyzer CreateAnalyzer(AnalysisType analysisType, FilterStrategy filterStrategy)
		{
			return analysisType switch
			{
				AnalysisType.DetectData => new DetectDataAnalyzer(filterStrategy),
				AnalysisType.Statistical => new StatisticalAnalyzer(filterStrategy),
				AnalysisType.FirstOccurrence or AnalysisType.LastOccurrence => new OccurrenceAnalyzer(analysisType, filterStrategy),
				AnalysisType.DetectRisingEdges or AnalysisType.DetectFallingEdges => new DetectEdgeAnalyzer(analysisType, filterStrategy),
				AnalysisType.StateTransitions => new StateTransitionsAnalyzer(filterStrategy),
				AnalysisType.StableValueRuns => new StableValueRunsAnalyzer(filterStrategy),
				AnalysisType.ThresholdCrossings => new ThresholdCrossingsAnalyzer(filterStrategy),
				AnalysisType.MatchingRecordRuns => new MatchingRecordRunsAnalyzer(filterStrategy),
				_ => throw new ArgumentOutOfRangeException(nameof(analysisType), analysisType, "Not a regular-expression analyzer."),
			};
		}

		/// <param name="regex">The expression the user enters, or <see langword="null"/> when the user cancels.</param>
		private static IUserDialog CreateDialog(string regex)
		{
			var isCancelled = regex is null;
			IUserDialog userDialog = Substitute.For<IUserDialog>();

			// The edge analyzers also ask for the analysis order.
			userDialog
				.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns("Ascending");

			userDialog
				.TryGetExpressions(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
				.Returns(x =>
				{
					x[2] = regex;
					return !isCancelled;
				});

			// Threshold Crossings asks for its expression, threshold and comparison in one dialog.
			userDialog
				.TryGetThreshold(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<string>(),
					out Arg.Any<string>(),
					out Arg.Any<string>(),
					out Arg.Any<string>())
				.Returns(x =>
				{
					x[4] = regex;
					x[5] = isCancelled ? null : "1";
					x[6] = isCancelled ? null : ">";
					return !isCancelled;
				});

			return userDialog;
		}

		private static RecordList BuildRecords(params string[] contents)
		{
			R builder = R.Create();

			for (var i = 0; i < contents.Length; i++)
			{
				builder = builder.WithContent($"15:02:{i:D2}.000 {contents[i]}");
			}

			return builder.GetRecords();
		}

		[TestMethod]
		[DataRow(AnalysisType.DetectData)]
		[DataRow(AnalysisType.Statistical)]
		[DataRow(AnalysisType.FirstOccurrence)]
		[DataRow(AnalysisType.LastOccurrence)]
		[DataRow(AnalysisType.DetectRisingEdges)]
		[DataRow(AnalysisType.DetectFallingEdges)]
		[DataRow(AnalysisType.StateTransitions)]
		[DataRow(AnalysisType.StableValueRuns)]
		[DataRow(AnalysisType.ThresholdCrossings)]
		[DataRow(AnalysisType.MatchingRecordRuns)]
		public void GivenTheUserCancelsTheDialog_WhenAnalyzed_ThenNothingIsFlaggedAndFlagsAreKept(AnalysisType analysisType)
		{
			RecordList records = BuildRecords("Value=1");
			var state = new RecordState();
			state.SetFlagged(records[0], true);

			Results results = CreateAnalyzer(analysisType, FilterStrategy.KeepAllRecords)
				.Analyze(records, state, string.Empty, CreateDialog(regex: null), canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(0);
			state.IsFlagged(records[0]).Should().BeTrue();
		}

		[TestMethod]
		[DataRow(AnalysisType.DetectData, "")]
		[DataRow(AnalysisType.DetectData, "   ")]
		[DataRow(AnalysisType.Statistical, "   ")]
		[DataRow(AnalysisType.FirstOccurrence, "   ")]
		[DataRow(AnalysisType.LastOccurrence, "   ")]
		[DataRow(AnalysisType.DetectRisingEdges, "   ")]
		[DataRow(AnalysisType.DetectFallingEdges, "   ")]
		[DataRow(AnalysisType.StateTransitions, "   ")]
		[DataRow(AnalysisType.StableValueRuns, "   ")]
		[DataRow(AnalysisType.ThresholdCrossings, "   ")]
		[DataRow(AnalysisType.MatchingRecordRuns, "   ")]
		public void GivenABlankExpression_WhenAnalyzed_ThenNothingIsFlaggedAndFlagsAreKept(AnalysisType analysisType, string regex)
		{
			RecordList records = BuildRecords("Value=1");
			var state = new RecordState();
			state.SetFlagged(records[0], true);

			Results results = CreateAnalyzer(analysisType, FilterStrategy.KeepAllRecords)
				.Analyze(records, state, string.Empty, CreateDialog(regex), canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(0);
			state.IsFlagged(records[0]).Should().BeTrue();
		}

		[TestMethod]
		[DataRow(AnalysisType.DetectData)]
		[DataRow(AnalysisType.Statistical)]
		[DataRow(AnalysisType.FirstOccurrence)]
		[DataRow(AnalysisType.LastOccurrence)]
		[DataRow(AnalysisType.DetectRisingEdges)]
		[DataRow(AnalysisType.DetectFallingEdges)]
		[DataRow(AnalysisType.StateTransitions)]
		[DataRow(AnalysisType.StableValueRuns)]
		[DataRow(AnalysisType.ThresholdCrossings)]
		public void GivenFlaggedRowsWithoutAValue_WhenAnalyzed_ThenTheirFlagsAreClearedWithoutCreatingARecord(AnalysisType analysisType)
		{
			// Flags are cleared in one step, by line number (#1150, #1258); the list throws if a record is read.
			RecordList records = BuildRecords("Starting", "Stopping");
			var state = new RecordState();
			state.Flags.SetFlagged(records.GetLineNumber(0), true);
			state.Flags.SetFlagged(records.GetLineNumber(1), true);

			Results results = CreateAnalyzer(analysisType, RecordAnalyzerTestContext.CreateFilterStrategy())
				.Analyze(new ColumnsOnlyRecordList(records), state, string.Empty, CreateDialog(ValueRegex), canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(0);
			state.Flags.IsFlagged(records.GetLineNumber(0)).Should().BeFalse();
			state.Flags.IsFlagged(records.GetLineNumber(1)).Should().BeFalse();
		}

		[TestMethod]
		[DataRow(AnalysisType.DetectData, 4)]
		[DataRow(AnalysisType.Statistical, 0)]
		[DataRow(AnalysisType.FirstOccurrence, 2)]
		[DataRow(AnalysisType.LastOccurrence, 2)]
		[DataRow(AnalysisType.DetectRisingEdges, 1)]
		[DataRow(AnalysisType.DetectFallingEdges, 1)]
		[DataRow(AnalysisType.StateTransitions, 3)]
		[DataRow(AnalysisType.StableValueRuns, 4)]
		[DataRow(AnalysisType.ThresholdCrossings, 2)]
		public void GivenMetadataThatCannotBeUpdated_WhenAnalyzed_ThenRowsAreCountedWithoutCreatingARecord(AnalysisType analysisType, int expectedCount)
		{
			// A record is created only for a row the analyzer flags (#1258); the list throws if a record is read.
			RecordList records = BuildRecords("Value=1", "Value=2", "Value=2", "Value=1");

			Results results = CreateAnalyzer(analysisType, RecordAnalyzerTestContext.CreateFilterStrategy())
				.Analyze(new ColumnsOnlyRecordList(records), new RecordState(), string.Empty, CreateDialog(ValueRegex), canUpdateMetadata: false);

			results.FlaggedRecords.Should().Be(expectedCount);
		}
	}
}
