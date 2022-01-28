namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.ComponentModel;
	using System.Diagnostics.Eventing.Reader;
	using System.Globalization;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Documents;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using System.Windows.Shapes;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter.Expressions.Regular;
	using LiveChartsCore;
	using LiveChartsCore.SkiaSharpView;
	using LiveChartsCore.Themes;
	
	/// <summary>
	/// Interaction logic for GraphDialog.xaml
	/// </summary>
	public partial class GraphDialog : Window, INotifyPropertyChanged
	{
		// https://github.com/beto-rodriguez/LiveCharts2/blob/92578602760fa5089ff2f638e52b3508ce57c6b2/samples/ViewModelsSamples/Axes/Shared/ViewModel.cs

		//public static readonly DependencyProperty XAxesProperty =
		//	DependencyProperty.Register(
		//		nameof(XAxes), typeof(string),
		//		typeof(GraphDialog));

		//public static readonly DependencyProperty YAxesProperty =
		//	DependencyProperty.Register(
		//		nameof(YAxes), typeof(string),
		//		typeof(GraphDialog));

		//public string XAxes
		//{
		//	get => (string)GetValue(XAxesProperty);
		//	set => SetValue(XAxesProperty, value);
		//}

		//public string YAxes
		//{
		//	get => (string)GetValue(YAxesProperty);
		//	set => SetValue(YAxesProperty, value);
		//}

		public static readonly DependencyProperty PatternProperty =
			DependencyProperty.Register(
				nameof(Pattern), typeof(string),
				typeof(GraphDialog));

		public static readonly DependencyProperty SampleDataProperty =
			DependencyProperty.Register(
				nameof(SampleData), typeof(string),
				typeof(GraphDialog));

		public static readonly DependencyProperty DataDetectedProperty =
			DependencyProperty.Register(
				nameof(DataDetected), typeof(string),
				typeof(GraphDialog));

		public string Pattern
		{
			get => (string)GetValue(PatternProperty);
			set
			{
				value = value ?? string.Empty;

				SetValue(PatternProperty, value);
				RaisePropertyChanged(nameof(this.Pattern));
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
		
		private readonly ImmutableArray<IRecord> _records;
		private ISeries[] _series;
		private Axis[] _xAxes;
		private Axis[] _yAxes;

		public GraphDialog(ImmutableArray<IRecord> records)
		{
			_records = records;
			this.Pattern = @"\.(?<Value>\d\d\d\d)";
			this.SampleData = records[0].Content;

			//this.XAxes = "X-Axis";
			//this.YAxes = "Y-Axis";

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

			XAxes = new Axis[] { new Axis() };
			YAxes = new Axis[] { new Axis() };

			XAxes[0].Name = "X-Axis";
			YAxes[0].Name = "Y-Axis";
			this.DataContext = this;

			InitializeComponent();
		}

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

		//public ISeries[] SeriesCollection2 { get; set; }
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
			if (string.IsNullOrEmpty(this.Pattern))
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
					var expression = new RegularExpression(this.Pattern);

					var matches = expression.GetKeyValuePairs(this.SampleData);

					if (matches.Any())
					{
						this.DataDetected = matches.First().Value;
					}
				}
			}
		}

		private void OnGraph(object sender, RoutedEventArgs e)
		{
			var expression = new RegularExpression(this.Pattern);

			var values = new List<float>();

			var parsingError = false;

			var validNumberFormat =
				NumberStyles.AllowLeadingWhite |
				NumberStyles.AllowTrailingWhite |
				NumberStyles.AllowThousands |
				NumberStyles.Integer |
				NumberStyles.AllowExponent |
				NumberStyles.AllowDecimalPoint;

			foreach (var record in _records)
			{
				var matches = expression.GetKeyValuePairs(record);

				if (matches.Any())
				{
					if (float.TryParse(matches.First().Value, validNumberFormat, CultureInfo.InvariantCulture, out var value))
					{
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

			this.Series = new ISeries[] { new LineSeries<float>
			{
				Name = "Series 1",
				Values = values,
				GeometrySize = 10,
			} };
		}
	}
}
