namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
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
			var emptyRecordCollection = new List<IRecord>();

			_ = new LineNumberNavigator(new RecordNavigator(emptyRecordCollection))
				.Find(8)
				.LineNumber;

			Assert.Fail("Test shouldn't reach here.");
		}

		[TestMethod]
		public void GoTo_LineNumberInCollection_ReturnsRequestedRecord()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(7),
				R.WithLineNumber(8),
				R.WithLineNumber(9),
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
				R.WithLineNumber(7),
				//R.WithLineNumber(8),
				R.WithLineNumber(9),
			};

			_ = new LineNumberNavigator(new RecordNavigator(records))
				.Find(8)
				.LineNumber;

			Assert.Fail("Test shouldn't reach here.");
		}

		[TestMethod]
		public void GoTo_ClosestLineNumber_Returns10()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10),
				R.WithLineNumber(20),
			};

			Assert.AreEqual(10, new LineNumberNavigator(new RecordNavigator(records))
				.Find(12, SearchType.ClosestMatch)
				.LineNumber);
		}

		[TestMethod]
		[DataRow(0, 10)]
		[DataRow(12, 10)]
		[DataRow(20, 20)]
		[DataRow(28, 30)]
		[DataRow(40, 30)]
		public void GoTo_ClosestLineNumber_ReturnsClosestMatch(int requestedLineNumber, int expectedLineNumber)
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10),
				R.WithLineNumber(20),
				R.WithLineNumber(30),
			};

			var actualLineNumber = new LineNumberNavigator(new RecordNavigator(records))
				.Find(requestedLineNumber, SearchType.ClosestMatch)
				.LineNumber;

			Assert.AreEqual(
				expectedLineNumber,
				actualLineNumber,
				$"Requested={requestedLineNumber}, Expected={expectedLineNumber}, Actual={actualLineNumber}");
		}
	}
}