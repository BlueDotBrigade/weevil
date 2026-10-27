namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class PinNavigatorTest
	{
		private readonly RecordState _state = new RecordState();

		[TestMethod]
		public void GoToNext_NoPinnedRecords_Throws()
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

			Action act = () => new PinNavigator(new ActiveRecord(records), _state).FindNext();
			act.Should().Throw<RecordNotFoundException>();
		}

		[TestMethod]
		public void GoToNext_TwoPinnedRecords_NavigatesToLowestPinnedRecord()
		{
			var records = new List<IRecord>();
			for (var lineNumber = 50; lineNumber <= 60; lineNumber++)
			{
				records.Add(
					new Record(
						lineNumber,
						DateTime.Now,
						SeverityType.Debug,
						"Sample log entry."));
			}

			// Note: the order of pinning should be irrelevant
			_state.SetPinned(records[8], true); // Line: 58 is pinned
			_state.SetPinned(records[2], true); // Line: 52 is pinned

			(new PinNavigator(new ActiveRecord(records), _state).FindNext().LineNumber).Should().Be(52);
		}

		[TestMethod]
		public void GoToNext_SeveralPinnedRecords_NavigatesInAscendingOrder()
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

			_state.SetPinned(records[2], true);
			_state.SetPinned(records[8], true);

			var navigator = new PinNavigator(new ActiveRecord(records), _state);

			(navigator.FindNext().LineNumber).Should().Be(52);
			(navigator.FindNext().LineNumber).Should().Be(58);
			(navigator.FindNext().LineNumber).Should().Be(52);
		}
	}
}
