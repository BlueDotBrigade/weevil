namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using NSubstitute;

	[TestClass]
	public class FilterManagerTests
	{
		private static readonly TimeSpan TimeLimit = TimeSpan.FromSeconds(30);

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
		[WorkItem(1248)]
		public void GivenAFilterStoppedOrStillRunning_WhenANewerFilterIsAppliedOnAnotherThread_ThenOnlyTheNewerFiltersResultsAreShown()
		{
			// A new filter, or the filter's own start, cleared the Stop meant for the older filter, which ran on and replaced the newer filter's results.
			RecordList records = CreateRecordsOneSecondApart(2_000);
			var older = new FilterCriteria(@"line number: \d*[13579]$");
			var newer = new FilterCriteria("line number: 7");
			var isStoppedWhileStarting = false;
			var olderIsHeld = new ManualResetEventSlim();
			var olderMayGoOn = new ManualResetEventSlim();
			var holdsOlderWhileFiltering = 0;
			var collector = new CountingCollector
			{
				// The older filter is held while it filters, until the newer filter has started.
				OnCount = () =>
				{
					if (Interlocked.Exchange(ref holdsOlderWhileFiltering, 0) == 1)
					{
						olderIsHeld.Set();
						olderMayGoOn.Wait(TimeLimit);
					}
				},
			};
			FilterManager filter = CreateFilterManager(records, collector, expression =>
			{
				if (expression == older.Include && isStoppedWhileStarting)
				{
					// The older filter is held while it builds its strategy, until the test has stopped it.
					olderIsHeld.Set();
					olderMayGoOn.Wait(TimeLimit);
				}
				else if (expression == newer.Include)
				{
					// The newer filter has started.
					olderMayGoOn.Set();
				}
			});
			filter.Apply(FilterType.PlainText, newer);
			var newerCount = filter.Results.Count;

			for (var i = 0; i < 1_000; i++)
			{
				// Every other time, the older filter is stopped while it starts; otherwise the newer filter alone replaces it.
				isStoppedWhileStarting = i % 2 == 0;
				holdsOlderWhileFiltering = isStoppedWhileStarting ? 0 : 1;
				olderIsHeld.Reset();
				olderMayGoOn.Reset();

				Task<bool> olderRun = Task.Run(() => TryApply(filter, FilterType.RegularExpression, older));
				olderIsHeld.Wait(TimeLimit).Should().BeTrue();

				if (isStoppedWhileStarting)
				{
					filter.Abort();
				}

				Task<bool> newerRun = Task.Run(() => TryApply(filter, FilterType.PlainText, newer));
				Task.WaitAll(new Task[] { olderRun, newerRun }, TimeLimit).Should().BeTrue();

				olderRun.Result.Should().BeFalse("the older filter is stopped (iteration {0})", i);
				newerRun.Result.Should().BeTrue();
				filter.Criteria.Should().Be(newer);
				filter.Results.Should().HaveCount(newerCount);
				filter.GetMetrics()[CountingCollector.Key].Should().Be(newerCount);
			}
		}

		[TestMethod]
		[WorkItem(1248)]
		public void GivenAFilterStoppedPartway_WhenTheMetricsAreRead_ThenTheyAreThoseOfTheResultsShown()
		{
			// The stopped filter had already counted some of its records, so the severity counts did not match the records shown.
			RecordList records = CreateRecordsOneSecondApart(100);
			var collector = new CountingCollector();
			FilterManager filter = CreateFilterManager(records, collector);
			filter.Apply(FilterType.PlainText, FilterCriteria.None);
			collector.OnCount = () => filter.Abort();

			Action apply = () => filter.Apply(FilterType.PlainText, new FilterCriteria("line number: 100"));

			apply.Should().Throw<FilterAbortedException>();
			filter.GetMetrics()[CountingCollector.Key].Should().Be(100);
		}

		[TestMethod]
		public void GivenAResultsChangedHandlerThatAppliesAFilter_WhenAFilterIsApplied_ThenTheHandlerIsRefusedRatherThanWaitingForever()
		{
			RecordList records = CreateRecordsOneSecondApart(10);
			FilterManager filter = CreateFilterManager(records, new CountingCollector());
			Exception refused = null;
			filter.ResultsChanged += (_, _) =>
			{
				try
				{
					filter.Apply(FilterType.PlainText, FilterCriteria.None);
				}
				catch (InvalidOperationException exception)
				{
					refused = exception;
				}
			};

			Task apply = Task.Run(() => filter.Apply(FilterType.PlainText, new FilterCriteria("line number: 1")));

			apply.Wait(TimeLimit).Should().BeTrue("the handler must not wait for the filter it handles");
			refused.Should().BeOfType<InvalidOperationException>();
			filter.Criteria.Include.Should().Be("line number: 1");
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

		private static bool TryApply(FilterManager filter, FilterType filterType, IFilterCriteria criteria)
		{
			try
			{
				filter.Apply(filterType, criteria);
				return true;
			}
			catch (FilterAbortedException)
			{
				return false;
			}
		}

		private static FilterManager CreateFilterManager(RecordList records, IMetricCollector collector, Action<string> onExpand = null)
		{
			IFilterAliasExpander aliasExpander = Substitute.For<IFilterAliasExpander>();
			aliasExpander.Expand(Arg.Any<string>()).Returns(x =>
			{
				onExpand?.Invoke(x.Arg<string>());
				return x.Arg<string>();
			});

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

		/// <summary>
		/// Counts the records a filter keeps.
		/// </summary>
		private sealed class CountingCollector : IMetricCollector
		{
			public const string Key = "Count";

			private int _count;

			public Action OnCount { get; set; }

			public void Count(IRecord record)
			{
				Interlocked.Increment(ref _count);
				OnCount?.Invoke();
			}

			public void Reset() => Interlocked.Exchange(ref _count, 0);

			public IDictionary<string, object> GetResults() => new Dictionary<string, object> { { Key, Volatile.Read(ref _count) } };
		}
	}
}
