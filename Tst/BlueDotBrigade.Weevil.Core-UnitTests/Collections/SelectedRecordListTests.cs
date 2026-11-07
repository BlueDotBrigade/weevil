namespace BlueDotBrigade.Weevil.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;

	[TestClass]
	public class SelectedRecordListTests
	{
		[TestMethod]
		public void GivenSelectedRangesApart_WhenReadByPosition_ThenEachPositionIsTheNextSelectedRecord()
		{
			ImmutableArray<IRecord> results = R.WithLineNumbers(11, 20);
			PositionRanges selection = PositionRanges.FromPositions(new List<int> { 1, 2, 5, 8, 9 });

			var selected = new SelectedRecordList(selection, results);

			selected.Count.Should().Be(5);
			selected.Select(r => r.LineNumber).Should().Equal(12, 13, 16, 19, 20);
			selected[2].Should().Be(results[5]);
			selected.GetCreatedAt(3).Should().Be(results[8].CreatedAt);
		}

		[TestMethod]
		public void GivenSelectedRanges_WhenTheirLinesAreRead_ThenNoRecordIsCreated()
		{
			ImmutableArray<IRecord> results = R.WithLineNumbers(11, 20);
			PositionRanges selection = PositionRanges.FromPositions(new List<int> { 0, 4, 5, 9 });

			var selected = new SelectedRecordList(selection, new ColumnsOnlyRecordList(results));

			Enumerable.Range(0, selected.Count).Select(selected.GetLineNumber).Should().Equal(11, 15, 16, 20);
		}

		[TestMethod]
		public void GivenASelection_WhenReadOutsideIt_ThenItThrows()
		{
			var selected = new SelectedRecordList(PositionRanges.FromPositions(new List<int> { 1, 2 }), R.WithLineNumbers(11, 20));

			FluentActions.Invoking(() => selected.GetLineNumber(-1)).Should().Throw<ArgumentOutOfRangeException>();
			FluentActions.Invoking(() => selected.GetLineNumber(2)).Should().Throw<ArgumentOutOfRangeException>();
		}
	}
}
