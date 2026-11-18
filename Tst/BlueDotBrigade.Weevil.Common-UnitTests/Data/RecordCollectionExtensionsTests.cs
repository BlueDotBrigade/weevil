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
		private RecordList GetRecords(IList<DateTime> timestamps)
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
			RecordList records = RecordList.Empty;

			(DateTime From, DateTime To) range = records.GetEstimatedRange();

			range.From.Should().Be(Record.CreationTimeUnknown);
			range.To.Should().Be(Record.CreationTimeUnknown);
		}

		[TestMethod]
		public void GetEstimatedRange_OneRecord_ReturnsSameDateTime()
		{
			DateTime now = DateTime.Now;

			RecordList records = GetRecords(new List<DateTime> { now });

			(DateTime From, DateTime To) range = records.GetEstimatedRange();

			range.From.Should().Be(now);
			range.To.Should().Be(now);
		}

		[TestMethod]
		public void GetEstimatedRange_MissingFirstTimestamp_ReturnsNextTimestamp()
		{
			RecordList records = GetRecords(new List<DateTime>
			{
				Record.CreationTimeUnknown,
				new DateTime(2021, 04, 11),
				new DateTime(2021, 04, 12),
			});

			(DateTime From, DateTime To) range = records.GetEstimatedRange();

			range.From.Should().Be(new DateTime(2021, 04, 11));
		}

		[TestMethod]
		public void GetEstimatedRange_MissingLastTimestamp_ReturnsSecondLastTimestamp()
		{
			RecordList records = GetRecords(new List<DateTime>
			{
				new DateTime(2021, 04, 11),
				new DateTime(2021, 04, 12),
				Record.CreationTimeUnknown,
			});

			(DateTime From, DateTime To) range = records.GetEstimatedRange();

			range.To.Should().Be(new DateTime(2021, 04, 12));
		}

		[TestMethod]
		public void GoToNext_UnknownStartingPosition_ReturnsRecord10()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10), // 0
				R.WithLineNumber(20), // 1 
				R.WithLineNumber(30), // 2
			};

			const int UnknownStartingPosition = -1;

			var indexOfResult = ((RecordList)records.ToImmutableArray())
				.GoToNext(UnknownStartingPosition, record => record.LineNumber > 0);

			records[indexOfResult].LineNumber.Should().Be(10);
		}

		[TestMethod]
		public void GoToNext_StartingAtFirst_ReturnsRecord20()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10), // 0
				R.WithLineNumber(20), // 1 
				R.WithLineNumber(30), // 2
			};

			const int IndexOfFirstRecord = 0;

			var indexOfResult = ((RecordList)records.ToImmutableArray())
				.GoToNext(IndexOfFirstRecord, record => record.LineNumber > 0);

			records[indexOfResult].LineNumber.Should().Be(20);
		}

		[TestMethod]
		public void GoToPrevious_UnknownStartingPosition_ReturnsRecord30()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10), // 0
				R.WithLineNumber(20), // 1 
				R.WithLineNumber(30), // 2
			};

			const int UnknownStartingPosition = -1;

			var indexOfResult = ((RecordList)records.ToImmutableArray())
				.GoToPrevious(UnknownStartingPosition, record => record.LineNumber > 0);

			records[indexOfResult].LineNumber.Should().Be(30);
		}

		[TestMethod]
		public void GoToPrevious_StartingAtLast_ReturnsRecord30()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10), // 0
				R.WithLineNumber(20), // 1 
				R.WithLineNumber(30), // 2
			};

			const int IndexOfLastRecord = 2;

			var indexOfResult = ((RecordList)records.ToImmutableArray())
				.GoToPrevious(IndexOfLastRecord, record => record.LineNumber > 0);

			records[indexOfResult].LineNumber.Should().Be(20);
		}

		[TestMethod]
		public void GivenAListThatCannotCreateRecords_WhenSearchedByLineNumber_ThenItFindsThePositionsAroundTheStart()
		{
			// Navigation tests only line numbers, so it creates no record (#1140).
			var records = new ColumnsOnlyRecordList(R.WithLineNumbers(11, 15));

			records.GoToNextLineNumber(1, lineNumber => lineNumber % 2 == 1).Should().Be(2);
			records.GoToPreviousLineNumber(1, lineNumber => lineNumber % 2 == 1).Should().Be(0);
			records.GoToNextLineNumber(4, lineNumber => lineNumber == 12).Should().Be(1);
		}
	}
}
