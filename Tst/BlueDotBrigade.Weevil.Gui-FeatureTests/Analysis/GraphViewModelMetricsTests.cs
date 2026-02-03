namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using FluentAssertions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class GraphViewModelMetricsTests
	{
		[TestMethod]
		public void SerializeMetrics_WithOneSeries_ShouldGenerateTabDelimitedText()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, new DateTime(2024, 1, 1, 10, 0, 0), SeverityType.Information, "Value=5.0"),
				new Record(2, new DateTime(2024, 1, 1, 10, 0, 1), SeverityType.Information, "Value=10.0"),
				new Record(3, new DateTime(2024, 1, 1, 10, 0, 2), SeverityType.Information, "Value=15.0"));

			var expression = "Value=(?<value>\\d+\\.?\\d*)";

			var viewModel = new GraphViewModel(records, expression, "title", "source");

			// Act
			var serialized = viewModel.SerializeMetrics();

			// Assert
			serialized.Should().NotBeNullOrEmpty();
			var lines = serialized.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			lines.Should().HaveCount(2); // Header + 1 data row

			// Check header
			lines[0].Should().Be("Series Name\tCount\tMin\tMax\tMean\tMedian\tRange Start\tRange End");

			// Check data row
			var dataRow = lines[1].Split('\t');
			dataRow.Should().HaveCount(8);
			dataRow[0].Should().Be("value"); // Series Name
			dataRow[1].Should().Be("3"); // Count
			dataRow[2].Should().Be("5.000"); // Min
			dataRow[3].Should().Be("15.000"); // Max
			dataRow[4].Should().Be("10.000"); // Mean
			dataRow[5].Should().Be("10.000"); // Median
			dataRow[6].Should().Be("2024-01-01 10:00:00"); // Range Start
			dataRow[7].Should().Be("2024-01-01 10:00:02"); // Range End
		}

		[TestMethod]
		public void SerializeMetrics_WithTwoSeries_ShouldGenerateTwoDataRows()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, new DateTime(2024, 1, 1, 10, 0, 0), SeverityType.Information, "CPU=5.0"),
				new Record(2, new DateTime(2024, 1, 1, 10, 0, 1), SeverityType.Information, "MEM=10.0"));

			var expression = $"CPU=(?<cpu>\\d+\\.?\\d*){Constants.FilterOrOperator}MEM=(?<mem>\\d+\\.?\\d*)";

			var viewModel = new GraphViewModel(records, expression, "title", "source");

			// Act
			var serialized = viewModel.SerializeMetrics();

			// Assert
			serialized.Should().NotBeNullOrEmpty();
			var lines = serialized.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			lines.Should().HaveCount(3); // Header + 2 data rows

			// Check header
			lines[0].Should().Be("Series Name\tCount\tMin\tMax\tMean\tMedian\tRange Start\tRange End");

			// Check first data row (CPU series)
			var dataRow1 = lines[1].Split('\t');
			dataRow1[0].Should().Be("cpu");
			dataRow1[1].Should().Be("1");
			dataRow1[2].Should().Be("5.000");

			// Check second data row (MEM series)
			var dataRow2 = lines[2].Split('\t');
			dataRow2[0].Should().Be("mem");
			dataRow2[1].Should().Be("1");
			dataRow2[2].Should().Be("10.000");
		}

		[TestMethod]
		public void SerializeMetrics_WithEmptySeries_ShouldShowNA()
		{
			// Arrange
			var records = ImmutableArray<IRecord>.Empty;
			var expression = "Value=(?<value>\\d+)";

			var viewModel = new GraphViewModel(records, expression, "title", "source");

			// Act
			var serialized = viewModel.SerializeMetrics();

			// Assert - When there are no records, there should be no series and thus no metrics
			serialized.Should().BeEmpty();
		}

		[TestMethod]
		public void CalculateSeriesMetrics_ShouldUpdateWhenSeriesChanges()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "MEM=10"));

			var expression = $"CPU=(?<cpu>\\d+){Constants.FilterOrOperator}MEM=(?<mem>\\d+)";

			var viewModel = new GraphViewModel(records, expression, "title", "source");

			// Assert initial metrics
			viewModel.SeriesMetrics.Should().HaveCount(2);
			viewModel.SeriesMetrics[0].SeriesName.Should().Be("cpu");
			viewModel.SeriesMetrics[1].SeriesName.Should().Be("mem");

			// Act - Change series name
			viewModel.Series1Name = "Modified CPU";
			viewModel.UpdateCommand.Execute(null);

			// Assert - Metrics should update with new name
			viewModel.SeriesMetrics.Should().HaveCount(2);
			viewModel.SeriesMetrics[0].SeriesName.Should().Be("Modified CPU");
			viewModel.SeriesMetrics[1].SeriesName.Should().Be("mem");
		}

		[TestMethod]
		public void SeriesMetrics_WithMultipleValues_ShouldCalculateCorrectStatistics()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, new DateTime(2024, 1, 1, 10, 0, 0), SeverityType.Information, "Value=1.0"),
				new Record(2, new DateTime(2024, 1, 1, 10, 0, 1), SeverityType.Information, "Value=2.0"),
				new Record(3, new DateTime(2024, 1, 1, 10, 0, 2), SeverityType.Information, "Value=3.0"),
				new Record(4, new DateTime(2024, 1, 1, 10, 0, 3), SeverityType.Information, "Value=4.0"),
				new Record(5, new DateTime(2024, 1, 1, 10, 0, 4), SeverityType.Information, "Value=5.0"));

			var expression = "Value=(?<value>\\d+\\.?\\d*)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");

			// Assert
			viewModel.SeriesMetrics.Should().HaveCount(1);
			var metrics = viewModel.SeriesMetrics[0];
			
			metrics.Count.Should().Be(5);
			metrics.Min.Should().Be(1.0);
			metrics.Max.Should().Be(5.0);
			metrics.Mean.Should().Be(3.0);
			metrics.Median.Should().Be(3.0);
			metrics.RangeStart.Should().Be(new DateTime(2024, 1, 1, 10, 0, 0));
			metrics.RangeEnd.Should().Be(new DateTime(2024, 1, 1, 10, 0, 4));
		}

		[TestMethod]
		public void SeriesMetrics_WithEvenNumberOfValues_ShouldCalculateMedianCorrectly()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "Value=1.0"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "Value=2.0"),
				new Record(3, DateTime.UtcNow.AddSeconds(2), SeverityType.Information, "Value=3.0"),
				new Record(4, DateTime.UtcNow.AddSeconds(3), SeverityType.Information, "Value=4.0"));

			var expression = "Value=(?<value>\\d+\\.?\\d*)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");

			// Assert
			viewModel.SeriesMetrics.Should().HaveCount(1);
			var metrics = viewModel.SeriesMetrics[0];
			
			// Median of [1, 2, 3, 4] is (2 + 3) / 2 = 2.5
			metrics.Median.Should().Be(2.5);
		}

		[TestMethod]
		public void SerializeMetrics_WithTabDelimitedFormatter_ShouldGenerateTabDelimitedText()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, new DateTime(2024, 1, 1, 10, 0, 0), SeverityType.Information, "Value=5.0"));

			var expression = "Value=(?<value>\\d+\\.?\\d*)";
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			var formatter = new BlueDotBrigade.Weevil.IO.TabDelimitedFormatter();

			// Act
			var serialized = viewModel.SerializeMetrics(formatter);

			// Assert
			serialized.Should().NotBeNullOrEmpty();
			serialized.Should().Contain("\t"); // Should have tab delimiters
			var lines = serialized.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			lines.Should().HaveCount(2); // Header + 1 data row
		}

		[TestMethod]
		public void SerializeMetrics_WithPlainTextFormatter_ShouldGeneratePlainText()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, new DateTime(2024, 1, 1, 10, 0, 0), SeverityType.Information, "Value=5.0"));

			var expression = "Value=(?<value>\\d+\\.?\\d*)";
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			var formatter = new BlueDotBrigade.Weevil.IO.PlainTextFormatter();

			// Act
			var serialized = viewModel.SerializeMetrics(formatter);

			// Assert
			serialized.Should().NotBeNullOrEmpty();
			serialized.Should().Contain("\t"); // Should still have tab delimiters in data
			var lines = serialized.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			lines.Should().HaveCount(2); // Header + 1 data row
		}

		[TestMethod]
		public void SerializeMetrics_WithMarkdownFormatter_ShouldGenerateMarkdownFormattedHeader()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, new DateTime(2024, 1, 1, 10, 0, 0), SeverityType.Information, "Value=5.0"));

			var expression = "Value=(?<value>\\d+\\.?\\d*)";
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			var formatter = new BlueDotBrigade.Weevil.IO.MarkdownFormatter();

			// Act
			var serialized = viewModel.SerializeMetrics(formatter);

			// Assert
			serialized.Should().NotBeNullOrEmpty();
			var lines = serialized.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			lines.Should().HaveCount(2); // Header + 1 data row
			lines[0].Should().StartWith("# "); // Markdown heading format
		}

		[TestMethod]
		public void SerializeMetrics_WithNullFormatter_ShouldThrowArgumentNullException()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, new DateTime(2024, 1, 1, 10, 0, 0), SeverityType.Information, "Value=5.0"));

			var expression = "Value=(?<value>\\d+\\.?\\d*)";
			var viewModel = new GraphViewModel(records, expression, "title", "source");

			// Act & Assert
			Action act = () => viewModel.SerializeMetrics(null);
			act.Should().Throw<ArgumentNullException>()
				.WithParameterName("formatter");
		}
	}
}
