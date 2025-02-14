namespace BlueDotBrigade.Weevil.Gui.Navigation
{
	using System;
	using System.Windows;

	/// <summary>
	/// Interaction logic for UserPromptDialog.xaml
	/// </summary>
	public partial class GoToDialog : Window
	{
		public static readonly DependencyProperty UserPromptProperty =
			 DependencyProperty.Register(
			 nameof(UserPrompt), typeof(string),
			 typeof(GoToDialog));

		public static readonly DependencyProperty UserInputProperty =
			 DependencyProperty.Register(
			 nameof(UserInput), typeof(string),
			 typeof(GoToDialog),
			new FrameworkPropertyMetadata
			{
				BindsTwoWayByDefault = true,
			});

		public string UserPrompt
		{
			get => (string)GetValue(UserPromptProperty);
			set => SetValue(UserPromptProperty, value);
		}

		public string UserInput
		{
			get => (string)GetValue(UserInputProperty);
			set => SetValue(UserInputProperty, value);
		}

		public GoToDialog()
		{
			this.Owner = Application.Current.MainWindow;

			this.Loaded += OnDialogLoaded;

			InitializeComponent();
			this.DataContext = this;
		}

		private void OnDialogLoaded(object sender, RoutedEventArgs e)
		{
			this.InputTextBox.SelectAll();
		}

		private void OnOkClicked(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}
	}
}
