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
		public void GoTo_NoRecords_ReturnsEmptyRecord()
		{
			var records = new List<IRecord>();

			var timestamp = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");

			Assert.AreEqual(
				Record.Dummy,
				new TimestampNavigator(records.ToImmutableArray()).GoTo(timestamp));
		}

		[TestMethod]
		public void GoTo_TimestampPastEndOfFile_ReturnsEmptyRecord()
		{
			var records = Get9to10();

			// Value is beyond the last timestamp in the log file.
			var requestedTime = "11:00:00";


			// Navigator: active record is right
			// ... GoTo return value is wrong

			Assert.AreEqual(
				Record.Dummy,
				new TimestampNavigator(records.ToImmutableArray()).GoTo(requestedTime));
		}

		[TestMethod]
		[DataRow("9:08")]
		[DataRow("9:08:00")]
		public void GoTo_TimeInRecords_ReturnsFirstMatch(string requestedTime)
		{
			var records = Get9to10();

			Assert.AreEqual(
				records[8],
				new TimestampNavigator(records.ToImmutableArray()).GoTo(requestedTime));
		}

		[TestMethod]
		[DataRow("2000/01/01 9:05:00")]
		public void GoTo_RequestedDateTimeInRecords_ReturnsFirstMatch(string requestedTime)
		{
			var records = Get9to10();

			Assert.AreEqual(
				records[5],
				new TimestampNavigator(records.ToImmutableArray()).GoTo(requestedTime));
		}
	}
}
