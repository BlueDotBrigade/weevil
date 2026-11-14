namespace OpenAsDialogPoC
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Globalization;
	using System.IO;
	using System.Runtime.CompilerServices;
	using System.Threading.Tasks;
	using BlueDotBrigade.Windows.Dialogs.Storage;

	/// <summary>
	/// Weevil-like options for the log selected: its details, which part to load, and RAM or Disk (#1157).
	/// </summary>
	/// <remarks>
	/// A stand-in for Weevil's own panel: it reads sidecars with plain XML, and suggests a mode with a simple rule.
	/// </remarks>
	internal sealed class LogOptionsModel : INotifyPropertyChanged
	{
		private const long Gigabyte = 1_000_000_000;

		private readonly ContainerReader _reader;
		private int _selection;
		private BrowserItem _file;
		private string _details = "Select a log to see its details.";
		private string _sidecarStatus = string.Empty;
		private IReadOnlyList<SidecarSection> _sections = Array.Empty<SidecarSection>();
		private IReadOnlyList<SidecarRegion> _regions = Array.Empty<SidecarRegion>();
		private SidecarSection _selectedSection;
		private SidecarRegion _selectedRegion;
		private Narrowing _narrowing = Narrowing.WholeLog;
		private string _fromLine = string.Empty;
		private string _toLine = string.Empty;
		private bool _isDisk;
		private bool _isModeChosen;
		private string _advice = string.Empty;
		private string _warning = string.Empty;

		public LogOptionsModel(ContainerReader reader)
		{
			_reader = reader;
		}

		public enum Narrowing
		{
			WholeLog,
			Section,
			Region,
			Lines,
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public bool HasFile => _file is not null;

		public string Details
		{
			get => _details;
			private set => Set(ref _details, value);
		}

		public string SidecarStatus
		{
			get => _sidecarStatus;
			private set => Set(ref _sidecarStatus, value);
		}

		public IReadOnlyList<SidecarSection> Sections
		{
			get => _sections;
			private set
			{
				Set(ref _sections, value);
				OnPropertyChanged(nameof(this.HasSections));
			}
		}

		public IReadOnlyList<SidecarRegion> Regions
		{
			get => _regions;
			private set
			{
				Set(ref _regions, value);
				OnPropertyChanged(nameof(this.HasRegions));
			}
		}

		public bool HasSections => _sections.Count > 0;

		public bool HasRegions => _regions.Count > 0;

		public SidecarSection SelectedSection
		{
			get => _selectedSection;
			set
			{
				if (Set(ref _selectedSection, value) && value is not null)
				{
					SetNarrowing(Narrowing.Section);
				}
			}
		}

		public SidecarRegion SelectedRegion
		{
			get => _selectedRegion;
			set
			{
				if (Set(ref _selectedRegion, value) && value is not null)
				{
					SetNarrowing(Narrowing.Region);
				}
			}
		}

		// One way to narrow the log at a time (decision 5 of the #1157 plan): a radio button each.
		public bool IsWholeLog { get => _narrowing == Narrowing.WholeLog; set => ChooseNarrowing(Narrowing.WholeLog, value); }

		public bool IsSection { get => _narrowing == Narrowing.Section; set => ChooseNarrowing(Narrowing.Section, value); }

		public bool IsRegion { get => _narrowing == Narrowing.Region; set => ChooseNarrowing(Narrowing.Region, value); }

		public bool IsLines { get => _narrowing == Narrowing.Lines; set => ChooseNarrowing(Narrowing.Lines, value); }

		public string FromLine
		{
			get => _fromLine;
			set
			{
				if (Set(ref _fromLine, value ?? string.Empty))
				{
					SetNarrowing(Narrowing.Lines);
				}
			}
		}

		public string ToLine
		{
			get => _toLine;
			set
			{
				if (Set(ref _toLine, value ?? string.Empty))
				{
					SetNarrowing(Narrowing.Lines);
				}
			}
		}

		// Once the analyst picks a mode, selecting another log does not change it (decision 7).
		public bool IsRam
		{
			get => !_isDisk;
			set
			{
				if (value && _isDisk)
				{
					_isModeChosen = true;
					SetMode(isDisk: false);
				}
			}
		}

		public bool IsDisk
		{
			get => _isDisk;
			set
			{
				if (value && !_isDisk)
				{
					_isModeChosen = true;
					SetMode(isDisk: true);
				}
			}
		}

		public string Advice
		{
			get => _advice;
			private set => Set(ref _advice, value);
		}

		public string Warning
		{
			get => _warning;
			private set
			{
				Set(ref _warning, value);
				OnPropertyChanged(nameof(this.HasWarning));
			}
		}

		public bool HasWarning => _warning.Length > 0;

		/// <summary>
		/// A log is selected, and the part chosen is complete: a section or region picked, or a valid range of lines.
		/// </summary>
		public bool CanOpen => _file is not null && _narrowing switch
		{
			Narrowing.Section => _selectedSection is not null,
			Narrowing.Region => _selectedRegion is not null,
			Narrowing.Lines => TryGetLines(out _, out _),
			_ => true,
		};

		/// <summary>
		/// What the analyst chose, for the sample application to show after Open.
		/// </summary>
		public string Summary => _narrowing switch
		{
			Narrowing.Section => $"Section: {_selectedSection?.ToString().Trim()}",
			Narrowing.Region => $"Region: {_selectedRegion}",
			Narrowing.Lines when TryGetLines(out var from, out var to) => $"Lines {from:N0}–{to:N0}",
			_ => "The whole log",
		} + $"; in {(_isDisk ? "Disk" : "RAM")} mode";

		/// <summary>
		/// Shows the details and options of the file selected, or none when a folder or nothing is selected.
		/// </summary>
		public async void Select(BrowserItem file)
		{
			var selection = ++_selection;

			_file = file;
			OnPropertyChanged(nameof(this.HasFile));
			this.Sections = Array.Empty<SidecarSection>();
			this.Regions = Array.Empty<SidecarRegion>();
			_selectedSection = null;
			_selectedRegion = null;
			OnPropertyChanged(nameof(this.SelectedSection));
			OnPropertyChanged(nameof(this.SelectedRegion));
			SetNarrowing(Narrowing.WholeLog);
			this.Warning = string.Empty;
			this.Advice = string.Empty;

			if (file is null)
			{
				this.Details = "Select a log to see its details.";
				this.SidecarStatus = string.Empty;
				OnPropertyChanged(nameof(this.CanOpen));
				return;
			}

			this.Details = Describe(file);
			SuggestMode(file);

			if (file.IsOnlineOnly)
			{
				// Reading an online-only file downloads it, so its sidecar waits until the analyst opens it.
				this.SidecarStatus = "Online only: its sections and regions are read when it is opened.";
				return;
			}

			this.SidecarStatus = "Looking for a sidecar…";

			(string status, IReadOnlyList<SidecarSection> sections, IReadOnlyList<SidecarRegion> regions) = await Task.Run(() => ReadCompanions(file));

			if (selection != _selection)
			{
				return;
			}

			this.SidecarStatus = status;
			this.Sections = sections;
			this.Regions = regions;
		}

		private (string Status, IReadOnlyList<SidecarSection> Sections, IReadOnlyList<SidecarRegion> Regions) ReadCompanions(BrowserItem file)
		{
			Location sidecar = ContainerReader.GetSibling(file.Location, file.Name + ".xml");
			var hasIndex = _reader.Exists(ContainerReader.GetSibling(file.Location, file.Name + ".idx"));
			var index = hasIndex ? " A saved index is beside it." : string.Empty;

			if (!_reader.Exists(sidecar))
			{
				return ($"No sidecar, so no sections or regions.{index}", Array.Empty<SidecarSection>(), Array.Empty<SidecarRegion>());
			}

			try
			{
				using Stream stream = _reader.OpenRead(sidecar);
				var (sections, regions) = Sidecar.Read(stream);
				return ($"Sidecar: {sections.Count} sections, {regions.Count} regions.{index}", sections, regions);
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Xml.XmlException or InvalidDataException)
			{
				return ($"The sidecar could not be read: {exception.Message}", Array.Empty<SidecarSection>(), Array.Empty<SidecarRegion>());
			}
		}

		private static string Describe(BrowserItem file)
		{
			var where = file.Location.IsInArchive ? $"In a zip: {file.Location.GetParent()}" : $"Folder: {file.Location.GetParent()}";
			var size = file.Size is long bytes ? $"{bytes / (double)Gigabyte:N2} GB ({bytes:N0} bytes)" : "unknown";
			return $"{file.Name}\nSize: {size}\nModified: {file.Modified:g}\n{where}";
		}

		/// <summary>
		/// Suggests RAM when the log fits with 1 GB to spare, and Disk otherwise: a simple stand-in for Weevil's own rule (#1028).
		/// </summary>
		private void SuggestMode(BrowserItem file)
		{
			var needed = (long)((file.Size ?? 0) * 1.25);
			GCMemoryInfo memory = GC.GetGCMemoryInfo();
			var free = memory.TotalAvailableMemoryBytes - memory.MemoryLoadBytes;
			var suggestsDisk = needed > free - Gigabyte;

			this.Advice = $"Suggested: {(suggestsDisk ? "Disk" : "RAM")}. In RAM it needs about {needed / (double)Gigabyte:N1} GB; {free / (double)Gigabyte:N1} GB is free.";

			if (!_isModeChosen)
			{
				SetMode(suggestsDisk);
			}

			UpdateWarning(file, needed, free);
		}

		private void UpdateWarning(BrowserItem file, long needed, long free)
		{
			var path = file.Location.FileSystemPath;
			var isNetwork = path.StartsWith(@"\\", StringComparison.Ordinal) || IsNetworkDrive(path);

			this.Warning = !_isDisk && needed > free - Gigabyte ? "This log may not fit in RAM."
				: _isDisk && isNetwork ? "Disk mode reads the log back over the network, which may be slow."
				: string.Empty;
		}

		private static bool IsNetworkDrive(string path)
		{
			try
			{
				var root = Path.GetPathRoot(path);
				return !string.IsNullOrEmpty(root) && new DriveInfo(root).DriveType == DriveType.Network;
			}
			catch (ArgumentException)
			{
				return false;
			}
		}

		private void SetMode(bool isDisk)
		{
			_isDisk = isDisk;
			OnPropertyChanged(nameof(this.IsRam));
			OnPropertyChanged(nameof(this.IsDisk));

			if (_file is not null)
			{
				GCMemoryInfo memory = GC.GetGCMemoryInfo();
				UpdateWarning(_file, (long)((_file.Size ?? 0) * 1.25), memory.TotalAvailableMemoryBytes - memory.MemoryLoadBytes);
			}
		}

		private void ChooseNarrowing(Narrowing narrowing, bool isChecked)
		{
			if (isChecked)
			{
				SetNarrowing(narrowing);
			}
		}

		private void SetNarrowing(Narrowing narrowing)
		{
			_narrowing = narrowing;
			OnPropertyChanged(nameof(this.IsWholeLog));
			OnPropertyChanged(nameof(this.IsSection));
			OnPropertyChanged(nameof(this.IsRegion));
			OnPropertyChanged(nameof(this.IsLines));
			OnPropertyChanged(nameof(this.CanOpen));
		}

		private bool TryGetLines(out int from, out int to)
		{
			to = 0;
			return int.TryParse(_fromLine, NumberStyles.Integer, CultureInfo.CurrentCulture, out from) &&
			       int.TryParse(_toLine, NumberStyles.Integer, CultureInfo.CurrentCulture, out to) &&
			       from >= 1 && to >= from;
		}

		private bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
			{
				return false;
			}

			field = value;
			OnPropertyChanged(name);
			OnPropertyChanged(nameof(this.CanOpen));
			return true;
		}

		private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
