namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.TestTools.Data;
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class TimeGapAnalyzerTest
	{
		private ImmutableArray<IRecord> _records;

		private IUserDialog GetUserDialog(int msBeforeFlaggedRaised)
		{
			var userDialog = Substitute.For<IUserDialog>();

			// Only a plugin knows what to ask the user.  Furthermore, the unit test has no idea about the implementation details
			// ... E.g. How many parameters are needed? What types of parameters is the plugin expecting?
			// TODO: re-write the `IUserDialog` interface so that the unit test doesn't care about the implementation details
			userDialog
				.ShowUserPrompt(Arg.Any<string>(),Arg.Any<string>(),Arg.Any<string>())
				.Returns(msBeforeFlaggedRaised.ToString());

			return userDialog;
		}
		[TestInitialize]
		public void PreTest()
		{
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			var records = new List<IRecord>
			{
				new Record(0, now.AddMinutes(0), severity, "content", isMultiLine: false, wasGeneratedByUi: true), // IsFlagged=false; no record to compare against
				new Record(1, Record.CreationTimeUnknown, severity, "application initializing", isMultiLine: false, wasGeneratedByUi: true), // IsFlagged=false; `MaxValue` represents an unknown timestamp
				new Record(2, now.AddMinutes(2), severity, "content", isMultiLine: false, wasGeneratedByUi: true), // IsFlagged=false; no timestamps for comparison
				new Record(3, now.AddMinutes(3), severity, "content", isMultiLine: false, wasGeneratedByUi: true), // IsFlagged=true; 
				new Record(4, now.AddMinutes(3), severity, "content", isMultiLine: false, wasGeneratedByUi: true), // IsFlagged=false; not enough time since last record
				new Record(5, now.AddMinutes(5), severity, "content", isMultiLine: false, wasGeneratedByUi: false), // IsFlagged=false; record not from UI thread
				new Record(6, now.AddMinutes(6), severity, "content", isMultiLine: false, wasGeneratedByUi: true), // IsFlagged=true; lots of time between two UI records
				new Record(7, Record.CreationTimeUnknown, severity, "application terminating", isMultiLine: false, wasGeneratedByUi: true), // IsFlagged=false; `MaxValue` represents an unknown timestamp
				new Record(8, now.AddMinutes(8), severity, "content", isMultiLine: false, wasGeneratedByUi: true), // IsFlagged=false; nothing to reference
			};

			_records = ImmutableArray.Create(records.ToArray());
		}

		[TestCleanup]
		public void PostTest()
		{
			foreach (IRecord record in _records)
			{
				Console.WriteLine(record.ToString());
			}

			_records.Clear();
		}

		[TestMethod]
		public void Analyze_CheckAllRecordsForGaps_FlaggedRecord20()
		{
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			var records = new List<IRecord>
			{
				new Record(10, now.AddSeconds(0), severity, "content", isMultiLine: false, wasGeneratedByUi: true),
				new Record(20, now.AddSeconds(7), severity, "content", isMultiLine: false, wasGeneratedByUi: false),
				new Record(30, now.AddSeconds(8), severity, "content", isMultiLine: false, wasGeneratedByUi: true),
			};

			var analyzer = new TimeGapAnalyzer();

			var state = new RecordState();

			analyzer.Analyze(
				records.ToImmutableArray(),
				state,
				EnvironmentHelper.GetExecutableDirectory(),
				GetUserDialog(3000),
				canUpdateMetadata: true);

			state.IsFlagged(records[0]).Should().BeFalse();
			state.IsFlagged(records[1]).Should().BeTrue();
			state.IsFlagged(records[2]).Should().BeFalse();
		}

		[TestMethod]
		[WorkItem(135)]
		public void Analyze_RecordMissingTimestamp_MissingTimestampShouldBeIgnored()
		{
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			var records = new List<IRecord>
			{
				new Record(10, now.AddSeconds(0), severity, "Index=0"),
				new Record(20, now.AddSeconds(1), severity, "Index=1"),
				new Record(30, Record.CreationTimeUnknown, severity, "Index=2"),
				new Record(40, now.AddSeconds(3), severity, "Index=3"),
				new Record(50, now.AddSeconds(4), severity, "Index=4"),
			};

			var analyzer = new TimeGapAnalyzer();

			var state = new RecordState();

			analyzer.Analyze(
				records.ToImmutableArray(),
				state,
				EnvironmentHelper.GetExecutableDirectory(),
				GetUserDialog(500),
				canUpdateMetadata: true);

			state.IsFlagged(records[2]).Should().BeFalse();
			state.IsFlagged(records[3]).Should().BeTrue();
			state.IsFlagged(records[4]).Should().BeTrue();
		}

		[TestMethod]
		public void Count_CheckAllRecordsForGaps_ResultMatchesCount()
		{
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			var records = new List<IRecord>
			{
				new Record(10, now.AddSeconds(0), severity, "content", isMultiLine: false, wasGeneratedByUi: true),
				new Record(20, now.AddSeconds(7), severity, "content", isMultiLine: false, wasGeneratedByUi: false),
				new Record(30, now.AddSeconds(8), severity, "content", isMultiLine: false, wasGeneratedByUi: true),
			};

			var analyzer = new TimeGapAnalyzer();

			var state = new RecordState();

			var results = analyzer.Analyze(
				records.ToImmutableArray(),
				state,
				EnvironmentHelper.GetExecutableDirectory(),
				GetUserDialog(3000),
				canUpdateMetadata: true);

			results.FlaggedRecords.Should().Be(1);
			analyzer.Count.Should().Be(1);
		}

		[TestMethod]
		public void Analyze_RecordWithoutTimestamp_MissingTimestampIsIgnored()
		{
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			var records = new List<IRecord>
			{
				new Record(0, Record.CreationTimeUnknown, severity, "fake header", isMultiLine: false, wasGeneratedByUi: false),
				new Record(1, now.AddSeconds(4), severity, "content", isMultiLine: false, wasGeneratedByUi: false),
				new Record(2, now.AddSeconds(8), severity, "content", isMultiLine: false, wasGeneratedByUi: false),
			};

			var analyzer = new TimeGapAnalyzer();

			var state = new RecordState();

			analyzer.Analyze(
				records.ToImmutableArray(),
				state,
				EnvironmentHelper.GetExecutableDirectory(),
				GetUserDialog(2000),
				canUpdateMetadata: true);

			state.IsFlagged(records[0]).Should().BeFalse();
			state.IsFlagged(records[1]).Should().BeFalse();
			state.IsFlagged(records[2]).Should().BeTrue();
		}

		[TestMethod]
		public void GivenUntimestampedPrefixWasFlagged_WhenAnalyzeRuns_ThenPrefixFlagIsCleared()
		{
			DateTime now = DateTime.Now;
			var records = new List<IRecord>
			{
				new Record(10, Record.CreationTimeUnknown, SeverityType.Debug, "header"),
				new Record(20, now, SeverityType.Debug, "first"),
				new Record(30, now.AddSeconds(1), SeverityType.Debug, "second"),
			};

			var analyzer = new TimeGapAnalyzer();
			var state = new RecordState();
			state.SetFlagged(records[0], true);

			analyzer.Analyze(records.ToImmutableArray(), state, TimeSpan.FromSeconds(5), canUpdateMetadata: true);

			state.IsFlagged(records[0]).Should().BeFalse();
		}

		[TestMethod]
		public void GivenPreviousRunHasGapsAndInvalidThreshold_WhenAnalyzeCalled_ThenPublicStateIsReset()
		{
			DateTime now = DateTime.Now;
			var records = R.Create()
				.WithCreatedAt(10, now)
				.WithCreatedAt(20, now.AddSeconds(10))
				.GetRecords();
			var userDialog = Substitute.For<IUserDialog>();
			userDialog
				.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns("not-a-number");
			var analyzer = new TimeGapAnalyzer();

			analyzer.Analyze(records, new RecordState(), TimeSpan.FromSeconds(5), canUpdateMetadata: false);
			analyzer.Count.Should().Be(1);
			Results results = analyzer.Analyze(records, new RecordState(), string.Empty, userDialog, canUpdateMetadata: false);

			results.Should().BeSameAs(Results.None);
			analyzer.Count.Should().Be(0);
			analyzer.MaximumPeriodDetected.Should().Be(TimeSpan.Zero);
			analyzer.FirstOccurrenceAt.Should().Be(DateTime.MaxValue);
		}

		[TestMethod]
		[WorkItem(935)]
		public void GivenFirstRunHasGaps_WhenSecondRunHasNoGaps_ThenFirstOccurrenceAtIsReset()
		{
			// Regression: Issue #935
			DateTime now = DateTime.Now;

			var recordsWithGap = R.Create()
				.WithCreatedAt(10, now.AddSeconds(0))
				.WithCreatedAt(20, now.AddSeconds(10))
				.GetRecords();

			var recordsWithNoGap = R.Create()
				.WithCreatedAt(30, now.AddSeconds(0))
				.WithCreatedAt(40, now.AddSeconds(1))
				.GetRecords();

			var analyzer = new TimeGapAnalyzer();

			analyzer.Analyze(recordsWithGap, new RecordState(), TimeSpan.FromSeconds(5), canUpdateMetadata: false);
			analyzer.Analyze(recordsWithNoGap, new RecordState(), TimeSpan.FromSeconds(5), canUpdateMetadata: false);

			analyzer.Count.Should().Be(0);
			analyzer.FirstOccurrenceAt.Should().Be(DateTime.MaxValue);
		}
	}
}
