namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Immutable;

	[TestClass]
	public class ElapsedTimesTests
	{
		private static readonly DateTime Start = new DateTime(2026, 10, 22, 10, 0, 0);

		[TestMethod]
		public void GivenNoRecords_WhenCalculated_ThenNoLineHasAnElapsedTime()
		{
			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(ImmutableArray<IRecord>.Empty);

			elapsedTimes.Get(1).Should().Be(Metadata.ElapsedTimeUnknown);
		}

		[TestMethod]
		public void GivenOneRecordWithTimestamp_WhenCalculated_ThenItHasNoElapsedTime()
		{
			ImmutableArray<IRecord> records = R.Create().WithCreatedAt(1, Start).GetRecords();

			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(records);

			elapsedTimes.Get(1).Should().Be(Metadata.ElapsedTimeUnknown);
		}

		[TestMethod]
		public void GivenTwoRecordsWithTimestamps_WhenCalculated_ThenTheSecondHasTheTimeBetweenThem()
		{
			ImmutableArray<IRecord> records = R.Create()
				.WithCreatedAt(1, Start.AddMilliseconds(100))
				.WithCreatedAt(2, Start.AddMilliseconds(300))
				.GetRecords();

			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(records);

			elapsedTimes.Get(1).Should().Be(Metadata.ElapsedTimeUnknown);
			elapsedTimes.Get(2).Should().Be(TimeSpan.FromMilliseconds(200));
		}

		[TestMethod]
		public void GivenRecordWithoutTimestamp_WhenCalculated_ThenTheNextIsMeasuredFromTheOneBefore()
		{
			ImmutableArray<IRecord> records = ImmutableArray.Create<IRecord>(
				new Record(1, Start, SeverityType.Debug, string.Empty),
				new Record(2, Record.CreationTimeUnknown, SeverityType.Debug, string.Empty),
				new Record(3, Start.AddMilliseconds(500), SeverityType.Debug, string.Empty));

			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(records);

			elapsedTimes.Get(1).Should().Be(Metadata.ElapsedTimeUnknown);
			elapsedTimes.Get(2).Should().Be(Metadata.ElapsedTimeUnknown);
			elapsedTimes.Get(3).Should().Be(TimeSpan.FromMilliseconds(500));
		}

		[TestMethod]
		public void GivenOnlyTheLastRecordHasATimestamp_WhenCalculated_ThenNeitherHasAnElapsedTime()
		{
			ImmutableArray<IRecord> records = ImmutableArray.Create<IRecord>(
				new Record(1, Record.CreationTimeUnknown, SeverityType.Debug, string.Empty),
				new Record(2, Start, SeverityType.Debug, string.Empty));

			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(records);

			elapsedTimes.Get(1).Should().Be(Metadata.ElapsedTimeUnknown);
			elapsedTimes.Get(2).Should().Be(Metadata.ElapsedTimeUnknown);
		}

		[TestMethod]
		public void GivenResultsOfAFilter_WhenCalculated_ThenEachIsMeasuredFromThePreviousResultAndHiddenLinesHaveNone()
		{
			// Lines 2, 10 and 11 are shown; lines 3 to 9 are hidden by the filter.
			ImmutableArray<IRecord> results = R.Create()
				.WithCreatedAt(2, Start)
				.WithCreatedAt(10, Start.AddSeconds(8))
				.WithCreatedAt(11, Start.AddSeconds(9))
				.GetRecords();

			ElapsedTimes elapsedTimes = ElapsedTimes.Calculate(results);

			elapsedTimes.Get(10).Should().Be(TimeSpan.FromSeconds(8));
			elapsedTimes.Get(11).Should().Be(TimeSpan.FromSeconds(1));
			elapsedTimes.Get(5).Should().Be(Metadata.ElapsedTimeUnknown);
			elapsedTimes.Get(12).Should().Be(Metadata.ElapsedTimeUnknown);
			elapsedTimes.Get(-1).Should().Be(Metadata.ElapsedTimeUnknown);
		}
	}
}
