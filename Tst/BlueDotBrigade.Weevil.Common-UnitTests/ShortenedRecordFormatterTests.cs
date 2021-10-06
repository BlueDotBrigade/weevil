namespace BlueDotBrigade.Weevil.Common
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ShortenedRecordFormatterTests
	{
		[TestMethod]
		public void Format_ContentIsNotTooLong_ReturnsContent()
		{
			var result =
				new ShortenedRecordFormatter(maximumLength: 45, truncatedLength: 19).Format(
					"The quick brown fox jumps over the lazy dog.");

			Assert.AreEqual(
				(object)"The quick brown fox jumps over the lazy dog.",
				result);
		}

		[TestMethod]
		public void Format_ContentIsTooLong_ReturnsShortenedString()
		{
			var result = new ShortenedRecordFormatter(maximumLength: 44, truncatedLength: 19)
				.Format("The quick brown fox jumps over the lazy dog.");

			Assert.AreEqual(
				(object)("The quick brown fox" + ShortenedRecordFormatter.EndOfLine),
				result);
		}
	}
}
