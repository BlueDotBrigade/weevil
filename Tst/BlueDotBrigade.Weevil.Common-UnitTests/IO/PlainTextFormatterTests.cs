namespace BlueDotBrigade.Weevil.IO
{
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class PlainTextFormatterTests
{
[TestMethod]
public void AsTable_WithValidData_ShouldGenerateTabDelimitedTable()
{
// Arrange
var formatter = new PlainTextFormatter();
var headers = new[] { "Name", "Age", "City" };
var rows = new[]
{
new[] { "Alice", "30", "New York" },
new[] { "Bob", "25", "Los Angeles" }
};

// Act
var result = formatter.AsTable(headers, rows);

// Assert
Assert.IsTrue(result.Contains("\t"));
var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
Assert.AreEqual(3, lines.Length); // Header + 2 data rows
Assert.AreEqual("Name\tAge\tCity", lines[0]);
Assert.AreEqual("Alice\t30\tNew York", lines[1]);
Assert.AreEqual("Bob\t25\tLos Angeles", lines[2]);
}

[TestMethod]
public void AsTable_WithEmptyRows_ShouldReturnOnlyHeader()
{
// Arrange
var formatter = new PlainTextFormatter();
var headers = new[] { "Name", "Age" };
var rows = new string[0][];

// Act
var result = formatter.AsTable(headers, rows);

// Assert
var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
Assert.AreEqual(1, lines.Length);
Assert.AreEqual("Name\tAge", lines[0]);
}
}
}
