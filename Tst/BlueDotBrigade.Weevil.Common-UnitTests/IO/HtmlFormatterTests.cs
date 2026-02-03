namespace BlueDotBrigade.Weevil.IO
{
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class HtmlFormatterTests
{
[TestMethod]
public void AsTable_WithValidData_ShouldGenerateHtmlTable()
{
// Arrange
var formatter = new HtmlFormatter();
var headers = new[] { "Name", "Age", "City" };
var rows = new[]
{
new[] { "Alice", "30", "New York" },
new[] { "Bob", "25", "Los Angeles" }
};

// Act
var result = formatter.AsTable(headers, rows);

// Assert
Assert.IsTrue(result.Contains("<table>"));
Assert.IsTrue(result.Contains("<thead>"));
Assert.IsTrue(result.Contains("<tbody>"));
Assert.IsTrue(result.Contains("</table>"));
Assert.IsTrue(result.Contains("<th>Name</th>"));
Assert.IsTrue(result.Contains("<td>Alice</td>"));
}

[TestMethod]
public void AsTable_WithEmptyRows_ShouldReturnTableWithHeaderOnly()
{
// Arrange
var formatter = new HtmlFormatter();
var headers = new[] { "Name", "Age" };
var rows = new string[0][];

// Act
var result = formatter.AsTable(headers, rows);

// Assert
Assert.IsTrue(result.Contains("<table>"));
Assert.IsTrue(result.Contains("<thead>"));
Assert.IsTrue(result.Contains("<tbody>"));
Assert.IsTrue(result.Contains("</table>"));
Assert.IsTrue(result.Contains("<th>Name</th>"));
Assert.IsTrue(result.Contains("<th>Age</th>"));
}
}
}
