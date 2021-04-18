namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.IO;
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Moq;

	[TestClass]
	public class DetectUnresponsiveUiAnalyzerTest
	{
		private ImmutableArray<IRecord> _records;

		private IUserDialog GetUserDialog(int msBeforeFlaggedRaised)
		{
			var userDialog = new Mock<IUserDialog>();

			// Only the plugin knows what to ask the user. That said, the unit test has know idea about the implementation details
			// ... Were 2 parameters passed in? Were 3?
			// TODO: re-write the `IUserDialog` interface so that the unit test doesn't care about the implementation details
			userDialog.Setup(x => x.ShowUserPrompt(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<string>())).Returns(msBeforeFlaggedRaised.ToString);

			return userDialog.Object;
		}

		[TestInitialize]
		public void PreTest()
		{
			DateTime now = DateTime.Now;
			SeverityType severity = SeverityType.Debug;

			var records = new List<IRecord>
			{
				new Record(0, now.AddMinutes(0), severity, "content", new Metadata { WasGeneratedByUi = true }), // IsFlagged=false; no record to compare against
				new Record(1, DateTime.MaxValue, severity, "application initializing", new Metadata { WasGeneratedByUi = true }), // IsFlagged=false; `MaxValue` represents an unknown timestamp
				new Record(2, now.AddMinutes(2), severity, "content", new Metadata { WasGeneratedByUi = true }), // IsFlagged=false; no timestamps for comparison
				new Record(3, now.AddMinutes(3), severity, "content", new Metadata { WasGeneratedByUi = true }), // IsFlagged=true; 
				new Record(4, now.AddMinutes(3), severity, "content", new Metadata { WasGeneratedByUi = true }), // IsFlagged=false; not enough time since last record
				new Record(5, now.AddMinutes(5), severity, "content", new Metadata { WasGeneratedByUi = false }), // IsFlagged=false; record not from UI thread
				new Record(6, now.AddMinutes(6), severity, "content", new Metadata { WasGeneratedByUi = true }), // IsFlagged=true; lots of time between two UI records
				new Record(7, DateTime.MaxValue, severity, "application terminating", new Metadata { WasGeneratedByUi = true }), // IsFlagged=false; `MaxValue` represents an unknown timestamp
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
		public void Analyze_NoPrecedingRecord_RecordNotFlagged()
		{
			var analyzer = new DetectUnresponsiveUiAnalyzer();

			analyzer.Analyze(_records, EnvironmentHelper.GetExecutableDirectory(), GetUserDialog(1000), false);

			Assert.IsFalse(_records[0].Metadata.IsFlagged);
		}

		[TestMethod]
		public void Analyze_ApplicationInitializing_RecordNotFlagged()
		{
			var analyzer = new DetectUnresponsiveUiAnalyzer();

			analyzer.Analyze(_records, EnvironmentHelper.GetExecutableDirectory(), GetUserDialog(1000), false);

			Assert.IsFalse(_records[1].Metadata.IsFlagged);
		}

		[TestMethod]
		public void Analyze_PrecedingRecordWasInitialing_RecordNotFlagged()
		{
			var analyzer = new DetectUnresponsiveUiAnalyzer();

			analyzer.Analyze(_records, EnvironmentHelper.GetExecutableDirectory(), GetUserDialog(1000), false);

			Assert.IsFalse(_records[2].Metadata.IsFlagged);
		}

		[TestMethod]
		public void Analyze_LongPeriodBetweenUiRecords_RecordFlagged()
		{
			var analyzer = new DetectUnresponsiveUiAnalyzer();

			analyzer.Analyze(_records, EnvironmentHelper.GetExecutableDirectory(), GetUserDialog(1000), true);

			Assert.IsTrue(_records[3].Metadata.IsFlagged); // here
		}

		[TestMethod]
		public void Analyze_ShortPeriodBetweenUiRecords_RecordNotFlagged()
		{
			var analyzer = new DetectUnresponsiveUiAnalyzer();

			analyzer.Analyze(_records, EnvironmentHelper.GetExecutableDirectory(), GetUserDialog(1000), false);

			Assert.IsFalse(_records[4].Metadata.IsFlagged);
		}

		[TestMethod]
		public void Analyze_RecordWasNotWrittenByUiThread_RecordNotFlagged()
		{
			var analyzer = new DetectUnresponsiveUiAnalyzer();

			analyzer.Analyze(_records, EnvironmentHelper.GetExecutableDirectory(), GetUserDialog(1000), false);

			Assert.IsFalse(_records[5].Metadata.IsFlagged);
		}

		[TestMethod]
		public void Analyze_MixedUiAndNotUiRecords_RecordFlagged()
		{
			var analyzer = new DetectUnresponsiveUiAnalyzer();

			analyzer.Analyze(_records, EnvironmentHelper.GetExecutableDirectory(), GetUserDialog(1000), true);

			Assert.IsTrue(_records[6].Metadata.IsFlagged);
		}

		[TestMethod]
		public void Analyze_ApplicationTerminatingMessage_RecordNotFlagged()
		{
			var analyzer = new DetectUnresponsiveUiAnalyzer();

			analyzer.Analyze(_records, EnvironmentHelper.GetExecutableDirectory(), GetUserDialog(1000), false);

			Assert.IsFalse(_records[7].Metadata.IsFlagged);
		}

		[TestMethod]
		public void Analyze_PrecedingRecordWasApplicationTerminating_RecordNotFlagged()
		{
			var analyzer = new DetectUnresponsiveUiAnalyzer();

			analyzer.Analyze(_records, EnvironmentHelper.GetExecutableDirectory(), GetUserDialog(1000), false);

			Assert.IsFalse(_records[8].Metadata.IsFlagged);
		}
	}
}
