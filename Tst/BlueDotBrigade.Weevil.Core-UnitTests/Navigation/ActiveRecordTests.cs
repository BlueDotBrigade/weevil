namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestingTools.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ActiveRecordTests
	{
		[TestMethod]
		[ExpectedException((typeof(ArgumentException)))]
		public void Constructor_UninitializedArray_ThrowsArgumentException()
		{
			var activeRecord = new ActiveRecord(new ImmutableArray<IRecord>());

			Assert.Fail("Uninitialized immutable array should throw exception.");
		}

		[TestMethod]
		[ExpectedException((typeof(ArgumentException)))]
		public void UpdateDataSource_UninitializedArray_ThrowsArgumentException()
		{
			var activeRecord = new ActiveRecord(new ImmutableArray<IRecord>());

			Assert.Fail("Uninitialized immutable array should throw exception.");
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

			Assert.AreEqual(
				Record.Dummy,
				navigator.Record);
		}
	}
}
