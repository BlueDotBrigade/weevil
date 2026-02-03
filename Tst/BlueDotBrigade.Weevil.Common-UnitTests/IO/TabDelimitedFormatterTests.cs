namespace BlueDotBrigade.Weevil.IO
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TabDelimitedFormatterTests
	{
		[TestMethod]
		public void AsText_ShouldReturnUnmodifiedMessage()
		{
			// Arrange
			var formatter = new TabDelimitedFormatter();
			var message = "Test message";

			// Act
			var result = formatter.AsText(message);

			// Assert
			Assert.AreEqual(message, result);
		}

		[TestMethod]
		public void AsHeading_ShouldReturnUnmodifiedMessage()
		{
			// Arrange
			var formatter = new TabDelimitedFormatter();
			var message = "Test heading";

			// Act
			var result = formatter.AsHeading(message);

			// Assert
			Assert.AreEqual(message, result);
		}

		[TestMethod]
		public void AsBullet_ShouldReturnUnmodifiedMessage()
		{
			// Arrange
			var formatter = new TabDelimitedFormatter();
			var message = "Test bullet";

			// Act
			var result = formatter.AsBullet(message);

			// Assert
			Assert.AreEqual(message, result);
		}

		[TestMethod]
		public void AsNumbered_ShouldPrependNumberAndTab()
		{
			// Arrange
			var formatter = new TabDelimitedFormatter();
			var message = "Test item";

			// Act
			var result1 = formatter.AsNumbered(message);
			var result2 = formatter.AsNumbered(message);

			// Assert
			Assert.AreEqual("1.\tTest item", result1);
			Assert.AreEqual("2.\tTest item", result2);
		}

		[TestMethod]
		public void AsError_ShouldPrependErrorAndTab()
		{
			// Arrange
			var formatter = new TabDelimitedFormatter();
			var message = "Error occurred";

			// Act
			var result = formatter.AsError(message);

			// Assert
			Assert.AreEqual("ERROR:\tError occurred", result);
		}

		[TestMethod]
		public void ResetNumbering_ShouldResetCounter()
		{
			// Arrange
			var formatter = new TabDelimitedFormatter();
			var message = "Test item";

			// Act
			formatter.AsNumbered(message);
			formatter.AsNumbered(message);
			formatter.ResetNumbering();
			var result = formatter.AsNumbered(message);

			// Assert
			Assert.AreEqual("1.\tTest item", result);
		}
	}
}
