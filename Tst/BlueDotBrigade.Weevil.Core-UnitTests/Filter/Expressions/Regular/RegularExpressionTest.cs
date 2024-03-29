﻿namespace BlueDotBrigade.Weevil.Filter.Expressions.Regular
{
	using Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Microsoft = System.Text.RegularExpressions;
	using NSubstitute;

	[TestClass]
	public class RegularExpressionTest
	{
		[TestMethod]
		public void IsMatch_CaseSensitiveSearch_Passes()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new RegularExpression("fox", Microsoft.RegexOptions.None); // default: case sensitive

			Assert.IsTrue(expression.IsMatch(record));
		}

		[TestMethod]
		public void IsMatch_CaseSensitiveSearch_Fails()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new RegularExpression("FOX", Microsoft.RegexOptions.None); // default: case sensitive

			Assert.IsFalse(expression.IsMatch(record));
		}

		[TestMethod]
		public void IsMatch_CaseInsensitiveSearch_Passes()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new RegularExpression("FoX", Microsoft.RegexOptions.IgnoreCase);

			Assert.IsTrue(expression.IsMatch(record));
		}

		[TestMethod]
		public void IsMatch_ExpressionNotInValue_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new RegularExpression("dinosaur");

			Assert.IsFalse(expression.IsMatch(record));
		}

		[TestMethod]
		public void IsMatch_StartsWithExpression_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var v = new RegularExpression("^The");

			Assert.IsTrue(v.IsMatch(record));
		}

		[TestMethod]
		public void IsMatch_EndsWithExpression_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new RegularExpression("dog$");

			Assert.IsTrue(expression.IsMatch(record));
		}

		[TestMethod]
		public void IsMatch_ExpressionInMiddleOf_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new RegularExpression("fox");

			Assert.IsTrue(expression.IsMatch(record));
		}
	}
}
