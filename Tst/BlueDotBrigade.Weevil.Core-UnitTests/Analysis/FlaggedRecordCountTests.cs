namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Analysis.Timeline;
	using BlueDotBrigade.Weevil.Data;

	[TestClass]
	public class FlaggedRecordCountTests
	{
		private const string TwoGroupsRegex = @"A=(?<A>\d+)\s+B=(?<B>\d+)";

		[TestMethod]
		[WorkItem(930)]
		public void GivenOneRecordWithTwoCapturedValues_WhenDetectDataRuns_ThenOneFlaggedRecordIsReported()
		{
			ImmutableArray<IRecord> records = BuildRecords("A=1 B=2");
			var analyzer = new DetectDataAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());

			var annotations = new AnnotationStore();
			var flags = new FlagSet();

			Results results = Analyze(analyzer, records, annotations, flags, TwoGroupsRegex);

			results.FlaggedRecords.Should().Be(1);
			flags.IsFlagged(records[0].LineNumber).Should().BeTrue();
		}

		[TestMethod]
		[WorkItem(930)]
		public void GivenOneRecordWithTwoFirstOccurrences_WhenFirstOccurrenceRuns_ThenOneFlaggedRecordIsReported()
		{
			ImmutableArray<IRecord> records = BuildRecords("A=1 B=2");
			var analyzer = new FirstOccurrenceAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());

			var annotations = new AnnotationStore();
			var flags = new FlagSet();

			Results results = Analyze(analyzer, records, annotations, flags, TwoGroupsRegex);

			results.FlaggedRecords.Should().Be(1);
			flags.IsFlagged(records[0].LineNumber).Should().BeTrue();
		}

		[TestMethod]
		[WorkItem(930)]
		public void GivenOneRecordWithTwoLastOccurrences_WhenLastOccurrenceRuns_ThenOneFlaggedRecordIsReported()
		{
			ImmutableArray<IRecord> records = BuildRecords("A=1 B=2");
			var analyzer = new LastOccurrenceAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());

			var annotations = new AnnotationStore();
			var flags = new FlagSet();

			Results results = Analyze(analyzer, records, annotations, flags, TwoGroupsRegex);

			results.FlaggedRecords.Should().Be(1);
			flags.IsFlagged(records[0].LineNumber).Should().BeTrue();
		}

		[TestMethod]
		[WorkItem(930)]
		public void GivenTwoRecordsWhereTwoValuesTransitionTogether_WhenStateTransitionsRuns_ThenTwoFlaggedRecordsAreReported()
		{
			ImmutableArray<IRecord> records = BuildRecords("A=1 B=10", "A=2 B=20");
			var analyzer = new StateTransitionsAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());

			var annotations = new AnnotationStore();
			var flags = new FlagSet();

			Results results = Analyze(analyzer, records, annotations, flags, TwoGroupsRegex);

			results.FlaggedRecords.Should().Be(2);
			records.Should().OnlyContain(record => flags.IsFlagged(record.LineNumber));
		}

		[TestMethod]
		[WorkItem(930)]
		public void GivenOneSingleRecordRun_WhenStableValueRunsRuns_ThenOneFlaggedRecordIsReported()
		{
			ImmutableArray<IRecord> records = BuildRecords("A=1");
			var analyzer = new StableValueRunsAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());

			var annotations = new AnnotationStore();
			var flags = new FlagSet();

			Results results = Analyze(analyzer, records, annotations, flags, @"A=(?<A>\d+)");

			results.FlaggedRecords.Should().Be(1);
			flags.IsFlagged(records[0].LineNumber).Should().BeTrue();
		}

		[TestMethod]
		[WorkItem(930)]
		public void GivenTwoValuesRiseTogether_WhenRisingEdgesRuns_ThenOneFlaggedRecordIsReported()
		{
			ImmutableArray<IRecord> records = BuildRecords("A=1 B=10", "A=2 B=20");
			var analyzer = new DetectRisingEdgeAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());

			var annotations = new AnnotationStore();
			var flags = new FlagSet();

			Results results = Analyze(analyzer, records, annotations, flags, TwoGroupsRegex);

			results.FlaggedRecords.Should().Be(1);
			flags.IsFlagged(records[0].LineNumber).Should().BeTrue();
			flags.IsFlagged(records[1].LineNumber).Should().BeFalse();
		}

		[TestMethod]
		[WorkItem(930)]
		public void GivenTwoValuesFallTogether_WhenFallingEdgesRuns_ThenOneFlaggedRecordIsReported()
		{
			ImmutableArray<IRecord> records = BuildRecords("A=2 B=20", "A=1 B=10");
			var analyzer = new DetectFallingEdgeAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());

			var annotations = new AnnotationStore();
			var flags = new FlagSet();

			Results results = Analyze(analyzer, records, annotations, flags, TwoGroupsRegex);

			results.FlaggedRecords.Should().Be(1);
			flags.IsFlagged(records[0].LineNumber).Should().BeTrue();
			flags.IsFlagged(records[1].LineNumber).Should().BeFalse();
		}

		[TestMethod]
		[WorkItem(930)]
		public void GivenNumericSamples_WhenStatisticsRuns_ThenNoFlaggedRecordsAreReported()
		{
			ImmutableArray<IRecord> records = BuildRecords("Value=1", "Value=2");
			var analyzer = new StatisticalAnalyzer(RecordAnalyzerTestContext.CreateFilterStrategy());

			var annotations = new AnnotationStore();
			var flags = new FlagSet();

			Results results = Analyze(analyzer, records, annotations, flags, @"Value=(?<Value>\d+)");

			results.FlaggedRecords.Should().Be(0);
			results.Data["Count"].Should().Be(2d);
			records.Should().NotContain(record => flags.IsFlagged(record.LineNumber));
		}

		private static Results Analyze(IRecordAnalyzer analyzer, ImmutableArray<IRecord> records, IAnnotations annotations, IFlags flags, string regex)
		{
			return analyzer.Analyze(
				records,
				annotations,
				flags,
				string.Empty,
				RecordAnalyzerTestContext.CreateDialog(regex),
				canUpdateMetadata: true);
		}

		private static ImmutableArray<IRecord> BuildRecords(params string[] contents)
		{
			DateTime start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

			return contents
				.Select((content, index) => (IRecord)new Record(
					index + 1,
					start.AddSeconds(index),
					SeverityType.Information,
					content,
					new Metadata()))
				.ToImmutableArray();
		}
	}
}
