namespace BlueDotBrigade.Weevil.IO
{
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class HtmlFormatterTests
{
[TestMethod]
public void GivenMessage_WhenAsSubHeadingCalled_ThenReturnsHtmlSubHeading()
{
// Arrange
var formatter = new HtmlFormatter();

// Act
var result = formatter.AsSubHeading("Summary");

// Assert
Assert.AreEqual("<h2>Summary</h2>", result);
}

[TestMethod]
public void GivenColumns_WhenAsTableHeaderAndAsTableRowCalled_ThenReturnsHtmlFragments()
{
// Arrange
var formatter = new HtmlFormatter();

// Act
var header = formatter.AsTableHeader(new[] { "Name", "Age" });
var row = formatter.AsTableRow(new[] { "Alice", "30" });

// Assert
Assert.IsTrue(header.Contains("<thead>"));
Assert.IsTrue(header.Contains("<th>Name</th>"));
Assert.IsTrue(header.Contains("<th>Age</th>"));
Assert.AreEqual("    <tr>" + Environment.NewLine + "      <td>Alice</td>" + Environment.NewLine + "      <td>30</td>" + Environment.NewLine + "    </tr>", row);
}

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
