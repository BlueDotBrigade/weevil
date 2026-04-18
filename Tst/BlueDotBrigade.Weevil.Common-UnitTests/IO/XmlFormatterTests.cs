namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Xml.Linq;
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
			result.Should().Be("<Text>hello world</Text>");
		}

		[TestMethod]
		public void GivenMessage_WhenAsHeadingCalled_ThenReturnsXmlHeadingElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsHeading("Summary");

			// Assert
			result.Should().Be("<Heading>Summary</Heading>");
		}

		[TestMethod]
		public void GivenMessage_WhenAsSubHeadingCalled_ThenReturnsXmlSubHeadingElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsSubHeading("Details");

			// Assert
			result.Should().Be("<SubHeading>Details</SubHeading>");
		}

		[TestMethod]
		public void GivenMessage_WhenAsBulletCalled_ThenReturnsXmlItemElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsBullet("first item");

			// Assert
			result.Should().Be("<Item>first item</Item>");
		}

		[TestMethod]
		public void GivenMessage_WhenAsNumberedCalled_ThenReturnsXmlNumberedElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsNumbered("step one");

			// Assert
			result.Should().Be("<Item Number=\"1\">step one</Item>");
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
			first.Should().Be("<Item Number=\"1\">step one</Item>");
			second.Should().Be("<Item Number=\"2\">step two</Item>");
		}

		[TestMethod]
		public void GivenMessage_WhenAsErrorCalled_ThenReturnsXmlErrorElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsError("something failed");

			// Assert
			result.Should().Be("<Error>something failed</Error>");
		}

		[TestMethod]
		public void GivenMessage_WhenAsWarningCalled_ThenReturnsXmlWarningElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsWarning("pay attention");

			// Assert
			result.Should().Be("<Warning>pay attention</Warning>");
		}

		[TestMethod]
		public void GivenSpecialCharacters_WhenAsTextCalled_ThenCharactersAreEscaped()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsText("value < 5 & value > 3");

			// Assert
			result.Should().Be("<Text>value &lt; 5 &amp; value &gt; 3</Text>");
		}

		[TestMethod]
		public void GivenColumns_WhenAsTableHeaderCalled_ThenReturnsXmlColumnsElement()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsTableHeader(new[] { "Name", "Age" });

			// Assert
			result.Should().Contain("<Table>");
			result.Should().Contain("<Columns>");
			result.Should().Contain("<Name />");
			result.Should().Contain("<Age />");
			result.Should().Contain("</Columns>");
		}

		[TestMethod]
		public void GivenColumns_WhenAsTableRowCalled_ThenReturnsXmlRowElementUsingFallbackColumnNames()
		{
			// Arrange
			var formatter = new XmlFormatter();

			// Act
			var result = formatter.AsTableRow(new[] { "Alice", "30" });

			// Assert
			result.Should().Contain("<Row>");
			result.Should().Contain("<Column1>Alice</Column1>");
			result.Should().Contain("<Column2>30</Column2>");
			result.Should().Contain("</Row>");
		}

		[TestMethod]
		public void GivenColumnNamesWithSpecialCharacters_WhenAsTableHeaderThenAsTableRowCalled_ThenUsesSanitizedTitleCaseColumnElementNames()
		{
			// Arrange
			var formatter = new XmlFormatter();
			formatter.AsTableHeader(new[] { "first_name", "age-years" });

			// Act
			var result = formatter.AsTableRow(new[] { "Alice", "30" });

			// Assert
			result.Should().Contain("<FirstName>Alice</FirstName>");
			result.Should().Contain("<AgeYears>30</AgeYears>");
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
			result.Should().Contain("<Table>");
			result.Should().Contain("</Table>");
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
			result.Should().Contain("<Columns>");
			result.Should().Contain("<Name />");
			result.Should().Contain("<Age />");
			result.Should().Contain("<City />");
			result.Should().Contain("</Columns>");
			result.Should().Contain("<Rows>");
			result.Should().Contain("<Row>");
			result.Should().Contain("<Name>Alice</Name>");
			result.Should().Contain("<Name>Bob</Name>");
			result.Should().Contain("</Rows>");
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
			result.Should().Contain("<Table>");
			result.Should().Contain("<Name />");
			result.Should().Contain("<Age />");
			result.Should().Contain("<Rows />");
			result.Should().Contain("</Table>");
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
			result.Should().StartWith("</Table>");
			result.Should().Contain("<Text>after table</Text>");
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
			result.Should().StartWith("</Table>");
			result.Should().Contain("<Heading>New Section</Heading>");
		}

		[TestMethod]
		public void GivenOpenTable_WhenAsNumberedCalled_ThenTableElementIsClosed()
		{
			// Arrange
			var formatter = new XmlFormatter();
			formatter.AsTableHeader(new[] { "Col1" });

			// Act
			var result = formatter.AsNumbered("step");

			// Assert
			result.Should().StartWith("</Table>");
			result.Should().Contain("<Item Number=\"1\">step</Item>");
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

		[TestMethod]
		public void GivenTableOutput_WhenAsTableCalled_ThenXmlIsWellFormed()
		{
			// Arrange
			var formatter = new XmlFormatter();
			var headers = new[] { "Name", "Age" };
			var rows = new[] { new[] { "Alice", "30" } };

			// Act
			var result = formatter.AsTable(headers, rows);

			// Assert
			Action parse = () => XDocument.Parse(result);
			parse.Should().NotThrow();
		}
	}
}
