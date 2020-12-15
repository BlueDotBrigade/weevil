namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class PinNavigatorTest
	{
		[TestMethod]
		public void GoToNextPin_NoPinnedRecords_ReturnsEmptyRecord()
		{
			var records = new List<IRecord>();
			for (var lineNumber = 50; lineNumber < 60; lineNumber++)
			{
				records.Add(
					new Record(
						lineNumber,
						DateTime.Now,
						SeverityType.Debug,
						string.Empty));
			}

			Assert.AreEqual(Record.Dummy, new PinNavigator(records.ToImmutableArray()).GoToNextPin());
		}

		[TestMethod]
		public void GoToNextPin_TwoPinnedRecords_NavigatesToLowestPinnedRecord()
		{
			var records = new List<IRecord>();
			for (var lineNumber = 50; lineNumber <= 60; lineNumber++)
			{
				records.Add(
					new Record(
						lineNumber,
						DateTime.Now,
						SeverityType.Debug,
						string.Empty));
			}

			// Note: the order of pinning should be irrelevant
			records[8].Metadata.IsPinned = true; // Line: 58 is pinned
			records[2].Metadata.IsPinned = true; // Line: 52 is pinned

			Assert.AreEqual(52, new PinNavigator(records.ToImmutableArray()).GoToNextPin().LineNumber);
		}

		[TestMethod]
		public void GoToNextPin_SeveralPinnedRecords_NavigatesInAscendingOrder()
		{
			var records = new List<IRecord>();
			for (var lineNumber = 50; lineNumber < 60; lineNumber++)
			{
				records.Add(
					new Record(
						lineNumber,
						DateTime.Now,
						SeverityType.Debug,
						string.Empty));
			}

			records[2].Metadata.IsPinned = true;
			records[8].Metadata.IsPinned = true;

			var navigator = new PinNavigator(records.ToImmutableArray());

			Assert.AreEqual(52, navigator.GoToNextPin().LineNumber);
			Assert.AreEqual(58, navigator.GoToNextPin().LineNumber);
			Assert.AreEqual(52, navigator.GoToNextPin().LineNumber);
		}
	}
}
