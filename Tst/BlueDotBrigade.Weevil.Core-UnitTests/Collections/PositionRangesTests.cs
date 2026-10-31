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
		public void GivenTwoSeparateRanges_WhenARangeTouchingBothIsAdded_ThenTheyMergeIntoOne()
		{
			PositionRanges ranges = PositionRanges.Union(Positions(10, 20), Positions(30, 40));

			PositionRanges merged = PositionRanges.Union(ranges, Positions(21, 29));

			merged.Ranges.Should().Equal(new PositionRange(10, 40));
			merged.Count.Should().Be(31);
		}

		[TestMethod]
		public void GivenOneRange_WhenAPositionInsideIsRemoved_ThenTheRangeSplitsInTwo()
		{
			PositionRanges ranges = Positions(10, 40);

			PositionRanges split = PositionRanges.Except(ranges, Positions(25, 25));

			split.Ranges.Should().Equal(new PositionRange(10, 24), new PositionRange(26, 40));
			split.Count.Should().Be(30);
			split.Contains(25).Should().BeFalse();
		}

		[TestMethod]
		public void GivenPositionsInAnyOrder_WhenBuilt_ThenDuplicatesAreIgnoredAndNeighboursMerge()
		{
			PositionRanges ranges = PositionRanges.FromPositions(new List<int> { 7, 3, 4, 3, 9, 8 });

			ranges.Ranges.Should().Equal(new PositionRange(3, 4), new PositionRange(7, 9));
			ranges.Count.Should().Be(5);
		}

		[TestMethod]
		public void GivenRandomChanges_WhenComparedWithASetOfPositions_ThenTheyMatch()
		{
			var random = new Random(1015);
			var ranges = new PositionRanges();
			var expected = new SortedSet<int>();

			for (var i = 0; i < 2_000; i++)
			{
				// Several scattered ranges per change, some of them overlapping.
				var positions = new List<int>();
				for (var r = random.Next(1, 6); r > 0; r--)
				{
					var first = random.Next(0, 2_000);
					positions.AddRange(Enumerable.Range(first, random.Next(1, 30)));
				}

				PositionRanges changes = PositionRanges.FromPositions(positions);
				if (random.Next(2) == 0)
				{
					ranges = PositionRanges.Union(ranges, changes);
					expected.UnionWith(positions);
				}
				else
				{
					ranges = PositionRanges.Except(ranges, changes);
					expected.ExceptWith(positions);
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
			PositionRanges selection = Positions(0, all.Length - 1);

			PositionRanges remapped = PositionRanges.Remap(selection, all, even, all);

			remapped.Ranges.Should().Equal(new PositionRange(0, even.Length - 1));
		}

		[TestMethod]
		public void GivenEveryShownRecordSelected_WhenANarrowerFilterIsApplied_ThenTheSelectedRecordsItStillShowsStaySelected()
		{
			ImmutableArray<IRecord> all = R.WithLineNumbers(1, 100);
			ImmutableArray<IRecord> even = Keep(all, r => r.LineNumber % 2 == 0);
			ImmutableArray<IRecord> tenth = Keep(all, r => r.LineNumber % 10 == 0);
			PositionRanges selection = Positions(0, even.Length - 1);

			PositionRanges remapped = PositionRanges.Remap(selection, even, tenth, all);

			remapped.Ranges.Should().Equal(new PositionRange(0, tenth.Length - 1));
		}

		[TestMethod]
		public void GivenSelectedRecords_WhenAFilterShowsRecordsThatWereHidden_ThenOnlyTheSelectedOnesAreSelected()
		{
			ImmutableArray<IRecord> all = R.WithLineNumbers(1, 10);
			ImmutableArray<IRecord> even = Keep(all, r => r.LineNumber % 2 == 0);
			PositionRanges selection = Positions(0, even.Length - 1);

			// Lists that cannot create records show that remapping, which runs after every filter, creates none (#1137).
			PositionRanges remapped = PositionRanges.Remap(selection, new ColumnsOnlyRecordList(even), new ColumnsOnlyRecordList(all), new ColumnsOnlyRecordList(all));

			remapped.EnumeratePositions().Select(p => all[p].LineNumber).Should().Equal(2, 4, 6, 8, 10);
			remapped.RangeCount.Should().Be(5);
		}

		[TestMethod]
		public void GivenASelectedRecord_WhenTheFilterHidesIt_ThenItIsNoLongerSelected()
		{
			ImmutableArray<IRecord> all = R.WithLineNumbers(1, 10);
			ImmutableArray<IRecord> odd = Keep(all, r => r.LineNumber % 2 == 1);
			PositionRanges selection = Positions(1, 1);

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
			PositionRanges selection = Positions(0, all.Length - 1);

			PositionRanges remapped = PositionRanges.Remap(selection, all, filtered, all);

			remapped.Ranges.Should().Equal(new PositionRange(0, filtered.Length - 1));
		}

		private static PositionRanges Positions(int first, int last)
		{
			return PositionRanges.FromPositions(Enumerable.Range(first, last - first + 1).ToList());
		}

		private static ImmutableArray<IRecord> Keep(ImmutableArray<IRecord> records, Func<IRecord, bool> filter)
		{
			return records.Where(filter).ToImmutableArray();
		}
	}
}
