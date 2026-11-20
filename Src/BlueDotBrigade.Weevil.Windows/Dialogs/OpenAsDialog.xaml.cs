namespace BlueDotBrigade.Weevil.Windows.Dialogs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;

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
		/// The folder a file in a zip was extracted to: <see langword="null"/> for a file on disk.
		/// </summary>
		/// <remarks>
		/// With <see cref="OpenAsDialog.CopiesFolder"/>, a folder of that file's own, kept between opens; otherwise a temporary folder, for the host to delete when it is done.
		/// </remarks>
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
			nameof(CanOpen), typeof(bool), typeof(OpenAsDialog), new PropertyMetadata(true, (dialog, _) => ((OpenAsDialog)dialog).OnCanOpenChanged()));

		private bool _isOpening;
		private bool _isClosed;

		/// <summary>
		/// Stops the extraction that is running, or <see langword="null"/>.
		/// </summary>
		private CancellationTokenSource _extraction;

		/// <summary>
		/// A file double-clicked, or chosen with Enter, before the host's options were valid: it opens once they are.
		/// </summary>
		private BrowserItem _pendingActivation;

		public OpenAsDialog()
		{
			InitializeComponent();

			this.FileBrowserControl.SelectionChanged += (_, item) =>
			{
				this.MessageText.Text = string.Empty;
				_pendingActivation = null;
				UpdateOpenButton();
				this.SelectionChanged?.Invoke(this, item);
			};

			this.FileBrowserControl.FileActivated += async (_, item) =>
			{
				// The host may still be reading the file, for example on a slow share, when the analyst double-clicks it.
				_pendingActivation = this.CanOpen ? null : item;
				await OpenAsync();
			};
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
		/// Where a file in a zip is extracted: to a folder of its own in here, named by <see cref="IContainerReader.GetCopyKey"/>,
		/// so the same file opened again finds the companions kept beside it (#1253). <see langword="null"/> extracts to a new temporary folder each time.
		/// </summary>
		public string CopiesFolder { get; set; }

		/// <summary>
		/// Checks the file chosen before it is extracted or the dialog closes, given the path it will have on disk: for a file in a zip, where it will be extracted to.
		/// An <see cref="IOException"/> it throws keeps the dialog open, shows its message, and nothing is extracted. Called off the UI thread; <see langword="null"/> checks nothing.
		/// </summary>
		/// <remarks>
		/// Weevil checks that another instance doesn't have the log open, which would share the copy of a file in a zip (#1254).
		/// </remarks>
		public Action<string> CheckBeforeOpening { get; set; }

		/// <summary>
		/// The file chosen, once the dialog closes with Open: otherwise <see langword="null"/>.
		/// </summary>
		public OpenAsResult Result { get; private set; }

		/// <remarks>
		/// Closing the window, for example with Cancel, stops an extraction that is running (#1251).
		/// </remarks>
		protected override void OnClosed(EventArgs e)
		{
			_isClosed = true;

			if (_extraction is not null)
			{
				Log.Default.Write(LogSeverityType.Information, "The Open As dialog closed during an extraction, so the extraction was cancelled.");
				_extraction.Cancel();
			}

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

			// While a file is extracted, another selection must not change the options it opens with.
			this.FileBrowserControl.IsEnabled = false;
			this.OptionsArea.IsEnabled = false;

			(var localPath, var destination) = await ExtractAsync(file);

			_isOpening = false;

			if (_isClosed)
			{
				// The copy finished as the window closed: nobody will open it. A copy in a kept folder stays: it may be the log the host has open,
				// which the dialog cannot tell, and the next open of the same file uses it (#1253).
				if (localPath is not null && file.Location.IsInArchive && this.CopiesFolder is null)
				{
					ContainerReader.TryDeleteFolder(destination);
				}
			}
			else if (localPath is null)
			{
				this.FileBrowserControl.IsEnabled = true;
				this.OptionsArea.IsEnabled = true;
				UpdateOpenButton();
			}
			else
			{
				this.Result = new OpenAsResult(file, localPath, file.Location.IsInArchive ? destination : null);
				this.DialogResult = true;
			}
		}

		/// <returns>
		/// The file on disk, or <see langword="null"/> when the extraction failed or was cancelled, leaving nothing behind; and the folder a file in a zip is extracted to.
		/// </returns>
		private async Task<(string LocalPath, string Destination)> ExtractAsync(BrowserItem file)
		{
			string destination = null;
			var copiesFolder = this.CopiesFolder;
			Action<string> checkBeforeOpening = this.CheckBeforeOpening;

			using var extraction = new CancellationTokenSource();
			_extraction = extraction;

			try
			{
				IContainerReader reader = this.Browser.Reader;
				IReadOnlyList<string> companionSuffixes = this.CompanionSuffixes;

				return await Task.Run(() =>
				{
					// The key reads the zip's directory, so it is worked out off the UI thread too.
					destination = file.Location.IsInArchive && copiesFolder is not null
						? Path.Combine(copiesFolder, reader.GetCopyKey(file.Location))
						: Path.Combine(Path.GetTempPath(), "BlueDotBrigade.OpenAs.Extracted", Guid.NewGuid().ToString("N"));

					checkBeforeOpening?.Invoke(file.Location.IsInArchive ? Path.Combine(destination, file.Location.Name) : file.Location.FileSystemPath);

					return (reader.Extract(file.Location, companionSuffixes, destination, extraction.Token), destination);
				});
			}
			catch (Exception exception) when (_isClosed)
			{
				// The analyst closed the window, so there is nothing to show. Logged, so a defect is not hidden.
				if (exception is not OperationCanceledException)
				{
					Log.Default.Write(LogSeverityType.Warning, exception, "An extraction failed after the Open As dialog closed.");
				}

				return (null, destination);
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
			{
				this.MessageText.Text = $"The file could not be opened: {exception.Message}";
				return (null, destination);
			}
			finally
			{
				_extraction = null;
			}
		}

		private async void OnCanOpenChanged()
		{
			UpdateOpenButton();

			// The host's options may become valid after the window closed.
			if (!_isClosed && this.CanOpen && _pendingActivation is { } file && file.Location.Equals(this.Browser.SelectedFile?.Location))
			{
				_pendingActivation = null;
				await OpenAsync();
			}
		}

		private void UpdateOpenButton()
		{
			this.OpenButton.IsEnabled = this.CanOpen && !_isOpening && this.Browser.SelectedFile is not null;
		}
	}
}
