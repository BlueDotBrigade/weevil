namespace BlueDotBrigade.Weevil.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;

	[TestClass]
	public class PositionRangesTests
	{
		[TestMethod]
		public void GivenTwoSeparateRanges_WhenARangeTouchesBoth_ThenTheyMergeIntoOne()
		{
			var ranges = new PositionRanges();
			ranges.Add(10, 20);
			ranges.Add(30, 40);

			ranges.Add(21, 29);

			ranges.RangeCount.Should().Be(1);
			ranges.Count.Should().Be(31);
		}

		[TestMethod]
		public void GivenOneRange_WhenAPositionInsideIsRemoved_ThenTheRangeSplitsInTwo()
		{
			var ranges = new PositionRanges();
			ranges.Add(10, 40);

			ranges.Remove(25, 25);

			ranges.Ranges.Should().Equal(new PositionRange(10, 24), new PositionRange(26, 40));
			ranges.Count.Should().Be(30);
			ranges.Contains(25).Should().BeFalse();
		}

		[TestMethod]
		public void GivenRandomChanges_WhenComparedWithASetOfPositions_ThenTheyMatch()
		{
			var random = new Random(1015);
			var ranges = new PositionRanges();
			var expected = new SortedSet<int>();

			for (var i = 0; i < 5_000; i++)
			{
				var first = random.Next(0, 2_000);
				var last = first + random.Next(0, 20);
				if (random.Next(2) == 0)
				{
					ranges.Add(last, first);
					expected.UnionWith(Enumerable.Range(first, last - first + 1));
				}
				else
				{
					ranges.Remove(first, last);
					expected.ExceptWith(Enumerable.Range(first, last - first + 1));
				}
			}

			ranges.EnumeratePositions().Should().Equal(expected);
			ranges.Count.Should().Be(expected.Count);
			ranges.Ranges.Zip(ranges.Ranges.Skip(1)).Should().OnlyContain(pair => pair.First.Last + 1 < pair.Second.First);
		}

		[TestMethod]
		public void GivenEveryRecordSelected_WhenANarrowerFilterIsApplied_ThenAllOfItsResultsAreSelectedInOneRange()
		{
			ImmutableArray<IRecord> all = R.WithLineNumbers(1, 100);
			ImmutableArray<IRecord> even = Keep(all, r => r.LineNumber % 2 == 0);
			var selection = new PositionRanges();
			selection.Add(0, all.Length - 1);

			PositionRanges remapped = PositionRanges.Remap(selection, all, even, all);

			remapped.Ranges.Should().Equal(new PositionRange(0, even.Length - 1));
		}

		[TestMethod]
		public void GivenEveryShownRecordSelected_WhenANarrowerFilterIsApplied_ThenTheSelectedRecordsItStillShowsStaySelected()
		{
			ImmutableArray<IRecord> all = R.WithLineNumbers(1, 100);
			ImmutableArray<IRecord> even = Keep(all, r => r.LineNumber % 2 == 0);
			ImmutableArray<IRecord> tenth = Keep(all, r => r.LineNumber % 10 == 0);
			var selection = new PositionRanges();
			selection.Add(0, even.Length - 1);

			PositionRanges remapped = PositionRanges.Remap(selection, even, tenth, all);

			remapped.Ranges.Should().Equal(new PositionRange(0, tenth.Length - 1));
		}

		[TestMethod]
		public void GivenSelectedRecords_WhenAFilterShowsRecordsThatWereHidden_ThenOnlyTheSelectedOnesAreSelected()
		{
			ImmutableArray<IRecord> all = R.WithLineNumbers(1, 10);
			ImmutableArray<IRecord> even = Keep(all, r => r.LineNumber % 2 == 0);
			var selection = new PositionRanges();
			selection.Add(0, even.Length - 1);

			PositionRanges remapped = PositionRanges.Remap(selection, even, all, all);

			remapped.EnumeratePositions().Select(p => all[p].LineNumber).Should().Equal(2, 4, 6, 8, 10);
			remapped.RangeCount.Should().Be(5);
		}

		[TestMethod]
		public void GivenASelectedRecord_WhenTheFilterHidesIt_ThenItIsNoLongerSelected()
		{
			ImmutableArray<IRecord> all = R.WithLineNumbers(1, 10);
			ImmutableArray<IRecord> odd = Keep(all, r => r.LineNumber % 2 == 1);
			var selection = new PositionRanges();
			selection.Add(1, 1);

			PositionRanges remapped = PositionRanges.Remap(selection, all, odd, all);

			remapped.Count.Should().Be(0);
		}

		[TestMethod]
		public void GivenRecordsWithGapsInTheirLineNumbers_WhenEveryOneIsSelectedAndFiltered_ThenTheShownOnesStaySelected()
		{
			// Multi-line records leave gaps in the line numbers: 1 to 3, then 10 to 12.
			ImmutableArray<IRecord> all = ImmutableArray.Create(
				R.WithLineNumber(1), R.WithLineNumber(2), R.WithLineNumber(3),
				R.WithLineNumber(10), R.WithLineNumber(11), R.WithLineNumber(12));
			ImmutableArray<IRecord> filtered = Keep(all, r => r.LineNumber != 2);
			var selection = new PositionRanges();
			selection.Add(0, all.Length - 1);

			PositionRanges remapped = PositionRanges.Remap(selection, all, filtered, all);

			remapped.Ranges.Should().Equal(new PositionRange(0, filtered.Length - 1));
		}

		private static ImmutableArray<IRecord> Keep(ImmutableArray<IRecord> records, Func<IRecord, bool> filter)
		{
			return records.Where(filter).ToImmutableArray();
		}
	}
}
