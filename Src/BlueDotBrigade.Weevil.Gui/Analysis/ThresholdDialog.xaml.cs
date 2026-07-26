namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System.Windows;

	public partial class ThresholdDialog : Window
	{
		public static readonly DependencyProperty RegularExpressionProperty =
			DependencyProperty.Register(
				nameof(RegularExpression),
				typeof(string),
				typeof(ThresholdDialog),
				new FrameworkPropertyMetadata
				{
					BindsTwoWayByDefault = true
				});

		public static readonly DependencyProperty ThresholdValueProperty =
			DependencyProperty.Register(
				nameof(ThresholdValue),
				typeof(string),
				typeof(ThresholdDialog),
				new FrameworkPropertyMetadata
				{
					BindsTwoWayByDefault = true
				});

		public static readonly DependencyProperty ComparisonProperty =
			DependencyProperty.Register(
				nameof(Comparison),
				typeof(string),
				typeof(ThresholdDialog),
				new FrameworkPropertyMetadata
				{
					BindsTwoWayByDefault = true
				});

		public static readonly DependencyProperty RecordsDescriptionProperty =
			DependencyProperty.Register(
				nameof(RecordsDescription),
				typeof(string),
				typeof(ThresholdDialog));

		public string RegularExpression
		{
			get => (string)GetValue(RegularExpressionProperty);
			set => SetValue(RegularExpressionProperty, value);
		}

		public string ThresholdValue
		{
			get => (string)GetValue(ThresholdValueProperty);
			set => SetValue(ThresholdValueProperty, value);
		}

		public string Comparison
		{
			get => (string)GetValue(ComparisonProperty);
			set => SetValue(ComparisonProperty, value);
		}

		public string RecordsDescription
		{
			get => (string)GetValue(RecordsDescriptionProperty);
			set => SetValue(RecordsDescriptionProperty, value);
		}

		public ThresholdDialog()
		{
			this.Owner = Application.Current?.MainWindow;
			this.Loaded += OnDialogLoaded;
			InitializeComponent();
			this.DataContext = this;
		}

		private void OnDialogLoaded(object sender, RoutedEventArgs e)
		{
			RegexTextBox.SelectAll();
		}

		private void OnAnalyzeClicked(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		private void OnCancelClicked(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}
	}
}
