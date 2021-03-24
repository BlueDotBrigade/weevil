namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System;
	using System.Collections.Immutable;
	using System.Windows;
	using BlueDotBrigade.Weevil.Analysis;

	/// <summary>
	/// Interaction logic for DashboardDialog.xaml
	/// </summary>
	public partial class DashboardDialog : Window
	{
		public static readonly DependencyProperty InsightsProperty =
			DependencyProperty.Register(
				nameof(Insights), typeof(IInsight[]),
				typeof(DashboardDialog));

		public static readonly DependencyProperty EngineProperty =
			DependencyProperty.Register(
				nameof(Engine), typeof(IEngine),
				typeof(DashboardDialog));

		public IInsight[] Insights
		{
			get => (IInsight[])GetValue(InsightsProperty);
			set => SetValue(InsightsProperty, value);
		}

		public IEngine Engine
		{
			get => (IEngine)GetValue(EngineProperty);
			set => SetValue(EngineProperty, value);
		}

		public DashboardDialog()
		{
			InitializeComponent();
			this.DataContext = this;
		}

		private void OnRefresh(object sender, RoutedEventArgs e)
		{
			foreach (IInsight insight in this.Insights)
			{
				insight.Refresh(Engine.Records);
			}
		}
	}
}
