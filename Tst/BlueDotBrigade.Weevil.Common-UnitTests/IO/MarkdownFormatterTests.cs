namespace BlueDotBrigade.Weevil.IO
{
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MarkdownFormatterTests
{
[TestMethod]
public void GivenMessage_WhenAsSubHeadingCalled_ThenReturnsMarkdownSubHeading()
{
// Arrange
var formatter = new MarkdownFormatter();

// Act
var result = formatter.AsSubHeading("Summary");

// Assert
(result).Should().Be("## Summary");
}

[TestMethod]
public void GivenColumns_WhenAsTableHeaderAndAsTableRowCalled_ThenReturnsMarkdownRows()
{
// Arrange
var formatter = new MarkdownFormatter();

// Act
var header = formatter.AsTableHeader(new[] { "Name", "Age" });
var row = formatter.AsTableRow(new[] { "Alice", "30" });
var lines = header.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

// Assert
(lines.Length).Should().Be(2);
(lines[0]).Should().Be("| Name | Age |");
(lines[1]).Should().Be("| --- | --- |");
(row).Should().Be("| Alice | 30 |");
}

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
(lines.Length).Should().Be(4); // Header + separator + 2 data rows
(lines[0]).Should().Be("| Name | Age | City |");
(lines[1]).Should().Be("| --- | --- | --- |");
(lines[2]).Should().Be("| Alice | 30 | New York |");
(lines[3]).Should().Be("| Bob | 25 | Los Angeles |");
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
(lines.Length).Should().Be(2); // Header + separator
(lines[0]).Should().Be("| Name | Age |");
(lines[1]).Should().Be("| --- | --- |");
}
}
}
