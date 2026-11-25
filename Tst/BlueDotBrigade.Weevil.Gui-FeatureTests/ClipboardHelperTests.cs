namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Immutable;
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
		public void GivenACallStack_WhenFormattedForCopyWithTheSimpleCallStack_ThenTheFrameworksFramesAreRemoved()
		{
			RecordList records = ImmutableArray.Create<IRecord>(new Record(
				7,
				DateTime.Now,
				SeverityType.Error,
				"Failed\r\n   at System.IO.File.Open()\r\n   at App.Program.Main()",
				isMultiLine: true,
				wasGeneratedByUi: false));

			var text = ClipboardHelper.FormatRaw(records, addLineNumberPrefix: false, new SimpleCallStackFormatter());

			text.Should().Be("Failed\r\n   at App.Program.Main()");
		}
	}
}
