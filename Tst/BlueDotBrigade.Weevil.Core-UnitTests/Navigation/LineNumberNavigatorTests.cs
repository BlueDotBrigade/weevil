namespace BlueDotBrigade.Weevil.Navigation
{
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class LineNumberNavigatorTests
	{
		[TestMethod]
		[ExpectedException(typeof(RecordNotFoundException))]
		public void GoTo_EmptyRecordCollection_ThrowsRecordNotFound()
		{
			var records = new List<IRecord>();

			_ = new LineNumberNavigator(new RecordNavigator(records))
				.Find(8)
				.LineNumber;

			Assert.Fail("Test shouldn't reach here.");
		}

		[TestMethod]
		public void GoTo_LineNumberInCollection_ReturnsRequestedRecord()
		{
			var records = new List<IRecord>
			{
				new Record(lineNumber: 7),
				new Record(lineNumber: 8),
				new Record(lineNumber: 9),
			};

			Assert.AreEqual(
				8,
				new LineNumberNavigator(new RecordNavigator(records)).Find(8).LineNumber);
		}

		[TestMethod]
		[ExpectedException(typeof(RecordNotFoundException))]
		public void GoTo_MissingLineNumber_ThrowsRecordNotFound()
		{
			var records = new List<IRecord>
			{
				new Record(lineNumber: 7),
				// new Record(lineNumber: 8),
				new Record(lineNumber: 9),
			};

			_ = new LineNumberNavigator(new RecordNavigator(records))
				.Find(8)
				.LineNumber;

			Assert.Fail("Test shouldn't reach here.");
		}
	}
}