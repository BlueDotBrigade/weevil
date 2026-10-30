namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ActiveRecordTests
	{
		[TestMethod]
		public void GivenNoRecordList_WhenCreated_ThenItThrows()
		{
			// An uninitialized array now becomes an empty record list (#1136), so only a missing list is refused.
			Action act = () => _ = new ActiveRecord(null);
			act.Should().Throw<ArgumentNullException>();
		}

		[TestMethod]
		public void GivenNoRecordList_WhenTheDataSourceIsUpdated_ThenItThrows()
		{
			var activeRecord = new ActiveRecord(ImmutableArray.Create<IRecord>());
			Action act = () => activeRecord.UpdateDataSource(null);
			act.Should().Throw<ArgumentNullException>();
		}

		[TestMethod]
		public void UpdateDataSource_ActiveRecordNoLongerExists_DefaultActiveRecord()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10), // 0
				R.WithLineNumber(20), // 1
				R.WithLineNumber(30), // 2
			};

			var navigator = new ActiveRecord(records);
			navigator.SetActiveIndex(2);

			navigator.UpdateDataSource(ImmutableArray.Create<IRecord>());

			navigator.Record.Should().Be(Record.Dummy);
		}
	}
}
