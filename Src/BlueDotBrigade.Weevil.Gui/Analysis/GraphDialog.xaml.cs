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
	using LiveChartsCore.SkiaSharpView;

	public partial class GraphDialog : Window, INotifyPropertyChanged
	{
		private static readonly string DefaultXAxisLabel = "X-Axis";
		private static readonly string DefaultYAxisLabel = "Y-Axis";
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
		private ISeries[] _series;
		private Axis[] _xAxes;
		private Axis[] _yAxes;

		public GraphDialog(ImmutableArray<IRecord> records)
		{
			_records = records;
			this.PatternSelected = @"\.(?<Value>\d\d\d\d)";
			this.SampleData = records[0].Content;

			LiveCharts.Configure(
				settings => settings
					.AddDefaultMappers()
					.AddSkiaSharp()
					.AddDarkTheme());

			var values1 = new int[50];
			var values2 = new int[50];
			var r = new Random();
			var t = 0;
			var t2 = 0;

			for (var i = 0; i < 50; i++)
			{
				t += r.Next(-90, 100);
				values1[i] = t;

				t2 += r.Next(-90, 100);
				values2[i] = t2;
			}

			this.Series = new ISeries[] { new LineSeries<int> { Values = values1 } };
			//SeriesCollection2 = new ISeries[] { new ColumnSeries<int> { Values = values2 } };

			XAxes = new Axis[]
			{
				new Axis
				{
					Name = DefaultXAxisLabel,
					Labeler = value => new DateTime((long)value).ToString("hh:mm:ss"),
					LabelsRotation = 15,
				}
			};

			YAxes = new Axis[]
			{
				new Axis
				{
					Name = DefaultYAxisLabel,
				}
			};

			this.DataContext = this;

			InitializeComponent();
		}

		public string XAxisLabel
		{
			get
			{
				if (this.XAxes?.Length > 0)
				{
					return this.XAxes[0].Name;
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
					},
				};
			}
		}

		public string YAxisLabel
		{
			get
			{
				if (this.YAxes?.Length > 0)
				{
					return this.YAxes[0].Name;
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
					new Axis
					{
						Name = value,
					},
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

		public ISeries[] Series
		{
			get => _series;
			set
			{
				_series = value;
				RaisePropertyChanged(nameof(Series));
			}
		}

		public Axis[] XAxes
		{
			get => _xAxes;
			set
			{
				_xAxes = value;
				RaisePropertyChanged(nameof(XAxes));
			}
		}

		public Axis[] YAxes
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
			var expression = new RegularExpression(this.PatternSelected);

			var values = new ObservableCollection<DateTimePoint>();

			var parsingError = false;

			foreach (var record in _records)
			{
				var matches = expression.GetKeyValuePairs(record);

				if (matches.Any())
				{
					if (float.TryParse(matches.First().Value, ValidNumberStyle, CultureInfo.InvariantCulture, out var value))
					{
						values.Add(new DateTimePoint(record.CreatedAt, value));
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

			this.Series = new ISeries[] { new LineSeries<DateTimePoint>
			{
				Name = "Series 1",
				Values = values,
				GeometrySize = 10,
			} };
		}
	}
}
