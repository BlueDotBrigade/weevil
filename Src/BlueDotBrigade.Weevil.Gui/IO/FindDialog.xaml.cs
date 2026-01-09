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

		public static readonly DependencyProperty SearchElapsedTimeProperty =
			DependencyProperty.Register(
				nameof(SearchElapsedTime), typeof(bool),
				typeof(FindDialog),
				new FrameworkPropertyMetadata
				{
					BindsTwoWayByDefault = true,
				});

		public static readonly DependencyProperty MinElapsedTimeMsProperty =
			DependencyProperty.Register(
				nameof(MinElapsedTimeMs), typeof(string),
				typeof(FindDialog),
				new FrameworkPropertyMetadata
				{
					BindsTwoWayByDefault = true,
				});

		public static readonly DependencyProperty MaxElapsedTimeMsProperty =
			DependencyProperty.Register(
				nameof(MaxElapsedTimeMs), typeof(string),
				typeof(FindDialog),
				new FrameworkPropertyMetadata
				{
					BindsTwoWayByDefault = true,
				});

		public static readonly DependencyProperty SearchCommentsProperty =
			DependencyProperty.Register(
				nameof(SearchComments), typeof(bool),
				typeof(FindDialog),
				new FrameworkPropertyMetadata
				{
					BindsTwoWayByDefault = true,
					PropertyChangedCallback = OnSearchCommentsChanged
				});

		public static readonly DependencyProperty SearchContentProperty =
			DependencyProperty.Register(
				nameof(SearchContent), typeof(bool),
				typeof(FindDialog),
				new FrameworkPropertyMetadata
				{
					BindsTwoWayByDefault = true,
					PropertyChangedCallback = OnSearchContentChanged
				});

		private static void OnSearchCommentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var dialog = (FindDialog)d;
			if ((bool)e.NewValue)
			{
				dialog.SearchContent = false;
			}
		}

		private static void OnSearchContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var dialog = (FindDialog)d;
			if ((bool)e.NewValue)
			{
				dialog.SearchComments = false;
			}
		}

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

		public bool SearchElapsedTime
		{
			get => (bool)GetValue(SearchElapsedTimeProperty);
			set => SetValue(SearchElapsedTimeProperty, value);
		}

		public string MinElapsedTimeMs
		{
			get => (string)GetValue(MinElapsedTimeMsProperty);
			set => SetValue(MinElapsedTimeMsProperty, value);
		}

		public string MaxElapsedTimeMs
		{
			get => (string)GetValue(MaxElapsedTimeMsProperty);
			set => SetValue(MaxElapsedTimeMsProperty, value);
		}

		public bool SearchComments
		{
			get => (bool)GetValue(SearchCommentsProperty);
			set => SetValue(SearchCommentsProperty, value);
		}

		public bool SearchContent
		{
			get => (bool)GetValue(SearchContentProperty);
			set => SetValue(SearchContentProperty, value);
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
			this.SearchElapsedTime = false;
			this.SearchComments = false;
			this.SearchContent = true; // Default to searching content
			this.MinElapsedTimeMs = string.Empty;
			this.MaxElapsedTimeMs = string.Empty;

			InitializeComponent();
			this.DataContext = this;
		}

		private void OnDialogLoaded(object sender, RoutedEventArgs e)
		{
			this.InputTextBox.SelectAll();
		}

		private void OnPreviousTextClicked(object sender, RoutedEventArgs e)
		{
			this.FindNext = false;
			this.SearchElapsedTime = false;
			// SearchComments property is already set by the radio button
			this.DialogResult = true;
		}

		private void OnNextTextClicked(object sender, RoutedEventArgs e)
		{
			this.FindNext = true;
			this.SearchElapsedTime = false;
			// SearchComments property is already set by the radio button
			this.DialogResult = true;
		}

		private void OnPreviousElapsedClicked(object sender, RoutedEventArgs e)
		{
			this.FindNext = false;
			this.SearchElapsedTime = true;
			this.DialogResult = true;
		}

		private void OnNextElapsedClicked(object sender, RoutedEventArgs e)
		{
			this.FindNext = true;
			this.SearchElapsedTime = true;
			this.DialogResult = true;
		}
	}
}
