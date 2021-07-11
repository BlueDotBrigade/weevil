namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Collections.Generic;
	using BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class LineNumberNavigatorTests
	{
		[TestMethod]
		public void GoTo_NoRecords_ReturnsEmptyRecord()
		{
			var records = new List<IRecord>();

			Assert.AreEqual(Record.Dummy, new LineNumberNavigator(records.ToImmutableArray()).GoTo(9));
		}

		[TestMethod]
		public void GoTo_SpecificRecordInCollection_ReturnsRequestedRecord()
		{
			var records = new List<IRecord>();
			for (var lineNumber = 1; lineNumber < 10; lineNumber++)
			{
				records.Add(
					new Record(
						lineNumber,
						DateTime.Now,
						SeverityType.Debug,
						"Sample log entry."));
			}

			Assert.AreEqual(9, new LineNumberNavigator(records.ToImmutableArray()).GoTo(9).LineNumber);
		}
	}
}