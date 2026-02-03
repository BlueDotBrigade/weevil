namespace BlueDotBrigade.Weevil.IO
{
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MarkdownFormatterTests
{
[TestMethod]
public void AsTable_WithValidData_ShouldGenerateMarkdownTable()
{
// Arrange
var formatter = new MarkdownFormatter();
var headers = new[] { "Name", "Age", "City" };
var rows = new[]
{
new[] { "Alice", "30", "New York" },
new[] { "Bob", "25", "Los Angeles" }
};

// Act
var result = formatter.AsTable(headers, rows);

// Assert
var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
Assert.AreEqual(4, lines.Length); // Header + separator + 2 data rows
Assert.AreEqual("| Name | Age | City |", lines[0]);
Assert.AreEqual("| --- | --- | --- |", lines[1]);
Assert.AreEqual("| Alice | 30 | New York |", lines[2]);
Assert.AreEqual("| Bob | 25 | Los Angeles |", lines[3]);
}

[TestMethod]
public void AsTable_WithEmptyRows_ShouldReturnHeaderAndSeparator()
{
// Arrange
var formatter = new MarkdownFormatter();
var headers = new[] { "Name", "Age" };
var rows = new string[0][];

// Act
var result = formatter.AsTable(headers, rows);

// Assert
var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
Assert.AreEqual(2, lines.Length); // Header + separator
Assert.AreEqual("| Name | Age |", lines[0]);
Assert.AreEqual("| --- | --- |", lines[1]);
}
}
}
