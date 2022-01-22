namespace BlueDotBrigade.Weevil.Navigation
{
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.TestingTools.Data;
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
	}
}
