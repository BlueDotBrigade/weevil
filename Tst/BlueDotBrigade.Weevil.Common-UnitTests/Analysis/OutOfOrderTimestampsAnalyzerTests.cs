namespace BlueDotBrigade.Weevil.Common.Analysis
{
	using System.Collections.Immutable;
	using System.Linq;
	using global::BlueDotBrigade.Weevil.Analysis;
	using global::BlueDotBrigade.Weevil.Data;
	using global::BlueDotBrigade.Weevil.IO;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	namespace BlueDotBrigade.Weevil.Common.Analysis.Tests
	{
		[TestClass]
		public class OutOfOrderTimestampsAnalyzerTests
		{
			[TestMethod]
			public void Analyze_NotInChronologicalOrder_CommentUpdated()
			{
				RecordList records = R.Create()
					.WithCreatedAt(0, "10:00:00")
					.WithCreatedAt(1, "10:15:00")
					.WithCreatedAt(2, "10:45:00")
					.WithCreatedAt(3, "10:30:00") // out of order
					.WithCreatedAt(4, "11:00:00")
					.GetRecords();

				var dialog = Substitute.For<IUserDialog>();
				dialog
					.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
					.Returns("0");

				var analyzer = new OutOfOrderTimestampsAnalyzer();
				var state = new RecordState();

				analyzer.Analyze(records, state, string.Empty, dialog, canUpdateMetadata: true);

				state.GetComment(records[3]).Should().Be("OutOfOrderTimestamps: -00:15:00");
			}

			[TestMethod]
			public void GivenAnEarlierRunFlaggedRecords_WhenAnalyzedAgain_ThenOnlyTheRecordOutOfOrderIsFlagged()
			{
				RecordList records = R.Create()
					.WithCreatedAt(0, "10:00:00")
					.WithCreatedAt(1, "10:15:00")
					.WithCreatedAt(2, "10:45:00")
					.WithCreatedAt(3, "10:30:00") // out of order
					.WithCreatedAt(4, "11:00:00")
					.GetRecords();
				var state = new RecordState();
				state.Flags.SetFlagged(1, true);
				state.Flags.SetFlagged(4, true);

				Results results = new OutOfOrderTimestampsAnalyzer().Analyze(records, state, string.Empty, GetUserDialog("0"), canUpdateMetadata: true);

				results.FlaggedRecords.Should().Be(1);
				Enumerable.Range(0, 5).Where(state.Flags.IsFlagged).Should().Equal(3);
			}

			[TestMethod]
			public void GivenTimestampsInOrder_WhenAnalyzed_ThenNoRecordIsCreated()
			{
				// Only a row the analyzer flags is created as a record (#1150); the list throws if any other is read.
				RecordList records = R.Create()
					.WithCreatedAt(0, "10:00:00")
					.WithCreatedAt(1, "10:15:00")
					.WithCreatedAt(2, "10:30:00")
					.GetRecords();
				var state = new RecordState();
				state.Flags.SetFlagged(1, true);

				Results results = new OutOfOrderTimestampsAnalyzer().Analyze(
					new ColumnsOnlyRecordList(records), state, string.Empty, GetUserDialog("0"), canUpdateMetadata: true);

				results.FlaggedRecords.Should().Be(0);
				state.Flags.IsFlagged(1).Should().BeFalse();
			}

			private static IUserDialog GetUserDialog(string thresholdInMs)
			{
				IUserDialog dialog = Substitute.For<IUserDialog>();
				dialog
					.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
					.Returns(thresholdInMs);

				return dialog;
			}
		}
	}
}
