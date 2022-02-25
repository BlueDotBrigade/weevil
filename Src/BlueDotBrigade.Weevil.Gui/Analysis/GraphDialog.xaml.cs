namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System.Collections.Immutable;
	using System.ComponentModel;
	using System.Runtime.CompilerServices;
	using System.Windows;
	using BlueDotBrigade.Weevil.Data;

	public partial class GraphDialog : Window, INotifyPropertyChanged
	{
		public GraphDialog()
		{
			InitializeComponent();
		}

		public GraphDialog(ImmutableArray<IRecord> records, string regExPattern)
		{
			InitializeComponent();
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
		private void OnDetectData(object sender, RoutedEventArgs e)
		{
			
		}

		private void OnUpdate(object sender, RoutedEventArgs e)
		{
		}
			// THIS VERSION WORKS
			// https://github.com/beto-rodriguez/LiveCharts2/blob/92578602760fa5089ff2f638e52b3508ce57c6b2/samples/WPFSample/Axes/DateTimeScaled/View.xaml
			// https://github.com/beto-rodriguez/LiveCharts2/blob/92578602760fa5089ff2f638e52b3508ce57c6b2/samples/ViewModelsSamples/Axes/DateTimeScaled/ViewModel.cs

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
			
	}
}
