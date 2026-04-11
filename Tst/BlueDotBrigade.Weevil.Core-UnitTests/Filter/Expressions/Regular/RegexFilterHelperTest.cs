namespace BlueDotBrigade.Weevil.Filter.Expressions.Regular
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class RegexFilterHelperTest
	{
		#region HasEscapedDoubleQuotes

		[TestMethod]
		public void GivenNullExpression_WhenHasEscapedDoubleQuotes_ThenReturnsFalse()
		{
			Assert.IsFalse(RegexFilterHelper.HasEscapedDoubleQuotes(null));
		}

		[TestMethod]
		public void GivenEmptyExpression_WhenHasEscapedDoubleQuotes_ThenReturnsFalse()
		{
			Assert.IsFalse(RegexFilterHelper.HasEscapedDoubleQuotes(string.Empty));
		}

		[TestMethod]
		public void GivenExpressionWithSingleDoubleQuotes_WhenHasEscapedDoubleQuotes_ThenReturnsFalse()
		{
			// e.g. the user typed: The dog says "woof" in the morning.
			Assert.IsFalse(RegexFilterHelper.HasEscapedDoubleQuotes("The dog says \"woof\" in the morning."));
		}

		[TestMethod]
		public void GivenExpressionWithEscapedDoubleQuotes_WhenHasEscapedDoubleQuotes_ThenReturnsTrue()
		{
			// e.g. the user pasted: The dog says ""woof"" in the morning.
			Assert.IsTrue(RegexFilterHelper.HasEscapedDoubleQuotes("The dog says \"\"woof\"\" in the morning."));
		}

		[TestMethod]
		public void GivenExpressionWithNoQuotes_WhenHasEscapedDoubleQuotes_ThenReturnsFalse()
		{
			Assert.IsFalse(RegexFilterHelper.HasEscapedDoubleQuotes("Voltage=51.9V"));
		}

		#endregion

		#region FixEscapedDoubleQuotes

		[TestMethod]
		public void GivenNullExpression_WhenFixEscapedDoubleQuotes_ThenReturnsNull()
		{
			Assert.IsNull(RegexFilterHelper.FixEscapedDoubleQuotes(null));
		}

		[TestMethod]
		public void GivenEmptyExpression_WhenFixEscapedDoubleQuotes_ThenReturnsEmpty()
		{
			Assert.AreEqual(string.Empty, RegexFilterHelper.FixEscapedDoubleQuotes(string.Empty));
		}

		[TestMethod]
		public void GivenExpressionWithEscapedDoubleQuotes_WhenFixEscapedDoubleQuotes_ThenQuotesAreFixed()
		{
			// Regression: Issue #424
			// e.g. the user pasted: The dog says ""woof"" in the morning.
			// After fix: The dog says "woof" in the morning.
			var input = "The dog says \"\"woof\"\" in the morning.";
			var expected = "The dog says \"woof\" in the morning.";

			Assert.AreEqual(expected, RegexFilterHelper.FixEscapedDoubleQuotes(input));
		}

		[TestMethod]
		public void GivenExpressionWithNoEscapedQuotes_WhenFixEscapedDoubleQuotes_ThenExpressionIsUnchanged()
		{
			var input = "The dog says \"woof\" in the morning.";

			Assert.AreEqual(input, RegexFilterHelper.FixEscapedDoubleQuotes(input));
		}

		#endregion
	}
}
