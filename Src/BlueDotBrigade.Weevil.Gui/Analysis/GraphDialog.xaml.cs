namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.ComponentModel;
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

		public static readonly DependencyProperty RegularExpressionProperty =
			DependencyProperty.Register(
				nameof(RegularExpression), typeof(string),
				typeof(GraphDialog));

		public static readonly DependencyProperty SampleRecordProperty =
			DependencyProperty.Register(
				nameof(SampleRecord), typeof(string),
				typeof(GraphDialog));

		public static readonly DependencyProperty DataDetectedProperty =
			DependencyProperty.Register(
				nameof(DataDetected), typeof(string),
				typeof(GraphDialog));

		public string RegularExpression
		{
			get => (string)GetValue(RegularExpressionProperty);
			set
			{
				SetValue(RegularExpressionProperty, value);
				RaisePropertyChanged(nameof(RegularExpression));
			}
		}

		public string SampleRecord
		{
			get => (string)GetValue(SampleRecordProperty);
			set
			{
				SetValue(SampleRecordProperty, value);
				RaisePropertyChanged(nameof(SampleRecord));
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
			this.RegularExpression = @"\.(?<Value>\d\d\d\d)";
			this.SampleRecord = records[0].Content;

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

		private void OnClick(object sender, RoutedEventArgs e)
		{
			Weevil.Filter.Expressions.Regular.RegularExpression expression =
				new RegularExpression(this.RegularExpression);

			var matches = expression.GetKeyValuePairs(_records[0]);

			if (matches.Any())
			{
				this.DataDetected = matches.First().Value;
			}
		}

		private void OnGraph(object sender, RoutedEventArgs e)
		{
			Weevil.Filter.Expressions.Regular.RegularExpression expression =
				new RegularExpression(this.RegularExpression);

			var values = new List<int>();

			foreach (var record in _records)
			{
				var matches = expression.GetKeyValuePairs(record);

				if (matches.Any())
				{
					values.Add(int.Parse(matches.First().Value) );
				}
			}

			this.Series = new ISeries[] { new LineSeries<int>
			{
				Name = "Series 1",
				Values = values
			} };
		}
	}
}
