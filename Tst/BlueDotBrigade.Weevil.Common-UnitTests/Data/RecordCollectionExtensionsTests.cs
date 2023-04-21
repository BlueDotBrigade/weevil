namespace BlueDotBrigade.Weevil.Common.Data
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Navigation;
	using BlueDotBrigade.Weevil.TestingTools.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class RecordCollectionExtensionsTests
	{
		const int DefaultLevel = 1;
		const int DefaultOffset = 0;

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

			var records = GetRecords(new List<DateTime> { now });

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

			var indexOfResult = records
				.ToImmutableArray()
				.GoToNext(UnknownStartingPosition, record => record.LineNumber > 0);

			Assert.AreEqual(
				10,
				records[indexOfResult].LineNumber);
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

			var indexOfResult = records
				.ToImmutableArray()
				.GoToNext(IndexOfFirstRecord, record => record.LineNumber > 0);

			Assert.AreEqual(
				20,
				records[indexOfResult].LineNumber);
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

			var indexOfResult = records
				.ToImmutableArray()
				.GoToPrevious(UnknownStartingPosition, record => record.LineNumber > 0);

			Assert.AreEqual(
				30,
				records[indexOfResult].LineNumber);
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

			var indexOfResult = records
				.ToImmutableArray()
				.GoToPrevious(IndexOfLastRecord, record => record.LineNumber > 0);

			Assert.AreEqual(
				20,
				records[indexOfResult].LineNumber);
		}

		private ImmutableArray<IRecord> GetRecords(int startAt, int count)
		{
			var now = new DateTime(2000, 12, 30);

			IList<IRecord> records = new List<IRecord>();

			var timeOffset = 0;

			for (var i = startAt; i <= startAt + count; i++)
			{
				timeOffset++;
				records.Add(new Record(i, now.AddSeconds(i + timeOffset), SeverityType.Information, "This is a test."));
			}

			return records.ToImmutableArray();
		}

		[TestMethod]
		public void GetSectionRecords_AlignedLineNumbers_ReturnsRecordsInSection()
		{
			var sections = new List<Section>
			{
				new Section("Part1", DefaultLevel, DefaultOffset, 110),
				new Section("Part2", DefaultLevel, DefaultOffset, 120),
				new Section("Part3", DefaultLevel, DefaultOffset, 130),
			};

			var toc = new TableOfContents(sections);

			var records = GetRecords(100, 50);

			var recordsInSection = records
				.GetSectionRecords(sections.First(x => x.Name.Equals("Part2")), toc)
				.ToImmutableArray();

			Assert.AreEqual(10, recordsInSection.Length);

			Assert.AreEqual(120, recordsInSection[0].LineNumber);
			Assert.AreEqual(121, recordsInSection[1].LineNumber);
			// ...
			Assert.AreEqual(128, recordsInSection[8].LineNumber);
			Assert.AreEqual(129, recordsInSection[9].LineNumber);
		}

		[TestMethod]
		public void GetSectionRecords_MisalignedLineNumbers_ReturnsRecordsInSection()
		{
			var sections = new List<Section>
			{
				new Section("Part1", DefaultLevel, DefaultOffset, 110),
				new Section("Part2", DefaultLevel, DefaultOffset, 120),
				new Section("Part3", DefaultLevel, DefaultOffset, 130),
			};

			var toc = new TableOfContents(sections);

			var records = GetRecords(100, 50);
			var oddRecords = records.Where((record, index) => index % 2 == 1).ToImmutableArray();

			var recordsInSection = oddRecords
				.GetSectionRecords(sections.First(x => x.Name.Equals("Part2")), toc)
				.ToImmutableArray();

			Assert.AreEqual(5, recordsInSection.Length);

			Assert.AreEqual(121, recordsInSection[0].LineNumber);
			Assert.AreEqual(123, recordsInSection[1].LineNumber);			
			Assert.AreEqual(125, recordsInSection[2].LineNumber);
			Assert.AreEqual(127, recordsInSection[3].LineNumber);
			Assert.AreEqual(129, recordsInSection[4].LineNumber);
		}
	}
}
