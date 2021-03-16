namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using BlueDotBrigade.DatenLokator.TestsTools.UnitTesting;
	using BlueDotBrigade.Weevil.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Moq;

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

		[TestMethod]
		public void TrySimplifyCallStack_SimpleCallStack_ReturnsContentWithoutSystemNamespaces()
		{
			var record = new Mock<IRecord>();
			record.Setup(x => x.Content).Returns(InputData.GetAsString());

			var wasSuccessful = ContentConverter.TrySimplifyCallstack(
				content: record.Object.Content,
				isMultiLine: true,
				out var actualResult);

			Assert.AreEqual(
				"Debug 2021-15-21 12:59:59 AcmeAssembly.dll Something bad happened. System.ObjectDisposedException: Cannot access a disposed object.\r\n" +
				"   at Company.Product.Component.DataCollector.Fetch()",
				actualResult);
		}

		[TestMethod]
		public void TrySimplifyCallStack_SimpleCallStack_ReturnsTrue()
		{
			var record = new Mock<IRecord>();
			record.Setup(x => x.Content).Returns(InputData.GetAsString());

			var wasContentChanged = ContentConverter.TrySimplifyCallstack(
				content: record.Object.Content,
				isMultiLine: true,
				out var actualResult);

			Assert.IsTrue(wasContentChanged);
		}

		[TestMethod]
		public void TrySimplifyCallStack_NoCallStack_ReturnsFalse()
		{
			var record = new Mock<IRecord>();
			record.Setup(x => x.Content).Returns("The quick brown fox jumps over the lazy dog.");

			var wasContentChanged = ContentConverter.TrySimplifyCallstack(
				content: record.Object.Content,
				isMultiLine: true,
				out var ignoredOutput);

			Assert.IsFalse(wasContentChanged);
		}
	}
}
