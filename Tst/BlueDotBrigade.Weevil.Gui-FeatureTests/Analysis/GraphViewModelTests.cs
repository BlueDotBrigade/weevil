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
		public void GraphViewModel_WithBothSeriesOnLeftAxis_ShouldPlotBothSeriesOnLeftAxis()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "MEM=10"));

			var expression = $"CPU=(?<cpu>\\d+){Constants.FilterOrOperator}MEM=(?<mem>\\d+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			// Default is both on left, but set explicitly for clarity
			viewModel.Series1Axis = GraphViewModel.YAxisLeft;
			viewModel.Series2Axis = GraphViewModel.YAxisLeft;
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
		public void GraphViewModel_WithSeries1OnRightAxis_ShouldPlotSeries1OnRightAxis()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "MEM=10"));

			var expression = $"CPU=(?<cpu>\\d+){Constants.FilterOrOperator}MEM=(?<mem>\\d+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			viewModel.Series1Axis = GraphViewModel.YAxisRight;
			viewModel.Series2Axis = GraphViewModel.YAxisLeft;
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
		public void GraphViewModel_WithSeries2OnRightAxis_ShouldPlotSeries2OnRightAxis()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "MEM=10"));

			var expression = $"CPU=(?<cpu>\\d+){Constants.FilterOrOperator}MEM=(?<mem>\\d+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			viewModel.Series1Axis = GraphViewModel.YAxisLeft;
			viewModel.Series2Axis = GraphViewModel.YAxisRight;
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

		[TestMethod]
		public void GraphViewModel_WithThreeExpressions_ShouldCreateThreeSeries()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "MEM=10"),
				new Record(3, DateTime.UtcNow.AddSeconds(2), SeverityType.Information, "DISK=15"));

			var expression = $"CPU=(?<cpu>\\d+){Constants.FilterOrOperator}MEM=(?<mem>\\d+){Constants.FilterOrOperator}DISK=(?<disk>\\d+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			var series = viewModel.Series.ToList();

			// Assert
			series.Should().HaveCount(3);
			series[0].Name.Should().Be("cpu");
			series[1].Name.Should().Be("mem");
			series[2].Name.Should().Be("disk");

			var firstSeries = series[0] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			var secondSeries = series[1] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			var thirdSeries = series[2] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;

			firstSeries.Should().NotBeNull();
			secondSeries.Should().NotBeNull();
			thirdSeries.Should().NotBeNull();

			firstSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().Should().ContainSingle();
			secondSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().Should().ContainSingle();
			thirdSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().Should().ContainSingle();
		}

		[TestMethod]
		public void GraphViewModel_WithFourExpressions_ShouldCreateFourSeries()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "MEM=10"),
				new Record(3, DateTime.UtcNow.AddSeconds(2), SeverityType.Information, "DISK=15"),
				new Record(4, DateTime.UtcNow.AddSeconds(3), SeverityType.Information, "NET=20"));

			var expression = $"CPU=(?<cpu>\\d+){Constants.FilterOrOperator}MEM=(?<mem>\\d+){Constants.FilterOrOperator}DISK=(?<disk>\\d+){Constants.FilterOrOperator}NET=(?<net>\\d+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			var series = viewModel.Series.ToList();

			// Assert
			series.Should().HaveCount(4);
			series[0].Name.Should().Be("cpu");
			series[1].Name.Should().Be("mem");
			series[2].Name.Should().Be("disk");
			series[3].Name.Should().Be("net");

			var firstSeries = series[0] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			var secondSeries = series[1] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			var thirdSeries = series[2] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			var fourthSeries = series[3] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;

			firstSeries.Should().NotBeNull();
			secondSeries.Should().NotBeNull();
			thirdSeries.Should().NotBeNull();
			fourthSeries.Should().NotBeNull();

			firstSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().Should().ContainSingle();
			secondSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().Should().ContainSingle();
			thirdSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().Should().ContainSingle();
			fourthSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().Should().ContainSingle();
		}

		[TestMethod]
		public void GraphViewModel_WithSingleExpressionFourGroups_ShouldCreateFourSeries()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5&MEM=10&DISK=15&NET=20"));

			var expression = "CPU=(?<cpu>\\d+)&MEM=(?<mem>\\d+)&DISK=(?<disk>\\d+)&NET=(?<net>\\d+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			var series = viewModel.Series.ToList();

			// Assert
			series.Should().HaveCount(4);
			series[0].Name.Should().Be("cpu");
			series[1].Name.Should().Be("mem");
			series[2].Name.Should().Be("disk");
			series[3].Name.Should().Be("net");

			var firstSeries = series[0] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			var secondSeries = series[1] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			var thirdSeries = series[2] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			var fourthSeries = series[3] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;

			firstSeries.Should().NotBeNull();
			secondSeries.Should().NotBeNull();
			thirdSeries.Should().NotBeNull();
			fourthSeries.Should().NotBeNull();

			firstSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().Should().ContainSingle();
			secondSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().Should().ContainSingle();
			thirdSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().Should().ContainSingle();
			fourthSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().Should().ContainSingle();
		}

		[TestMethod]
		public void GraphViewModel_WithMoreThanFourGroups_ShouldLimitToFourSeries()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "MEM=10"),
				new Record(3, DateTime.UtcNow.AddSeconds(2), SeverityType.Information, "DISK=15"),
				new Record(4, DateTime.UtcNow.AddSeconds(3), SeverityType.Information, "NET=20"),
				new Record(5, DateTime.UtcNow.AddSeconds(4), SeverityType.Information, "GPU=25"));

			// Five expressions, but should only create 4 series
			var expression = $"CPU=(?<cpu>\\d+){Constants.FilterOrOperator}MEM=(?<mem>\\d+){Constants.FilterOrOperator}DISK=(?<disk>\\d+){Constants.FilterOrOperator}NET=(?<net>\\d+){Constants.FilterOrOperator}GPU=(?<gpu>\\d+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			var series = viewModel.Series.ToList();

			// Assert
			series.Should().HaveCount(4);
			series[0].Name.Should().Be("cpu");
			series[1].Name.Should().Be("mem");
			series[2].Name.Should().Be("disk");
			series[3].Name.Should().Be("net");
		}

		[TestMethod]
		public void GraphViewModel_WithAllTrueValues_ShouldInterpretAsOne()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "IsEnabled=true"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "IsEnabled=true"),
				new Record(3, DateTime.UtcNow.AddSeconds(2), SeverityType.Information, "IsEnabled=true"));

			var expression = "IsEnabled=(?<IsEnabled>\\w+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			var series = viewModel.Series.ToList();

			// Assert
			series.Should().ContainSingle();
			var lineSeries = series[0] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			lineSeries.Should().NotBeNull();
			var points = lineSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().ToList();
			points.Should().HaveCount(3);
			points.All(p => p.Value == 1.0).Should().BeTrue();
		}

		[TestMethod]
		public void GraphViewModel_WithAllFalseValues_ShouldInterpretAsZero()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "IsEnabled=false"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "IsEnabled=false"),
				new Record(3, DateTime.UtcNow.AddSeconds(2), SeverityType.Information, "IsEnabled=false"));

			var expression = "IsEnabled=(?<IsEnabled>\\w+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			var series = viewModel.Series.ToList();

			// Assert
			series.Should().ContainSingle();
			var lineSeries = series[0] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			lineSeries.Should().NotBeNull();
			var points = lineSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().ToList();
			points.Should().HaveCount(3);
			points.All(p => p.Value == 0.0).Should().BeTrue();
		}

		[TestMethod]
		public void GraphViewModel_WithMixedBooleanValues_ShouldInterpretTrueAsOneAndFalseAsZero()
		{
			// Arrange
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "IsEnabled=true"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "IsEnabled=false"),
				new Record(3, DateTime.UtcNow.AddSeconds(2), SeverityType.Information, "IsEnabled=True"),
				new Record(4, DateTime.UtcNow.AddSeconds(3), SeverityType.Information, "IsEnabled=False"));

			var expression = "IsEnabled=(?<IsEnabled>\\w+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			var series = viewModel.Series.ToList();

			// Assert
			series.Should().ContainSingle();
			var lineSeries = series[0] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			lineSeries.Should().NotBeNull();
			var points = lineSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().ToList();
			points.Should().HaveCount(4);
			points[0].Value.Should().Be(1.0);
			points[1].Value.Should().Be(0.0);
			points[2].Value.Should().Be(1.0);
			points[3].Value.Should().Be(0.0);
		}

		[TestMethod]
		public void GraphViewModel_WithMixedBooleanAndNumericValues_ShouldNotConvertBooleans()
		{
			// Arrange: series has both boolean and numeric values - booleans should not be converted
			var records = ImmutableArray.Create<IRecord>(
				new Record(1, DateTime.UtcNow, SeverityType.Information, "IsEnabled=true"),
				new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "IsEnabled=5"));

			var expression = "IsEnabled=(?<IsEnabled>\\w+)";

			// Act
			var viewModel = new GraphViewModel(records, expression, "title", "source");
			var series = viewModel.Series.ToList();

			// Assert: only the numeric value (5) should produce a data point
			series.Should().ContainSingle();
			var lineSeries = series[0] as LineSeries<LiveChartsCore.Defaults.DateTimePoint>;
			lineSeries.Should().NotBeNull();
			var points = lineSeries!.Values.Cast<LiveChartsCore.Defaults.DateTimePoint>().ToList();
			points.Should().ContainSingle();
			points[0].Value.Should().Be(5.0);
		}
	}
}
