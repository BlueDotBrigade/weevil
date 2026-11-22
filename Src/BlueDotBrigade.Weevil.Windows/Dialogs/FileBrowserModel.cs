namespace BlueDotBrigade.Weevil.Windows.Dialogs
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;

	public enum SortColumn
	{
		Name,
		Modified,
		Type,
		Size,
	}

	/// <summary>
	/// What the file browser shows, and where it is going.
	/// </summary>
	/// <remarks>
	/// Change notification is written by hand, not with Metalama, so another application needs nothing more than WPF.
	/// A folder is listed on a background thread. A newer navigation discards an older one's result, because a listing blocked
	/// on an unreachable network share cannot be cancelled.
	/// </remarks>
	internal sealed class FileBrowserModel : INotifyPropertyChanged, IDisposable
	{
		private const int MaxRecentFolders = 8;

		private readonly NavigationHistory _history = new NavigationHistory();
		private IReadOnlyList<BrowserItem> _items = Array.Empty<BrowserItem>();
		private int _navigation;

		private Location _currentLocation;
		private string _pathText = string.Empty;
		private string _nameFilter = string.Empty;
		private IReadOnlyList<FileTypeFilter> _fileTypes = new[] { FileTypeFilter.AllFiles };
		private FileTypeFilter _selectedFileType = FileTypeFilter.AllFiles;
		private BrowserItem _selectedItem;
		private string _error;
		private bool _isBusy;
		private SortColumn _sortColumn = SortColumn.Name;
		private bool _isSortDescending;

		public FileBrowserModel(IContainerReader reader)
		{
			this.Reader = reader;

			this.Places = new ObservableCollection<Place> { new Place("This PC", Glyphs.Computer, Location.Computer) };
			AddPlace("Desktop", Glyphs.Desktop, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
			AddPlace("Documents", Glyphs.Document, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
			AddPlace("Downloads", Glyphs.Download, KnownFolders.GetDownloads());
		}

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// The analyst opened a file: by double-clicking it, or pressing Enter on it.
		/// </summary>
		public event EventHandler<BrowserItem> FileActivated;

		/// <summary>
		/// A navigation finished, and the list shows the new place.
		/// </summary>
		public event EventHandler Navigated;

		public IContainerReader Reader { get; }

		public ObservableCollection<BrowserItem> VisibleItems { get; } = new ObservableCollection<BrowserItem>();

		public ObservableCollection<Place> Places { get; }

		public ObservableCollection<Place> RecentFolders { get; } = new ObservableCollection<Place>();

		public Location CurrentLocation
		{
			get => _currentLocation;
			private set
			{
				if (Set(ref _currentLocation, value))
				{
					OnPropertyChanged(nameof(this.CanGoUp));
					OnPropertyChanged(nameof(this.LocationKind));
					OnPropertyChanged(nameof(this.LocationGlyph));
				}
			}
		}

		/// <summary>
		/// What the browser is showing, a zip or a folder, for the icon in the path box, the item count and Narrator.
		/// </summary>
		public string LocationKind => _currentLocation switch
		{
			null => string.Empty,
			{ IsComputer: true } => "Computer",
			{ IsInArchive: true } => "Zip",
			_ when IsDrive(_currentLocation) => "Drive",
			_ => "Folder",
		};

		public string LocationGlyph => this.LocationKind switch
		{
			"Computer" => Glyphs.Computer,
			"Zip" => Glyphs.Zip,
			"Drive" => Glyphs.Drive,
			_ => Glyphs.Folder,
		};

		/// <summary>
		/// The text of the path box: where the browser is, or what the analyst is typing.
		/// </summary>
		public string PathText
		{
			get => _pathText;
			set => Set(ref _pathText, value ?? string.Empty);
		}

		/// <summary>
		/// Shows only the items whose name contains this text, in place of Windows' search.
		/// </summary>
		public string NameFilter
		{
			get => _nameFilter;
			set
			{
				if (Set(ref _nameFilter, value ?? string.Empty))
				{
					RefreshView();
				}
			}
		}

		public IReadOnlyList<FileTypeFilter> FileTypes
		{
			get => _fileTypes;
			set
			{
				if (Set(ref _fileTypes, value is { Count: > 0 } ? value : new[] { FileTypeFilter.AllFiles }))
				{
					this.SelectedFileType = _fileTypes[0];
				}
			}
		}

		public FileTypeFilter SelectedFileType
		{
			get => _selectedFileType;
			set
			{
				if (Set(ref _selectedFileType, value ?? FileTypeFilter.AllFiles))
				{
					RefreshView();
				}
			}
		}

		public BrowserItem SelectedItem
		{
			get => _selectedItem;
			set => Set(ref _selectedItem, value);
		}

		/// <summary>
		/// Why the current place could not be listed, or <see langword="null"/>.
		/// </summary>
		public string Error
		{
			get => _error;
			private set
			{
				if (Set(ref _error, value))
				{
					OnPropertyChanged(nameof(this.HasError));
				}
			}
		}

		public bool HasError => _error is not null;

		public bool IsBusy
		{
			get => _isBusy;
			private set => Set(ref _isBusy, value);
		}

		public bool CanGoBack => _history.CanGoBack;

		public bool CanGoForward => _history.CanGoForward;

		public bool CanGoUp => _currentLocation is not null && !_currentLocation.IsComputer;

		public string StatusText
		{
			get
			{
				if (this.IsBusy)
				{
					return "Loading…";
				}

				var count = this.VisibleItems.Count == 1 ? "1 item" : $"{this.VisibleItems.Count:N0} items";
				return _currentLocation is { IsInArchive: true } ? $"{count} in a zip" : count;
			}
		}

		private static bool IsDrive(Location location) =>
			string.Equals(Path.GetPathRoot(location.FileSystemPath), location.FileSystemPath, StringComparison.OrdinalIgnoreCase);

		public Task NavigateAsync(Location location) => NavigateAsync(location, selectName: null, isBackOrForward: false);

		public Task GoBackAsync() => this.CanGoBack ? NavigateAsync(_history.GoBack(), selectName: null, isBackOrForward: true) : Task.CompletedTask;

		public Task GoForwardAsync() => this.CanGoForward ? NavigateAsync(_history.GoForward(), selectName: null, isBackOrForward: true) : Task.CompletedTask;

		/// <summary>
		/// Goes to the folder or zip that holds this place, and selects where the browser came from, as Explorer does.
		/// </summary>
		public Task GoUpAsync() => this.CanGoUp ? NavigateAsync(_currentLocation.GetParent(), _currentLocation.Name, isBackOrForward: false) : Task.CompletedTask;

		public Task RefreshAsync() => _currentLocation is null ? Task.CompletedTask : NavigateAsync(_currentLocation, _selectedItem?.Name, isBackOrForward: true);

		/// <summary>
		/// Opens a folder or zip, or reports a file as opened.
		/// </summary>
		public Task ActivateAsync(BrowserItem item)
		{
			if (item is null)
			{
				return Task.CompletedTask;
			}

			if (item.IsFile)
			{
				this.FileActivated?.Invoke(this, item);
				return Task.CompletedTask;
			}

			return NavigateAsync(item.Open);
		}

		/// <summary>
		/// Goes to the path the analyst typed: a folder or zip is opened, and a file is selected in its folder.
		/// </summary>
		public async Task GoToPathTextAsync()
		{
			var text = this.PathText;
			var navigation = ++_navigation;
			this.IsBusy = true;

			(Location location, ItemKind? kind) = await Task.Run(() =>
			{
				Location parsed = string.Equals(text?.Trim(), Location.Computer.Name, StringComparison.OrdinalIgnoreCase)
					? Location.Computer
					: Location.Parse(text, this.Reader.IsContainerName);
				return (parsed, parsed is null ? null : this.Reader.GetKind(parsed));
			});

			if (navigation != _navigation)
			{
				return;
			}

			this.IsBusy = false;

			if (kind is null)
			{
				this.Error = $"`{text}` was not found.";
				return;
			}

			if (kind == ItemKind.File)
			{
				// A file the selected type hides, such as one given on a host's command line, is shown under the first type that takes it, so it can be selected.
				if (!_selectedFileType.Matches(location.Name) && _fileTypes.FirstOrDefault(type => type.Matches(location.Name)) is { } showingType)
				{
					this.SelectedFileType = showingType;
				}

				await NavigateAsync(location.GetParent(), location.Name, isBackOrForward: false);
			}
			else
			{
				await NavigateAsync(location);
			}
		}

		public void SortBy(SortColumn column)
		{
			_isSortDescending = column == _sortColumn && !_isSortDescending;
			_sortColumn = column;
			RefreshView();
		}

		public void Dispose() => this.Reader.Dispose();

		private async Task NavigateAsync(Location location, string selectName, bool isBackOrForward)
		{
			var navigation = ++_navigation;
			this.IsBusy = true;
			OnPropertyChanged(nameof(this.StatusText));
			this.PathText = location.ToString();

			ListingResult listing = await Task.Run(() => this.Reader.List(location));

			if (navigation != _navigation)
			{
				return;
			}

			if (!isBackOrForward)
			{
				_history.Visit(location);
			}

			this.IsBusy = false;
			this.CurrentLocation = location;
			this.PathText = location.ToString();
			this.Error = listing.Error;
			_items = listing.Items;
			RefreshView();

			// Nothing else is selected, so entering a folder does not make the host read the first file.
			this.SelectedItem = this.VisibleItems.FirstOrDefault(item => string.Equals(item.Name, selectName, StringComparison.OrdinalIgnoreCase));

			if (listing.Error is null && !location.IsComputer)
			{
				RememberRecent(location);
			}

			OnPropertyChanged(nameof(this.CanGoBack));
			OnPropertyChanged(nameof(this.CanGoForward));
			this.Navigated?.Invoke(this, EventArgs.Empty);
		}

		private void RefreshView()
		{
			var selectedName = _selectedItem?.Name;

			IEnumerable<BrowserItem> shown = _items.Where(item =>
				(!item.IsFile || _selectedFileType.Matches(item.Name)) &&
				(_nameFilter.Length == 0 || item.Name.Contains(_nameFilter, StringComparison.OrdinalIgnoreCase)));

			// Folders and drives first, then zips and files together, as Explorer sorts them.
			IOrderedEnumerable<BrowserItem> sorted = shown.OrderBy(item => item.Kind is ItemKind.File or ItemKind.Container ? 1 : 0);

			Func<BrowserItem, object> key = _sortColumn switch
			{
				SortColumn.Modified => item => item.Modified ?? DateTime.MinValue,
				SortColumn.Type => item => item.TypeName,
				SortColumn.Size => item => item.Size ?? -1,
				_ => item => item.Name,
			};

			IComparer<object> comparer = Comparer<object>.Create((left, right) => left is string a && right is string b
				? StringComparer.OrdinalIgnoreCase.Compare(a, b)
				: Comparer<object>.Default.Compare(left, right));

			sorted = _isSortDescending ? sorted.ThenByDescending(key, comparer) : sorted.ThenBy(key, comparer);

			this.VisibleItems.Clear();

			foreach (BrowserItem item in sorted)
			{
				this.VisibleItems.Add(item);
			}

			this.SelectedItem = this.VisibleItems.FirstOrDefault(item => item.Name == selectedName);
			OnPropertyChanged(nameof(this.StatusText));
		}

		private void RememberRecent(Location location)
		{
			Place existing = this.RecentFolders.FirstOrDefault(place => place.Location.Equals(location));

			if (existing is not null)
			{
				this.RecentFolders.Remove(existing);
			}

			this.RecentFolders.Insert(0, new Place(location.Name, location.IsInArchive ? Glyphs.Zip : Glyphs.Folder, location));

			while (this.RecentFolders.Count > MaxRecentFolders)
			{
				this.RecentFolders.RemoveAt(this.RecentFolders.Count - 1);
			}
		}

		private void AddPlace(string name, string glyph, string path)
		{
			if (!string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path))
			{
				this.Places.Add(new Place(name, glyph, Location.ForFileSystem(path)));
			}
		}

		private bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
			{
				return false;
			}

			field = value;
			OnPropertyChanged(name);
			return true;
		}

		private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
