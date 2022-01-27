namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
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
	using LiveChartsCore;
	using LiveChartsCore.SkiaSharpView;
	using LiveChartsCore.Themes;

	/// <summary>
	/// Interaction logic for GraphDialog.xaml
	/// </summary>
	public partial class GraphDialog : Window
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

		public GraphDialog()
		{
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

		public ISeries[] Series { get; set; }
		//public ISeries[] SeriesCollection2 { get; set; }
		public Axis[] XAxes { get; set; }
		public Axis[] YAxes { get; set; }
	}
}
