namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ContentConverterTests
	{
		[TestMethod]
		public void TruncateIf_ContentIsNotTooLong_ReturnsContent()
		{
			var result = ContentConverter.TruncateIf(
				content: "The quick brown fox jumps over the lazy dog.",
				isSingleLine: true,
				maximumLength: 45,
				truncatedLength: 19);

			Assert.AreEqual(
				"The quick brown fox jumps over the lazy dog.",
				result);
		}

		[TestMethod]
		public void TruncateIf_ContentIsTooLong_ReturnsShortenedString()
		{
			var result = ContentConverter.TruncateIf(
				content: "The quick brown fox jumps over the lazy dog.",
				isSingleLine:true,
				maximumLength: 44,
				truncatedLength:19);

			Assert.AreEqual(
				"The quick brown fox" + ContentConverter.EndOfLine,
				result);
		}

		[TestMethod]
		public void TruncateIf_MultiLineContentIsTooLong_ReturnsContent()
		{
			var result = ContentConverter.TruncateIf(
				content: "The quick brown fox jumps over the lazy dog.",
				isSingleLine: false,
				maximumLength: 44,
				truncatedLength: 19);

			Assert.AreEqual(
				"The quick brown fox jumps over the lazy dog.",
				result);
		}
	}
}
