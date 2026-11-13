namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Immutable;
	using System.Linq;

	[TestClass]
	public class ElapsedTimesTests
	{
		private static readonly DateTime Start = new DateTime(2026, 10, 22, 10, 0, 0);

		[TestMethod]
		public void GivenNoRecords_WhenCalculated_ThenNoLineHasAnElapsedTime()
		{
			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(RecordList.Empty);

			elapsedTimes.Get(1).Should().Be(ElapsedTimes.Unknown);
		}

		[TestMethod]
		public void GivenOneRecordWithTimestamp_WhenCalculated_ThenItHasNoElapsedTime()
		{
			RecordList records = R.Create().WithCreatedAt(1, Start).GetRecords();

			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(records);

			elapsedTimes.Get(1).Should().Be(ElapsedTimes.Unknown);
		}

		[TestMethod]
		public void GivenTwoRecordsWithTimestamps_WhenCalculated_ThenTheSecondHasTheTimeBetweenThem()
		{
			RecordList records = R.Create()
				.WithCreatedAt(1, Start.AddMilliseconds(100))
				.WithCreatedAt(2, Start.AddMilliseconds(300))
				.GetRecords();

			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(records);

			elapsedTimes.Get(1).Should().Be(ElapsedTimes.Unknown);
			elapsedTimes.Get(2).Should().Be(TimeSpan.FromMilliseconds(200));
		}

		[TestMethod]
		public void GivenRecordWithoutTimestamp_WhenCalculated_ThenTheNextIsMeasuredFromTheOneBefore()
		{
			RecordList records = ImmutableArray.Create<IRecord>(
				new Record(1, Start, SeverityType.Debug, string.Empty),
				new Record(2, Record.CreationTimeUnknown, SeverityType.Debug, string.Empty),
				new Record(3, Start.AddMilliseconds(500), SeverityType.Debug, string.Empty));

			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(records);

			elapsedTimes.Get(1).Should().Be(ElapsedTimes.Unknown);
			elapsedTimes.Get(2).Should().Be(ElapsedTimes.Unknown);
			elapsedTimes.Get(3).Should().Be(TimeSpan.FromMilliseconds(500));
		}

		[TestMethod]
		public void GivenOnlyTheLastRecordHasATimestamp_WhenCalculated_ThenNeitherHasAnElapsedTime()
		{
			RecordList records = ImmutableArray.Create<IRecord>(
				new Record(1, Record.CreationTimeUnknown, SeverityType.Debug, string.Empty),
				new Record(2, Start, SeverityType.Debug, string.Empty));

			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(records);

			elapsedTimes.Get(1).Should().Be(ElapsedTimes.Unknown);
			elapsedTimes.Get(2).Should().Be(ElapsedTimes.Unknown);
		}

		[TestMethod]
		public void GivenResultsOfAFilter_WhenCalculated_ThenEachIsMeasuredFromThePreviousResultAndHiddenLinesHaveNone()
		{
			// Lines 2, 10 and 11 are shown; lines 3 to 9 are hidden by the filter.
			RecordList results = R.Create()
				.WithCreatedAt(2, Start)
				.WithCreatedAt(10, Start.AddSeconds(8))
				.WithCreatedAt(11, Start.AddSeconds(9))
				.GetRecords();

			// A list that cannot create records shows that the calculation creates none (#1137).
			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(new ColumnsOnlyRecordList(results));

			elapsedTimes.Get(10).Should().Be(TimeSpan.FromSeconds(8));
			elapsedTimes.Get(11).Should().Be(TimeSpan.FromSeconds(1));
			elapsedTimes.Get(5).Should().Be(ElapsedTimes.Unknown);
			elapsedTimes.Get(12).Should().Be(ElapsedTimes.Unknown);
			elapsedTimes.Get(-1).Should().Be(ElapsedTimes.Unknown);
		}

		[TestMethod]
		public void GivenResultsWithRecordsWithoutTimestamps_WhenMeasuredByPosition_ThenEachHasTheElapsedTimeOfItsLine()
		{
			// The first record, records in between and the last record have no timestamp, and the filter hid lines 4, 7, 10 and 11.
			RecordList results = R.Create()
				.WithCreatedAt(2, Record.CreationTimeUnknown)
				.WithCreatedAt(3, Start)
				.WithCreatedAt(5, Record.CreationTimeUnknown)
				.WithCreatedAt(6, Record.CreationTimeUnknown)
				.WithCreatedAt(8, Start.AddSeconds(4))
				.WithCreatedAt(9, Start.AddSeconds(5))
				.WithCreatedAt(12, Record.CreationTimeUnknown)
				.GetRecords();

			// A list that cannot create records shows that neither way of reading creates one (#1137).
			var columnsOnly = new ColumnsOnlyRecordList(results);
			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(columnsOnly);
			TimeSpan[] byPosition = Enumerable.Range(0, results.Count).Select(p => ElapsedTimes.Measure(columnsOnly, p)).ToArray();
			TimeSpan[] byLineNumber = Enumerable.Range(0, results.Count).Select(p => elapsedTimes.Get(results.GetLineNumber(p))).ToArray();

			byPosition.Should().Equal(
				ElapsedTimes.Unknown,
				ElapsedTimes.Unknown,
				ElapsedTimes.Unknown,
				ElapsedTimes.Unknown,
				TimeSpan.FromSeconds(4),
				TimeSpan.FromSeconds(1),
				ElapsedTimes.Unknown);
			byPosition.Should().Equal(byLineNumber);
		}

		[TestMethod]
		[DataRow(-1)]
		[DataRow(-2)]
		public void GivenATimestampedRecordOnANegativeLine_WhenTheNextRecordIsMeasured_ThenItMatchesTheLookupByLineNumber(int negativeLine)
		{
			// Neither way measures the negative line itself, and neither measures from line -1, Record.Dummy's line.
			RecordList results = R.Create()
				.WithCreatedAt(negativeLine, Start)
				.WithCreatedAt(1, Start.AddSeconds(2))
				.GetRecords();
			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(results);

			ElapsedTimes.Measure(results, 0).Should().Be(ElapsedTimes.Unknown);
			ElapsedTimes.Measure(results, 1).Should().Be(elapsedTimes.Get(1));
		}
	}
}
