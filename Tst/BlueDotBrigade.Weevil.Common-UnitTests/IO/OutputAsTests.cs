namespace BlueDotBrigade.Weevil.IO
{
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class OutputAsTests
	{
		[TestMethod]
		public void GivenOutputAsWithInlineValue_WhenResolveFormatter_ThenReturnsSpecifiedFormatter()
		{
			// Regression: Global OutputAs parameter should work for any CLI command.
			var formatter = OutputAs.ResolveFormatter(["insight", "--output-as=json"], new MarkdownFormatter());

			formatter.Should().BeOfType<JsonFormatter>();
		}

		[TestMethod]
		public void GivenOutputAsWithSeparateValue_WhenResolveFormatter_ThenReturnsSpecifiedFormatter()
		{
			var formatter = OutputAs.ResolveFormatter(["filter", "--OutputAs", "xml"], new MarkdownFormatter());

			formatter.Should().BeOfType<XmlFormatter>();
		}

		[TestMethod]
		public void GivenUnknownOutputAsValue_WhenResolveFormatter_ThenReturnsFallbackFormatter()
		{
			var fallback = new MarkdownFormatter();

			var formatter = OutputAs.ResolveFormatter(["insight", "--output-as", "unsupported"], fallback);

			formatter.Should().BeSameAs(fallback);
		}

		[TestMethod]
		public void GivenOutputAsWithSeparateValue_WhenRemoveFromArguments_ThenRemovesOptionAndValue()
		{
			var arguments = OutputAs.RemoveFromArguments(["insight", "--output-as", "html", "--verbose"]);

			arguments.Should().Equal("insight", "--verbose");
		}

		[TestMethod]
		public void GivenOutputAsWithInlineValue_WhenRemoveFromArguments_ThenRemovesOptionOnly()
		{
			var arguments = OutputAs.RemoveFromArguments(["insight", "--outputas=plain", "--verbose"]);

			arguments.Should().Equal("insight", "--verbose");
		}
	}
}
