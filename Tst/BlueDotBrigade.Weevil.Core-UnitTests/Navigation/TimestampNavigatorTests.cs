namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Collections.Generic;
	using BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TimestampNavigatorTests
	{
		// ReSharper disable once InconsistentNaming
		private static ImmutableArray<IRecord> Get9to10()
		{
			var records = new List<IRecord>();

			var hour9 = new DateTime(2000, 1, 1, 9, 0, 0);

			for (var minutes = 0; minutes <= 60; minutes++)
			{
				records.Add(
					new Record(
						minutes,
						hour9.AddMinutes(minutes),
						SeverityType.Debug,
						"Sample log entry."));
			}

			return records.ToImmutableArray();
		}

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
		public void GoTo_TimestampPastEndOfFile_Throws()
		{
			var records = Get9to10();

			// Value is beyond the last timestamp in the log file.
			var requestedTime = "11:00:00";


			// Navigator: active record is right
			// ... GoTo return value is wrong

			Assert.AreEqual(
				Record.Dummy,
				new TimestampNavigator(new RecordNavigator(records)).Find(requestedTime));
		}

		[TestMethod]
		[DataRow("9:08")]
		[DataRow("9:08:00")]
		public void GoTo_TimeInRecords_ReturnsFirstMatch(string requestedTime)
		{
			var records = Get9to10();

			Assert.AreEqual(
				records[8],
				new TimestampNavigator(new RecordNavigator(records)).Find(requestedTime));
		}

		[TestMethod]
		[DataRow("2000/01/01 9:05:00")]
		public void GoTo_RequestedDateTimeInRecords_ReturnsFirstMatch(string requestedTime)
		{
			var records = Get9to10();

			Assert.AreEqual(
				records[5],
				new TimestampNavigator(new RecordNavigator(records)).Find(requestedTime));
		}

		[TestMethod]
		[DataRow(3, "10:30", 3)]
		[DataRow(3, "10:35", 4)]
		[DataRow(3, "10:35", 4)]
		[DataRow(3, "12:00", 5)]
		public void GoTo_TimeInRecords_ReturnsMatch(int startingAt, string searchValue, int expectedLineNumber)
		{
			var records = R.Create()
				.WithCreatedAt(1, "10:00:00")
				.WithCreatedAt(2, "10:15:00")
				.WithCreatedAt(3, "10:30:00")
				.WithCreatedAt(4, "10:45:00")
				.WithCreatedAt(5, "11:00:00")
				.GetRecords();

			var navigator = new RecordNavigator(records);
			navigator.SetActiveLineNumber(startingAt);

			Assert.AreEqual(
				expectedLineNumber,
				new TimestampNavigator(navigator).Find(searchValue).LineNumber);
		}

		//[TestMethod]
		//[DataRow(3, "10:30", 3)]
		//[ExpectedException(typeof(RecordNotFoundException))]
		//public void GoTo_TimeInRecords_ThrowsRecordNotFound(int startingAt, string searchValue, int expectedLineNumber)
		//{
		//	var records = R.Create()
		//		.WithCreatedAt(1, "10:00:00")
		//		.WithCreatedAt(2, "10:15:00")
		//		.WithCreatedAt(3, "10:30:00")
		//		.WithCreatedAt(4, "10:45:00")
		//		.WithCreatedAt(5, "11:00:00")
		//		.GetRecords();

		//	var navigator = new RecordNavigator(records);
		//	navigator.SetActiveLineNumber(startingAt);

		//	Assert.AreEqual(
		//		expectedLineNumber,
		//		new TimestampNavigator(navigator).Find(searchValue).LineNumber);
		//}

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
