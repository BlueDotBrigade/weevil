namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
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
			RecordList records = R.Create()
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
			RecordList records = R.Create()
				.WithCreatedAt(1, "2024-01-01 11:00:00") // day 1
				.WithCreatedAt(2, "2024-01-02 11:00:00") // day 2
				.GetRecords();

			var activeRecord = new ActiveRecord(records);
			activeRecord.SetActiveIndex(1); // select the Jan 2 record

			// Navigate using time-only (no date) - should use the active record's date (Jan 2)
			IRecord result = new TimestampNavigator(activeRecord).Find("11:00:00");

			result.LineNumber.Should().Be(2);
		}

		[TestMethod]
		[WorkItem(1225)]
		[DataRow("th-TH")]
		[DataRow("fa-IR")]
		[DataRow("ar-SA")]
		public void GivenANonGregorianCalendar_WhenGoingToADateAndTime_ThenTheRecordAtThatTimeIsFound(string cultureName)
		{
			// The text was read with the regional calendar, such as the Thai Buddhist one, while records' timestamps are Gregorian.
			RecordList records = R.Create()
				.WithCreatedAt(1, "2024-01-01 10:00:00")
				.WithCreatedAt(2, "2024-01-01 10:00:01")
				.WithCreatedAt(3, "2024-01-01 10:00:02")
				.WithCreatedAt(4, "2024-01-01 10:00:03")
				.WithCreatedAt(5, "2024-01-01 10:00:04")
				.GetRecords();
			CultureInfo original = CultureInfo.CurrentCulture;

			try
			{
				CultureInfo.CurrentCulture = new CultureInfo(cultureName);

				IRecord result = new TimestampNavigator(new ActiveRecord(records)).Find("2024-01-01 10:00:03");

				result.LineNumber.Should().Be(4);
			}
			finally
			{
				CultureInfo.CurrentCulture = original;
			}
		}

		[TestMethod]
		[WorkItem(1235)]
		public void GivenADateWrittenWithDots_WhenGoingToIt_ThenTheRecordOnThatDayIsFound()
		{
			// A date written with dots was taken for a time alone, on the selected record's day.
			RecordList records = R.Create()
				.WithCreatedAt(1, "2024-01-01 10:00:00")
				.WithCreatedAt(2, "2024-01-02 10:00:00")
				.GetRecords();
			CultureInfo original = CultureInfo.CurrentCulture;

			try
			{
				CultureInfo.CurrentCulture = new CultureInfo("de-DE");

				IRecord result = new TimestampNavigator(new ActiveRecord(records)).Find("02.01.2024 10:00:00");

				result.LineNumber.Should().Be(2);
			}
			finally
			{
				CultureInfo.CurrentCulture = original;
			}
		}

		[TestMethod]
		[WorkItem(1235)]
		public void GivenATimeWithFractionsOfASecond_WhenGoingToIt_ThenTheSelectedRecordsDayIsUsed()
		{
			// The dot in a time's fractions of a second must not be read as a date.
			RecordList records = R.Create()
				.WithCreatedAt(1, "2024-01-01 11:00:00")
				.WithCreatedAt(2, "2024-01-02 11:00:00")
				.GetRecords();
			var activeRecord = new ActiveRecord(records);
			activeRecord.SetActiveIndex(1);

			IRecord result = new TimestampNavigator(activeRecord).Find("11:00:00.000");

			result.LineNumber.Should().Be(2);
		}

		[TestMethod]
		[WorkItem(1225)]
		public void GivenTextThatIsNotADate_WhenGoingToIt_ThenNoRecordIsFound()
		{
			// Text that could not be read went to the first record, as if it were a time before every record.
			RecordList records = R.Create()
				.WithCreatedAt(1, "2024-01-01 10:00:00")
				.WithCreatedAt(2, "2024-01-01 10:00:01")
				.GetRecords();

			Action act = () => new TimestampNavigator(new ActiveRecord(records)).Find("2024-13-45 10:00");

			act.Should().Throw<RecordNotFoundException>();
		}
	}
}
