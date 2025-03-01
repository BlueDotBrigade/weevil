namespace BlueDotBrigade.Weevil.Filter.Expressions.PlainText
{
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class PlainTextExpressionTest
	{
		[TestMethod]
		public void IsMatch_IsCaseSensitive_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new PlainTextExpression("FOX", false);

			Assert.IsTrue(expression.IsMatch(record));
		}

		[TestMethod]
		public void IsMatch_IsNotCaseSensitive_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new PlainTextExpression("FOX", true);

			Assert.IsFalse(expression.IsMatch(record));
		}

		[TestMethod]
		public void IsMatch_ExpressionNotInValue_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new PlainTextExpression("dinosaur", true);

			Assert.IsFalse(expression.IsMatch(record));
		}
	}
}
