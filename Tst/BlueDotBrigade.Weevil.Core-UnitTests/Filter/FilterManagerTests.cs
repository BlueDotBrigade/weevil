namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Collections.Concurrent;
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
			ImmutableArray<IRecord> records = CreateRecordsOneSecondApart(100);
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
			ImmutableArray<IRecord> records = CreateRecordsOneSecondApart(6);
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
		public void GivenShownRecords_WhenAFilterChangesTheirElapsedTimes_ThenOnlyThoseRecordsRaiseTheChange()
		{
			ImmutableArray<IRecord> records = CreateRecordsOneSecondApart(6);
			FilterManager filter = CreateFilterManager(records, Substitute.For<IMetricCollector>());
			filter.Apply(FilterType.PlainText, FilterCriteria.None);

			var changedLines = new ConcurrentBag<int>();
			foreach (IRecord record in records)
			{
				record.Metadata.PropertyChanged += (sender, e) =>
				{
					if (e.PropertyName == nameof(Metadata.ElapsedTime))
					{
						changedLines.Add(record.LineNumber);
					}
				};
			}

			filter.Apply(FilterType.PlainText, new FilterCriteria("line number: 2||line number: 4||line number: 6"));

			// Line 1 had no elapsed time before, and has none after.
			changedLines.Should().BeEquivalentTo(new[] { 2, 3, 4, 5, 6 });
		}

		private static ImmutableArray<IRecord> CreateRecordsOneSecondApart(int count)
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

		private static FilterManager CreateFilterManager(ImmutableArray<IRecord> records, IMetricCollector collector)
		{
			var view = new ViewAnnotations();
			foreach (IRecord record in records)
			{
				record.Metadata.Attach(view, record.LineNumber);
			}

			IFilterAliasExpander aliasExpander = Substitute.For<IFilterAliasExpander>();
			aliasExpander.Expand(Arg.Any<string>()).Returns(x => x.Arg<string>());

			return new FilterManager(
				Substitute.For<ICoreExtension>(),
				new ContextDictionary(),
				aliasExpander,
				records,
				ImmutableArray.Create(collector),
				Substitute.For<IRegionManager>(),
				Substitute.For<IBookmarkManager>(),
				view);
		}

		private static Dictionary<int, TimeSpan> ElapsedTimesByLine(IFilter filter, ImmutableArray<IRecord> records)
		{
			return records.ToDictionary(r => r.LineNumber, r => filter.ElapsedTimes.Get(r.LineNumber));
		}
	}
}
