namespace BlueDotBrigade.Weevil.IO
{
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class PlainTextFormatterTests
{
[TestMethod]
public void GivenMessage_WhenAsSubHeadingCalled_ThenReturnsMessageAsIs()
{
// Arrange
var formatter = new PlainTextFormatter();

// Act
var result = formatter.AsSubHeading("Summary");

// Assert
(result).Should().Be("Summary");
}

[TestMethod]
public void GivenColumns_WhenAsTableHeaderAndAsTableRowCalled_ThenReturnsTabDelimitedLines()
{
// Arrange
var formatter = new PlainTextFormatter();

// Act
var header = formatter.AsTableHeader(new[] { "Name", "Age" });
var row = formatter.AsTableRow(new[] { "Alice", "30" });

// Assert
(header).Should().Be("Name\tAge");
(row).Should().Be("Alice\t30");
}

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
(result.Contains("\t")).Should().BeTrue();
var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
(lines.Length).Should().Be(3); // Header + 2 data rows
(lines[0]).Should().Be("Name\tAge\tCity");
(lines[1]).Should().Be("Alice\t30\tNew York");
(lines[2]).Should().Be("Bob\t25\tLos Angeles");
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
(lines.Length).Should().Be(1);
(lines[0]).Should().Be("Name\tAge");
}
}
}
