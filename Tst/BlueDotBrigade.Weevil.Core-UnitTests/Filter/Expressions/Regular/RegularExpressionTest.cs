namespace BlueDotBrigade.Weevil.Filter.Expressions.Regular
{
	using System.Linq;
	using System.Threading.Tasks;
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

			(expression.IsMatch(record)).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_CaseSensitiveSearch_Fails()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new RegularExpression("FOX", Microsoft.RegexOptions.None); // default: case sensitive

			(expression.IsMatch(record)).Should().BeFalse();
		}

		[TestMethod]
		public void IsMatch_CaseInsensitiveSearch_Passes()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new RegularExpression("FoX", Microsoft.RegexOptions.IgnoreCase);

			(expression.IsMatch(record)).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_ExpressionNotInValue_ReturnsFalse()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new RegularExpression("dinosaur");

			(expression.IsMatch(record)).Should().BeFalse();
		}

		[TestMethod]
		public void IsMatch_StartsWithExpression_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var v = new RegularExpression("^The");

			(v.IsMatch(record)).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_EndsWithExpression_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new RegularExpression("dog$");

			(expression.IsMatch(record)).Should().BeTrue();
		}

		[TestMethod]
		public void IsMatch_ExpressionInMiddleOf_ReturnsTrue()
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns("The quick brown fox jumps over the lazy dog");

			var expression = new RegularExpression("fox");

			(expression.IsMatch(record)).Should().BeTrue();
		}

		[TestMethod]
		public void GivenManyThreads_WhenMatching_ThenEachResultIsCorrect()
		{
			var expression = new RegularExpression("fox");
			IRecord[] records = Enumerable.Range(1, 10_000)
				.Select(lineNumber => R.WithContent(lineNumber, lineNumber % 2 == 0 ? "The quick brown fox" : "The lazy dog"))
				.ToArray();
			var results = new bool[records.Length];

			Parallel.For(0, records.Length, i => results[i] = expression.IsMatch(records[i]));

			results.Should().Equal(records.Select(r => r.Content.Contains("fox")));
		}
	}
}
