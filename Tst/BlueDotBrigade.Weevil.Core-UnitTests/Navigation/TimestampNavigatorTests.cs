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

		[TestMethod]
		public void GoTo_TimeOnlySpanningMultipleDays_UsesActiveRecordDate()
		{
			// Records span two days: Jan 1 and Jan 2
			var records = R.Create()
				.WithCreatedAt(1, "2024-01-01 11:00:00") // day 1
				.WithCreatedAt(2, "2024-01-02 11:00:00") // day 2
				.GetRecords();

			var activeRecord = new ActiveRecord(records);
			activeRecord.SetActiveIndex(1); // select the Jan 2 record

			// Navigate using time-only (no date) - should use the active record's date (Jan 2)
			var result = new TimestampNavigator(activeRecord).Find("11:00:00");

			Assert.AreEqual(2, result.LineNumber);
		}
	}
}
