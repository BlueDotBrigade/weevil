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
(result).Should().Be("<h2>Summary</h2>");
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
(header.Contains("<thead>")).Should().BeTrue();
(header.Contains("<th>Name</th>")).Should().BeTrue();
(header.Contains("<th>Age</th>")).Should().BeTrue();
(row).Should().Be("    <tr>" + Environment.NewLine + "      <td>Alice</td>" + Environment.NewLine + "      <td>30</td>" + Environment.NewLine + "    </tr>");
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
(result.Contains("<table>")).Should().BeTrue();
(result.Contains("<thead>")).Should().BeTrue();
(result.Contains("<tbody>")).Should().BeTrue();
(result.Contains("</table>")).Should().BeTrue();
(result.Contains("<th>Name</th>")).Should().BeTrue();
(result.Contains("<td>Alice</td>")).Should().BeTrue();
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
(result.Contains("<table>")).Should().BeTrue();
(result.Contains("<thead>")).Should().BeTrue();
(result.Contains("<tbody>")).Should().BeTrue();
(result.Contains("</table>")).Should().BeTrue();
(result.Contains("<th>Name</th>")).Should().BeTrue();
(result.Contains("<th>Age</th>")).Should().BeTrue();
}
}
}
