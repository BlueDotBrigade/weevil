namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Text;
	using System.Threading;
	using BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ContentNavigatorTests
	{
		[TestMethod]
		public void GivenASearchAbortedWhileItReadsRecords_WhenItGoesOn_ThenItStopsAtTheNextRecordAndKeepsTheActiveRecord()
		{
			// Abort cancels the search, which stops within one record rather than reading the rest of a log on disk (#1027).
			RecordList records = R.Create()
				.WithContent("First")
				.WithContent("Second")
				.WithContent("Third")
				.WithContent("The text searched for")
				.GetRecords();
			using var abort = new CancellationTokenSource();
			var list = new AbortingRecordList(records, abort, abortAtRead: 1);
			var activeRecord = new ActiveRecord(list);
			activeRecord.SetActiveIndex(0);

			Action search = () => new ContentNavigator(activeRecord).FindNext("searched", isCaseSensitive: true, useRegex: false, abort.Token);

			search.Should().Throw<OperationCanceledException>();
			list.Reads.Should().Be(1, "the search stops at the record after the abort");
			activeRecord.Index.Should().Be(0);
		}

		[TestMethod]
		public void FindNext_CaseIgnored_RecordFound()
		{
			var records = R.Create()
				.WithContent("The quick brown fox")
				.WithContent("jumps OVER")
				.WithContent("the lazy dog")
				.GetRecords();

			var activeRecord = new ActiveRecord(records);
			var result = new ContentNavigator(activeRecord).FindNext("over", isCaseSensitive: false);

			result.LineNumber.Should().Be(2);
		}

		[TestMethod]
		public void FindNext_CaseSensitive_Throws()
		{
			var records = R.Create()
				.WithContent("The quick brown fox")
				.WithContent("jumps OVER")
				.WithContent("the lazy dog")
				.GetRecords();

			var activeRecord = new ActiveRecord(records);
			Action act1 = () => new ContentNavigator(activeRecord).FindNext("over", isCaseSensitive: true);
			act1.Should().Throw<RecordNotFoundException>();
		}

		[TestMethod]
		public void FindNext_RegexPattern_RecordFound()
		{
			var records = R.Create()
				.WithContent("The quick brown fox")
				.WithContent("jumps over 123")
				.WithContent("the lazy dog")
				.GetRecords();

			var activeRecord = new ActiveRecord(records);
			var result = new ContentNavigator(activeRecord).FindNext(@"\d+", isCaseSensitive: false, useRegex: true);

			result.LineNumber.Should().Be(2);
		}

		[TestMethod]
		public void FindNext_RegexCaseInsensitive_RecordFound()
		{
			var records = R.Create()
				.WithContent("The quick brown fox")
				.WithContent("jumps OVER")
				.WithContent("the lazy dog")
				.GetRecords();

			var activeRecord = new ActiveRecord(records);
			var result = new ContentNavigator(activeRecord).FindNext("over", isCaseSensitive: false, useRegex: true);

			result.LineNumber.Should().Be(2);
		}

		[TestMethod]
		public void FindNext_RegexCaseSensitive_Throws()
		{
			var records = R.Create()
				.WithContent("The quick brown fox")
				.WithContent("jumps OVER")
				.WithContent("the lazy dog")
				.GetRecords();

			var activeRecord = new ActiveRecord(records);
			Action act2 = () => new ContentNavigator(activeRecord).FindNext("over", isCaseSensitive: true, useRegex: true);
			act2.Should().Throw<RecordNotFoundException>();
		}

		[TestMethod]
		public void FindPrevious_RegexPattern_RecordFound()
		{
			var records = R.Create()
				.WithContent("The quick brown fox")
				.WithContent("jumps over 456")
				.WithContent("the lazy dog 789")
				.GetRecords();

			var activeRecord = new ActiveRecord(records);
			activeRecord.SetActiveIndex(2); // Start from the last record
			var result = new ContentNavigator(activeRecord).FindPrevious(@"\d+", isCaseSensitive: false, useRegex: true);

			result.LineNumber.Should().Be(2);
		}

		[TestMethod]
		[DataRow("one$", false, true, 1, DisplayName = "Bytes")]
		[DataRow("start\r\nBeta", true, false, 2, DisplayName = "Multi-line bytes, joined with CRLF")]
		[DataRow(@"^Beta start\r\nBeta end$", true, true, 2, DisplayName = "Multi-line bytes, whole content")]
		[DataRow("GAMMA", false, false, 4, DisplayName = "String")]
		[DataRow("Delta � bad", true, false, 5, DisplayName = "Invalid byte")]
		public void GivenARecordTable_WhenTextIsFound_ThenItIsTheRecordFoundInAnArrayOfTheTableRecords(
			string value, bool isCaseSensitive, bool useRegex, int expectedLineNumber)
		{
			var table = new TableRecordList(CreateTable());
			RecordList array = table.ToImmutableArray();

			foreach (RecordList records in new[] { table, array })
			{
				new ContentNavigator(new ActiveRecord(records)).FindNext(value, isCaseSensitive, useRegex)
					.LineNumber.Should().Be(expectedLineNumber);
				new ContentNavigator(new ActiveRecord(records)).FindPrevious(value, isCaseSensitive, useRegex)
					.LineNumber.Should().Be(expectedLineNumber);
			}
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void GivenAListThatCannotCreateRecords_WhenTextIsNotFound_ThenEveryRowWasSearchedWithoutCreatingOne(bool useRegex)
		{
			// Find reads each row's content by position, so it creates no record while it searches (#1150).
			RecordList records = new ColumnsOnlyRecordList(R.WithLineNumbers(1, 10));

			FluentActions.Invoking(() => new ContentNavigator(new ActiveRecord(records)).FindNext("NoSuchText", true, useRegex))
				.Should().Throw<RecordNotFoundException>();
			FluentActions.Invoking(() => new ContentNavigator(new ActiveRecord(records)).FindPrevious("NoSuchText", true, useRegex))
				.Should().Throw<RecordNotFoundException>();
		}

		/// <summary>
		/// Lines 1 to 5: bytes, multi-line bytes with a lone line feed, a string, and bytes with an invalid byte.
		/// </summary>
		private static RecordTable CreateTable()
		{
			byte[] single = Encoding.UTF8.GetBytes("Alpha one");
			byte[] multiLine = Encoding.UTF8.GetBytes("Beta start\nBeta end");
			byte[] invalid = [.. Encoding.UTF8.GetBytes("Delta "), 0xFF, .. Encoding.UTF8.GetBytes(" bad")];
			var chunk = new ContentChunk(0, [.. single, .. multiLine, .. invalid], Encoding.UTF8);

			var facts = new RecordFacts(SeverityType.Information, isMultiLine: false, wasGeneratedByUi: false);
			var multiLineFacts = new RecordFacts(SeverityType.Information, isMultiLine: true, wasGeneratedByUi: false);
			var builder = new RecordTable.Builder();

			builder.Add(1, Record.CreationTimeUnknown, facts, chunk, 0, single.Length);
			builder.Add(2, Record.CreationTimeUnknown, multiLineFacts, chunk, single.Length, multiLine.Length);
			builder.Add(4, Record.CreationTimeUnknown, facts, "Gamma string");
			builder.Add(5, Record.CreationTimeUnknown, facts, chunk, single.Length + multiLine.Length, invalid.Length);

			return builder.Build();
		}

		/// <summary>
		/// Reads another list, and aborts a search when it reads the content of its Nth record.
		/// </summary>
		private sealed class AbortingRecordList : RecordList
		{
			private readonly RecordList _records;
			private readonly CancellationTokenSource _abort;
			private readonly int _abortAtRead;

			public AbortingRecordList(RecordList records, CancellationTokenSource abort, int abortAtRead)
			{
				_records = records;
				_abort = abort;
				_abortAtRead = abortAtRead;
			}

			public int Reads { get; private set; }

			public override int Count => _records.Count;

			public override IRecord this[int position] => _records[position];

			public override int GetLineNumber(int position) => _records.GetLineNumber(position);

			public override DateTime GetCreatedAt(int position) => _records.GetCreatedAt(position);

			public override RecordFacts GetFacts(int position) => _records.GetFacts(position);

			public override ReadOnlySpan<char> ReadContent(int position, ContentBuffer content)
			{
				this.Reads++;

				if (this.Reads == _abortAtRead)
				{
					_abort.Cancel();
				}

				return _records.ReadContent(position, content);
			}
		}
	}
}
