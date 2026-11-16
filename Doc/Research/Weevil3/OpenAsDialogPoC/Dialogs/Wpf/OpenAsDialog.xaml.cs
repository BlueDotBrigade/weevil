namespace BlueDotBrigade.Windows.Dialogs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading.Tasks;
	using System.Windows;
	using BlueDotBrigade.Windows.Dialogs.Storage;

	/// <summary>
	/// The file chosen in an <see cref="OpenAsDialog"/>, as a path on disk the host can open.
	/// </summary>
	public sealed class OpenAsResult
	{
		public OpenAsResult(BrowserItem item, string localPath, string extractedFolder)
		{
			this.Item = item;
			this.LocalPath = localPath;
			this.ExtractedFolder = extractedFolder;
		}

		/// <summary>
		/// The file as the analyst chose it, possibly inside a zip.
		/// </summary>
		public BrowserItem Item { get; }

		/// <summary>
		/// The file on disk: where it is, or where it was extracted to, with its companions beside it.
		/// </summary>
		public string LocalPath { get; }

		/// <summary>
		/// The folder a file in a zip was extracted to, for the host to delete when it is done: <see langword="null"/> for a file on disk.
		/// </summary>
		public string ExtractedFolder { get; }
	}

	/// <summary>
	/// One window to find a file and choose how to open it: the file browser, beside an options area the host fills (#1157).
	/// </summary>
	/// <remarks>
	/// The host listens to <see cref="SelectionChanged"/> to update its options, and sets <see cref="CanOpen"/> when they are valid.
	/// </remarks>
	public partial class OpenAsDialog : Window
	{
		public static readonly DependencyProperty OptionsContentProperty = DependencyProperty.Register(
			nameof(OptionsContent), typeof(object), typeof(OpenAsDialog));

		public static readonly DependencyProperty CanOpenProperty = DependencyProperty.Register(
			nameof(CanOpen), typeof(bool), typeof(OpenAsDialog), new PropertyMetadata(true, (dialog, _) => ((OpenAsDialog)dialog).UpdateOpenButton()));

		private bool _isOpening;

		public OpenAsDialog()
		{
			InitializeComponent();

			this.FileBrowserControl.SelectionChanged += (_, item) =>
			{
				this.MessageText.Text = string.Empty;
				UpdateOpenButton();
				this.SelectionChanged?.Invoke(this, item);
			};

			this.FileBrowserControl.FileActivated += async (_, _) => await OpenAsync();
		}

		/// <summary>
		/// The item selected changed: a file, folder or zip, or nothing.
		/// </summary>
		public event EventHandler<BrowserItem> SelectionChanged;

		public FileBrowser Browser => this.FileBrowserControl;

		/// <summary>
		/// The host's options, shown beside the browser.
		/// </summary>
		public object OptionsContent
		{
			get => GetValue(OptionsContentProperty);
			set => SetValue(OptionsContentProperty, value);
		}

		/// <summary>
		/// The host's options are valid for the file selected. Open needs this and a file.
		/// </summary>
		public bool CanOpen
		{
			get => (bool)GetValue(CanOpenProperty);
			set => SetValue(CanOpenProperty, value);
		}

		public string InitialLocation
		{
			get => this.Browser.InitialLocation;
			set => this.Browser.InitialLocation = value;
		}

		public IReadOnlyList<FileTypeFilter> FileTypes
		{
			get => this.Browser.FileTypes;
			set => this.Browser.FileTypes = value;
		}

		public IReadOnlyList<string> RecentFolders
		{
			get => this.Browser.RecentFolders;
			set => this.Browser.RecentFolders = value;
		}

		/// <summary>
		/// What a companion's name adds to the file's, such as <c>.xml</c>: companions inside a zip are extracted with the file.
		/// </summary>
		public IReadOnlyList<string> CompanionSuffixes { get; set; } = Array.Empty<string>();

		/// <summary>
		/// The file chosen, once the dialog closes with Open: otherwise <see langword="null"/>.
		/// </summary>
		public OpenAsResult Result { get; private set; }

		protected override void OnClosed(EventArgs e)
		{
			this.Browser.Dispose();
			base.OnClosed(e);
		}

		private async void OnOpenClick(object sender, RoutedEventArgs e) => await OpenAsync();

		private async Task OpenAsync()
		{
			BrowserItem file = this.Browser.SelectedFile;

			if (file is null || !this.CanOpen || _isOpening)
			{
				return;
			}

			_isOpening = true;
			UpdateOpenButton();
			this.MessageText.Text = file.Location.IsInArchive ? "Extracting…" : string.Empty;

			try
			{
				var destination = Path.Combine(Path.GetTempPath(), "BlueDotBrigade.OpenAs.Extracted", Guid.NewGuid().ToString("N"));
				var localPath = await Task.Run(() => this.Browser.Reader.Extract(file.Location, this.CompanionSuffixes, destination));

				this.Result = new OpenAsResult(file, localPath, file.Location.IsInArchive ? destination : null);
				this.DialogResult = true;
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
			{
				this.MessageText.Text = $"The file could not be opened: {exception.Message}";
			}
			finally
			{
				_isOpening = false;

				if (this.IsLoaded)
				{
					UpdateOpenButton();
				}
			}
		}

		private void UpdateOpenButton()
		{
			this.OpenButton.IsEnabled = this.CanOpen && !_isOpening && this.Browser.SelectedFile is not null;
		}
	}
}
