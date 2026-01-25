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

		[TestMethod]
		public void GraphViewModel_WithSecondaryAxisSeriesNone_ShouldPlotBothSeriesOnLeftAxis()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "MEM=10"));

			var expression = $"CPU=(?<cpu>\\d+){Constants.FilterOrOperator}MEM=(?<mem>\\d+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			viewModel.SecondaryAxisSeries = "None";
			var series = viewModel.Series.ToList();

			// Assert
			series.Should().HaveCount(2);
			
			var firstSeries = series[0] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			var secondSeries = series[1] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;

			firstSeries.Should().NotBeNull();
			secondSeries.Should().NotBeNull();

			// Both should be on axis 0 (left axis)
			firstSeries!.ScalesYAt.Should().Be(0);
			secondSeries!.ScalesYAt.Should().Be(0);
			
			// Should have only one Y-axis
			viewModel.YAxes.Should().ContainSingle();
		}

		[TestMethod]
		public void GraphViewModel_WithSecondaryAxisSeriesSeries1_ShouldPlotSeries1OnRightAxis()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "MEM=10"));

			var expression = $"CPU=(?<cpu>\\d+){Constants.FilterOrOperator}MEM=(?<mem>\\d+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			viewModel.SecondaryAxisSeries = "Series 1";
			var series = viewModel.Series.ToList();

			// Assert
			series.Should().HaveCount(2);
			
			var firstSeries = series[0] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			var secondSeries = series[1] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;

			firstSeries.Should().NotBeNull();
			secondSeries.Should().NotBeNull();

			// Series 1 should be on axis 1 (right axis), Series 2 on axis 0 (left axis)
			firstSeries!.ScalesYAt.Should().Be(1);
			secondSeries!.ScalesYAt.Should().Be(0);
			
			// Should have two Y-axes
			viewModel.YAxes.Should().HaveCount(2);
		}

		[TestMethod]
		public void GraphViewModel_WithSecondaryAxisSeriesSeries2_ShouldPlotSeries2OnRightAxis()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "MEM=10"));

			var expression = $"CPU=(?<cpu>\\d+){Constants.FilterOrOperator}MEM=(?<mem>\\d+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			viewModel.SecondaryAxisSeries = "Series 2";
			var series = viewModel.Series.ToList();

			// Assert
			series.Should().HaveCount(2);
			
			var firstSeries = series[0] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			var secondSeries = series[1] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;

			firstSeries.Should().NotBeNull();
			secondSeries.Should().NotBeNull();

			// Series 1 should be on axis 0 (left axis), Series 2 on axis 1 (right axis)
			firstSeries!.ScalesYAt.Should().Be(0);
			secondSeries!.ScalesYAt.Should().Be(1);
			
			// Should have two Y-axes
			viewModel.YAxes.Should().HaveCount(2);
		}
	}
}
