namespace BlueDotBrigade.Weevil.Windows.Analysis
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;
	using BlueDotBrigade.Weevil.Analysis;

	public partial class InsightControl : UserControl
	{
		public static readonly DependencyProperty ImageProperty =
			DependencyProperty.Register(
				nameof(Image),
				typeof(ImageSource),
				typeof(InsightControl));

		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register(
				nameof(Value),
				typeof(IInsight),
				typeof(InsightControl));

		public ImageSource Image
		{
			get => (ImageSource)GetValue(ImageProperty);
			set => SetValue(ImageProperty, (object)value);
		}

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
