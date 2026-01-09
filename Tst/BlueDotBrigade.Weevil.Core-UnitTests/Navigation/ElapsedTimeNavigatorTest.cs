namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ElapsedTimeNavigatorTest
	{
		[TestMethod]
		public void FindNext_NoMatchingRecords_Throws()
		{
			var records = new List<IRecord>();
			for (var lineNumber = 50; lineNumber < 60; lineNumber++)
			{
				records.Add(
					new Record(
						lineNumber,
						DateTime.Now,
						SeverityType.Debug,
						"Sample log entry."));
			}

			// All records have elapsed time of 0 (no elapsed time set)
			// Should throw RecordNotFoundException
			Action act = () => new ElapsedTimeNavigator(new ActiveRecord(records)).FindNext(100, 200);
			act.Should().Throw<RecordNotFoundException>();
		}

		[TestMethod]
		public void FindNext_RecordWithinRange_FindsRecord()
		{
			var records = new List<IRecord>();
			var baseTime = DateTime.Parse("2025-01-01 10:00:00");
			
			for (var i = 0; i < 10; i++)
			{
				var record = new Record(
					50 + i,
					baseTime.AddSeconds(i),
					SeverityType.Debug,
					"Sample log entry.");
				
				if (i > 0)
				{
					// Simulate elapsed times: 1000ms, 2000ms, 3000ms, etc.
					record.Metadata.ElapsedTime = TimeSpan.FromMilliseconds(i * 1000);
				}
				
				records.Add(record);
			}

			// Find records with elapsed time between 2000ms and 4000ms
			// Should find the first matching record with 2000ms elapsed time (lineNumber 52)
			var result = new ElapsedTimeNavigator(new ActiveRecord(records)).FindNext(2000, 4000);
			
			Assert.AreEqual(52, result.LineNumber);
		}

		[TestMethod]
		public void FindNext_OnlyMinimum_FindsRecordsAboveMinimum()
		{
			var records = new List<IRecord>();
			var baseTime = DateTime.Parse("2025-01-01 10:00:00");
			
			for (var i = 0; i < 5; i++)
			{
				var record = new Record(
					50 + i,
					baseTime.AddSeconds(i),
					SeverityType.Debug,
					"Sample log entry.");
				
				if (i > 0)
				{
					record.Metadata.ElapsedTime = TimeSpan.FromMilliseconds(i * 100);
				}
				
				records.Add(record);
			}

			// Find records with elapsed time >= 200ms (no maximum)
			// Should find the first record with 200ms elapsed time (lineNumber 52)
			var result = new ElapsedTimeNavigator(new ActiveRecord(records)).FindNext(200, null);
			
			Assert.AreEqual(52, result.LineNumber);
		}

		[TestMethod]
		public void FindNext_OnlyMaximum_FindsRecordsBelowMaximum()
		{
			var records = new List<IRecord>();
			var baseTime = DateTime.Parse("2025-01-01 10:00:00");
			
			for (var i = 0; i < 5; i++)
			{
				var record = new Record(
					50 + i,
					baseTime.AddSeconds(i),
					SeverityType.Debug,
					"Sample log entry.");
				
				if (i > 0)
				{
					record.Metadata.ElapsedTime = TimeSpan.FromMilliseconds(i * 100);
				}
				
				records.Add(record);
			}

			// Find records with elapsed time <= 200ms (no minimum)
			// Should find the first record with 100ms elapsed time (lineNumber 51)
			var result = new ElapsedTimeNavigator(new ActiveRecord(records)).FindNext(null, 200);
			
			Assert.AreEqual(51, result.LineNumber);
		}

		[TestMethod]
		public void FindPrevious_RecordWithinRange_FindsRecordInReverseOrder()
		{
			var records = new List<IRecord>();
			var baseTime = DateTime.Parse("2025-01-01 10:00:00");
			
			for (var i = 0; i < 10; i++)
			{
				var record = new Record(
					50 + i,
					baseTime.AddSeconds(i),
					SeverityType.Debug,
					"Sample log entry.");
				
				if (i > 0)
				{
					record.Metadata.ElapsedTime = TimeSpan.FromMilliseconds(i * 1000);
				}
				
				records.Add(record);
			}

			var activeRecord = new ActiveRecord(records);
			// Set active record to the end
			activeRecord.SetActiveIndex(records.Count - 1);
			
			// Find records with elapsed time between 2000ms and 4000ms, going backwards
			// Should find the first matching record going backwards with 4000ms elapsed time (lineNumber 54)
			var result = new ElapsedTimeNavigator(activeRecord).FindPrevious(2000, 4000);
			
			Assert.AreEqual(54, result.LineNumber);
		}

		[TestMethod]
		public void FindNext_MultipleRecordsInRange_NavigatesInAscendingOrder()
		{
			var records = new List<IRecord>();
			var baseTime = DateTime.Parse("2025-01-01 10:00:00");
			
			for (var i = 0; i < 10; i++)
			{
				var record = new Record(
					50 + i,
					baseTime.AddSeconds(i),
					SeverityType.Debug,
					"Sample log entry.");
				
				if (i > 0)
				{
					// Create elapsed times: 100, 200, 300, 400, 500, etc.
					record.Metadata.ElapsedTime = TimeSpan.FromMilliseconds(i * 100);
				}
				
				records.Add(record);
			}

			var navigator = new ElapsedTimeNavigator(new ActiveRecord(records));

			// Find records with elapsed time between 200ms and 500ms
			// Should find records at line 52, 53, 54, 55 in order
			Assert.AreEqual(52, navigator.FindNext(200, 500).LineNumber);
			Assert.AreEqual(53, navigator.FindNext(200, 500).LineNumber);
			Assert.AreEqual(54, navigator.FindNext(200, 500).LineNumber);
			Assert.AreEqual(55, navigator.FindNext(200, 500).LineNumber);
		}
	}
}
