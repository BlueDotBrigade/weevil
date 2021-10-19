namespace BlueDotBrigade.Weevil.Common.Data
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class RecordCollectionExtensionsTests
	{
		private ImmutableArray<IRecord> GetRecords(IList<DateTime> timestamps)
		{
			var records = new List<IRecord>();

			var lineNumber = 1;

			foreach (DateTime timestamp in timestamps)
			{
				records.Add(new Record(
					lineNumber,
					timestamp,
					SeverityType.Information,
					"No content."));
			}

			return records.ToImmutableArray();
		}

		[TestMethod]
		public void GetEstimatedRange_NoRecords_ReturnsUnknownCreationTime()
		{
			var records = ImmutableArray<IRecord>.Empty;

			var range = records.GetEstimatedRange();

			Assert.AreEqual(Record.CreationTimeUnknown, range.From);
			Assert.AreEqual(Record.CreationTimeUnknown, range.To);
		}

		[TestMethod]
		public void GetEstimatedRange_OneRecord_ReturnsSameDateTime()
		{
			var now = DateTime.Now;

			var records = GetRecords(new List<DateTime> {now});

			var range = records.GetEstimatedRange();

			Assert.AreEqual(now, range.From);
			Assert.AreEqual(now, range.To);
		}

		[TestMethod]
		public void GetEstimatedRange_MissingFirstTimestamp_ReturnsNextTimestamp()
		{
			var records = GetRecords(new List<DateTime>
			{
				Record.CreationTimeUnknown, 
				new DateTime(2021, 04, 11),
				new DateTime(2021, 04, 12),
			});

			var range = records.GetEstimatedRange();

			Assert.AreEqual(new DateTime(2021, 04, 11), range.From);
		}

		[TestMethod]
		public void GetEstimatedRange_MissingLastTimestamp_ReturnsSecondLastTimestamp()
		{
			var records = GetRecords(new List<DateTime>
			{
				new DateTime(2021, 04, 11),
				new DateTime(2021, 04, 12),
				Record.CreationTimeUnknown,
			});

			var range = records.GetEstimatedRange();

			Assert.AreEqual(new DateTime(2021, 04, 12), range.To);
		}
	}
}
