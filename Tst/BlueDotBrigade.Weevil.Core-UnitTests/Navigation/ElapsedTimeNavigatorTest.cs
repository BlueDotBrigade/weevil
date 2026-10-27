namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ElapsedTimeNavigatorTest
	{
		// The elapsed times of the records the view shows.
		private readonly RecordState _state = new RecordState();

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
			Action act = () => new ElapsedTimeNavigator(new ActiveRecord(records), _state).FindNext(100, 200);
			act.Should().Throw<RecordNotFoundException>();
		}

		[TestMethod]
		public void FindNext_RecordWithinRange_FindsRecord()
		{
			// Record i is i x 1000 ms after the one before.
			List<IRecord> records = CreateRecordsShownByView(10, stepMilliseconds: 1000);

			// Find records with elapsed time between 2000ms and 4000ms
			// Should find the first matching record with 2000ms elapsed time (lineNumber 52)
			var result = new ElapsedTimeNavigator(new ActiveRecord(records), _state).FindNext(2000, 4000);
			
			result.LineNumber.Should().Be(52);
		}

		[TestMethod]
		public void FindNext_OnlyMinimum_FindsRecordsAboveMinimum()
		{
			// Record i is i x 100 ms after the one before.
			List<IRecord> records = CreateRecordsShownByView(5, stepMilliseconds: 100);

			// Find records with elapsed time >= 200ms (no maximum)
			// Should find the first record with 200ms elapsed time (lineNumber 52)
			var result = new ElapsedTimeNavigator(new ActiveRecord(records), _state).FindNext(200, null);
			
			result.LineNumber.Should().Be(52);
		}

		[TestMethod]
		public void FindNext_OnlyMaximum_FindsRecordsBelowMaximum()
		{
			// Record i is i x 100 ms after the one before.
			List<IRecord> records = CreateRecordsShownByView(5, stepMilliseconds: 100);

			// Find records with elapsed time <= 200ms (no minimum)
			// Should find the first record with 100ms elapsed time (lineNumber 51)
			var result = new ElapsedTimeNavigator(new ActiveRecord(records), _state).FindNext(null, 200);
			
			result.LineNumber.Should().Be(51);
		}

		[TestMethod]
		public void FindPrevious_RecordWithinRange_FindsRecordInReverseOrder()
		{
			// Record i is i x 1000 ms after the one before.
			List<IRecord> records = CreateRecordsShownByView(10, stepMilliseconds: 1000);

			var activeRecord = new ActiveRecord(records);
			// Set active record to the end
			activeRecord.SetActiveIndex(records.Count - 1);
			
			// Find records with elapsed time between 2000ms and 4000ms, going backwards
			// Should find the first matching record going backwards with 4000ms elapsed time (lineNumber 54)
			var result = new ElapsedTimeNavigator(activeRecord, _state).FindPrevious(2000, 4000);
			
			result.LineNumber.Should().Be(54);
		}

		[TestMethod]
		public void FindNext_MultipleRecordsInRange_NavigatesInAscendingOrder()
		{
			// Record i is i x 100 ms after the one before.
			List<IRecord> records = CreateRecordsShownByView(10, stepMilliseconds: 100);

			var navigator = new ElapsedTimeNavigator(new ActiveRecord(records), _state);

			// Find records with elapsed time between 200ms and 500ms
			// Should find records at line 52, 53, 54, 55 in order
			(navigator.FindNext(200, 500).LineNumber).Should().Be(52);
			(navigator.FindNext(200, 500).LineNumber).Should().Be(53);
			(navigator.FindNext(200, 500).LineNumber).Should().Be(54);
			(navigator.FindNext(200, 500).LineNumber).Should().Be(55);
		}

		/// <summary>
		/// Creates records on lines 50, 51, and so on, each <c>index x step</c> after the one before, so record i has an
		/// elapsed time of <c>i x step</c>. A view shows them and calculates their elapsed times, as it does after a filter (#1016).
		/// </summary>
		private List<IRecord> CreateRecordsShownByView(int count, int stepMilliseconds)
		{
			var records = new List<IRecord>();
			DateTime createdAt = DateTime.Parse("2025-01-01 10:00:00");

			for (var i = 0; i < count; i++)
			{
				createdAt = createdAt.AddMilliseconds(i * stepMilliseconds);
				records.Add(new Record(50 + i, createdAt, SeverityType.Debug, "Sample log entry."));
			}

			_state.ElapsedTimes = ElapsedTimes.Calculate(records.ToImmutableArray());

			return records;
		}
	}
}
