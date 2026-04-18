namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Text.Json;
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
			Assert.AreEqual("Hello", doc.RootElement.GetProperty("text").GetString());
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
			Assert.AreEqual("Title", doc.RootElement.GetProperty("heading").GetString());
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
			Assert.AreEqual("Summary", doc.RootElement.GetProperty("subHeading").GetString());
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
			Assert.AreEqual("Item", doc.RootElement.GetProperty("bullet").GetString());
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
			Assert.AreEqual(1, doc1.RootElement.GetProperty("number").GetInt32());
			Assert.AreEqual("First", doc1.RootElement.GetProperty("text").GetString());

			using var doc2 = JsonDocument.Parse(second);
			Assert.AreEqual(2, doc2.RootElement.GetProperty("number").GetInt32());
			Assert.AreEqual("Second", doc2.RootElement.GetProperty("text").GetString());
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
			Assert.AreEqual("Something failed", doc.RootElement.GetProperty("error").GetString());
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
			Assert.AreEqual(2, headers.GetArrayLength());
			Assert.AreEqual("Name", headers[0].GetString());
			Assert.AreEqual("Age", headers[1].GetString());
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
			Assert.AreEqual(2, row.GetArrayLength());
			Assert.AreEqual("Alice", row[0].GetString());
			Assert.AreEqual("30", row[1].GetString());
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
			Assert.AreEqual(3, headersElement.GetArrayLength());
			Assert.AreEqual("Name", headersElement[0].GetString());

			var rowsElement = doc.RootElement.GetProperty("rows");
			Assert.AreEqual(2, rowsElement.GetArrayLength());
			Assert.AreEqual("Alice", rowsElement[0][0].GetString());
			Assert.AreEqual("Los Angeles", rowsElement[1][2].GetString());
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
			var headersElement = doc.RootElement.GetProperty("headers");
			Assert.AreEqual(2, headersElement.GetArrayLength());

			var rowsElement = doc.RootElement.GetProperty("rows");
			Assert.AreEqual(0, rowsElement.GetArrayLength());
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
			Assert.AreEqual(1, doc.RootElement.GetProperty("number").GetInt32());
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
			Assert.AreEqual("Line1\nLine2\tTabbed \"quoted\"", doc.RootElement.GetProperty("text").GetString());
		}
	}
}
