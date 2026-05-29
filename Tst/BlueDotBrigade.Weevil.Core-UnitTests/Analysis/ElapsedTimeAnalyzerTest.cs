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
			Action analyzeAction = () => new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			analyzeAction.Should().NotThrow("Analyzer should not throw an exception.");
		}

		[TestMethod]
		public void HasElapsedTime_1RecordWithTimestamp_ReturnsFalse()
		{
			var records = new List<IRecord>()
				{
					 new Record(1, DateTime.Now, SeverityType.Debug, string.Empty),
				};

			new ElapsedTimeAnalyzer(records.ToImmutableArray()).Analyze();

			(records[0].Metadata.HasElapsedTime).Should().BeFalse();
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

			(records[0].Metadata.HasElapsedTime).Should().BeFalse();
			(records[1].Metadata.HasElapsedTime).Should().BeFalse();
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

			(records[0].Metadata.HasElapsedTime).Should().BeFalse();
			(records[1].Metadata.HasElapsedTime).Should().BeFalse();
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

			(records[0].Metadata.HasElapsedTime).Should().BeFalse();
			(records[1].Metadata.HasElapsedTime).Should().BeTrue();
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

			(records[0].Metadata.ElapsedTime).Should().Be(Metadata.ElapsedTimeUnknown);
			(records[1].Metadata.ElapsedTime).Should().Be(Metadata.ElapsedTimeUnknown);
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

			(records[0].Metadata.ElapsedTime).Should().Be(Metadata.ElapsedTimeUnknown);
			(records[1].Metadata.ElapsedTime).Should().Be(Metadata.ElapsedTimeUnknown);
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

			(records[0].Metadata.ElapsedTime).Should().Be(Metadata.ElapsedTimeUnknown);
			(records[1].Metadata.ElapsedTime).Should().Be(TimeSpan.FromMilliseconds(200));
		}
	}
}
