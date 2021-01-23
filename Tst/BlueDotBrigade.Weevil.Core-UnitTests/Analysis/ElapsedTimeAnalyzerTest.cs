namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using Data;

	[TestClass]
	public class ElapsedTimeAnalyzerTest
	{
		[TestMethod]
		public void Analyze_RecordCount0_DoesNotThrow()
		{
			var records = new List<IRecord>();

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();
		}

		[TestMethod]
		public void Analyze_RecordCount1_ElapsedTimeNotCalculated()
		{
			var records = new List<IRecord>()
				{
					 new Record(1, DateTime.Now, SeverityType.Debug, string.Empty),
				};

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			Assert.AreEqual(Metadata.ElapsedTimeUnknown, records[0].Metadata.ElapsedTime);
		}

		[TestMethod]
		public void Analyze_RecordCount2_OnlyLastHasElapsedTime()
		{
			DateTime now = DateTime.Now;

			var records = new List<IRecord>()
				{
					 new Record(1, now.AddMilliseconds(100), SeverityType.Debug, string.Empty),
					 new Record(2, now.AddMilliseconds(300), SeverityType.Debug, string.Empty),
				};

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			Assert.AreEqual(Metadata.ElapsedTimeUnknown, records[0].Metadata.ElapsedTime);
			Assert.IsTrue(records[1].Metadata.ElapsedTime > TimeSpan.Zero);
		}

		[TestMethod]
		public void Analyze_TwoValidTimestamps_CalculatedCorrectElapsedTime()
		{
			DateTime now = DateTime.Now;

			var records = new List<IRecord>()
				{
					 new Record(1, now.AddMilliseconds(100), SeverityType.Debug, string.Empty),
					 new Record(2, now.AddMilliseconds(300), SeverityType.Debug, string.Empty),
				};

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			Assert.AreEqual(TimeSpan.FromMilliseconds(200), records[1].Metadata.ElapsedTime);
		}

		[TestMethod]
		public void Analyze_Record1MissingTimestamp_NoElapsedTimeCalculated()
		{
			DateTime now = DateTime.Now;

			var records = new List<IRecord>()
				{
					 new Record(1, Record.CreationTimeUnknown, SeverityType.Debug, string.Empty),
					 new Record(2, now.AddMilliseconds(300), SeverityType.Debug, string.Empty),
				};

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			Assert.AreEqual(Metadata.ElapsedTimeUnknown, records[0].Metadata.ElapsedTime);
			Assert.AreEqual(Metadata.ElapsedTimeUnknown, records[1].Metadata.ElapsedTime);
		}

		[TestMethod]
		public void Analyze_Record2MissingTimestamp_NoElapsedTimeCalculated()
		{
			DateTime now = DateTime.Now;

			var records = new List<IRecord>()
				{
					 new Record(1, now.AddMilliseconds(300), SeverityType.Debug, string.Empty),
					 new Record(2, Record.CreationTimeUnknown, SeverityType.Debug, string.Empty),
				};

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			Assert.AreEqual(Metadata.ElapsedTimeUnknown, records[0].Metadata.ElapsedTime);
			Assert.AreEqual(Metadata.ElapsedTimeUnknown, records[1].Metadata.ElapsedTime);
		}
	}
}
