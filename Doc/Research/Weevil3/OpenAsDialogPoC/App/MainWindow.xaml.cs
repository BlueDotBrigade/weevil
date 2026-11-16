namespace OpenAsDialogPoC
{
	using System;
	using System.Collections.Generic;
	using System.Windows;
	using System.Runtime.InteropServices;
	using System.Windows.Input;
	using System.Windows.Interop;
	using System.Windows.Media;
	using BlueDotBrigade.Windows.Dialogs;
	using BlueDotBrigade.Windows.Dialogs.Storage;

	/// <summary>
	/// A host for the generic Open As dialog, as Weevil would use it (#1157).
	/// </summary>
	public partial class MainWindow : Window
	{
		private static readonly FileTypeFilter LogFiles = new FileTypeFilter(
			"Log files (*.log, *.log.1, *.txt, *.tsv, *.csv)",
			new[] { "*.log", "*.log.*", "*.txt", "*.tsv", "*.csv" },
			new[] { "*.xml", "*.idx" });

		private readonly ResourceDictionary _darkTheme = new ResourceDictionary { Source = new Uri("/OpenAsDialogPoC;component/DarkTheme.xaml", UriKind.Relative) };
		private string _lastFolder;
		private IReadOnlyList<string> _recentFolders = Array.Empty<string>();

		public MainWindow()
		{
			InitializeComponent();
		}

		private void OnOpen(object sender, ExecutedRoutedEventArgs e)
		{
			var dialog = new OpenAsDialog
			{
				Owner = this,
				Title = "Open a log",
				FileTypes = new[] { LogFiles, FileTypeFilter.AllFiles },
				CompanionSuffixes = new[] { ".xml", ".idx" },
				InitialLocation = _lastFolder,
				RecentFolders = _recentFolders,
				CanOpen = false,
			};

			var options = new LogOptionsModel(dialog.Browser.Reader);
			dialog.OptionsContent = new LogOptions { DataContext = options };
			dialog.SelectionChanged += (_, item) => options.Select(item is { IsFile: true } ? item : null);
			options.PropertyChanged += (_, args) =>
			{
				if (args.PropertyName == nameof(LogOptionsModel.CanOpen))
				{
					dialog.CanOpen = options.CanOpen;
				}
			};

			ApplyTheme(dialog);

			if (dialog.ShowDialog() == true)
			{
				OpenAsResult result = dialog.Result;
				this.ResultText.Text =
					$"Chosen: {result.Item.Location}\n" +
					$"Opened from: {result.LocalPath}\n" +
					(result.ExtractedFolder is null ? string.Empty : $"Extracted to: {result.ExtractedFolder} (with its sidecar and index, if the zip had them)\n") +
					$"Options: {options.Summary}";
			}
			else
			{
				this.ResultText.Text = "Cancelled: nothing was opened.";
			}

			_lastFolder = dialog.Browser.CurrentLocation?.ToString();
			_recentFolders = dialog.RecentFolders;
		}

		private void OnThemeChanged(object sender, RoutedEventArgs e)
		{
			if (this.DarkThemeBox.IsChecked == true)
			{
				Application.Current.Resources.MergedDictionaries.Add(_darkTheme);
			}
			else
			{
				Application.Current.Resources.MergedDictionaries.Remove(_darkTheme);
			}

			ApplyTheme(this);
		}

		/// <summary>
		/// Styles by type do not reach a window of a derived class, so the host sets a window's colours itself, as Weevil's theme would.
		/// </summary>
		private void ApplyTheme(Window window)
		{
			var isDark = this.DarkThemeBox.IsChecked == true;
			window.Background = isDark ? (Brush)_darkTheme["DarkBackground"] : SystemColors.WindowBrush;
			window.Foreground = isDark ? (Brush)_darkTheme["DarkText"] : SystemColors.WindowTextBrush;
			SetDarkTitleBar(window, isDark);
		}

		/// <summary>
		/// Styles cannot reach a window's title bar, which Windows draws, so the host asks Windows for a dark one.
		/// </summary>
		private static void SetDarkTitleBar(Window window, bool isDark)
		{
			var handle = new WindowInteropHelper(window).EnsureHandle();
			var value = isDark ? 1 : 0;
			DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref value, sizeof(int));
		}

		// DWMWA_USE_IMMERSIVE_DARK_MODE: Windows 10 20H1 and later.
		private const int UseImmersiveDarkMode = 20;

		[DllImport("dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
	}
}
