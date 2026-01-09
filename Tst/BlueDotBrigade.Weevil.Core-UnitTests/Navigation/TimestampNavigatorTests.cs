namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TimestampNavigatorTests
	{
		[TestMethod]
		public void GoTo_NoRecords_Throws()
		{
			var records = new List<IRecord>();

			var timestamp = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");

			Action act = () => new TimestampNavigator(new ActiveRecord(records)).Find(timestamp);
			act.Should().Throw<RecordNotFoundException>();
		}

		[TestMethod]
		public void GoTo_RecordsWithoutTimestamps_Throws()
		{
			var records = R.Create()
				.WithCreatedAt(1, Record.CreationTimeUnknown)
				.WithCreatedAt(2, Record.CreationTimeUnknown)
				.WithCreatedAt(3, Record.CreationTimeUnknown)
				.GetRecords();

			var activeRecord = new ActiveRecord(records);
			Action act = () => new TimestampNavigator(activeRecord).Find("10:30:00");
			act.Should().Throw<RecordNotFoundException>();
		}
	}
}
