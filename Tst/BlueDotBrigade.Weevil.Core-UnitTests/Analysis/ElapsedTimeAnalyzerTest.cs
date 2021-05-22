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
		public void Analyze_0Records_DoesNotThrow()
		{
			var records = new List<IRecord>();

			try
			{
				new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();
			}
			catch (Exception)
			{
				Assert.Fail("Analyzer should not throw an exception.");
			}
		}

		[TestMethod]
		public void HasElapsedTime_1RecordWithTimestamp_ReturnsFalse()
		{
			var records = new List<IRecord>()
				{
					 new Record(1, DateTime.Now, SeverityType.Debug, string.Empty),
				};

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			Assert.IsFalse(records[0].Metadata.HasElapsedTime);
		}

		[TestMethod]
		public void HasElapsedTime_FirstWithTimestamp_BothReturnFalse()
		{
			var records = new List<IRecord>()
			{
				new Record(1, DateTime.Now, SeverityType.Debug, string.Empty),
				new Record(2, Record.CreationTimeUnknown, SeverityType.Debug, string.Empty),
			};

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			Assert.IsFalse(records[0].Metadata.HasElapsedTime);
			Assert.IsFalse(records[1].Metadata.HasElapsedTime);
		}

		[TestMethod]
		public void HasElapsedTime_LastWithTimestamp_BothReturnFalse()
		{
			var records = new List<IRecord>()
			{
				new Record(1, Record.CreationTimeUnknown, SeverityType.Debug, string.Empty),
				new Record(2, DateTime.Now, SeverityType.Debug, string.Empty),
			};

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			Assert.IsFalse(records[0].Metadata.HasElapsedTime);
			Assert.IsFalse(records[1].Metadata.HasElapsedTime);
		}

		[TestMethod]
		public void HasElapsedTime_BothWithTimestamp_LastReturnsTrue()
		{
			var records = new List<IRecord>()
			{
				new Record(1, DateTime.Now.AddSeconds(1), SeverityType.Debug, string.Empty),
				new Record(2, DateTime.Now.AddSeconds(2), SeverityType.Debug, string.Empty),
			};

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			Assert.IsFalse(records[0].Metadata.HasElapsedTime);
			Assert.IsTrue(records[1].Metadata.HasElapsedTime);
		}

		[TestMethod]
		public void ElapsedTime_FirstWithTimestamp_BothElapsedTimeUnknown()
		{
			var records = new List<IRecord>()
			{
				new Record(1, DateTime.Now, SeverityType.Debug, string.Empty),
				new Record(2, Record.CreationTimeUnknown, SeverityType.Debug, string.Empty),
			};

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			Assert.AreEqual(Metadata.ElapsedTimeUnknown, records[0].Metadata.ElapsedTime);
			Assert.AreEqual(Metadata.ElapsedTimeUnknown, records[1].Metadata.ElapsedTime);
		}

		[TestMethod]
		public void ElapsedTime_LastWithTimestamp_BothElapsedTimeUnknown()
		{
			var records = new List<IRecord>()
			{
				new Record(1, Record.CreationTimeUnknown, SeverityType.Debug, string.Empty),
				new Record(2, DateTime.Now, SeverityType.Debug, string.Empty),
			};

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			Assert.AreEqual(Metadata.ElapsedTimeUnknown, records[0].Metadata.ElapsedTime);
			Assert.AreEqual(Metadata.ElapsedTimeUnknown, records[1].Metadata.ElapsedTime);
		}

		[TestMethod]
		public void ElapsedTime_BothWithTimestamps_CalculateCorrectElapsedTime()
		{
			DateTime now = DateTime.Now;

			var records = new List<IRecord>()
			{
				new Record(1, now.AddMilliseconds(100), SeverityType.Debug, string.Empty),
				new Record(2, now.AddMilliseconds(300), SeverityType.Debug, string.Empty),
			};

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			Assert.AreEqual(Metadata.ElapsedTimeUnknown, records[0].Metadata.ElapsedTime);
			Assert.AreEqual(TimeSpan.FromMilliseconds(200), records[1].Metadata.ElapsedTime);
		}
	}
}
