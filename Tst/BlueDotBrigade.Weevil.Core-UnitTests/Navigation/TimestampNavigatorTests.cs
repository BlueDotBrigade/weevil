namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestingTools.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TimestampNavigatorTests
	{
		[TestMethod]
		[ExpectedException(typeof(RecordNotFoundException))]
		public void GoTo_NoRecords_Throws()
		{
			var records = new List<IRecord>();

			var timestamp = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");

			Assert.AreEqual(
				Record.Dummy,
				new TimestampNavigator(new RecordNavigator(records)).Find(timestamp));
		}

		[TestMethod]
		[ExpectedException(typeof(RecordNotFoundException))]
		public void GoTo_RecordsWithoutTimestamps_Throws()
		{
			var records = R.Create()
				.WithCreatedAt(1, Record.CreationTimeUnknown)
				.WithCreatedAt(2, Record.CreationTimeUnknown)
				.WithCreatedAt(3, Record.CreationTimeUnknown)
				.GetRecords();

			var navigator = new RecordNavigator(records);
			var result = new TimestampNavigator(navigator).Find("10:30:00");

			Assert.Fail("Because only a time was provided, and no date, an exception should be thrown.");
		}

		[TestMethod]
		[DataRow("9:59", 1)]
		[DataRow("10:30", 3)]
		[DataRow("10:31", 3)]
		[DataRow("10:34", 3)]
		[DataRow("10:44", 4)]
		[DataRow("12:00", 5)]
		public void GoTo_TimeInRecords_ReturnsMatch(string searchValue, int expectedLineNumber)
		{
			var records = R.Create()
				.WithCreatedAt(1, "10:00:00")
				.WithCreatedAt(2, "10:15:00")
				.WithCreatedAt(3, "10:30:00")
				.WithCreatedAt(4, "10:45:00")
				.WithCreatedAt(5, "11:00:00")
				.GetRecords();

			var navigator = new RecordNavigator(records);

			Assert.AreEqual(
				expectedLineNumber,
				new TimestampNavigator(navigator).Find(searchValue).LineNumber);
		}


		/* RECORDS
		 * 
		 * SCENARIOS
		 *
		 * Selected=10:30
		 * GoTo=12:00
		 * Result=11:00
		 *
		 * Selected=10:30
		 * GoTo=9:00
		 * Result=10:00
		 *
		 * RULES
		 * start with next record
		 * for each record
		 *		if timestamp == GoToValue
		 *			match found
		 *
		 *		
		 *	endfor
		 */

		// What is considered a match?
		// - Pinned = true/false
		// - Text = contains value true/false
		// - Time = 
		// 
		// Time & LineNumber may need different algorithm (than Pinned & 
	}
}
