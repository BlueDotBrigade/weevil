namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using BlueDotBrigade.Weevil.IO;
	using Directory = System.IO.Directory;
	using File = System.IO.File;

	[TestClass]
	public class RecordListTests
	{
		[TestMethod]
		public void GivenAnArrayOfRecords_WhenWrapped_ThenTheListReadsThemInOrder()
		{
			RecordList records = R.WithLineNumbers(11, 13);

			RecordList list = records;

			list.Count.Should().Be(3);
			list[1].LineNumber.Should().Be(12);
			list.Select(record => record.LineNumber).Should().Equal(11, 12, 13);
			list.ToImmutableArray().Should().Equal(records);
		}

		[TestMethod]
		public void GivenARecordList_WhenEnumerated_ThenItReadsEveryRecordInOrder()
		{
			RecordList list = R.WithLineNumbers(11, 13);

			var lineNumbers = new List<int>();
			foreach (IRecord record in list)
			{
				lineNumbers.Add(record.LineNumber);
			}

			lineNumbers.Should().Equal(11, 12, 13);
		}

		[TestMethod]
		public void GivenADefaultArray_WhenConverted_ThenTheListIsEmpty()
		{
			RecordList list = default(ImmutableArray<IRecord>);

			list.IsEmpty.Should().BeTrue();
			list.Should().BeSameAs(RecordList.Empty);
		}

		[TestMethod]
		public void GivenAnotherRecordForALineInTheList_WhenLookedFor_ThenItIsFoundByItsLineNumber()
		{
			RecordList list = R.WithLineNumbers(11, 13);

			list.IndexOf(R.WithLineNumber(12)).Should().Be(1);
			list.Contains(R.WithLineNumber(13)).Should().BeTrue();
			list.IndexOf(R.WithLineNumber(14)).Should().Be(-1);
			list.IndexOf(null).Should().Be(-1);
		}

		[TestMethod]
		public void GivenAnArrayTooSmall_WhenRecordsAreCopiedToIt_ThenNothingIsWritten()
		{
			RecordList list = R.WithLineNumbers(11, 13);
			var array = new IRecord[2];

			FluentActions.Invoking(() => list.CopyTo(array, 0)).Should().Throw<ArgumentException>();
			array.Should().OnlyContain(record => record == null);
		}

		[TestMethod]
		public void GivenARecordList_WhenChanged_ThenTheChangeIsRefused()
		{
			IList<IRecord> list = (RecordList)R.WithLineNumbers(11, 13);

			FluentActions.Invoking(() => list.Add(R.WithLineNumber(14))).Should().Throw<NotSupportedException>();
			FluentActions.Invoking(() => list[0] = R.WithLineNumber(14)).Should().Throw<NotSupportedException>();
			list.IsReadOnly.Should().BeTrue();
		}

		[TestMethod]
		public void GivenRecordsOfALogOnDisk_WhenCopiedWithTextAndTheLogIsDeleted_ThenTheCopiesAreTheSameRecordsWithTheirText()
		{
			// The graph keeps its copies after its log is gone, so a copy never reads the log (#1028).
			var folder = Directory.CreateTempSubdirectory("Weevil.RecordListTests.").FullName;

			try
			{
				var logPath = Path.Combine(folder, "Log.txt");
				var bytes = Encoding.UTF8.GetBytes("First\nSecond\nand its second line\n");
				File.WriteAllBytes(logPath, bytes);
				var builder = new LogFileSourceBuilder(logPath, 0, bytes.Length);
				builder.Append(bytes);
				var chunk = new ContentChunk(builder.Build(), 0, bytes.Length, Encoding.UTF8);
				var createdAt = new DateTime(2026, 11, 25, 3, 0, 0);
				RecordList list = ImmutableArray.Create<IRecord>(
					new Record(3, createdAt, SeverityType.Information, chunk, 0, 5, isMultiLine: false, wasGeneratedByUi: false),
					new Record(5, createdAt.AddSeconds(1), SeverityType.Warning, chunk, 6, 26, isMultiLine: true, wasGeneratedByUi: false));

				RecordList copies = list.CopyWithText();
				File.Delete(logPath);

				copies.Select(copy => copy.Content).Should().Equal("First", "Second\r\nand its second line");
				copies.Select(copy => copy.LineNumber).Should().Equal(3, 5);
				copies.Select(copy => copy.CreatedAt).Should().Equal(createdAt, createdAt.AddSeconds(1));
				copies.Select(copy => copy.Severity).Should().Equal(SeverityType.Information, SeverityType.Warning);
				copies.Select(copy => copy.IsMultiLine).Should().Equal(false, true);
			}
			finally
			{
				Directory.Delete(folder, recursive: true);
			}
		}

		[TestMethod]
		public void GivenACancelledToken_WhenCopiedWithText_ThenTheCopyStops()
		{
			RecordList list = R.WithLineNumbers(11, 13);

			FluentActions.Invoking(() => list.CopyWithText(new CancellationToken(canceled: true))).Should().Throw<OperationCanceledException>();
		}
	}
}
