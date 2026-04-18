namespace BlueDotBrigade.Weevil.IO
{
	using System.Text.Json;
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class JsonFormatterTests
	{
		[TestMethod]
		public void GivenMessage_WhenAsTextCalled_ThenReturnsJsonWithTextProperty()
		{
			// Arrange
			var formatter = new JsonFormatter();

			// Act
			var result = formatter.AsText("Hello");

			// Assert
			using var doc = JsonDocument.Parse(result);
			doc.RootElement.GetProperty("text").GetString().Should().Be("Hello");
		}

		[TestMethod]
		public void GivenMessage_WhenAsHeadingCalled_ThenReturnsJsonWithHeadingProperty()
		{
			// Arrange
			var formatter = new JsonFormatter();

			// Act
			var result = formatter.AsHeading("Title");

			// Assert
			using var doc = JsonDocument.Parse(result);
			doc.RootElement.GetProperty("heading").GetString().Should().Be("Title");
		}

		[TestMethod]
		public void GivenMessage_WhenAsSubHeadingCalled_ThenReturnsJsonWithSubHeadingProperty()
		{
			// Arrange
			var formatter = new JsonFormatter();

			// Act
			var result = formatter.AsSubHeading("Summary");

			// Assert
			using var doc = JsonDocument.Parse(result);
			doc.RootElement.GetProperty("subHeading").GetString().Should().Be("Summary");
		}

		[TestMethod]
		public void GivenMessage_WhenAsBulletCalled_ThenReturnsJsonWithBulletProperty()
		{
			// Arrange
			var formatter = new JsonFormatter();

			// Act
			var result = formatter.AsBullet("Item");

			// Assert
			using var doc = JsonDocument.Parse(result);
			doc.RootElement.GetProperty("bullet").GetString().Should().Be("Item");
		}

		[TestMethod]
		public void GivenMessage_WhenAsNumberedCalledTwice_ThenReturnsIncrementingNumbers()
		{
			// Arrange
			var formatter = new JsonFormatter();

			// Act
			var first = formatter.AsNumbered("First");
			var second = formatter.AsNumbered("Second");

			// Assert
			using var doc1 = JsonDocument.Parse(first);
			doc1.RootElement.GetProperty("number").GetInt32().Should().Be(1);
			doc1.RootElement.GetProperty("text").GetString().Should().Be("First");

			using var doc2 = JsonDocument.Parse(second);
			doc2.RootElement.GetProperty("number").GetInt32().Should().Be(2);
			doc2.RootElement.GetProperty("text").GetString().Should().Be("Second");
		}

		[TestMethod]
		public void GivenMessage_WhenAsErrorCalled_ThenReturnsJsonWithErrorProperty()
		{
			// Arrange
			var formatter = new JsonFormatter();

			// Act
			var result = formatter.AsError("Something failed");

			// Assert
			using var doc = JsonDocument.Parse(result);
			doc.RootElement.GetProperty("error").GetString().Should().Be("Something failed");
		}

		[TestMethod]
		public void GivenHeaders_WhenAsTableHeaderCalled_ThenReturnsJsonWithHeadersArray()
		{
			// Arrange
			var formatter = new JsonFormatter();

			// Act
			var result = formatter.AsTableHeader(new[] { "Name", "Age" });

			// Assert
			using var doc = JsonDocument.Parse(result);
			var headers = doc.RootElement.GetProperty("headers");
			headers.GetArrayLength().Should().Be(2);
			headers[0].GetString().Should().Be("Name");
			headers[1].GetString().Should().Be("Age");
		}

		[TestMethod]
		public void GivenColumns_WhenAsTableRowCalled_ThenReturnsJsonWithRowArray()
		{
			// Arrange
			var formatter = new JsonFormatter();

			// Act
			var result = formatter.AsTableRow(new[] { "Alice", "30" });

			// Assert
			using var doc = JsonDocument.Parse(result);
			var row = doc.RootElement.GetProperty("row");
			row.GetArrayLength().Should().Be(2);
			row[0].GetString().Should().Be("Alice");
			row[1].GetString().Should().Be("30");
		}

		[TestMethod]
		public void GivenValidData_WhenAsTableCalled_ThenReturnsJsonWithHeadersAndRows()
		{
			// Arrange
			var formatter = new JsonFormatter();
			var headers = new[] { "Name", "Age", "City" };
			var rows = new[]
			{
				new[] { "Alice", "30", "New York" },
				new[] { "Bob", "25", "Los Angeles" }
			};

			// Act
			var result = formatter.AsTable(headers, rows);

			// Assert
			using var doc = JsonDocument.Parse(result);
			var headersElement = doc.RootElement.GetProperty("headers");
			headersElement.GetArrayLength().Should().Be(3);
			headersElement[0].GetString().Should().Be("Name");

			var rowsElement = doc.RootElement.GetProperty("rows");
			rowsElement.GetArrayLength().Should().Be(2);
			rowsElement[0][0].GetString().Should().Be("Alice");
			rowsElement[1][2].GetString().Should().Be("Los Angeles");
		}

		[TestMethod]
		public void GivenEmptyRows_WhenAsTableCalled_ThenReturnsJsonWithHeadersAndEmptyRows()
		{
			// Arrange
			var formatter = new JsonFormatter();
			var headers = new[] { "Name", "Age" };
			var rows = new string[0][];

			// Act
			var result = formatter.AsTable(headers, rows);

			// Assert
			using var doc = JsonDocument.Parse(result);
			doc.RootElement.GetProperty("headers").GetArrayLength().Should().Be(2);
			doc.RootElement.GetProperty("rows").GetArrayLength().Should().Be(0);
		}

		[TestMethod]
		public void GivenNumberedItems_WhenResetNumberingCalled_ThenNumberingRestartsFromOne()
		{
			// Arrange
			var formatter = new JsonFormatter();
			formatter.AsNumbered("First");
			formatter.AsNumbered("Second");

			// Act
			formatter.ResetNumbering();
			var result = formatter.AsNumbered("After Reset");

			// Assert
			using var doc = JsonDocument.Parse(result);
			doc.RootElement.GetProperty("number").GetInt32().Should().Be(1);
		}

		[TestMethod]
		public void GivenSpecialCharacters_WhenAsTextCalled_ThenCharactersAreProperlyEscaped()
		{
			// Arrange
			var formatter = new JsonFormatter();

			// Act
			var result = formatter.AsText("Line1\nLine2\tTabbed \"quoted\"");

			// Assert
			using var doc = JsonDocument.Parse(result);
			doc.RootElement.GetProperty("text").GetString().Should().Be("Line1\nLine2\tTabbed \"quoted\"");
		}
	}
}
