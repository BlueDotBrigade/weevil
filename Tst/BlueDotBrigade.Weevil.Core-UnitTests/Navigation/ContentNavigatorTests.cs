namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ContentNavigatorTests
	{
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

			Assert.AreEqual(2, result.LineNumber);
		}

		[TestMethod]
		[ExpectedException(typeof(RecordNotFoundException))]
		public void FindNext_CaseSensitive_Throws()
		{
			var records = R.Create()
				.WithContent("The quick brown fox")
				.WithContent("jumps OVER")
				.WithContent("the lazy dog")
				.GetRecords();

			var activeRecord = new ActiveRecord(records);
			var result = new ContentNavigator(activeRecord).FindNext("over", isCaseSensitive: true);

			Assert.Fail("Should have failed because: `OVER` is uppercase.");
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

			Assert.AreEqual(2, result.LineNumber);
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

			Assert.AreEqual(2, result.LineNumber);
		}

		[TestMethod]
		[ExpectedException(typeof(RecordNotFoundException))]
		public void FindNext_RegexCaseSensitive_Throws()
		{
			var records = R.Create()
				.WithContent("The quick brown fox")
				.WithContent("jumps OVER")
				.WithContent("the lazy dog")
				.GetRecords();

			var activeRecord = new ActiveRecord(records);
			var result = new ContentNavigator(activeRecord).FindNext("over", isCaseSensitive: true, useRegex: true);

			Assert.Fail("Should have failed because: `OVER` is uppercase.");
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

			Assert.AreEqual(2, result.LineNumber);
		}
	}
}
