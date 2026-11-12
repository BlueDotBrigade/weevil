namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Windows;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// Weevil's Open As window: RAM or Disk for the log about to open, with the suggested mode already chosen (#1028).
	/// </summary>
	/// <remarks>
	/// It only shows what <see cref="OpenAsAdvice"/> works out, so the suggestion and the warnings are tested without a window.
	/// </remarks>
	public partial class OpenAsDialog : Window
	{
		public static readonly DependencyProperty IsRamChosenProperty =
			DependencyProperty.Register(
				nameof(IsRamChosen),
				typeof(bool),
				typeof(OpenAsDialog),
				new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnChoiceChanged));

		public static readonly DependencyProperty IsDiskChosenProperty =
			DependencyProperty.Register(
				nameof(IsDiskChosen),
				typeof(bool),
				typeof(OpenAsDialog),
				new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnChoiceChanged));

		public static readonly DependencyProperty WarningProperty =
			DependencyProperty.Register(
				nameof(Warning),
				typeof(string),
				typeof(OpenAsDialog),
				new PropertyMetadata(string.Empty));

		private readonly OpenAsAdvice _advice;

		internal OpenAsDialog(OpenAsAdvice advice)
		{
			_advice = advice ?? throw new ArgumentNullException(nameof(advice));

			this.Owner = Application.Current.MainWindow;

			InitializeComponent();

			this.IsRamChosen = advice.DefaultMode == StorageMode.Ram;
			this.IsDiskChosen = !this.IsRamChosen;
			this.Warning = advice.GetWarning(this.StorageMode);

			this.DataContext = this;
		}

		public string FileDescription => _advice.FileDescription;

		public string RamDescription => _advice.RamDescription;

		public string DiskDescription => OpenAsAdvice.DiskDescription;

		public bool IsRamChosen
		{
			get => (bool)GetValue(IsRamChosenProperty);
			set => SetValue(IsRamChosenProperty, value);
		}

		public bool IsDiskChosen
		{
			get => (bool)GetValue(IsDiskChosenProperty);
			set => SetValue(IsDiskChosenProperty, value);
		}

		/// <summary>
		/// The warning for the mode chosen, or an empty string. It never stops the open.
		/// </summary>
		public string Warning
		{
			get => (string)GetValue(WarningProperty);
			set => SetValue(WarningProperty, value);
		}

		internal StorageMode StorageMode => this.IsDiskChosen ? StorageMode.Disk : StorageMode.Ram;

		private static void OnChoiceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is OpenAsDialog dialog && dialog._advice != null)
			{
				dialog.Warning = dialog._advice.GetWarning(dialog.StorageMode);
			}
		}

		private void OnOpenClicked(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}
	}
}
