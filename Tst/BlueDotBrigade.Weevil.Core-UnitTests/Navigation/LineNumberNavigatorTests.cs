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
		public void GoTo_EmptyRecordCollection_ThrowsRecordNotFound()
		{
			var emptyRecordCollection = new List<IRecord>();

			Action act = () =>
			{
				_ = new LineNumberNavigator(new ActiveRecord(emptyRecordCollection))
					.Find(8)
					.LineNumber;
			};
			act.Should().Throw<RecordNotFoundException>();
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
				new LineNumberNavigator(new ActiveRecord(records)).Find(8).LineNumber);
		}

		[TestMethod]
		public void GoTo_MissingLineNumber_ThrowsRecordNotFound()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(7),
				//R.WithLineNumber(8),
				R.WithLineNumber(9),
			};

			Action act = () =>
			{
				_ = new LineNumberNavigator(new ActiveRecord(records))
					.Find(8)
					.LineNumber;
			};
			act.Should().Throw<RecordNotFoundException>();
		}

		[TestMethod]
		public void GoTo_ClosestLineNumber_Returns10()
		{
			var records = new List<IRecord>
			{
				R.WithLineNumber(10),
				R.WithLineNumber(20),
			};

			Assert.AreEqual(10, new LineNumberNavigator(new ActiveRecord(records))
				.Find(12, RecordSearchType.NearestNeighbor)
				.LineNumber);
		}
	}
}