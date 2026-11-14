namespace BlueDotBrigade.Windows.Dialogs
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Threading;
	using BlueDotBrigade.Windows.Dialogs.Storage;

	/// <summary>
	/// Browses drives, folders and zips, as the Windows file dialog does, and reports the item selected.
	/// It knows nothing about the application that hosts it (#1157).
	/// </summary>
	/// <remarks>
	/// Keys: Enter opens, Backspace or Alt+Up goes up, Alt+Left and Alt+Right go back and forward, F5 refreshes,
	/// Ctrl+L goes to the path box and Ctrl+F to the name filter.
	/// </remarks>
	public partial class FileBrowser : UserControl, IDisposable
	{
		private readonly FileBrowserModel _model;
		private bool _isFocusingList;

		public FileBrowser()
		{
			InitializeComponent();

			_model = new FileBrowserModel(new ContainerReader());
			this.LayoutRoot.DataContext = _model;

			_model.PropertyChanged += OnModelPropertyChanged;
			_model.FileActivated += (_, item) => this.FileActivated?.Invoke(this, item);
			_model.Navigated += OnNavigated;

			this.Loaded += OnLoaded;
		}

		/// <summary>
		/// The item selected changed: a file, folder or zip, or nothing.
		/// </summary>
		public event EventHandler<BrowserItem> SelectionChanged;

		/// <summary>
		/// The analyst opened a file, by double-clicking it or pressing Enter on it.
		/// </summary>
		public event EventHandler<BrowserItem> FileActivated;

		/// <summary>
		/// Reads folders, zips and files, including a file's companions inside a zip.
		/// </summary>
		public ContainerReader Reader => _model.Reader;

		/// <summary>
		/// The folder, zip or file to show first: a file is selected in its folder. By default, the user's documents.
		/// </summary>
		public string InitialLocation { get; set; }

		public IReadOnlyList<FileTypeFilter> FileTypes
		{
			get => _model.FileTypes;
			set => _model.FileTypes = value;
		}

		/// <summary>
		/// Recent folders, most recent first: given by the host when it opens the browser, and read back afterwards to keep them.
		/// </summary>
		public IReadOnlyList<string> RecentFolders
		{
			get => _model.RecentFolders.Select(place => place.Location.ToString()).ToList();
			set
			{
				_model.RecentFolders.Clear();

				foreach (var path in value ?? Array.Empty<string>())
				{
					Location location = Location.Parse(path, this.Reader.IsContainerName);

					if (location is not null)
					{
						_model.RecentFolders.Add(new Place(location.Name, location.IsInArchive ? Glyphs.Zip : Glyphs.Folder, location));
					}
				}
			}
		}

		public BrowserItem SelectedItem => _model.SelectedItem;

		/// <summary>
		/// The file selected, or <see langword="null"/> when a folder or zip, or nothing, is selected.
		/// </summary>
		public BrowserItem SelectedFile => _model.SelectedItem is { IsFile: true } file ? file : null;

		public Location CurrentLocation => _model.CurrentLocation;

		public void Dispose() => _model.Dispose();

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			this.Loaded -= OnLoaded;

			var initial = string.IsNullOrWhiteSpace(this.InitialLocation)
				? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
				: this.InitialLocation;

			_model.PathText = initial;
			_isFocusingList = true;
			await _model.GoToPathTextAsync();

			if (_model.CurrentLocation is null)
			{
				await _model.NavigateAsync(Location.Computer);
			}
		}

		private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(FileBrowserModel.SelectedItem))
			{
				this.SelectionChanged?.Invoke(this, _model.SelectedItem);
			}
		}

		/// <summary>
		/// Puts keyboard focus on the item selected, or the first, after a navigation started from the list or the toolbar.
		/// </summary>
		private void OnNavigated(object sender, EventArgs e)
		{
			if (!_isFocusingList)
			{
				return;
			}

			_isFocusingList = false;

			this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
			{
				var item = _model.SelectedItem ?? _model.VisibleItems.FirstOrDefault();

				if (item is null)
				{
					this.ItemsList.Focus();
					return;
				}

				this.ItemsList.ScrollIntoView(item);

				if (this.ItemsList.ItemContainerGenerator.ContainerFromItem(item) is ListViewItem container)
				{
					container.Focus();
				}
			}));
		}

		private async void OnBackClick(object sender, RoutedEventArgs e)
		{
			_isFocusingList = true;
			await _model.GoBackAsync();
		}

		private async void OnForwardClick(object sender, RoutedEventArgs e)
		{
			_isFocusingList = true;
			await _model.GoForwardAsync();
		}

		private async void OnUpClick(object sender, RoutedEventArgs e)
		{
			_isFocusingList = true;
			await _model.GoUpAsync();
		}

		private async void OnRefreshClick(object sender, RoutedEventArgs e) => await _model.RefreshAsync();

		private async void OnPathBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				// Handled, so the host window's default button does not open the file selected before.
				e.Handled = true;
				_isFocusingList = true;
				await _model.GoToPathTextAsync();
			}
			else if (e.Key == Key.Escape && _model.CurrentLocation is not null && _model.PathText != _model.CurrentLocation.ToString())
			{
				// The first Escape undoes the typing; the next one reaches the host window, which cancels.
				e.Handled = true;
				_model.PathText = _model.CurrentLocation.ToString();
			}
		}

		private async void OnItemsListPreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && _model.SelectedItem is not null)
			{
				e.Handled = true;
				_isFocusingList = true;
				await _model.ActivateAsync(_model.SelectedItem);
			}
			else if (e.Key == Key.Back)
			{
				e.Handled = true;
				_isFocusingList = true;
				await _model.GoUpAsync();
			}
		}

		private async void OnItemDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (sender is ListViewItem { DataContext: BrowserItem item } && e.ChangedButton == MouseButton.Left)
			{
				e.Handled = true;
				_isFocusingList = true;
				await _model.ActivateAsync(item);
			}
		}

		private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
		{
			ModifierKeys modifiers = Keyboard.Modifiers;
			Key key = e.Key == Key.System ? e.SystemKey : e.Key;

			if (modifiers == ModifierKeys.Alt && key is Key.Left or Key.Right or Key.Up)
			{
				e.Handled = true;
				_isFocusingList = true;
				await (key == Key.Left ? _model.GoBackAsync() : key == Key.Right ? _model.GoForwardAsync() : _model.GoUpAsync());
			}
			else if (modifiers == ModifierKeys.None && key == Key.F5)
			{
				e.Handled = true;
				await _model.RefreshAsync();
			}
			else if ((modifiers == ModifierKeys.Control && key == Key.L) || (modifiers == ModifierKeys.Alt && key == Key.D))
			{
				e.Handled = true;
				this.PathBox.Focus();
				this.PathBox.SelectAll();
			}
			else if (modifiers == ModifierKeys.Control && key is Key.F or Key.E)
			{
				e.Handled = true;
				this.NameFilterBox.Focus();
				this.NameFilterBox.SelectAll();
			}
		}

		private async void OnPlaceKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && sender is ListBox { SelectedItem: Place place })
			{
				e.Handled = true;
				_isFocusingList = true;
				await _model.NavigateAsync(place.Location);
			}
		}

		private async void OnPlaceMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && sender is ListBox { SelectedItem: Place place })
			{
				await _model.NavigateAsync(place.Location);
			}
		}

		private void OnHeaderClick(object sender, RoutedEventArgs e)
		{
			if (sender is GridViewColumnHeader { Tag: SortColumn column })
			{
				_model.SortBy(column);
			}
		}
	}
}
