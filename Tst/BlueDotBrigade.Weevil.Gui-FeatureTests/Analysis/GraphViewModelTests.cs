namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using FluentAssertions;
	using LiveChartsCore.SkiaSharpView;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class GraphViewModelTests
	{
		[TestMethod]
		public void GraphViewModel_WithMultipleExpressions_ShouldCreateTwoSeries()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "MEM=10"));

			var expression = $"CPU=(?<cpu>\\d+){Constants.FilterOrOperator}MEM=(?<mem>\\d+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			var series = viewModel.Series.ToList();

			// Assert
			series.Should().HaveCount(2);
			series[0].Name.Should().Be("cpu");
			series[1].Name.Should().Be("mem");

			var firstSeries = series[0] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			var secondSeries = series[1] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;

			firstSeries.Should().NotBeNull();
			secondSeries.Should().NotBeNull();

			firstSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().Should().ContainSingle();
			secondSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().Should().ContainSingle();
		}
	}
}
