namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Windows;

	/// <summary>
	/// Interaction logic for FindDialog.xaml
	/// </summary>
	public partial class FindDialog : Window
	{
		public static readonly DependencyProperty IsCaseSensitiveProperty =
			DependencyProperty.Register(
				nameof(IsCaseSensitive), typeof(bool),
				typeof(FindDialog),
				new FrameworkPropertyMetadata
				{
					BindsTwoWayByDefault = true,
				});

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

		public static readonly DependencyProperty IsRegexModeProperty =
			DependencyProperty.Register(
				nameof(IsRegexMode), typeof(bool),
				typeof(FindDialog),
				new FrameworkPropertyMetadata
				{
					BindsTwoWayByDefault = true,
				});

		public static readonly DependencyProperty IsPlainTextModeProperty =
			DependencyProperty.Register(
				nameof(IsPlainTextMode), typeof(bool),
				typeof(FindDialog),
				new FrameworkPropertyMetadata
				{
					BindsTwoWayByDefault = true,
				});

		public bool IsCaseSensitive
		{
			get => (bool)GetValue(IsCaseSensitiveProperty);
			set => SetValue(IsCaseSensitiveProperty, value);
		}

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

		public bool IsRegexMode
		{
			get => (bool)GetValue(IsRegexModeProperty);
			set => SetValue(IsRegexModeProperty, value);
		}

		public bool IsPlainTextMode
		{
			get => (bool)GetValue(IsPlainTextModeProperty);
			set => SetValue(IsPlainTextModeProperty, value);
		}

		public FindDialog(string defaultValue)
		{
			this.Owner = Application.Current.MainWindow;

			this.Loaded += OnDialogLoaded;

			this.IsCaseSensitive = false;
			this.UserInput = defaultValue ?? string.Empty;
			this.FindNext = true;
			this.IsPlainTextMode = true; // Default to Plain Text mode
			this.IsRegexMode = false;

			InitializeComponent();
			this.DataContext = this;
		}

		private void OnDialogLoaded(object sender, RoutedEventArgs e)
		{
			this.InputTextBox.SelectAll();
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
