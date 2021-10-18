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
				new TimestampNavigator(new ActiveRecord(records)).Find(timestamp));
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

			var activeRecord = new ActiveRecord(records);
			var result = new TimestampNavigator(activeRecord).Find("10:30:00");

			Assert.Fail("Because only a time was provided, and no date, an exception should be thrown.");
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
