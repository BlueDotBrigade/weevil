namespace BlueDotBrigade.Weevil.Filter.Expressions.Monikers
{
	using Data;

	[TestClass]
	public class ContentLengthExpressionTests
	{
		[TestMethod]
		public void GivenContentAsLongAsTheThreshold_WhenTested_ThenItMatches()
		{
			var expression = new ContentLengthExpression("@ContentLength=5");

			expression.IsMatch(R.WithContent(1, "abcde"), new ContentBuffer()).Should().BeTrue();
		}

		[TestMethod]
		public void GivenContentShorterThanTheThreshold_WhenTested_ThenItDoesNotMatch()
		{
			var expression = new ContentLengthExpression("@ContentLength=5");

			expression.IsMatch(R.WithContent(1, "abcd"), new ContentBuffer()).Should().BeFalse();
		}

		[TestMethod]
		public void GivenContentOfOnlyWhitespace_WhenTested_ThenItDoesNotMatch()
		{
			var expression = new ContentLengthExpression("@ContentLength=0");

			expression.IsMatch(R.WithContent(1, " \t "), new ContentBuffer()).Should().BeFalse();
		}
	}
}
