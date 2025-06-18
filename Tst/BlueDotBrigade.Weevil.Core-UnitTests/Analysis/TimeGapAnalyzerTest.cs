namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class TimeGapAnalyzerTest
	{
		private ImmutableArray<IRecord> _records;

		private IUserDialog GetUserDialog(int msBeforeFlaggedRaised)
		{
			var userDialog = Substitute.For<IUserDialog>();

			// Only a plugin knows what to ask the user.  Furthermore, the unit test has no idea about the implementation details
			// ... E.g. How many parameters are needed? What types of parameters is the plugin expecting?
			// TODO: re-write the `IUserDialog` interface so that the unit test doesn't care about the implementation details
			userDialog
				.ShowUserPrompt(Arg.Any<string>(),Arg.Any<string>(),Arg.Any<string>())
				.Returns(msBeforeFlaggedRaised.ToString());

			return userDialog;
		}

		[TestInitialize]
		public void PreTest()
		{
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			var records = new List<IRecord>
			{
				new Record(0, now.AddMinutes(0), severity, "content", new Metadata { WasGeneratedByUi = true }), // IsFlagged=false; no record to compare against
				new Record(1, Record.CreationTimeUnknown, severity, "application initializing", new Metadata { WasGeneratedByUi = true }), // IsFlagged=false; `MaxValue` represents an unknown timestamp
				new Record(2, now.AddMinutes(2), severity, "content", new Metadata { WasGeneratedByUi = true }), // IsFlagged=false; no timestamps for comparison
				new Record(3, now.AddMinutes(3), severity, "content", new Metadata { WasGeneratedByUi = true }), // IsFlagged=true; 
				new Record(4, now.AddMinutes(3), severity, "content", new Metadata { WasGeneratedByUi = true }), // IsFlagged=false; not enough time since last record
				new Record(5, now.AddMinutes(5), severity, "content", new Metadata { WasGeneratedByUi = false }), // IsFlagged=false; record not from UI thread
				new Record(6, now.AddMinutes(6), severity, "content", new Metadata { WasGeneratedByUi = true }), // IsFlagged=true; lots of time between two UI records
				new Record(7, Record.CreationTimeUnknown, severity, "application terminating", new Metadata { WasGeneratedByUi = true }), // IsFlagged=false; `MaxValue` represents an unknown timestamp
				new Record(8, now.AddMinutes(8), severity, "content", new Metadata { WasGeneratedByUi = true }), // IsFlagged=false; nothing to reference
			};

			_records = ImmutableArray.Create(records.ToArray());
		}

		[TestCleanup]
		public void PostTest()
		{
			foreach (IRecord record in _records)
			{
				Console.WriteLine(record.ToString());
			}

			_records.Clear();
		}

		[TestMethod]
		public void Analyze_CheckAllRecordsForGaps_FlaggedRecord20()
		{
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			var records = new List<IRecord>
			{
				new Record(10, now.AddSeconds(0), severity, "content", new Metadata { WasGeneratedByUi = true }),
				new Record(20, now.AddSeconds(7), severity, "content", new Metadata { WasGeneratedByUi = false }),
				new Record(30, now.AddSeconds(8), severity, "content", new Metadata { WasGeneratedByUi = true }),
			};

			var analyzer = new TimeGapAnalyzer();

			analyzer.Analyze(
				records.ToImmutableArray(),
				EnvironmentHelper.GetExecutableDirectory(),
				GetUserDialog(3000),
				canUpdateMetadata: true);

			Assert.IsFalse(records[0].Metadata.IsFlagged);
			Assert.IsTrue(records[1].Metadata.IsFlagged);
			Assert.IsFalse(records[2].Metadata.IsFlagged);
		}

		[TestMethod]
		[WorkItem(135)]
		public void Analyze_RecordMissingTimestamp_MissingTimestampShouldBeIgnored()
		{
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			var records = new List<IRecord>
			{
				new Record(10, now.AddSeconds(0), severity, "Index=0"),
				new Record(20, now.AddSeconds(1), severity, "Index=1"),
				new Record(30, Record.CreationTimeUnknown, severity, "Index=2"),
				new Record(40, now.AddSeconds(3), severity, "Index=3"),
				new Record(50, now.AddSeconds(4), severity, "Index=4"),
			};

			var analyzer = new TimeGapAnalyzer();

			analyzer.Analyze(
				records.ToImmutableArray(),
				EnvironmentHelper.GetExecutableDirectory(),
				GetUserDialog(500),
				canUpdateMetadata: true);

			Assert.IsFalse(records[2].Metadata.IsFlagged);
			Assert.IsTrue(records[3].Metadata.IsFlagged);
			Assert.IsTrue(records[4].Metadata.IsFlagged);
		}

		[TestMethod]
		public void Count_CheckAllRecordsForGaps_ResultMatchesCount()
		{
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			var records = new List<IRecord>
			{
				new Record(10, now.AddSeconds(0), severity, "content", new Metadata { WasGeneratedByUi = true }),
				new Record(20, now.AddSeconds(7), severity, "content", new Metadata { WasGeneratedByUi = false }),
				new Record(30, now.AddSeconds(8), severity, "content", new Metadata { WasGeneratedByUi = true }),
			};

			var analyzer = new TimeGapAnalyzer();

			var results = analyzer.Analyze(
				records.ToImmutableArray(),
				EnvironmentHelper.GetExecutableDirectory(),
				GetUserDialog(3000),
				canUpdateMetadata: true);

			Assert.AreEqual(1, results.FlaggedRecords);
			Assert.AreEqual(1, analyzer.Count);
		}

		[TestMethod]
		public void Analyze_RecordWithoutTimestamp_MissingTimestampIsIgnored()
		{
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			var records = new List<IRecord>
			{
				new Record(0, Record.CreationTimeUnknown, severity, "fake header", new Metadata { WasGeneratedByUi = false }),
				new Record(1, now.AddSeconds(4), severity, "content", new Metadata { WasGeneratedByUi = false }),
				new Record(2, now.AddSeconds(8), severity, "content", new Metadata { WasGeneratedByUi = false }),
			};

			var analyzer = new TimeGapAnalyzer();

			analyzer.Analyze(
				records.ToImmutableArray(),
				EnvironmentHelper.GetExecutableDirectory(),
				GetUserDialog(2000),
				canUpdateMetadata: true);

			Assert.IsFalse(records[0].Metadata.IsFlagged);
			Assert.IsFalse(records[1].Metadata.IsFlagged);
			Assert.IsTrue(records[2].Metadata.IsFlagged);
		}
	}
}
