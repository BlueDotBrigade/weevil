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

			(expression.IsMatch(record)).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_IsNotCaseSensitive_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new PlainTextExpression("FOX", true);

			(expression.IsMatch(record)).Should().BeFalse();
		}

		[TestMethod]
		public void IsMatch_ExpressionNotInValue_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new PlainTextExpression("dinosaur", true);

			(expression.IsMatch(record)).Should().BeFalse();
		}
	}
}
