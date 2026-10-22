namespace BlueDotBrigade.Weevil.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;

	[TestClass]
	public class SelectedRecordsTests
	{
		[TestMethod]
		public void GivenSelectedPositions_WhenRead_ThenTheRecordsAreKeyedByLineNumberInOrder()
		{
			ImmutableArray<IRecord> results = R.WithLineNumbers(11, 20);
			PositionRanges selection = PositionRanges.FromPositions(new List<int> { 1, 2, 5 });

			var selected = new SelectedRecords(selection, results);

			selected.Count.Should().Be(3);
			selected.Keys.ToArray().Should().Equal(12, 13, 16);
			selected.Values.Select(r => r.LineNumber).Should().Equal(12, 13, 16);
			selected.First().Value.Should().BeSameAs(results[1]);
			selected[16].Should().BeSameAs(results[5]);
		}

		[TestMethod]
		public void GivenALineThatIsNotSelected_WhenLookedUp_ThenItIsNotFound()
		{
			ImmutableArray<IRecord> results = R.WithLineNumbers(11, 20);
			PositionRanges selection = PositionRanges.FromPositions(new List<int> { 1 });

			var selected = new SelectedRecords(selection, results);

			selected.ContainsKey(11).Should().BeFalse();
			selected.ContainsKey(99).Should().BeFalse();
			selected.TryGetValue(11, out IRecord record).Should().BeFalse();
			record.Should().BeNull();
			Action read = () => _ = selected[11];
			read.Should().Throw<KeyNotFoundException>();
		}

		[TestMethod]
		public void GivenTheSelectedRecords_WhenChangedDirectly_ThenTheChangeIsRefused()
		{
			var selected = new SelectedRecords(new PositionRanges(), R.WithLineNumbers(1, 3));

			Action add = () => selected.Add(1, R.WithLineNumber(1));

			selected.IsReadOnly.Should().BeTrue();
			add.Should().Throw<NotSupportedException>();
		}
	}
}
