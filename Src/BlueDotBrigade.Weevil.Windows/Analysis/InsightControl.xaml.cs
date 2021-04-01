namespace BlueDotBrigade.Weevil.Windows.Analysis
{
	using System.Windows;
	using System.Windows.Controls;
	using BlueDotBrigade.Weevil.Analysis;

	/// <summary>
	/// Interaction logic for InsightControl.xaml
	/// </summary>
	public partial class InsightControl : UserControl
	{
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register(
				nameof(Value),
				typeof(IInsight),
				typeof(InsightControl));

		public IInsight Value
		{
			get => (IInsight)GetValue(ValueProperty);
			set => SetValue(ValueProperty, (object) value);
		}

		public InsightControl()
		{
			InitializeComponent();
		}
	}
}
