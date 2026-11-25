namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Immutable;
	using System.Threading;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class ClipboardHelperTests
	{
		private static readonly RecordList Records = ImmutableArray.Create(R.WithContent(3, "First"), R.WithContent(5, "Second"));

		[TestMethod]
		public void GivenRecords_WhenFormattedForCopy_ThenEachIsOnALineOfItsOwn()
		{
			var text = ClipboardHelper.FormatRaw(Records, addLineNumberPrefix: false, new RawRecordFormatter());

			text.Should().Be("First" + Environment.NewLine + "Second");
		}

		[TestMethod]
		public void GivenTheLineNumberPrefix_WhenFormattedForCopy_ThenEachLineStartsWithItsLineNumber()
		{
			var text = ClipboardHelper.FormatRaw(Records, addLineNumberPrefix: true, new RawRecordFormatter());

			text.Should().Be("3 === First" + Environment.NewLine + "5 === Second");
		}

		[TestMethod]
		public void GivenTheLineNumberPrefixAndTheSimpleCallStack_WhenFormattedForCopy_ThenEachShortenedRecordStartsWithItsLineNumber()
		{
			RecordList records = ImmutableArray.Create<IRecord>(
				R.WithContent(3, "First"),
				new Record(5, DateTime.Now, SeverityType.Error, "Failed\r\n   at System.IO.File.Open()\r\n   at App.Program.Main()", isMultiLine: true, wasGeneratedByUi: false));

			var text = ClipboardHelper.FormatRaw(records, addLineNumberPrefix: true, new SimpleCallStackFormatter());

			text.Should().Be("3 === First" + Environment.NewLine + "5 === Failed\r\n   at App.Program.Main()");
		}

		[TestMethod]
		public void GivenCopyIsAborted_WhenTheFirstRecordIsBeingFormatted_ThenNoOtherRecordIsFormatted()
		{
			// Escape stopped the read of a log on disk, but not the formatting after it, which can take long with the simple call stack.
			using var abort = new CancellationTokenSource();
			var formatter = new AbortingFormatter(abort);

			Action formatting = () => ClipboardHelper.FormatRaw(Records, addLineNumberPrefix: false, formatter, abort.Token);

			formatting.Should().Throw<OperationCanceledException>();
			formatter.FormattedCount.Should().Be(1);
		}

		/// <summary>
		/// Aborts as it formats its first record, as Escape pressed during a copy does.
		/// </summary>
		private sealed class AbortingFormatter : IRecordFormatter
		{
			private readonly CancellationTokenSource _abort;

			public AbortingFormatter(CancellationTokenSource abort)
			{
				_abort = abort;
			}

			public int FormattedCount { get; private set; }

			public string Format(IRecord record)
			{
				this.FormattedCount++;
				_abort.Cancel();

				return record.Content;
			}
		}
	}
}
