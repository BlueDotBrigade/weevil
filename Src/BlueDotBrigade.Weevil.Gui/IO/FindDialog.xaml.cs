namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System.Windows;

	/// <summary>
	/// Interaction logic for FindDialog.xaml
	/// </summary>
	public partial class FindDialog : Window
	{
		public static readonly DependencyProperty UserInputProperty =
			 DependencyProperty.Register(
			 nameof(UserInput), typeof(string),
			 typeof(FindDialog),
			new FrameworkPropertyMetadata
			{
				BindsTwoWayByDefault = true,
			});

		public static readonly DependencyProperty FindNextProperty =
			DependencyProperty.Register(
				nameof(FindNext), typeof(bool),
				typeof(FindDialog),
				new FrameworkPropertyMetadata
				{
					BindsTwoWayByDefault = true,
				});

		public string UserInput
		{
			get => (string)GetValue(UserInputProperty);
			set => SetValue(UserInputProperty, value);
		}

		public bool FindNext
		{
			get => (bool)GetValue(FindNextProperty);
			set => SetValue(FindNextProperty, value);
		}

		public FindDialog()
		{
			this.UserInput = string.Empty;
			this.FindNext = true;

			InitializeComponent();
			this.DataContext = this;
		}

		private void OnPreviousClicked(object sender, RoutedEventArgs e)
		{
			this.FindNext = false;
			this.DialogResult = true;
		}

		private void OnNextClicked(object sender, RoutedEventArgs e)
		{
			this.FindNext = true;
			this.DialogResult = true;
		}
	}
}
