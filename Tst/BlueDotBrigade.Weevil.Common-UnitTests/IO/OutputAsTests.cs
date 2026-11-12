namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class OutputAsTests
	{
		[TestMethod]
		public void GivenOutputAsWithInlineValue_WhenResolveFormat_ThenReturnsTheFormat()
		{
			// Regression: Global OutputAs parameter should work for any CLI command.
			OutputAs.ResolveFormat(["insight", "--output-as=json"], OutputFormat.Markdown).Should().Be(OutputFormat.Json);
		}

		[TestMethod]
		public void GivenOutputAsWithSeparateValue_WhenResolveFormat_ThenReturnsTheFormat()
		{
			OutputAs.ResolveFormat(["filter", "--OutputAs", "plain"], OutputFormat.Markdown).Should().Be(OutputFormat.PlainText);
		}

		[TestMethod]
		public void GivenNoOutputAs_WhenResolveFormat_ThenTheFallbackIsUsed()
		{
			OutputAs.ResolveFormat(["insight", "--verbose"], OutputFormat.Markdown).Should().Be(OutputFormat.Markdown);
		}

		[TestMethod]
		[DataRow("xml")]
		[DataRow("html")]
		[DataRow("unsupported")]
		public void GivenAFormatWeevilDoesNotWrite_WhenResolveFormat_ThenTheErrorNamesTheSupportedFormats(string value)
		{
			// An unsupported value was ignored, or wrapped each line in XML or HTML; it is now an error (#837).
			Action resolve = () => OutputAs.ResolveFormat(["insight", "--output-as", value], OutputFormat.Markdown);

			resolve.Should().Throw<ArgumentException>()
				.WithMessage($"--output-as {value} is not supported. Use markdown, plaintext or json.");
		}

		[TestMethod]
		public void GivenOutputAsWithSeparateValue_WhenRemoveFromArguments_ThenRemovesOptionAndValue()
		{
			var arguments = OutputAs.RemoveFromArguments(["insight", "--output-as", "json", "--verbose"]);

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
