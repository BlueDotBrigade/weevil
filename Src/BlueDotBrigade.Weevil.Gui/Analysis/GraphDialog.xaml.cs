namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.ComponentModel;
	using System.Runtime.CompilerServices;
	using System.Windows;
	using BlueDotBrigade.Weevil.Data;
	using LiveChartsCore;
	using LiveChartsCore.SkiaSharpView;

	public partial class GraphDialog : Window
	{
		private readonly GraphViewModel _viewModel;

		public GraphDialog(ImmutableArray<IRecord> records, string regExPattern)
		{
			LiveCharts.Configure(
				settings => settings
					.AddDefaultMappers()
					.AddSkiaSharp()
					.AddDarkTheme());

			_viewModel = new GraphViewModel(records, regExPattern);
			this.DataContext = _viewModel;

			InitializeComponent();
		}
		
		
		private void OnDetectData(object sender, RoutedEventArgs e)
		{
			//if (string.IsNullOrEmpty(this.PatternSelected))
			//{
			//	this.DataDetected = "(missing regular expression)";
			//}
			//else
			//{
			//	if (string.IsNullOrEmpty(this.SampleData))
			//	{
			//		this.DataDetected = "(missing sample data)";
			//	}
			//	else
			//	{
			//		var expression = new RegularExpression(this.PatternSelected);

			//		var matches = expression.GetKeyValuePairs(this.SampleData);

			//		if (matches.Any())
			//		{
			//			this.DataDetected = matches.First().Value;
			//		}
			//	}
			//}
		}

		private void OnUpdate(object sender, RoutedEventArgs e)
		{
		}
	}
}
