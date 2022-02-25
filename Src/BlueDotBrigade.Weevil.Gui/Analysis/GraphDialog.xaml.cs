namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Globalization;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Windows;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter.Expressions.Regular;
	using LiveChartsCore;
	using LiveChartsCore.Defaults;
	using LiveChartsCore.Kernel.Sketches;
	using LiveChartsCore.SkiaSharpView;

	public partial class GraphDialog : Window, INotifyPropertyChanged
	{
		private static readonly string DefaultXAxisLabel = "Time";
		private static readonly string DefaultYAxisLabel = "Handle Count";

		private static readonly NumberStyles ValidNumberStyle =
			NumberStyles.AllowLeadingWhite |
			NumberStyles.AllowTrailingWhite |
			NumberStyles.AllowThousands |
			NumberStyles.Integer |
			NumberStyles.AllowExponent |
			NumberStyles.AllowDecimalPoint;

		public static readonly DependencyProperty PatternSelectedProperty =
			DependencyProperty.Register(
				nameof(PatternSelected), typeof(string),
				typeof(GraphDialog));

		public static readonly DependencyProperty PatternOptionsProperty =
			DependencyProperty.Register(
				nameof(PatternOptions), typeof(IList<string>),
				typeof(GraphDialog));

		public static readonly DependencyProperty SampleDataProperty =
			DependencyProperty.Register(
				nameof(SampleData), typeof(string),
				typeof(GraphDialog));

		public static readonly DependencyProperty DataDetectedProperty =
			DependencyProperty.Register(
				nameof(DataDetected), typeof(string),
				typeof(GraphDialog));

		private readonly ImmutableArray<IRecord> _records;
		private IEnumerable<ISeries> _series;
		private IEnumerable<ICartesianAxis> _xAxes;
		private IEnumerable<ICartesianAxis> _yAxes;

		public GraphDialog(ImmutableArray<IRecord> records, string regExPattern)
		{
			_records = records;
			this.PatternSelected = @"\.(?<Value>\d\d\d\d)";
			this.SampleData = records[0].Content;

			LiveCharts.Configure(
				settings => settings
					.AddDefaultMappers()
					.AddSkiaSharp()
					.AddDarkTheme());

			this.XAxisLabel = DefaultXAxisLabel;
			this.YAxisLabel = DefaultYAxisLabel;

			this.PatternSelected = regExPattern;
			this.Series = GetSeries(_records, new RegularExpression(this.PatternSelected));

			this.DataContext = this;

			InitializeComponent();
		}

		public string XAxisLabel
		{
			get
			{
				if (this.XAxes?.Count() > 0)
				{
					return this.XAxes.First().Name;
				}
				else
				{
					return DefaultXAxisLabel;
				}
			}
			set
			{
				// You cannot simply update the Axis name
				// ... LiveGraph only detects changes when the entire Axis is replaced.
				// ... Be mindful of any settings that might be lost. 
				this.XAxes = new Axis[]
				{
					new Axis
					{
						Name = value,
						Labeler = point => new DateTime((long)point).ToString("hh:mm:ss"),
						LabelsRotation = 15,
						//UnitWidth = TimeSpan.FromMinutes(15).Ticks,
						//MinStep = TimeSpan.FromDays(1).Ticks // mark
					}
				};
			}
		}

		public string YAxisLabel
		{
			get
			{
				if (this.YAxes?.Count() > 0)
				{
					return this.YAxes.First().Name;
				}
				else
				{
					return DefaultYAxisLabel;
				}
			}
			set
			{
				// You cannot simply update the Axis name
				// ... LiveGraph only detects changes when the entire Axis is replaced.
				// ... Be mindful of any settings that might be lost. 
				this.YAxes = new Axis[]
				{
					new Axis { Name = value, },
				};
			}
		}

		public string PatternSelected
		{
			get => (string)GetValue(PatternSelectedProperty);
			set
			{
				value = value ?? string.Empty;

				SetValue(PatternSelectedProperty, value);
				RaisePropertyChanged(nameof(this.PatternSelected));
			}
		}

		public IList<string> PatternOptions
		{
			get => (IList<string>)GetValue(PatternOptionsProperty);
			set
			{
				value = value ?? new List<string>();

				SetValue(PatternOptionsProperty, value);
				RaisePropertyChanged(nameof(this.PatternOptions));
			}
		}

		public string SampleData
		{
			get => (string)GetValue(SampleDataProperty);
			set
			{
				SetValue(SampleDataProperty, value);
				RaisePropertyChanged(nameof(SampleData));
			}
		}


		public string DataDetected
		{
			get => (string)GetValue(DataDetectedProperty);
			private set
			{
				SetValue(DataDetectedProperty, value);
				RaisePropertyChanged(nameof(DataDetected));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChangedEventHandler handler = PropertyChanged;

			if (handler != null)
			{
				handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public IEnumerable<ISeries> Series
		{
			get => _series;
			set
			{
				_series = value;
				RaisePropertyChanged(nameof(Series));
			}
		}

		public IEnumerable<ICartesianAxis> XAxes
		{
			get => _xAxes;
			set
			{
				_xAxes = value;
				RaisePropertyChanged(nameof(XAxes));
			}
		}

		public IEnumerable<ICartesianAxis> YAxes
		{
			get => _yAxes;
			set
			{
				_yAxes = value;
				RaisePropertyChanged(nameof(YAxes));
			}
		}

		private void OnDetectData(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(this.PatternSelected))
			{
				this.DataDetected = "(missing regular expression)";
			}
			else
			{
				if (string.IsNullOrEmpty(this.SampleData))
				{
					this.DataDetected = "(missing sample data)";
				}
				else
				{
					var expression = new RegularExpression(this.PatternSelected);

					var matches = expression.GetKeyValuePairs(this.SampleData);

					if (matches.Any())
					{
						this.DataDetected = matches.First().Value;
					}
				}
			}
		}

		private void OnUpdate(object sender, RoutedEventArgs e)
		{
			this.Series = GetSeries(_records, new RegularExpression(this.PatternSelected));
		}

		private static ISeries[] GetSeries(ImmutableArray<IRecord> records, RegularExpression expression)
		{
			//var values = new ObservableCollection<DateTimePoint>();
			var values = new ObservableCollection<float>();

			var parsingError = false;

			foreach (var record in records)
			{
				var matches = expression.GetKeyValuePairs(record);

				if (matches.Any())
				{
					if (float.TryParse(matches.First().Value, ValidNumberStyle, CultureInfo.InvariantCulture,
							out var value))
					{
						//values.Add(new DateTimePoint(record.CreatedAt, value));
						values.Add(value);
					}
					else
					{
						if (!parsingError)
						{
							Log.Default.Write(
								LogSeverityType.Warning,
								$"Unable to graph the datapoint because the matching value is not a float. {matches.First().Value}");
							parsingError = true;
						}
					}

				}
			}

			return new ISeries[]
			{
				// TOOLTIP displays when mouse over point for `float`, but not for `DateTimePoint`
				//new LineSeries<DateTimePoint>
				new LineSeries<float>
				{
					Name = "SeriesOne", 
					Values = values,
					//GeometrySize = 10,
					TooltipLabelFormatter = (chartPoint) => $"TOOLTIP: {chartPoint.Context.Series.Name}: {chartPoint.PrimaryValue}",
					//DataLabelsFormatter = (chartPoint) => $"{chartPoint.Context.Series.Name}: {chartPoint.PrimaryValue}",
				}
			};

			// THIS VERSION WORKS

			// Configuration example
			// https://github.com/beto-rodriguez/LiveCharts2/issues/303
			//
			// Specifying target frameworks
			// https://docs.microsoft.com/en-us/dotnet/standard/frameworks#how-to-specify-a-target-framework

			// https://github.com/beto-rodriguez/LiveCharts2/blob/master/samples/WPFSample/General/TemplatedTooltips/View.xaml
			// https://github.com/beto-rodriguez/LiveCharts2/blob/a343a3b12445b05fa1c2b19a4a5f4c353a6d4e6d/docs/cartesianChart/tooltips.md
			// https://github.com/Live-Charts/Live-Charts/blob/master/Examples/Wpf/CartesianChart/DateAxis/DateAxisExample.xaml
			// https://github.com/beto-rodriguez/LiveCharts2/blob/92578602760fa5089ff2f638e52b3508ce57c6b2/samples/ViewModelsSamples/Axes/DateTimeScaled/ViewModel.cs
			// https://github.com/beto-rodriguez/LiveCharts2/blob/92578602760fa5089ff2f638e52b3508ce57c6b2/src/LiveChartsCore/Kernel/LiveChartsSettings.cs
			// https://github.com/beto-rodriguez/LiveCharts2/blob/87045ed72c8ce3b22f885a7c5f22fa4b5c061ac5/samples/WPFSample/General/TemplatedTooltips/View.xaml
		}
	}
}
