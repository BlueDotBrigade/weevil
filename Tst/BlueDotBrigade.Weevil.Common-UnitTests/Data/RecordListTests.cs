namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;

	[TestClass]
	public class RecordListTests
	{
		[TestMethod]
		public void GivenAnArrayOfRecords_WhenWrapped_ThenTheListReadsThemInOrder()
		{
			ImmutableArray<IRecord> records = R.WithLineNumbers(11, 13);

			RecordList list = records;

			list.Count.Should().Be(3);
			list[1].LineNumber.Should().Be(12);
			list.Select(record => record.LineNumber).Should().Equal(11, 12, 13);
			list.ToImmutableArray().Should().Equal(records);
		}

		[TestMethod]
		public void GivenARecordList_WhenEnumerated_ThenItReadsEveryRecordInOrder()
		{
			RecordList list = R.WithLineNumbers(11, 13);

			var lineNumbers = new List<int>();
			foreach (IRecord record in list)
			{
				lineNumbers.Add(record.LineNumber);
			}

			lineNumbers.Should().Equal(11, 12, 13);
		}

		[TestMethod]
		public void GivenADefaultArray_WhenConverted_ThenTheListIsEmpty()
		{
			RecordList list = default(ImmutableArray<IRecord>);

			list.IsEmpty.Should().BeTrue();
			list.Should().BeSameAs(RecordList.Empty);
		}

		[TestMethod]
		public void GivenAnotherRecordForALineInTheList_WhenLookedFor_ThenItIsFoundByItsLineNumber()
		{
			RecordList list = R.WithLineNumbers(11, 13);

			list.IndexOf(R.WithLineNumber(12)).Should().Be(1);
			list.Contains(R.WithLineNumber(13)).Should().BeTrue();
			list.IndexOf(R.WithLineNumber(14)).Should().Be(-1);
			list.IndexOf(null).Should().Be(-1);
		}

		[TestMethod]
		public void GivenARecordList_WhenChanged_ThenTheChangeIsRefused()
		{
			IList<IRecord> list = (RecordList)R.WithLineNumbers(11, 13);

			FluentActions.Invoking(() => list.Add(R.WithLineNumber(14))).Should().Throw<NotSupportedException>();
			FluentActions.Invoking(() => list[0] = R.WithLineNumber(14)).Should().Throw<NotSupportedException>();
			list.IsReadOnly.Should().BeTrue();
		}
	}
}
