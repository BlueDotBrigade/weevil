namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System.ComponentModel;
	using System.Windows;

	/// <summary>
	/// Interaction logic for AnalysisDialog.xaml
	/// </summary>
	public partial class AnalysisDialog : Window, INotifyPropertyChanged
	{
		public static readonly DependencyProperty RegularExpressionProperty =
			DependencyProperty.Register(
				nameof(RegularExpression), typeof(string),
				typeof(AnalysisDialog),
				new FrameworkPropertyMetadata
				{
					BindsTwoWayByDefault = true,
				});

		public static readonly DependencyProperty RecordsDescriptionProperty =
			DependencyProperty.Register(
				nameof(RecordsDescription), typeof(string),
				typeof(AnalysisDialog));

		public string RegularExpression
		{
			get => (string)GetValue(RegularExpressionProperty);
			set => SetValue(RegularExpressionProperty, value);
		}

		public string RecordsDescription
		{
			get => (string)GetValue(RecordsDescriptionProperty);
			set => SetValue(RecordsDescriptionProperty, value);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void RaisePropertyChanged(string name)
		{
			var handler = this.PropertyChanged;

			if (handler != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}

		public AnalysisDialog()
		{
			this.Owner = System.Windows.Application.Current.MainWindow;
			this.Loaded += OnDialogLoaded;
			InitializeComponent();
			this.DataContext = this;
		}

		private void OnDialogLoaded(object sender, RoutedEventArgs e)
		{
			this.RegexTextBox.SelectAll();
		}

		private void OnAnalyzeClicked(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}

		private void OnCancelClicked(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}
	}
}
