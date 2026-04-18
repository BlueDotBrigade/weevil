namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class XmlFormatterTests
	{
		[TestMethod]
		public void GivenMessage_WhenAsTextCalled_ThenReturnsXmlTextElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsText("hello world");

			// Assert
			result.Should().Be("<text>hello world</text>");
		}

		[TestMethod]
		public void GivenMessage_WhenAsHeadingCalled_ThenReturnsXmlHeadingElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsHeading("Summary");

			// Assert
			result.Should().Be("<heading>Summary</heading>");
		}

		[TestMethod]
		public void GivenMessage_WhenAsSubHeadingCalled_ThenReturnsXmlSubHeadingElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsSubHeading("Details");

			// Assert
			result.Should().Be("<subheading>Details</subheading>");
		}

		[TestMethod]
		public void GivenMessage_WhenAsBulletCalled_ThenReturnsXmlItemElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsBullet("first item");

			// Assert
			result.Should().Be("<item>first item</item>");
		}

		[TestMethod]
		public void GivenMessage_WhenAsNumberedCalled_ThenReturnsXmlNumberedElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsNumbered("step one");

			// Assert
			result.Should().Be("<item number=\"1\">step one</item>");
		}

		[TestMethod]
		public void GivenTwoMessages_WhenAsNumberedCalled_ThenReturnsIncrementingNumbers()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var first = formatter.AsNumbered("step one");
			var second = formatter.AsNumbered("step two");

			// Assert
			first.Should().Be("<item number=\"1\">step one</item>");
			second.Should().Be("<item number=\"2\">step two</item>");
		}

		[TestMethod]
		public void GivenMessage_WhenAsErrorCalled_ThenReturnsXmlErrorElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsError("something failed");

			// Assert
			result.Should().Be("<error>something failed</error>");
		}

		[TestMethod]
		public void GivenSpecialCharacters_WhenAsTextCalled_ThenCharactersAreEscaped()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsText("value < 5 & value > 3");

			// Assert
			result.Should().Be("<text>value &lt; 5 &amp; value &gt; 3</text>");
		}

		[TestMethod]
		public void GivenColumns_WhenAsTableHeaderCalled_ThenReturnsXmlColumnsElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsTableHeader(new[] { "Name", "Age" });

			// Assert
			result.Should().Contain("<table>");
			result.Should().Contain("<columns>");
			result.Should().Contain("<column>Name</column>");
			result.Should().Contain("<column>Age</column>");
			result.Should().Contain("</columns>");
		}

		[TestMethod]
		public void GivenColumns_WhenAsTableRowCalled_ThenReturnsXmlRowElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsTableRow(new[] { "Alice", "30" });

			// Assert
			result.Should().Contain("<row>");
			result.Should().Contain("<cell>Alice</cell>");
			result.Should().Contain("<cell>30</cell>");
			result.Should().Contain("</row>");
		}

		[TestMethod]
		public void GivenValidData_WhenAsTableCalled_ThenIncludesXmlDeclaration()
		{
			// Arrange
			var formatter = new XmlFormatter();
			var headers = new[] { "Name" };
			var rows = new[] { new[] { "Alice" } };

			// Act
			var result = formatter.AsTable(headers, rows);

			// Assert
			result.Should().StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
		}

		[TestMethod]
		public void GivenValidData_WhenAsTableCalled_ThenRootElementIsClosed()
		{
			// Arrange
			var formatter = new XmlFormatter();
			var headers = new[] { "Name", "Age" };
			var rows = new[]
			{
				new[] { "Alice", "30" },
				new[] { "Bob", "25" }
			};

			// Act
			var result = formatter.AsTable(headers, rows);

			// Assert
			result.Should().Contain("<table>");
			result.Should().Contain("</table>");
		}

		[TestMethod]
		public void GivenValidData_WhenAsTableCalled_ThenReturnsCompleteXmlDocument()
		{
			// Arrange
			var formatter = new XmlFormatter();
			var headers = new[] { "Name", "Age", "City" };
			var rows = new[]
			{
				new[] { "Alice", "30", "New York" },
				new[] { "Bob", "25", "Los Angeles" }
			};

			// Act
			var result = formatter.AsTable(headers, rows);

			// Assert
			result.Should().Contain("<columns>");
			result.Should().Contain("<column>Name</column>");
			result.Should().Contain("<column>Age</column>");
			result.Should().Contain("<column>City</column>");
			result.Should().Contain("</columns>");
			result.Should().Contain("<rows>");
			result.Should().Contain("<row>");
			result.Should().Contain("<cell>Alice</cell>");
			result.Should().Contain("<cell>Bob</cell>");
			result.Should().Contain("</rows>");
		}

		[TestMethod]
		public void GivenEmptyRows_WhenAsTableCalled_ThenReturnsTableWithColumnsOnly()
		{
			// Arrange
			var formatter = new XmlFormatter();
			var headers = new[] { "Name", "Age" };
			var rows = new string[0][];

			// Act
			var result = formatter.AsTable(headers, rows);

			// Assert
			result.Should().Contain("<table>");
			result.Should().Contain("<column>Name</column>");
			result.Should().Contain("<column>Age</column>");
			result.Should().Contain("<rows />");
			result.Should().Contain("</table>");
		}

		[TestMethod]
		public void GivenOpenTable_WhenAsTextCalled_ThenTableElementIsClosed()
		{
			// Arrange
			var formatter = new XmlFormatter();
			formatter.AsTableHeader(new[] { "Col1" });

			// Act
			var result = formatter.AsText("after table");

			// Assert
			result.Should().StartWith("</table>");
			result.Should().Contain("<text>after table</text>");
		}

		[TestMethod]
		public void GivenOpenTable_WhenAsHeadingCalled_ThenTableElementIsClosed()
		{
			// Arrange
			var formatter = new XmlFormatter();
			formatter.AsTableHeader(new[] { "Col1" });

			// Act
			var result = formatter.AsHeading("New Section");

			// Assert
			result.Should().StartWith("</table>");
			result.Should().Contain("<heading>New Section</heading>");
		}

		[TestMethod]
		public void GivenSpecialCharactersInTable_WhenAsTableCalled_ThenCharactersAreEscaped()
		{
			// Arrange
			var formatter = new XmlFormatter();
			var headers = new[] { "Expression" };
			var rows = new[] { new[] { "x < 10 & y > 5" } };

			// Act
			var result = formatter.AsTable(headers, rows);

			// Assert
			result.Should().Contain("x &lt; 10 &amp; y &gt; 5");
		}
	}
}
