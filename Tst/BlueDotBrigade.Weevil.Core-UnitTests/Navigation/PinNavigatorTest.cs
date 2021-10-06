namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class PinNavigatorTest
	{
		[TestMethod]
		[ExpectedException(typeof(RecordNotFoundException))]
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

			Assert.AreEqual(
				Record.Dummy, 
				new PinNavigator(new RecordNavigator(records)).FindNext());
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
			records[8].Metadata.IsPinned = true; // Line: 58 is pinned
			records[2].Metadata.IsPinned = true; // Line: 52 is pinned

			Assert.AreEqual(
				52, 
				new PinNavigator(new RecordNavigator(records)).FindNext().LineNumber);
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

			records[2].Metadata.IsPinned = true;
			records[8].Metadata.IsPinned = true;

			var navigator = new PinNavigator(new RecordNavigator(records));

			Assert.AreEqual(52, navigator.FindNext().LineNumber);
			Assert.AreEqual(58, navigator.FindNext().LineNumber);
			Assert.AreEqual(52, navigator.FindNext().LineNumber);
		}
	}
}
