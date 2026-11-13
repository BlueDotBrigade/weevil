namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using NSubstitute;

	[TestClass]
	public class FilterManagerTests
	{
		[TestMethod]
		public void GivenAbortedFilter_WhenTheResultsBeforeItStayShown_ThenTheyKeepTheirElapsedTimes()
		{
			// Regression: Issue #1016. The filter erased the elapsed time of each record it hid, before it checked
			// whether it had been aborted, so the results still shown lost their elapsed times.
			RecordList records = CreateRecordsOneSecondApart(100);
			var abortsWhenARecordIsKept = false;
			FilterManager filter = null;
			IMetricCollector collector = Substitute.For<IMetricCollector>();
			collector.When(c => c.Count(Arg.Any<IRecord>())).Do(_ =>
			{
				if (abortsWhenARecordIsKept)
				{
					filter.Abort();
				}
			});
			filter = CreateFilterManager(records, collector);
			filter.Apply(FilterType.PlainText, FilterCriteria.None);

			// The new filter keeps only the last record, so it hides the others before it is aborted.
			abortsWhenARecordIsKept = true;
			Action apply = () => filter.Apply(FilterType.PlainText, new FilterCriteria("line number: 100"));

			apply.Should().Throw<FilterAbortedException>();
			filter.Results.Should().HaveCount(100);
			records.Skip(1).Should().OnlyContain(r => filter.ElapsedTimes.Get(r.LineNumber) == TimeSpan.FromSeconds(1));
		}

		[TestMethod]
		public void GivenRecordsOneSecondApart_WhenEveryOtherIsShown_ThenElapsedTimesAreMeasuredBetweenTheShownRecords()
		{
			RecordList records = CreateRecordsOneSecondApart(6);
			FilterManager filter = CreateFilterManager(records, Substitute.For<IMetricCollector>());
			filter.Apply(FilterType.PlainText, FilterCriteria.None);

			filter.Apply(FilterType.PlainText, new FilterCriteria("line number: 2||line number: 4||line number: 6"));

			ElapsedTimesByLine(filter, records).Should().Equal(
				new Dictionary<int, TimeSpan>
				{
					{ 1, ElapsedTimes.Unknown },
					{ 2, ElapsedTimes.Unknown },
					{ 3, ElapsedTimes.Unknown },
					{ 4, TimeSpan.FromSeconds(2) },
					{ 5, ElapsedTimes.Unknown },
					{ 6, TimeSpan.FromSeconds(2) },
				});
		}

		[TestMethod]
		public void GivenTheResultsOfAFilterOverARecordTable_WhenTheirElapsedTimesAreRead_ThenOnlyTheFirstLookupByLineNumberCalculatesThoseOfEveryLine()
		{
			// The filter allocated 8 bytes for every line up to its last result, for elapsed times that are mostly only displayed (#1150).
			const int LineCount = 5_000;
			const long BytesForEveryLine = LineCount * 8L;
			FilterManager filter = CreateFilterManager(new TableRecordList(CreateTableOneSecondApart(LineCount)), Substitute.For<IMetricCollector>());

			filter.Apply(FilterType.PlainText, FilterCriteria.None);

			RecordList results = filter.Results;
			ElapsedTimes elapsedTimes = filter.ElapsedTimes;
			var byPosition = new TimeSpan[results.Count];
			var byLineNumber = new TimeSpan[results.Count];

			var readingByPosition = CountBytesAllocated(() =>
			{
				for (var position = 0; position < results.Count; position++)
				{
					byPosition[position] = ElapsedTimes.Measure(results, position);
				}
			});
			var firstReadingByLineNumber = CountBytesAllocated(() => elapsedTimes.Get(1));
			var readingByLineNumber = CountBytesAllocated(() =>
			{
				for (var position = 0; position < results.Count; position++)
				{
					byLineNumber[position] = elapsedTimes.Get(results.GetLineNumber(position));
				}
			});

			readingByPosition.Should().BeLessThan(BytesForEveryLine, "the grid measures elapsed times by position, one at a time");
			firstReadingByLineNumber.Should().BeGreaterThanOrEqualTo(BytesForEveryLine, "the filter did not calculate the elapsed times of every line");
			readingByLineNumber.Should().BeLessThan(BytesForEveryLine, "the first read by line number keeps them");
			byLineNumber.Should().Equal(byPosition);
			byPosition.Skip(1).Should().OnlyContain(elapsedTime => elapsedTime == TimeSpan.FromSeconds(1));
		}

		private static long CountBytesAllocated(Action read)
		{
			var before = GC.GetAllocatedBytesForCurrentThread();
			read();
			return GC.GetAllocatedBytesForCurrentThread() - before;
		}

		/// <summary>
		/// Lines 1 to <paramref name="count"/>, one second apart, in a record table, as an opened log's records are (#1140).
		/// </summary>
		private static RecordTable CreateTableOneSecondApart(int count)
		{
			var facts = new RecordFacts(SeverityType.Information, isMultiLine: false, wasGeneratedByUi: false);
			var start = new DateTime(2026, 11, 7, 9, 0, 0);
			var builder = new RecordTable.Builder();

			for (var lineNumber = 1; lineNumber <= count; lineNumber++)
			{
				builder.Add(lineNumber, start.AddSeconds(lineNumber), facts, $"Line {lineNumber}");
			}

			return builder.Build();
		}

		private static RecordList CreateRecordsOneSecondApart(int count)
		{
			// Each record's content ends with its line number, for example "Has line number: 7".
			R records = R.Create();
			var start = new DateTime(2026, 10, 22, 10, 0, 0);
			for (var i = 0; i < count; i++)
			{
				records.WithCreatedAt(start.AddSeconds(i).ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
			}

			return records.GetRecords();
		}

		private static FilterManager CreateFilterManager(RecordList records, IMetricCollector collector)
		{
			IFilterAliasExpander aliasExpander = Substitute.For<IFilterAliasExpander>();
			aliasExpander.Expand(Arg.Any<string>()).Returns(x => x.Arg<string>());

			return new FilterManager(
				Substitute.For<ICoreExtension>(),
				new ContextDictionary(),
				aliasExpander,
				new RecordScope(records),
				ImmutableArray.Create(collector),
				Substitute.For<IRegionManager>(),
				Substitute.For<IBookmarkManager>(),
				new RecordState(),
				new FilterHistory(),
				new FilterHistory());
		}

		private static Dictionary<int, TimeSpan> ElapsedTimesByLine(IFilter filter, RecordList records)
		{
			return records.ToDictionary(r => r.LineNumber, r => filter.ElapsedTimes.Get(r.LineNumber));
		}
	}
}
