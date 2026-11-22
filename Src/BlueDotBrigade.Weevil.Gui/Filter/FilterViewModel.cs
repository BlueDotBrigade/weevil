namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.IO.Compression;
	using System.Linq;
	using System.Net.Http;
	using System.Reflection;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Input;
	using System.Windows.Threading;
	using BlueDotBrigade.Weevil;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Configuration;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using BlueDotBrigade.Weevil.Gui.Analysis;
	using BlueDotBrigade.Weevil.Gui.Help;
	using BlueDotBrigade.Weevil.Gui.IO;
	using BlueDotBrigade.Weevil.Gui.Navigation;
	using BlueDotBrigade.Weevil.Gui.Properties;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Navigation;
	using BlueDotBrigade.Weevil.Reports;
	using BlueDotBrigade.Weevil.Runtime.Serialization;
	using BlueDotBrigade.Weevil.Utilities;
	using Metalama.Patterns.Observability;
	using Newtonsoft.Json.Linq;
	using Directory = System.IO.Directory;
	using File = System.IO.File;

	[Observable]
	internal partial class FilterViewModel
	{
		const string TsvFileName = "SelectedRecords.tsv";
		const string RawFileName = "SelectedRecords.log";

		/// <summary>
		/// How long a save, a split or a search for text runs before the progress bar replaces the records: they usually take less (#1028).
		/// </summary>
		private static readonly TimeSpan QuickWorkProgressDelay = TimeSpan.FromMilliseconds(250);

		private static readonly Uri NewReleaseUrl =
			new Uri(@"https://raw.githubusercontent.com/BlueDotBrigade/weevil/master/Doc/Notes/Release/NewReleaseNotification.xml");

		private static readonly Uri RegEx101Url = new Uri(@"https://regex101.com/r/dRrRat/1");
		// Copying more would freeze the window while the clipboard fills, and Export saves any number of records to a file.
		// Pasting one comment onto more is a mistake, and each commented record costs memory.
		// A graph keeps its records for as long as its window is open.
		private const int ClipboardLimit = 100_000;
		private const string CopyAdvice = "Use Export > Original Records (Ctrl+Shift+S) to save them to a file.";

		// The title of the message shown when another Weevil has the log open (#1254).
		private const string LogInUseCaption = "Log Already Open";

		private static readonly string HelpFilePath = Path.GetFullPath(EnvironmentHelper.GetExecutableDirectory() + @"\..\Doc\Help.html");
		internal static readonly string LicensePath = Path.GetFullPath(EnvironmentHelper.GetExecutableDirectory() + @"\..\Licenses\License.md");
		private static readonly string ThirdPartyNoticesPath = Path.GetFullPath(EnvironmentHelper.GetExecutableDirectory() + @"\..\Licenses\ThirdPartyNoticesAndInformation.txt");

		private static readonly string NewReleaseFilePath = @"C:\ProgramData\BlueDotBrigade\Weevil\Logs\";

		private static readonly ImmutableArray<IInsight> NoInsight = ImmutableArray.Create(Array.Empty<IInsight>());

		#region Fields & Object Lifetime
		/// <summary>
		/// Provides a mechanism for background threads to queue work for the UI's main message loop.
		/// </summary>
		/// <remarks>
		/// When a background thread attempts to update a WPF application's user interface
		/// (1) an exception may be thrown, or
		/// (2) the action may be silently ignored.
		///
		/// Furthermore, there is no guarantee as to when the <see cref="Dispatcher.BeginInvoke"/> will executed the queued action.
		/// </remarks>
		/// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcher">MSDN: Dispatcher</seealso>
		private readonly IUiDispatcher _uiDispatcher;

		private readonly IBulletinMediator _bulletinMediator;

		private readonly DispatcherTimer initializationTimer;

		private readonly IDialogBoxService _dialogBox;

		private IEngine _engine;

		// The file the open log was read from: for a log opened from a zip, its copy, which the engine names by the zip (#1312).
		private string _openLogFilePath;

		/// <summary>
		/// The copy the open log was extracted to, when it came from a zip.
		/// </summary>
		private ExtractedLog _openExtractedLog;

		/// <summary>
		/// The copy a log is being opened from, until the open has finished.
		/// </summary>
		private ExtractedLog _extractedLogBeingOpened;
		private RowSelection _rowSelection;

		private readonly DragAndDropViewModel _dragAndDrop;

		private string _inclusiveFilter;
		private string _exclusiveFilter;

                private FilterCriteria _previousFilterCriteria;
                private FilterType _previousFilterType;

		private string _findText;

		// The one operation running, such as an open, a filter or an analysis: Open, the menus, the filter box and the progress bar follow it (#1257).
		[NotObservable]
		private readonly BusyState _busy = new BusyState();

		// The operation the filters running share: a new filter replaces the one running, and the operation ends once the last has ended (#1248).
		// Read and written on the UI thread.
		[NotObservable]
		private BusyState.Operation _filterOperation;

		// Each open and each Reload starts a new generation of the log, so a failed read is handled once per log, and a read of an older one is ignored (#1028).
		// Not a property, so it can be passed to `Interlocked` and `Volatile`.
		[NotObservable]
		private int _logGeneration;

		// The open log has comments, pins, bookmarks, regions or remarks that no save has written yet, so leaving it where the sidecar can't be written asks first (#1307).
		// Set on the thread that made the change.
		[NotObservable]
		private volatile bool _hasUnsavedChanges;

		// The generations whose unreadable log, and whose changed log, were handled last. Read and written on the UI thread.
		private int _unreadableLogGeneration = -1;
		private int _changedLogGeneration = -1;
		private bool _findIsCaseSensitive;
		private bool _findUseRegex;
		private bool _findSearchComments;
		private bool _findSearchElapsedTime;
		private int? _findMinElapsedMs;
		private int? _findMaxElapsedMs;

                private FilterType _currentfilterType;
                private FilterType _filterExpressionType;
		private IFilterCriteria _currentfilterCriteria;

		// The filters asked for that have not ended yet.
		// Prevent Metalama from converting this to a property, which cannot be passed into `Interlocked`.
		[NotObservable]
		private int _concurrentFilterCount;

		private ITableOfContents _tableOfContents;

		private ImmutableArray<IInsight> _insights;

		public FilterViewModel(IUiDispatcher uiDispatcher, IBulletinMediator bulletinMediator)
			: this(uiDispatcher, bulletinMediator, new DialogBoxService())
		{
			// nothing to do
		}

		public FilterViewModel(IUiDispatcher uiDispatcher, IBulletinMediator bulletinMediator, IDialogBoxService dialogBox)
		{
			_uiDispatcher = uiDispatcher;
			_dialogBox = dialogBox;

			_bulletinMediator = bulletinMediator;

			_engine = Engine.Surrogate;
			_rowSelection = new RowSelection(_engine);

			this.IsLogFileOpen = false;
			this.AreInsightsReady = false;
			_busy.Changed += OnBusyChanged;

			this.FilterOptionsViewModel = new FilterOptionsViewModel();
			this.FilterOptionsViewModel.OptionsChanged += OnFilterOptionsChanged;

			_inclusiveFilter = string.Empty;
			_exclusiveFilter = string.Empty;

                        _concurrentFilterCount = 0;
                        _currentfilterCriteria = FilterCriteria.None;
                        _currentfilterType = FilterType.RegularExpression;
                        _previousFilterCriteria = FilterCriteria.None;
                        _previousFilterType = FilterType.RegularExpression;

			_findText = string.Empty;
			_findIsCaseSensitive = false;

                        _filterExpressionType = this.FilterOptionsViewModel.Options.FilterExpressionType;
			this.AreFilterOptionsVisible = false;

			this.InclusiveFilterHistory = new ObservableCollection<string>();
			this.ExclusiveFilterHistory = new ObservableCollection<string>();

          this.WeevilVersion = typeof(FilterViewModel).Assembly.GetName().Version ?? new Version(128, 128, 128);
			this.WeevilDisplayVersion = GetWeevilDisplayVersion();

			initializationTimer = new DispatcherTimer();
			initializationTimer.Tick += (sender, args) => OnInitialize();
			initializationTimer.Interval = TimeSpan.FromSeconds(2);
			initializationTimer.Start();

			_dragAndDrop = new DragAndDropViewModel();
			_dragAndDrop.DroppedFile += OnFileDropped;

			_tableOfContents = new TableOfContents();

			this.CustomAnalyzerCommands = new ObservableCollection<MenuItemViewModel>();

			// Subscribe to navigation requests from the Dashboard
			_bulletinMediator.Subscribe<NavigateToInsightRecordBulletin>(this, OnNavigateToInsightRecord);
		}

		private static ApplicationInfo GetApplicationInfo()
		{
			ApplicationInfo result = ApplicationInfo.NotSpecified;

			try
			{
				using (HttpClient client = new HttpClient())
				{
					var webRequest = new HttpRequestMessage(HttpMethod.Get, NewReleaseUrl);
					var response = client.Send(webRequest);

					result = TypeFactory.LoadFromXml<ApplicationInfo>(response.Content.ReadAsStream());
				}
			}
			catch (Exception e)
			{
				var message = $"Unable to determine the latest official release. Reason=`{e.Message}`";
				Log.Default.Write(LogSeverityType.Warning, message);
			}

			return result;
		}

		private static string GetWeevilDisplayVersion()
		{
			var guiAssembly = typeof(FilterViewModel).Assembly;
			var informationalVersion = guiAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

			if (!string.IsNullOrWhiteSpace(informationalVersion))
			{
				return informationalVersion.Split('+')[0];
			}

			var fileVersion = guiAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

			if (!string.IsNullOrWhiteSpace(fileVersion))
			{
				var revisionSeparator = fileVersion.LastIndexOf('.');

				if (revisionSeparator > 0)
				{
					return fileVersion.Substring(0, revisionSeparator);
				}
			}

			return (guiAssembly.GetName().Version ?? new Version(128, 128, 128)).ToString(3);
		}
		#endregion

		#region Properties

		public IList<IRecord> VisibleItems { get; private set; }

		/// <summary>
		/// The grid's rows for <see cref="VisibleItems"/> (#1015).
		/// </summary>
		public RowList Rows { get; private set; }

		public Version WeevilVersion { get; private set; }

		public string WeevilDisplayVersion { get; private set; }

		[NotObservable]
		public bool IsMenuEnabled
		{
			get
			{
				return this.IsLogFileOpen && this.CanOpenLogFile;
			}
		}

		public bool IsLogFileOpen { get; private set; }

		/// <summary>
		/// Nothing is running, such as an open, a filter or an analysis (#1257).
		/// </summary>
		[NotObservable]
		public bool CanOpenLogFile => !_busy.IsBusy;

		public bool AreInsightsReady { get; private set; }

		[NotObservable]
		public bool IsDashboardEnabled
		{
			get
			{
				return this.IsMenuEnabled && this.AreInsightsReady;
			}
		}
		
		public bool IncludePinned
		{
			get => this.FilterOptionsViewModel?.Options.IncludePinned ?? true;
			set
			{
				if (this.FilterOptionsViewModel?.Options != null)
				{
					this.FilterOptionsViewModel.Options.IncludePinned = value;
				}
			}
		}

		public bool IncludeBookmarks
		{
			get => this.FilterOptionsViewModel?.Options.IncludeBookmarks ?? true;
			set
			{
				if (this.FilterOptionsViewModel?.Options != null)
				{
					this.FilterOptionsViewModel.Options.IncludeBookmarks = value;
				}
			}
		}

		public bool IsFilterCaseSensitive
		{
			get => this.FilterOptionsViewModel?.Options.IsFilterCaseSensitive ?? true;
			set
			{
				if (this.FilterOptionsViewModel?.Options != null)
				{
					this.FilterOptionsViewModel.Options.IsFilterCaseSensitive = value;
				}
			}
		}

		[NotObservable]
		public bool IsFilterInProgress => Volatile.Read(ref _concurrentFilterCount) > 0;

		public bool AreFilterOptionsVisible { get; set; }

		/// <summary>
		/// A log is open, and no operation that changes its records or reads it is running. A filter leaves it enabled, so a new filter can replace it (#1248).
		/// </summary>
		[NotObservable]
		public bool IsFilterToolboxEnabled => this.IsLogFileOpen && !_busy.IsFilterLocked;

		public string InclusiveFilter
		{
			get =>
				// When a ComboBox's TextBox value changes a `set` is called, followed by a `get`.
				// ... Changing the filter will begin the process of filtering the records, 
				// ... but there is no guarantee as to when the background filtering thread will begin.
				// ... So we need to keep a local copy of the `InclusiveFilter` in case a `get` is called.
				_inclusiveFilter;
			set
			{
				if (value != _inclusiveFilter)
				{
					_inclusiveFilter = value;
				}
			}
		}

		public string ExclusiveFilter
		{
			get =>
				// When a ComboBox's TextBox value changes a `set` is called, followed by a `get`.
				// ... Changing the filter will begin the process of filtering the records, 
				// ... but there is no guarantee as to when the background filtering thread will begin.
				// ... So we need to keep a local copy of the `InclusiveFilter` in case a `get` is called. 
				_exclusiveFilter;
			set
			{
				if (value != _exclusiveFilter)
				{
					_exclusiveFilter = value;
				}
			}
		}

		public ObservableCollection<string> InclusiveFilterHistory { get; }
		public ObservableCollection<string> ExclusiveFilterHistory { get; }

		[NotObservable]
		public string SourceFileRemarks
		{
			get
			{
				return _engine.SourceFileRemarks;
			}
			set
			{
				if (value != _engine.SourceFileRemarks)
				{
					_engine.SourceFileRemarks = value;
					_hasUnsavedChanges = true;
					_bulletinMediator.Post(new SourceFileRemarksChangedBulletin
					{
						HasSourceFileRemarks = _engine.SourceFileRemarks.Any()
					});
					OnPropertyChanged(nameof(this.SourceFileRemarks));
				}
			}
		}

		/// <summary>
		/// The position the last navigation command went to.
		/// </summary>
		public int ActiveRecordIndex { get; set; }

		public bool IncludeDebugRecords
		{
			get => this.FilterOptionsViewModel?.Options.IncludeDebugRecords ?? true;
			set
			{
				if (this.FilterOptionsViewModel?.Options != null)
				{
					this.FilterOptionsViewModel.Options.IncludeDebugRecords = value;
				}
			}
		}

                public bool IncludeTraceRecords
                {
			get => this.FilterOptionsViewModel?.Options.IncludeTraceRecords ?? true;
			set
			{
				if (this.FilterOptionsViewModel?.Options != null)
				{
					this.FilterOptionsViewModel.Options.IncludeTraceRecords = value;
				}
			}
		}

                public FilterType FilterExpressionType
                {
                        get => this.FilterOptionsViewModel?.Options.FilterExpressionType ?? _filterExpressionType;
                        set
                        {
                                if (_filterExpressionType != value)
                                {
                                        _filterExpressionType = value;
                                        if (this.FilterOptionsViewModel?.Options != null)
                                        {
                                                this.FilterOptionsViewModel.Options.FilterExpressionType = value;
                                        }
										OnPropertyChanged(nameof(this.FilterExpressionType));
                                }
                        }
                }

		/// <summary>
		/// The progress bar replaces the records, while the operation running shows it.
		/// </summary>
		[NotObservable]
		public bool IsProcessingLongOperation => _busy.IsShowingProgress;

		/// <summary>
		/// Completes when the latest background work has finished: an analysis, a save, a split or a search for text (#996, #1028).
		/// </summary>
		[NotObservable]
		public Task BackgroundTask { get; private set; } = Task.CompletedTask;

		/// <summary>
		/// Completes when the operation running now ends, such as an open, a filter or an analysis, or at once when none runs (#1257).
		/// </summary>
		/// <remarks>
		/// An open ends before its insights are ready (<see cref="AreInsightsReady"/>).
		/// Clear ends just before the filter it applies again begins; the task <see cref="ClearRecords"/> returns completes once that filter has begun.
		/// </remarks>
		internal Task WhenIdle()
		{
			return _busy.Idle;
		}

		public FilterOptionsViewModel FilterOptionsViewModel { get; private set; }

		public ObservableCollection<MenuItemViewModel> CustomAnalyzerCommands { get; }

		/// <summary>
		/// Indicates that the records have changed due to either:
		/// <list type="bullet">
		///		<item>opening a new file</item>
		///		<item>applying a filter</item>
		/// </list>
		/// </summary>
		public event EventHandler ResultsChanged;

		/// <summary>
		/// Indicates that a region of interest has been added, removed, or modified.
		/// </summary>
		public event EventHandler RegionsChanged;

		/// <summary>
		/// Indicates that a bookmark has been added or removed.
		/// </summary>
		public event EventHandler BookmarksChanged;

		public event EventHandler FileOpened;

		/// <summary>
		/// Asks the view to scroll to the row at the given position and give it keyboard focus.
		/// </summary>
		public event EventHandler<int> ScrollRequested;
		#endregion

		#region Event Handlers
		private void OnInitialize()
		{
			initializationTimer.IsEnabled = false;

			_bulletinMediator.Post(new SoftwareDetailsBulletin(
					this.WeevilVersion,
				GetApplicationInfo()));

			var args = Environment.GetCommandLineArgs();

			if (args.Length > 1)
			{
				if (File.Exists(args[1]))
				{
					OpenCompressedAsync(args[1]);
				}
			}
		}

		internal void OnFileDropped(object sender, DroppedFileEventArgs e)
		{
			// An analysis, a filter or an open is in progress (#996).
			if (!this.CanOpenLogFile)
			{
				return;
			}

			OpenCompressedAsync(e.FilePath);
		}

		private void OnFilterOptionsChanged(object sender, EventArgs e)
		{
			// Update the backing field for FilterExpressionType
			this._filterExpressionType = this.FilterOptionsViewModel.Options.FilterExpressionType;

			// Re-apply the filter with the new filter options
			var filterCriteria = new FilterCriteria(_inclusiveFilter, _exclusiveFilter, GetFilterConfiguration());
			FilterAsynchronously(this.FilterOptionsViewModel.Options.FilterExpressionType, filterCriteria);
		}

		/// <summary>
		/// An operation began or ended, or changed what it shows: the properties that follow it change. On the UI thread.
		/// </summary>
		/// <remarks>
		/// Raised here, because Metalama can't see a change inside <see cref="BusyState"/>.
		/// </remarks>
		private void OnBusyChanged(object sender, EventArgs e)
		{
			OnPropertyChanged(nameof(this.CanOpenLogFile));
			OnPropertyChanged(nameof(this.IsMenuEnabled));
			OnPropertyChanged(nameof(this.IsDashboardEnabled));
			OnPropertyChanged(nameof(this.IsFilterToolboxEnabled));
			OnPropertyChanged(nameof(this.IsProcessingLongOperation));
			CommandManager.InvalidateRequerySuggested();
		}

		#endregion

		#region Commands: General
		public async void OpenAsync()
		{
			// One window finds the log and chooses RAM or Disk (#1215). A log in a zip is extracted, with its sidecar and index, when the user opens it.
			// Nothing else runs while it is shown, and the open that follows ends the operation once the records are shown.
			using BusyState.Operation operation = _busy.TryBegin("The open", isShowingProgress: false, isFilterLocked: true);

			if (operation is null)
			{
				return;
			}

			var isAccepted = _dialogBox.TryShowOpenAs(ThrowIfOpenElsewhere, out OpenAsChoice choice);
			LogOpenAs(choice?.Advice, isAccepted ? choice.StorageMode : null);

			if (!isAccepted)
			{
				return;
			}

			await (choice.ExtractedFolder is null
				? OpenFromDiskAsync(choice.LogPath, choice.StorageMode, operation, choice.Part, choice.Context)
				: OpenExtractedAsync(choice.LogPath, choice.StorageMode, choice.DisplayPath, operation, choice.Part, choice.Context));
		}

		/// <remarks>
		/// Does nothing while another operation runs, such as a filter or an analysis (#996).
		/// </remarks>
		public async Task OpenCompressedAsync(string sourceFilePath)
		{
			// Nothing else runs while the zip is read, which takes a while for a large log, and Open As is shown.
			// The open that follows ends the operation once the records are shown.
			using BusyState.Operation operation = _busy.TryBegin("The open", isShowingProgress: false, isFilterLocked: true);

			if (operation is null)
			{
				return;
			}

			if (new FileInfo(sourceFilePath).Extension.ToUpperInvariant() != ".ZIP")
			{
				try
				{
					// Before RAM or Disk is asked for: another Weevil may have it open (#1254). Off the UI thread, since the log may be on a share.
					await Task.Run(() => ThrowIfOpenElsewhere(sourceFilePath));

					// Cancelled: nothing opens, and the open log stays (#1028).
					if (await AskStorageAsync(sourceFilePath) is { } storageMode)
					{
						await OpenFromDiskAsync(sourceFilePath, storageMode, operation);
					}
				}
				catch (LogInUseException e)
				{
					// Core logged it. The open log stays.
					_dialogBox.ShowMessage(e.Message, LogInUseCaption);
				}

				return;
			}

			// Weevil opens a copy of the chosen file, extracted to a folder of that file's own that is kept between opens,
			// and deletes only the copy of the log, never the zip (#1156, #1253).
			string extractedLog = null;
			var isOpening = false;

			try
			{
				string[] entryNames = await Task.Run(() =>
				{
					using ZipArchive archive = ZipFile.OpenRead(sourceFilePath);

					// Shown as paths, as Windows writes them; a folder's entry names no file.
					return archive.Entries
						.Where(entry => entry.Name.Length > 0)
						.Select(entry => entry.FullName.Replace('/', Path.DirectorySeparatorChar))
						.ToArray();
				});

				if (entryNames.Length == 0)
				{
					_dialogBox.ShowMessage($"{Path.GetFileName(sourceFilePath)} holds no file to open.", "Nothing to Open");
					return;
				}

				if (!_dialogBox.TrySelectCompressedFile(entryNames, out var selectedFileName))
				{
					return;
				}

				var file = BlueDotBrigade.Weevil.Windows.Dialogs.Storage.Location.ForArchive(sourceFilePath, selectedFileName.Replace(Path.DirectorySeparatorChar, '/'));
				var copiesFolder = this.CopiesFolder;
				extractedLog = await Task.Run(() =>
				{
					using var reader = new BlueDotBrigade.Weevil.Windows.Dialogs.Storage.ContainerReader();
					var destination = Path.Combine(copiesFolder, reader.GetCopyKey(file));

					// Before anything is extracted: the copy may be open in another Weevil (#1254).
					ThrowIfOpenElsewhere(Path.Combine(destination, file.Name));

					return reader.Extract(file, ExtractedLog.CompanionSuffixes, destination);
				});

				// Open As follows the list, so its suggestion uses the size of the log chosen (#1028).
				if (await AskStorageAsync(extractedLog) is { } storageMode)
				{
					isOpening = true;
					await OpenExtractedAsync(extractedLog, storageMode, file.ToString(), operation);
				}
			}
			catch (LogInUseException e)
			{
				// Core logged it. Nothing was extracted, and the open log stays.
				_dialogBox.ShowMessage(e.Message, LogInUseCaption);
			}
			finally
			{
				if (!isOpening)
				{
					operation.Dispose();

					// Nothing was opened from this copy. What was kept beside it from earlier opens stays,
					// and so does the copy when it is the log that is open, chosen again from the same zip.
					if (extractedLog != null && !IsOpenFrom(new ExtractedLog(extractedLog)))
					{
						ExtractedLog copy = new ExtractedLog(extractedLog);
						await Task.Run(copy.Delete);
					}
				}
			}
		}

		/// <summary>
		/// Throws when another Weevil has the log open, so Open As or a zip dropped on Weevil doesn't extract it for nothing (#1254). The open checks again.
		/// </summary>
		/// <remarks>
		/// Called off the UI thread. The log open here may be opened again: its engine hands its lock to the one that replaces it.
		/// </remarks>
		/// <param name="logPath">The file that would be opened: for a log in a zip, the path it would be extracted to.</param>
		private void ThrowIfOpenElsewhere(string logPath)
		{
			var openLogPath = _openLogFilePath;
			var isOpenHere = Engine.IsRealInstance(_engine) &&
			                 openLogPath is not null &&
			                 string.Equals(Path.GetFullPath(logPath), Path.GetFullPath(openLogPath), StringComparison.OrdinalIgnoreCase);

			if (!isOpenHere)
			{
				Engine.ThrowIfInUse(logPath);
			}
		}

		/// <summary>
		/// Where a log opened from a zip is extracted, to a folder of its own (#1253). A test sets a folder of its own.
		/// </summary>
		/// <remarks>
		/// The Open As dialog extracts to <see cref="ExtractedLog.DefaultCopiesFolder"/>.
		/// </remarks>
		internal string CopiesFolder { get; set; } = ExtractedLog.DefaultCopiesFolder;

		/// <summary>
		/// Opens a log that is not in a zip, and deletes the copy of the zip the previous log was opened from, if nothing reads it any more.
		/// </summary>
		/// <param name="operation">The open's operation, which ends once the records are shown.</param>
		/// <param name="part">The part of the log to open: the whole log when <see langword="null"/> (#1214).</param>
		/// <param name="context">What the log's plugin needs to read it, from its own Open As options (#1216): none when <see langword="null"/>.</param>
		private async Task OpenFromDiskAsync(string logPath, StorageMode storageMode, BusyState.Operation operation, LogPart part = null, ContextDictionary context = null)
		{
			await OpenAsync(logPath, storageMode, displayPath: null, operation, part, context);

			// A failed open leaves the zipped log open, which still needs its copy.
			if (!IsOpenFrom(_openExtractedLog))
			{
				await ReplaceExtractedLogAsync(null);
			}
		}

		/// <summary>
		/// Opens a log Weevil extracted from a zip, keeps its copy while it is open, and deletes the copy of the log once nothing reads it (#1162).
		/// </summary>
		/// <remarks>
		/// The folder it was extracted to is kept, with the sidecar and saved index the open saves beside the log (#1253).
		/// </remarks>
		/// <param name="displayPath">The log as the analyst knows it: the zip, read like a folder, and the log's path in it (#1312).</param>
		/// <param name="operation">The open's operation, which ends once the records are shown.</param>
		/// <param name="part">The part of the log to open: the whole log when <see langword="null"/> (#1214).</param>
		/// <param name="context">What the log's plugin needs to read it, from its own Open As options (#1216): none when <see langword="null"/>.</param>
		private async Task OpenExtractedAsync(string logPath, StorageMode storageMode, string displayPath, BusyState.Operation operation, LogPart part = null, ContextDictionary context = null)
		{
			ExtractedLog extracted = null;

			try
			{
				extracted = new ExtractedLog(logPath);

				// Weevil may close before the open finishes.
				_extractedLogBeingOpened = extracted;
				await OpenAsync(logPath, storageMode, displayPath, operation, part, context);
			}
			finally
			{
				// Another open may have started while this one finished its insights.
				if (_extractedLogBeingOpened == extracted)
				{
					_extractedLogBeingOpened = null;
				}

				if (extracted == null)
				{
					// The copy failed to open.
					operation.Dispose();
				}
				else if (IsOpenFrom(extracted))
				{
					// Reload and disk mode read the log again, so its copy stays while it is open (#1162).
					await ReplaceExtractedLogAsync(extracted);
				}
				else
				{
					// Only the log is deleted: what an open that failed saved beside it, and what earlier opens kept there, stays (#1253).
					await Task.Run(extracted.Delete);
				}
			}
		}

		/// <summary>
		/// Returns <see langword="true"/> when the open log is the log in <paramref name="extracted"/>.
		/// </summary>
		private bool IsOpenFrom(ExtractedLog extracted)
		{
			return extracted != null && Engine.IsRealInstance(_engine) && extracted.Contains(_openLogFilePath);
		}

		/// <summary>
		/// Keeps the copy the open log was extracted to, if any, and deletes the previous log's copy, which nothing reads any more.
		/// </summary>
		/// <remarks>
		/// The copy is replaced at once, on the UI thread; only the deletion runs in the background.
		/// </remarks>
		private Task ReplaceExtractedLogAsync(ExtractedLog extracted)
		{
			ExtractedLog previous = _openExtractedLog;
			_openExtractedLog = extracted;

			// The same file opened again from the same zip reuses its copy, which the open log still reads (#1253).
			return previous == null || (extracted != null && extracted.Contains(previous.LogPath))
				? Task.CompletedTask
				: Task.Run(previous.Delete);
		}

		/// <summary>
		/// Lets go of the open log as Weevil closes: releases its lock, so another Weevil can open it (#1254), then deletes the copies Weevil extracted from zips (#1162).
		/// </summary>
		/// <remarks>
		/// Exit calls it once it has saved, and so does closing Weevil while Exit cannot run, for example while a filter, Reload or an open is running.
		/// A copy an open still running holds is kept: the next open of the same file reuses it.
		/// </remarks>
		public void Close()
		{
			_engine.Dispose();
			_extractedLogBeingOpened?.Delete();
			_openExtractedLog?.Delete();
			_openExtractedLog = null;
		}

		private void RefreshHistory(ObservableCollection<string> history, IEnumerable<string> newValues)
		{
			_uiDispatcher.Invoke(() =>
			{
				history.Clear();

				foreach (var value in newValues)
				{
					history.Add(value);
				}
			});
		}

		/// <summary>
		/// Shows Weevil's Open As window for the log about to open, with RAM or Disk already chosen from its size and the free memory (#1028).
		/// </summary>
		/// <returns>The mode chosen, or <see langword="null"/> when the user cancelled.</returns>
		/// <remarks>
		/// The open's operation keeps anything else from running while the estimate runs and the window is shown.
		/// </remarks>
		private async Task<StorageMode?> AskStorageAsync(string logPath)
		{
			// Off the UI thread: the estimate reads up to 1 MB of the log, which takes a moment on a network drive.
			OpenAsAdvice advice = await Task.Run(() => OpenAsAdvice.Read(logPath));
			var isAccepted = _dialogBox.TryShowOpenAs(advice, out StorageMode storageMode);

			LogOpenAs(advice, isAccepted ? storageMode : null);

			return isAccepted ? storageMode : null;
		}

		/// <summary>
		/// Logs what Open As suggested and what the user chose, so the suggestion can be tuned from real use.
		/// </summary>
		/// <remarks>
		/// Not in the engine's context, which the sidecar saves.
		/// </remarks>
		/// <param name="advice">The advice for the log chosen, or <see langword="null"/> when none was worked out, for example when the user cancelled before selecting a log.</param>
		/// <param name="chosen">The mode chosen, or <see langword="null"/> when the user cancelled.</param>
		private static void LogOpenAs(OpenAsAdvice advice, StorageMode? chosen)
		{
			Log.Default.Write(
				LogSeverityType.Information,
				chosen is null ? "Open As was cancelled." : "Open As: a storage mode was chosen.",
				new Dictionary<string, object>
				{
					{ "FileSize", advice?.FileSize.Bytes },
					{ "EstimatedMemory", advice?.Estimate?.Bytes },
					{ "AvailableMemory", advice?.AvailableMemory?.Bytes },
					{ "IsOnNetworkDrive", advice?.IsOnNetworkDrive },
					{ "SuggestedStorageMode", advice?.DefaultMode },
					{ "ChosenStorageMode", chosen },
				});
		}

		/// <summary>
		/// Opens the part of the log chosen in the Open As dialog (#1214), with Weevil's options or the plugin's own (#1216).
		/// </summary>
		/// <remarks>
		/// Lines start reading at the first line's byte offset.
		/// </remarks>
		private static IEngineBuilder CreateBuilder(string sourceFilePath, LogPart part)
		{
			return part.SectionName is not null ? Engine.UsingPath(sourceFilePath).UsingSection(part.SectionName)
				: part.RegionName is not null ? Engine.UsingPath(sourceFilePath).UsingRegion(part.RegionName)
				: !part.IsWhole ? Engine.UsingPath(sourceFilePath, part.Lines.Start.Value).UsingRange(part.Lines)
				: Engine.UsingPath(sourceFilePath);
		}

		public Task OpenAsync(string sourceFilePath)
		{
			return OpenAsync(sourceFilePath, storageMode: null);
		}

		/// <param name="storageMode">RAM or Disk, or <see langword="null"/> for the engine's default: RAM, unless a Debug build's <c>WEEVIL_TEST_STORAGE</c> says otherwise.</param>
		/// <param name="displayPath">The log as the analyst knows it, when <paramref name="sourceFilePath"/> is a copy, such as one extracted from a zip (#1312).</param>
		/// <remarks>
		/// Does nothing while another operation runs, such as a filter or an analysis.
		/// </remarks>
		internal async Task OpenAsync(string sourceFilePath, StorageMode? storageMode, string displayPath = null)
		{
			using BusyState.Operation operation = _busy.TryBegin("The open", isShowingProgress: true, isFilterLocked: true);

			if (operation != null)
			{
				await OpenAsync(sourceFilePath, storageMode, displayPath, operation);
			}
		}

		/// <param name="operation">The open's operation. It ends once the records are shown, before the insights are ready.</param>
		/// <param name="part">The part of the log to open: the whole log when <see langword="null"/> (#1214).</param>
		/// <param name="context">What the log's plugin needs to read it, from its own Open As options (#1216): none when <see langword="null"/>.</param>
		private async Task OpenAsync(string sourceFilePath, StorageMode? storageMode, string displayPath, BusyState.Operation operation, LogPart part = null, ContextDictionary context = null)
		{
			// Until the records are shown, the progress bar replaces them and the filter box is disabled.
			operation.IsShowingProgress = true;
			operation.IsFilterLocked = true;
			this.AreInsightsReady = false;

			// The opened log's generation, for what runs after the open: a Reload may already have started another (#1028).
			Action<Exception> openedLogReporter = _ => { };

			await Task.Run(() =>
			{
				try
				{
					// The analyst may choose to stay on the open log, to keep changes its sidecar can't take yet (#1307).
					if (Engine.IsRealInstance(_engine) && !TrySaveBeforeLeaving(deleteBackup: true, isAskingFirst: true))
					{
						Log.Default.Write(LogSeverityType.Information, "The analyst chose to stay on the open log, whose changes can't be saved.");

						// The analyst keeps working on this log, whose filter box comes back when the operation ends.
						return;
					}

					Log.Default.Write($"File is being opened... Path={sourceFilePath}");

					IEngineBuilder builder = CreateBuilder(sourceFilePath, part ?? LogPart.Whole)
						.UsingTelemetry(TelemetrySessionLifecycle.Shared)
						.UsingContext(context ?? new ContextDictionary())
						.UsingDisplayPath(displayPath)
						.Replacing(_engine);

					if (storageMode is { } mode)
					{
						builder = builder.UsingStorage(mode);
					}

					IEngine openedEngine = builder.Open();

					// Once per open log: the engine keeps raising it after Clear and Reload (#1011).
					// Moved only once the open succeeds, so a failed open leaves the open log subscribed.
					_engine.FilterHistoryChanged -= OnFilterHistoryChanged;
					_engine.Annotations.Changed -= OnAnnotationsChanged;
					IEngine replaced = _engine;
					_engine = openedEngine;

					// Closed at once, so another Weevil can open it (#1254): saved before the open, and still read by what shows it until it is replaced.
					replaced.Dispose();

					_openLogFilePath = sourceFilePath;
					_hasUnsavedChanges = false;
					Interlocked.Increment(ref _logGeneration);
					openedLogReporter = CreateFailedReadReporter();
					_engine.FilterHistoryChanged += OnFilterHistoryChanged;
					_rowSelection = new RowSelection(_engine);
					_engine.Annotations.Changed += OnAnnotationsChanged;

					// The dashboard reads the engine it was shown with, so it would keep the previous log in memory,
					// and its insights would point at lines of that log (#1139).
					_uiDispatcher.Invoke(() => _dialogBox.CloseDashboard());

					this.FileOpened?.Invoke(this, EventArgs.Empty);

					_bulletinMediator.Post(new BookmarksChangedBulletin
					{
						BookmarkCount = _engine.Bookmarks.Bookmarks.Length,
					});

					_bulletinMediator.Post(new RegionsChangedBulletin
					{
						RegionCount = _engine.Regions.Regions.Length,
					});

					_bulletinMediator.Post(new SourceFileOpenedBulletin
					{
						SourceFilePath = _engine.SourceFilePath,
						SourceFileSize = _engine.Metrics.FileSize,
						SourceFileLoadingPeriod = _engine.Metrics.SourceFileLoadingPeriod,
						Context = _engine.Context,
						TotalRecordCount = _engine.Count,
						StorageMode = _engine.StorageMode,
						Part = _engine.Part,
					});

					// The Remarks box reads the engine, which is now the opened log's. Assigning the remarks to themselves would raise nothing (#1116).
					_uiDispatcher.Invoke(() => OnPropertyChanged(nameof(this.SourceFileRemarks)));

					_bulletinMediator.Post(new SourceFileRemarksChangedBulletin
					{
						HasSourceFileRemarks = _engine.SourceFileRemarks.Any()
					});

					_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));

					_bulletinMediator.Post(new AnalysisCompleteBulletin(0));

					Log.Default.Write("Updating filter history on the UI.");
					RefreshHistory(this.InclusiveFilterHistory, _engine.Filter.IncludeHistory);
					RefreshHistory(this.ExclusiveFilterHistory, _engine.Filter.ExcludeHistory);

					RefreshFilterResults();

					var analyzers = _engine
						.Analyzer
						.GetAnalyzers(ComponentType.Extension)
						.OrderBy(x => x.DisplayName)
						.ToArray();

					_uiDispatcher.Invoke(() =>
					{
						this.CustomAnalyzerCommands.Clear();

						foreach (IRecordAnalyzer analyzer in analyzers)
						{
							var menuItem = new MenuItemViewModel(
								analyzer.Key,
								analyzer.DisplayName,
								this.CustomAnalyzerCommand);

							this.CustomAnalyzerCommands.Add(menuItem);
						}
					});

					Log.Default.Write(
						LogSeverityType.Information,
						$"File has been opened. Path={sourceFilePath}");
				}
				catch (LogInUseException e)
				{
					// Core logged it. The open log stays.
					_dialogBox.ShowMessage(e.Message, LogInUseCaption);
				}
				catch (InvalidFileFormatException e)
				{
					var message = "Unknown File Format";
					Log.Default.Write(LogSeverityType.Error, e, message);
					_dialogBox.ShowMessage(e.Message, message);
				}
				catch (Exception e)
				{
					var message = "Unable to open the log file.";
					Log.Default.Write(LogSeverityType.Error, e, message);
					_dialogBox.ShowMessage(e.Message, message);
				}
				finally
				{
					// The records, the menus and the filter box come back without waiting for the insights.
					_uiDispatcher.Invoke(() =>
					{
						this.IsLogFileOpen = Engine.IsRealInstance(_engine);
						operation.Dispose();
					});
				}
			}).ContinueWith((x) =>
				{
					// Taken once: Reload or Clear can already run, and replace what the engine holds (#1028).
					IEngine engine = _engine;
					Action<Exception> reportFailedRead = openedLogReporter;

					try
					{
						// Continue computing insights asynchronously (UI is already enabled)
						if (engine.Navigate.TableOfContents.Sections.Count == 0)
						{
							engine.Navigate.RebuildTableOfContents();
						}

						_tableOfContents = engine.Navigate.TableOfContents;
						_insights = engine.Analyzer.GetInsights();

						_bulletinMediator.Post(new InsightChangedBulletin
						{
							HasInsight = _insights.Length > 0,
							InsightNeedingAttention = _insights.Count(i => i.IsAttentionRequired)
						});

						// Set insights ready and requery commands after insights are ready
						_uiDispatcher.Invoke(() =>
						{
							this.AreInsightsReady = true;
							OnPropertyChanged(nameof(this.IsDashboardEnabled));
							CommandManager.InvalidateRequerySuggested();
						});
					}
					catch (Exception e)
					{
						// Caught, so the open still finishes, and a zipped log's previous copy is still released (#1028).
						Log.Default.Write(LogSeverityType.Error, e, "The table of contents and the insights could not be prepared.");

						if (e is IOException or UnauthorizedAccessException)
						{
							reportFailedRead(e);
						}
					}
				}
			);
		}

		private void OnAnnotationsChanged(object sender, AnnotationChangedEventArgs e)
		{
			_hasUnsavedChanges = true;

			// On the thread that made the change: the rows refresh later, on the UI thread (decision 4).
			this.Rows?.QueueRefresh(e);
		}

		private void OnFilterHistoryChanged(object sender, HistoryChangedEventArgs e)
		{
			IFilter engine = _engine.Filter;

			_uiDispatcher.Invoke(() =>
			{
				// Changing the ComboBox history results in the Inclusive/Exclusive value being set to `null`.
				if (sender.Equals(engine.IncludeHistory))
				{
					switch (e.ChangeType)
					{
						case HistoryChangeType.Added:
							this.InclusiveFilterHistory.Insert(e.Index, e.Value);
							break;

						case HistoryChangeType.Removed:
							this.InclusiveFilterHistory.RemoveAt(e.Index);
							break;

						case HistoryChangeType.Moved:
							if (e.Index == 0)
							{
								// Do nothing.
								// ... Moving an item to it's own index position has an unexpected side effect
								// ... whereby the combo box sets it's `SelectedValue` to string.Empty
								// ... which we don't want because then the application thinks another filter
								// ... is being applied.
							}
							else
							{
								this.InclusiveFilterHistory.Move(e.Index, 0);
							}
							break;
					}
				}

				if (sender.Equals(engine.ExcludeHistory))
				{
					switch (e.ChangeType)
					{
						case HistoryChangeType.Added:
							this.ExclusiveFilterHistory.Insert(e.Index, e.Value);
							break;

						case HistoryChangeType.Removed:
							this.ExclusiveFilterHistory.RemoveAt(e.Index);
							break;

						case HistoryChangeType.Moved:
							if (e.Index == 0)
							{
								// Do nothing.
								// ... Moving an item to it's own index position has an unexpected side effect
								// ... whereby the combo box sets it's `SelectedValue` to string.Empty
								// ... which we don't want because then the application thinks another filter
								// ... is being applied.
							}
							else
							{
								this.ExclusiveFilterHistory.Move(e.Index, 0);
							}

							break;
					}
				}
			});
		}

		public void SaveMetadata()
		{
			_engine.Save();
			_hasUnsavedChanges = false;
		}

		/// <remarks>
		/// Does nothing while another operation runs, such as a filter or an analysis.
		/// </remarks>
		public void Reload()
		{
			BusyState.Operation operation = _busy.TryBegin("Reload", isShowingProgress: true, isFilterLocked: true);

			if (operation is null)
			{
				return;
			}

			// A read of the log as it was before Reload is no longer handled (#1028).
			Interlocked.Increment(ref _logGeneration);

			// Creating a background thread for processing.
			// ... Risk: any events raised by the Weevil library will execute on a background thead,
			// ... and not the UI thread which is required in order update/change the UI.
			Task.Run(async () =>
			{
				try
				{
					try
					{
						// Saved first: after a failed read, saving keeps only the comments and pins already saved (#1126).
						// Never asks: Reload keeps the changes in memory (#1107).
						TrySaveBeforeLeaving(deleteBackup: false, isAskingFirst: false);

						// The rows hold the old records, so drop them: Reload frees the records before it reads the log again (#1126).
						// The grid is hidden meanwhile.
						_uiDispatcher.Invoke(() =>
						{
							this.VisibleItems = null;
							this.Rows = null;
						});

						// Reload keeps the selection in Core (#1062).
						_engine.Reload();
					}
					catch (Exception e)
					{
						// What follows then shows what the engine holds: the log as it was if saving failed, or none of its records if reading failed.
						var message = "Unable to reload the log file.";
						Log.Default.Write(LogSeverityType.Error, e, message);
						_dialogBox.ShowMessage(e.Message, message);
					}

					// Reload keeps the engine's history (#1109); this keeps the lists shown in step with it.
					RefreshHistory(this.InclusiveFilterHistory, _engine.Filter.IncludeHistory);
					RefreshHistory(this.ExclusiveFilterHistory, _engine.Filter.ExcludeHistory);

					_bulletinMediator.Post(new SourceFileOpenedBulletin
					{
						SourceFilePath = _engine.SourceFilePath,
						SourceFileSize = _engine.Metrics.FileSize,
						SourceFileLoadingPeriod = _engine.Metrics.SourceFileLoadingPeriod,
						Context = _engine.Context,
						TotalRecordCount = _engine.Count,
						StorageMode = _engine.StorageMode,
						Part = _engine.Part,
					});

					RefreshFilterResults();

					RaiseResultsChanged();
				}
				finally
				{
					_uiDispatcher.Invoke(operation.Dispose);
				}
			});
		}

		public void ClipboardCopyRaw(bool readableCallstack)
		{
			if (IsWithinClipboardLimit("Copy", CopyAdvice))
			{
				IRecordFormatter formatter = readableCallstack
					? new SimpleCallStackFormatter() as IRecordFormatter
					: new RawRecordFormatter() as IRecordFormatter;

				try
				{
					ClipboardHelper.CopyRawFromSelected(_engine, Settings.Default.AddLineNumberPrefix, formatter);
				}
				catch (Exception e) when (e is IOException or UnauthorizedAccessException)
				{
					// The text of a log on disk that changed, or is gone (#1028).
					ReportFailedRead(e, Volatile.Read(ref _logGeneration));
				}
			}
		}

		public void ClipboardCopyLineNumbers()
		{
			if (IsWithinClipboardLimit("Copy", CopyAdvice))
			{
				ClipboardHelper.CopySelectedLineNumbers(_engine);
			}
		}

		public void ClipboardCopyTimestamps()
		{
			if (IsWithinClipboardLimit("Copy", CopyAdvice))
			{
				ClipboardHelper.CopySelectedTimestamps(_engine);
			}
		}

		/// <remarks>
		/// Not limited: it copies only the records with a comment, so selecting every record still copies a few lines.
		/// </remarks>
		public void ClipboardCopyComment()
		{
			ClipboardHelper.CopySelectedComments(_engine);
		}

		/// <summary>
		/// Copy and Paste (decision 5 of the #990 plan), and Graph (decided 2026-10-30), refuse more than <see cref="ClipboardLimit"/> selected records,
		/// and say what to do instead.
		/// </summary>
		/// <param name="action">The command's name, which is also the message's title.</param>
		/// <param name="advice">What to do instead.</param>
		private bool IsWithinClipboardLimit(string action, string advice)
		{
			var count = _engine.Selector.SelectedCount;

			if (count > ClipboardLimit)
			{
				_dialogBox.ShowMessage(
					$"{action} is limited to {ClipboardLimit:N0} records, and {count:N0} are selected.\n\n{advice}",
					action);

				return false;
			}

			return true;
		}

		public void ClipboardPaste(bool allowOverwrite)
		{
			if (IsWithinClipboardLimit("Paste", "Select fewer records, then paste again."))
			{
				ClipboardHelper.PasteToSelected(_engine, allowOverwrite);
				RefreshFilterResults();
			}
		}

		/// <summary>
		/// The row with keyboard focus, or -1 when there is none.
		/// </summary>
		[NotObservable]
		public int CaretPosition => this.Rows is null ? -1 : _rowSelection.GetCaretPosition(this.Rows);

		/// <summary>
		/// A row was clicked. WPF never selects a row: the click changes Core's selection (#1064).
		/// </summary>
		public void PressRow(RowViewModel row, bool isRightButton, ModifierKeys modifiers)
		{
			var position = this.Rows?.IndexOf(row) ?? -1;

			if (this.CanOpenLogFile && position >= 0)
			{
				if (isRightButton)
				{
					_rowSelection.RightClick(this.Rows, position, modifiers);
				}
				else
				{
					_rowSelection.Click(this.Rows, position, modifiers);
				}

				OnSelectionChanged();
			}
		}

		/// <summary>
		/// Keyboard focus moved into a row: the caret moves there, and the selection does not change.
		/// </summary>
		public void MoveCaretTo(RowViewModel row)
		{
			var position = this.Rows?.IndexOf(row) ?? -1;

			if (position >= 0)
			{
				_rowSelection.MoveCaretTo(this.Rows, position);
			}
		}

		/// <summary>
		/// A key was pressed in the grid.
		/// </summary>
		/// <returns><see langword="true"/> when it was a selection key.</returns>
		public bool PressKey(Key key, ModifierKeys modifiers, int pageSize, bool isOnRow)
		{
			if (!this.CanOpenLogFile || this.Rows is null)
			{
				return false;
			}

			var isHandled = _rowSelection.TryPressKey(this.Rows, key, modifiers, pageSize, isOnRow, out var scrollToPosition);

			if (isHandled)
			{
				OnSelectionChanged();

				if (scrollToPosition >= 0)
				{
					this.ScrollRequested?.Invoke(this, scrollToPosition);
				}
			}

			return isHandled;
		}

		private void OnSelectionChanged()
		{
			this.Rows.RefreshSelection();
			_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
		}

		public void DragOver(DragEventArgs e)
		{
			_dragAndDrop.DragOver(e);

			// The drop will be ignored, because another operation is in progress (#996).
			if (!this.CanOpenLogFile)
			{
				e.Effects = DragDropEffects.None;
			}
		}

		public void Drop(DragEventArgs e)
		{
			_dragAndDrop.Drop(e);
		}

		public void Exit()
		{
			TryExit();
		}

		/// <summary>
		/// Saves the sidecar and deletes the copies extracted from zips, as Weevil closes.
		/// </summary>
		/// <returns><see langword="false"/> when the analyst chose to stay, to keep changes the sidecar can't take yet (#1307): Weevil doesn't close.</returns>
		/// <remarks>
		/// Any other failure to save is shown, and Weevil still closes, as the Exit command did.
		/// </remarks>
		public bool TryExit()
		{
			if (Engine.IsRealInstance(_engine))
			{
				try
				{
					if (!TrySaveBeforeLeaving(deleteBackup: true, isAskingFirst: true))
					{
						Log.Default.Write(LogSeverityType.Information, "The analyst chose to stay, as the open log's changes can't be saved.");
						return false;
					}
				}
				catch (Exception exception)
				{
					var message = "Unable to save the sidecar.";
					Log.Default.Write(LogSeverityType.Error, exception, message);
					_dialogBox.ShowMessage(exception.Message, message);
				}
			}

			Close();
			return true;
		}

		/// <summary>
		/// Saves the sidecar before the open log is reloaded, replaced or closed.
		/// </summary>
		/// <remarks>
		/// Where the sidecar can never be written, as in a read-only folder, it logs a warning, as Clear does (#1246, #1307): no save could keep the changes there.
		/// When <paramref name="isAskingFirst"/> and the log has changes no save has written, the analyst chooses to carry on, losing them, or to stay,
		/// so the folder can be made writable and the log saved. Any other failure, such as a locked sidecar, still throws.
		/// The Save command calls <see cref="ICoreEngine.Save()"/> itself, so a save the analyst asked for still reports its failure.
		/// </remarks>
		/// <returns><see langword="false"/> when the analyst chose to stay.</returns>
		private bool TrySaveBeforeLeaving(bool deleteBackup, bool isAskingFirst)
		{
			try
			{
				_engine.Save(deleteBackup);
				_hasUnsavedChanges = false;
				return true;
			}
			catch (Exception exception) when (FileHelper.IsNeverWritable(exception))
			{
				Log.Default.Write(LogSeverityType.Warning, exception, "The sidecar can't be written.");

				if (!isAskingFirst || !_hasUnsavedChanges)
				{
					return true;
				}

				var logName = Path.GetFileName(_engine.SourceFilePath);
				var isContinuing = false;

				_uiDispatcher.Invoke(() => isContinuing = _dialogBox.ShowQuestion(
					$"Your changes to {logName}, such as comments and pins, can't be saved:\r\n{exception.Message}\r\n\r\n" +
					"Continue anyway, and lose them? Choose No to stay, so you can make the folder writable and save.",
					"Unable to Save Changes"));

				return isContinuing;
			}
		}

		/// <remarks>
		/// Runs in the background, so a log on disk does not freeze the window while it is read, and Abort (Escape) stops it (#1028).
		/// A save that stops keeps an earlier file (#1027).
		/// </remarks>
		private Task SaveSelected(FileFormatType fileFormatType)
		{
			IEngine engine = _engine;
			// The folder of the file read: for a zipped log, the folder kept for it (#1253, #1312).
			var destinationFolder = engine.SourceDirectory;

			var destinationFilePath = fileFormatType == FileFormatType.Tsv ?
				Path.Combine(destinationFolder, TsvFileName) :
				Path.Combine(destinationFolder, RawFileName);

			return RunInBackground("The save", operation =>
			{
				try
				{
					engine.Selector.SaveSelection(destinationFilePath, fileFormatType, operation.CancellationToken);
				}
				catch (IOException e) when (e.HResult.Equals(-2147024864))
				{
					Log.Default.Write(LogSeverityType.Error, e, "Unable to save the selected records. The target file is being used by another process.");
					_uiDispatcher.Invoke(() => _dialogBox.ShowMessage("Unable to save the selected records\r\nThe target file is being used by another process.", "Error"));
				}
			},
			progressDelay: QuickWorkProgressDelay);
		}

		public void ShowFileExplorer()
		{
			WindowsProcess.Start(
				WindowsProcessType.FileExplorer,
				_engine.SourceDirectory);
		}

		public void ShowRegExTool()
		{
			WindowsProcess.Start(
				WindowsProcessType.DefaultApplication,
				RegEx101Url.ToString());
		}

		public void ShowApplicationLogFile()
		{
			WindowsProcess.Start(WindowsProcessType.FileExplorer, NewReleaseFilePath);
		}

		public void ShowHelp()
		{
			TelemetrySessionLifecycle.Shared.RecordHelpOpen();

			if (File.Exists(HelpFilePath))
			{
				WindowsProcess.Start(
					WindowsProcessType.DefaultApplication,
					HelpFilePath);
			}
			else
			{
				MessageBox.Show("Unable to locate the requested help file.");
			}
		}

		public void ShowAbout()
		{
			try
			{
				var dialog = new AboutDialog(
					_uiDispatcher,
					this.WeevilDisplayVersion,
					LicensePath,
					ThirdPartyNoticesPath,
					_openLogFilePath);

				dialog.ShowDialog();
			}
			catch (Exception e)
			{
				Log.Default.Write(
					LogSeverityType.Information,
					e);
				MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <remarks>
		/// Runs in the background, and Abort (Escape) stops it (#1028).
		/// </remarks>
		private Task SplitCurrentLog()
		{
			// The splits are saved beside the file read, and read the log through its records.
			var splitter = new LogFileSplitter(_openLogFilePath);
			RecordList records = _engine.Filter.Results;

			return RunInBackground("The split", operation => splitter.Run(records, operation.CancellationToken), progressDelay: QuickWorkProgressDelay);
		}

		private void ShowDashboard()
		{
			TelemetrySessionLifecycle.Shared.RecordDashboardOpen();

			// A plugin may read the file to tell whether it is one of its logs.
			IPlugin plugin = new PluginFactory().Create(_openLogFilePath);

			if (plugin.CanShowDashboard)
			{
				plugin.ShowDashboard(this.WeevilVersion, _engine, _insights.ToArray());
			}
			else
			{
				_dialogBox.ShowDashboard(this.WeevilVersion, _engine, _insights, _bulletinMediator);
			}
		}

		/// <summary>
		/// Copies the records with their text, so a graph never reads a log: it can outlive the log it was opened for (#1028).
		/// </summary>
		/// <remarks>
		/// One buffer for the whole pass, so each chunk of a log on disk is read once (#1027).
		/// </remarks>
		/// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
		private static RecordList CopyWithText(RecordList records, CancellationToken cancellationToken)
		{
			var buffer = new ContentBuffer();
			var copies = ImmutableArray.CreateBuilder<IRecord>(records.Count);

			foreach (IRecord record in records)
			{
				cancellationToken.ThrowIfCancellationRequested();

				copies.Add(new Record(
					record.LineNumber,
					record.CreatedAt,
					record.Severity,
					buffer.Read(record).ToString(),
					record.IsMultiLine,
					record.WasGeneratedByUi));
			}

			return copies.MoveToImmutable();
		}

		/// <summary>
		/// Returns what a reader gives a failed read of the open log to: it carries the log's generation, so a failure from an earlier one is ignored (#1028).
		/// </summary>
		private Action<Exception> CreateFailedReadReporter()
		{
			var generation = Volatile.Read(ref _logGeneration);

			return exception => ReportFailedRead(exception, generation);
		}

		/// <summary>
		/// Handles a read of a log on disk that failed, from any thread (#1028).
		/// </summary>
		/// <remarks>
		/// Always queued, so a read that fails while the grid draws its rows, or on a background thread, never shows a message from inside that read.
		/// </remarks>
		private void ReportFailedRead(Exception exception, int generation)
		{
			_uiDispatcher.BeginInvoke(() => OnFailedRead(exception, generation), DispatcherPriority.Background);
		}

		/// <summary>
		/// Shows one warning, naming Ctrl+F5, for a log that changed on disk, or one error for a log that can no longer be read, once per log (#1028).
		/// </summary>
		/// <remarks>
		/// Nothing reloads by itself: the analyst decides when to read the latest data (#1247).
		/// Each open and each Reload starts a new generation, so a failure from an earlier one, such as a row still on screen from before Reload, is ignored.
		/// Later failures of the same kind and generation show nothing, so scrolling a log that is gone does not repeat the message.
		/// </remarks>
		private void OnFailedRead(Exception exception, int generation)
		{
			// A log that is gone can't be reloaded, so Ctrl+F5 is named only for one that changed (#1295).
			var isChange = exception is LogFileChangedException { IsUnreadable: false };

			// A change is reported even after the log could not be read, for example while it was replaced.
			if (generation != Volatile.Read(ref _logGeneration) ||
			    generation == (isChange ? _changedLogGeneration : _unreadableLogGeneration))
			{
				return;
			}

			// A filter, an open or other work is running: handle it once that has ended, if the log is still the same (#1257).
			if (_busy.IsBusy)
			{
				_ = _busy.Idle.ContinueWith(_ => ReportFailedRead(exception, generation), TaskScheduler.Default);
				return;
			}

			if (isChange)
			{
				_changedLogGeneration = generation;
				Log.Default.Write(LogSeverityType.Warning, exception, "The log changed on disk. The analyst is told to press Ctrl+F5 to reload it.");
				_dialogBox.ShowMessage("The log changed on disk. Press Ctrl+F5 to reload the latest data.", "Log Changed");
			}
			else
			{
				_unreadableLogGeneration = generation;
				Log.Default.Write(LogSeverityType.Error, exception, "The log can no longer be read.");
				// What the read threw, rather than the log's own exception, which repeats the first sentence for the application's log (#1295).
				var reason = exception is LogFileChangedException { IsUnreadable: true, InnerException: { } cause } ? cause.Message : exception.Message;
				// Named as the analyst knows it: the reason may name the copy Weevil reads (#1312).
				_dialogBox.ShowMessage($"Weevil can't read the log {_engine.SourceFilePath}.\r\n\r\n{reason}", "Error");
			}
		}

		/// <remarks>
		/// Like Copy and Paste, the graph takes at most <see cref="ClipboardLimit"/> selected records (decided 2026-10-30).
		/// Its window can outlive the log, so it keeps its own array of them, not the log's list of records (#1139).
		/// On disk it also copies their text, once the expressions are confirmed, so it never reads a log that is gone (#1028).
		/// </remarks>
		private void GraphData()
		{
			if (!IsWithinClipboardLimit("Graph", "Select fewer records, then graph again."))
			{
				return;
			}

			var recordsDescription = _engine.Selector.SelectedCount.ToString("N0");

			if (!_dialogBox.TryGetExpressions(_inclusiveFilter, recordsDescription, out var confirmedFilter))
			{
				return;
			}

			IEngine engine = _engine;
			RecordList selected = engine.Selector.GetSelected().ToImmutableArray();
			var sourceFilePath = engine.SourceFilePath;

			// In RAM a record keeps its text, so only a log on disk needs the copy.
			if (engine.StorageMode != StorageMode.Disk)
			{
				ShowGraph(selected, confirmedFilter, sourceFilePath);
				return;
			}

			// The copy reads up to 100,000 records from the log, slowly on a network drive, so it runs in the background, where Escape stops it (#1028).
			RecordList records = RecordList.Empty;

			_ = RunInBackground(
				"The graph",
				operation => records = CopyWithText(selected, operation.CancellationToken),
				onSucceeded: () => ShowGraph(records, confirmedFilter, sourceFilePath),
				progressDelay: QuickWorkProgressDelay);
		}

		private void ShowGraph(RecordList records, string confirmedFilter, string sourceFilePath)
		{
			TelemetrySessionLifecycle.Shared.RecordGraphOpen();

			_dialogBox.ShowGraph(records, confirmedFilter, sourceFilePath);
		}

		private void ForceGarbageCollection()
		{
			GC.Collect(3, GCCollectionMode.Forced, true, true);
		}
		#endregion

		#region Commands: Filtering

		/// <remarks>
		/// Clear saves the sidecar, then narrows the engine's records and filters them again, which takes seconds on a large log, so it runs in the background,
		/// as Reload does, with the progress bar a filter shows (#1071). It does nothing while another operation runs, such as a filter or an analysis.
		/// </remarks>
		public Task ClearRecords(ClearOperation operation)
		{
			BusyState.Operation clearing = _busy.TryBegin("Clear", isShowingProgress: true, isFilterLocked: true);

			if (clearing is null)
			{
				return Task.CompletedTask;
			}

			return Task.Run(() =>
			{
				var wasCleared = false;

				try
				{
					// The rows hold records that the Clear may remove, so they are dropped first, as Reload does (#1139):
					// once Clear frees the memory of cleared records (#1022, #1140), the grid must not keep them. The grid is hidden meanwhile.
					_uiDispatcher.Invoke(() =>
					{
						this.VisibleItems = null;
						this.Rows = null;
					});

					_engine.Clear(operation);
					wasCleared = true;

					_bulletinMediator.Post(new ClearRecordsBulletin
					{
						TotalRecordCount = _engine.Count
					});

					RefreshFilterResults();
					RaiseResultsChanged();
				}
				catch (Exception exception)
				{
					var message = "Unable to clear the records.";

					Log.Default.Write(LogSeverityType.Error, exception, message);
					_uiDispatcher.Invoke(() => MessageBox.Show(message, "Clear Failed", MessageBoxButton.OK, MessageBoxImage.Error));

					// The grid shows the records the engine still holds.
					RefreshFilterResults();
				}
				finally
				{
					_uiDispatcher.Invoke(() =>
					{
						clearing.Dispose();

						// Clear resets the engine's filter, so the filter shown is applied again.
						if (wasCleared)
						{
							FilterAsynchronously(_currentfilterType, _currentfilterCriteria);
						}
					});
				}
			});
		}

		public void Filter()
		{
			var filters = new object[] { _inclusiveFilter, _exclusiveFilter };
			FilterManually(filters);
		}

		public void FilterManually(object[] filters)
		{
			ApplyFilterBoxes(filters[0].ToString(), filters[1].ToString());
		}

		/// <summary>
		/// Applies the filter boxes' text, as the Filter button does.
		/// </summary>
		/// <returns>A task that completes with <see langword="true"/> once this filter's results are shown (see <see cref="FilterAsynchronously"/>).</returns>
		private Task<bool> ApplyFilterBoxes(string inclusiveFilter, string exclusiveFilter)
		{
			_inclusiveFilter = inclusiveFilter;
			_exclusiveFilter = exclusiveFilter;

			var filterCriteria = new FilterCriteria(_inclusiveFilter, _exclusiveFilter, GetFilterConfiguration());

			return FilterAsynchronously(this.FilterExpressionType, filterCriteria);
		}

		public void FilterOrCancel(object[] filters)
		{
			if (this.IsFilterInProgress)
			{
				_engine.Filter.Abort();
			}
			else
			{
				FilterManually(filters);
			}
		}

		private void FilterByComment()
		{
			var configuration = GetFilterConfiguration();
			var filter = new FilterCriteria("@Comment", string.Empty, configuration);

			FilterAsynchronously(FilterType.PlainText, filter);
		}

		private void FilterByPinned()
		{
			var configuration = GetFilterConfiguration();
			var filter = new FilterCriteria("@Pinned", string.Empty, configuration);

			FilterAsynchronously(FilterType.PlainText, filter);
		}

		private void FilterByRegions()
		{
			var configuration = GetFilterConfiguration();
			var filter = new FilterCriteria("@Regions", string.Empty, configuration);

			FilterAsynchronously(FilterType.PlainText, filter);
		}

		private void FilterByBookmarks()
		{
			var configuration = GetFilterConfiguration();
			var filter = new FilterCriteria("@Bookmarks", string.Empty, configuration);

			FilterAsynchronously(FilterType.PlainText, filter);
		}

		public void ToggleFilters()
		{
			if (_engine.Filter.Criteria.Equals(FilterCriteria.None))
			{
                                FilterAsynchronously(_previousFilterType, _previousFilterCriteria);
			}
			else
			{
                                _previousFilterCriteria = (FilterCriteria)_engine.Filter.Criteria;
                                _previousFilterType = _currentfilterType;
                                FilterAsynchronously(FilterType.RegularExpression, FilterCriteria.None);
			}
		}

		/// <summary>
		/// Runs the current filter again (F5) on a background thread, as any filter runs, so the progress bar shows and Escape stops it (#1245).
		/// </summary>
		/// <remarks>
		/// On disk, a filter reads every record's text back from the log, so on the UI thread it froze the window.
		/// Once it finishes, the results and the columns' widths are refreshed (<see cref="ResultsChanged"/>).
		/// A log that changed, or is gone, is reported once, as any other read (#1028).
		/// </remarks>
		private void Refresh()
		{
			IFilter filter = _engine.Filter;

			// To verify #1245 in Weevil's log: this line has the UI thread's ThreadId, and Core's "Re-applying the most recent filter" that follows has another.
			Log.Default.Write(
				LogSeverityType.Information,
				"Refresh (F5) queued the filter to run again on a background thread.");

			FilterAsynchronously(filter.FilterType, filter.Criteria, () => _engine.ReApplyAsync(CancellationToken.None));
		}

		/// <summary>
		/// Stops the filter or the analysis in progress (#1027).
		/// </summary>
		private void AbortFilter()
		{
			_engine.Filter.Abort();
			_busy.Cancel();
		}
		#endregion

		#region Commands: Navigation

		private void FindText()
		{
			// Use local variables for out parameters since Metalama converts fields to properties
			bool isCaseSensitive;
			bool findNext;
			bool useRegex;
			string findText;
			bool searchElapsedTime;
			int? minElapsedMs;
			int? maxElapsedMs;
			bool searchComments;
			
			if (_dialogBox.TryShowFind(
				_findText, 
				out isCaseSensitive, 
				out findNext, 
				out useRegex, 
				out findText,
				out searchElapsedTime,
				out minElapsedMs,
				out maxElapsedMs,
				out searchComments))
			{
				// Assign to fields after the method call
				_findIsCaseSensitive = isCaseSensitive;
				_findUseRegex = useRegex;
				_findText = findText;
				_findSearchElapsedTime = searchElapsedTime;
				_findMinElapsedMs = minElapsedMs;
				_findMaxElapsedMs = maxElapsedMs;
				_findSearchComments = searchComments;
				
				if (_findSearchElapsedTime)
				{
					if (findNext)
					{
						FindNextElapsedTime(_findMinElapsedMs, _findMaxElapsedMs);
					}
					else
					{
						FindPreviousElapsedTime(_findMinElapsedMs, _findMaxElapsedMs);
					}
				}
				else if (_findSearchComments)
				{
					if (findNext)
					{
						FindNextComment();
					}
					else
					{
						FindPreviousComment();
					}
				}
				else
				{
					if (findNext)
					{
						FindNext();
					}
					else
					{
						FindPrevious();
					}
				}
			}
		}

		private void FindNext()
		{
			if (_findSearchElapsedTime)
			{
				FindNextElapsedTime(_findMinElapsedMs, _findMaxElapsedMs);
			}
			else if (_findSearchComments)
			{
				FindNextComment();
			}
			else if (!string.IsNullOrWhiteSpace(_findText))
			{
				FindContent(findNext: true);
			}
		}

		private void FindPrevious()
		{
			if (_findSearchElapsedTime)
			{
				FindPreviousElapsedTime(_findMinElapsedMs, _findMaxElapsedMs);
			}
			else if (_findSearchComments)
			{
				FindPreviousComment();
			}
			else if (!string.IsNullOrWhiteSpace(_findText))
			{
				FindContent(findNext: false);
			}
		}

		/// <summary>
		/// Searches the filter results for the text last entered in Find, on a background thread, because it reads each record's content:
		/// from the log itself in disk mode. Abort (Escape) stops it, and leaves the active record where it was (#1027, #1028).
		/// </summary>
		/// <remarks>
		/// The other searches, such as for a comment or an elapsed time, read no content, so they stay on the UI thread.
		/// </remarks>
		private Task FindContent(bool findNext)
		{
			IEngine engine = _engine;
			var text = _findText;
			var isCaseSensitive = _findIsCaseSensitive;
			var useRegex = _findUseRegex;
			var userMessage = $"Unable to find the provided text in the search results.\r\n\r\nSearching for: {text}\r\nCase sensitive: {isCaseSensitive}\r\nUse regex: {useRegex}";

			int? position = null;

			return RunInBackground(
				"The search",
				operation =>
				{
					try
					{
						IRecord found = findNext
							? engine.Navigate.NextContent(text, isCaseSensitive, useRegex, operation.CancellationToken)
							: engine.Navigate.PreviousContent(text, isCaseSensitive, useRegex, operation.CancellationToken);

						position = found.ToIndexUsing(engine.Filter.Results);
					}
					catch (RecordNotFoundException e)
					{
						Log.Default.Write(LogSeverityType.Warning, e, userMessage);

						// Through the dialog service, so a test that finds nothing gets no message box, which would wait for a click forever.
						_uiDispatcher.Invoke(() => _dialogBox.ShowMessage(userMessage, "Not Found"));
					}
				},
				onSucceeded: () =>
				{
					if (position is { } foundPosition)
					{
						// Queued behind the layout that shows the records again, so the record found can take the focus (#1028).
						_uiDispatcher.BeginInvoke(() => ShowFoundRecord(foundPosition), DispatcherPriority.Background);
					}
				},
				progressDelay: QuickWorkProgressDelay);
		}

		private void FindNextComment()
		{
			if (!string.IsNullOrWhiteSpace(_findText))
			{
				SearchFilterResults(
					$"Unable to find the provided text in comments.\r\n\r\nSearching for: {_findText}\r\nCase sensitive: {_findIsCaseSensitive}\r\nUse regex: {_findUseRegex}",
					() => _engine
						.Navigate
						.NextCommentWithText(_findText, _findIsCaseSensitive, _findUseRegex)
						.ToIndexUsing(_engine.Filter.Results));
			}
		}

		private void FindPreviousComment()
		{
			if (!string.IsNullOrWhiteSpace(_findText))
			{
				SearchFilterResults(
					$"Unable to find the provided text in comments.\r\n\r\nSearching for: {_findText}\r\nCase sensitive: {_findIsCaseSensitive}\r\nUse regex: {_findUseRegex}",
					() => _engine
						.Navigate
						.PreviousCommentWithText(_findText, _findIsCaseSensitive, _findUseRegex)
						.ToIndexUsing(_engine.Filter.Results));
			}
		}

		private void FindNextElapsedTime(int? minMilliseconds, int? maxMilliseconds)
		{
			var minDesc = minMilliseconds.HasValue ? minMilliseconds.Value.ToString() : "none";
			var maxDesc = maxMilliseconds.HasValue ? maxMilliseconds.Value.ToString() : "none";
			
			SearchFilterResults(
				$"Unable to find a record with the specified elapsed time in the search results.\r\n\r\nMinimum (ms): {minDesc}\r\nMaximum (ms): {maxDesc}",
				() => _engine
					.Navigate
					.NextElapsedTime(minMilliseconds, maxMilliseconds)
					.ToIndexUsing(_engine.Filter.Results));
		}

		private void FindPreviousElapsedTime(int? minMilliseconds, int? maxMilliseconds)
		{
			var minDesc = minMilliseconds.HasValue ? minMilliseconds.Value.ToString() : "none";
			var maxDesc = maxMilliseconds.HasValue ? maxMilliseconds.Value.ToString() : "none";
			
			SearchFilterResults(
				$"Unable to find a record with the specified elapsed time in the search results.\r\n\r\nMinimum (ms): {minDesc}\r\nMaximum (ms): {maxDesc}",
				() => _engine
					.Navigate
					.PreviousElapsedTime(minMilliseconds, maxMilliseconds)
					.ToIndexUsing(_engine.Filter.Results));
		}

		public void GoTo()
		{
			if (_dialogBox.TryShowGoTo(string.Empty, out var userValue))
			{
				if (string.IsNullOrWhiteSpace(userValue))
				{
					var message = "A timestamp or line number is needed to perform the GoTo operation.";

					Log.Default.Write(
						LogSeverityType.Error,
						message);

					MessageBox.Show(message,
						"Error",
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				}
				else
				{
					// Did the user provide a timestamp?
					if (userValue.Contains(":"))
					{
						SearchFilterResults(
							$"Unable to find the timestamp in the search results. Value={userValue}",
							() => _engine
								.Navigate
								.GoTo(userValue, RecordSearchType.NearestNeighbor)
								.ToIndexUsing(_engine.Filter.Results));
					}
					else
					{
						var validNumberFormat =
							NumberStyles.AllowLeadingWhite |
							NumberStyles.AllowTrailingWhite |
							NumberStyles.AllowThousands;

						if (int.TryParse(userValue, validNumberFormat, CultureInfo.InvariantCulture, out var lineNumber))
						{
							SearchFilterResults(
								$"Unable to find the line number in the search results. Value={userValue}",
								() => _engine
									.Navigate
									.GoTo(lineNumber, RecordSearchType.NearestNeighbor)
									.ToIndexUsing(_engine.Filter.Results));
						}
						else
						{
							var message = "Unable to perform the GoTo operation. The provided value is expected to be a timestamp or line number.";

							Log.Default.Write(
								LogSeverityType.Error,
								message);

							MessageBox.Show(message,
								"Error",
								MessageBoxButton.OK,
								MessageBoxImage.Error);
						}
					}
				}
			}
		}


		private void NavigateToSourceCode()
		{
			try
			{
				// The count first, so a large selection creates no records (#1139).
				RecordList selected = _engine.Selector.GetSelected();
				IRecord selectedRecord = selected.Count == 1 ? selected[0] : null;
				if (selectedRecord == null)
				{
					MessageBox.Show("A single record must be selected.", "Source Code", MessageBoxButton.OK, MessageBoxImage.Information);
					return;
				}

				var target = TryExtractSourceTarget(selectedRecord.Content);
				if (string.IsNullOrWhiteSpace(target))
				{
					MessageBox.Show("No source code location or symbol could be extracted from the selected record.", "Source Code", MessageBoxButton.OK, MessageBoxImage.Information);
					return;
				}

				if (Process.GetProcessesByName("devenv").Length == 0)
				{
					MessageBox.Show("Visual Studio is not running. Please start Visual Studio 2026 and open the appropriate source code, then try again.", "Source Code", MessageBoxButton.OK, MessageBoxImage.Information);
					return;
				}

				Clipboard.SetText(target);
				MessageBox.Show($"Target copied to clipboard:\r\n\r\n{target}\r\n\r\nPaste into Visual Studio navigation (e.g. Ctrl+T) to open the location.", "Source Code", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception e)
			{
				Log.Default.Write(LogSeverityType.Error, e, "Unexpected error while navigating to source code.");
			}
		}

		private string TryExtractSourceTarget(string logEntry)
		{
			if (string.IsNullOrWhiteSpace(logEntry))
			{
				return string.Empty;
			}

			try
			{
				var appSettingsPath = Path.Combine(EnvironmentHelper.GetExecutableDirectory(), "appsettings.json");
				if (!File.Exists(appSettingsPath))
				{
					return string.Empty;
				}

				var json = JObject.Parse(File.ReadAllText(appSettingsPath));
				var sourceSection = json["SourceCodeNavigation"];
				var fileRegex = sourceSection?["FilePathAndLineRegex"]?.ToString();
				var symbolRegex = sourceSection?["FullyQualifiedMemberRegex"]?.ToString();

				if (!string.IsNullOrWhiteSpace(fileRegex))
				{
					var match = Regex.Match(logEntry, fileRegex);
					if (match.Success && match.Groups["path"].Success && match.Groups["line"].Success)
					{
						var path = match.Groups["path"].Value.Trim();
						var line = int.TryParse(match.Groups["line"].Value, out var rawLine) ? Math.Max(1, rawLine) : 1;
						return $"{path}({line},1)";
					}
				}

				if (!string.IsNullOrWhiteSpace(symbolRegex))
				{
					var match = Regex.Match(logEntry, symbolRegex);
					if (match.Success)
					{
						return match.Groups["symbol"].Success ? match.Groups["symbol"].Value.Trim() : match.Value.Trim();
					}
				}
			}
			catch (Exception e)
			{
				Log.Default.Write(LogSeverityType.Warning, e, "Unable to extract source code target from appsettings or record content.");
			}

			return string.Empty;
		}

		private void OnNavigateToInsightRecord(NavigateToInsightRecordBulletin bulletin)
		{
			// An analysis, a filter or an open is in progress, and may be changing the same flags (#996).
			if (!this.IsMenuEnabled)
			{
				return;
			}

			if (bulletin?.RelatedLineNumbers != null && bulletin.RelatedLineNumbers.Count > 0)
			{
				// Option 6: Flag insight records and add @Flagged to filter
				
				// Steps 1 and 2: Core replaces the flags with those of the insight records still in memory (#1013)
				var insightLineNumbers = new HashSet<int>(bulletin.RelatedLineNumbers);
				var insightRecordCount = insightLineNumbers.Count;
				var successfullyFlaggedCount = _engine.Analyzer.FlagOnly(insightLineNumbers);

				// Check if any records were missing and notify user
				if (successfullyFlaggedCount < insightRecordCount)
				{
					var missingRecordCount = insightRecordCount - successfullyFlaggedCount;
					var message = $"{missingRecordCount} insight-related record(s) were cleared and could not be flagged.";

					Log.Default.Write(
						LogSeverityType.Warning,
						$"Insight navigation: {missingRecordCount} out of {insightRecordCount} records are no longer available in memory.");

					_uiDispatcher.Invoke(() =>
					{
						MessageBox.Show(
							$"Some insight-related records were cleared and are no longer available. Flagging has been adjusted.\n\n{message}",
							"Records Not Available",
							MessageBoxButton.OK,
							MessageBoxImage.Information);
					});
				}

				// Step 3: Append @Flagged to include filter
				var currentIncludeFilter = _inclusiveFilter ?? string.Empty;
				string newIncludeFilter;

				// Check if @Flagged already exists in the filter (case-insensitive)
				if (currentIncludeFilter.Contains("@Flagged", StringComparison.OrdinalIgnoreCase))
				{
					// Already has @Flagged, no need to append
					newIncludeFilter = currentIncludeFilter;
				}
				else if (string.IsNullOrWhiteSpace(currentIncludeFilter))
				{
					// Empty filter, just set to @Flagged
					newIncludeFilter = "@Flagged";
				}
				else
				{
					// Append ||@Flagged to existing filter
					newIncludeFilter = $"{currentIncludeFilter}||@Flagged";
				}

				// Step 4: Apply the filter, as the Filter button does. Setting the filter box's text alone filters nothing (#998).
				Log.Default.Write(
					LogSeverityType.Information,
					$"Insight navigation is filtering to show the insight's records, then going to the first. Include=`{newIncludeFilter}`");

				var firstLineNumber = bulletin.RelatedLineNumbers[0];

				// Step 5: Go to the first record once the filter's results are shown, however long the filter takes (#998).
				ApplyFilterBoxes(newIncludeFilter, _exclusiveFilter).ContinueWith(
					filter =>
					{
						if (filter.Status != TaskStatus.RanToCompletion || !filter.Result)
						{
							Log.Default.Write(
								LogSeverityType.Information,
								$"Insight navigation is not going to the record, because the filter did not show its results. Line={firstLineNumber}");
							return;
						}

						_uiDispatcher.Invoke(() =>
						{
							// The exclude filter can still hide the insight's records: go to the first one shown, never to a neighbour (#998).
							var lineNumber = FindFirstShown(_engine.Filter.Results, bulletin.RelatedLineNumbers);

							if (lineNumber is null)
							{
								var message = "The insight's records are not shown, because the exclude filter hides them.";
								Log.Default.Write(LogSeverityType.Warning, $"{message} FirstLine={firstLineNumber}");
								_dialogBox.ShowMessage(message, "Not Found");
								return;
							}

							SearchFilterResults(
								$"Unable to find the insight's related record in the search results. Line={lineNumber}",
								() => _engine
									.Navigate
									.GoTo(lineNumber.Value, RecordSearchType.ExactMatch)
									.ToIndexUsing(_engine.Filter.Results));
						});
					},
					TaskScheduler.Default);
			}
		}

		/// <summary>
		/// Returns the first of <paramref name="lineNumbers"/> that <paramref name="results"/> shows, or <see langword="null"/> when it shows none.
		/// </summary>
		private static int? FindFirstShown(RecordList results, IEnumerable<int> lineNumbers)
		{
			foreach (var lineNumber in lineNumbers)
			{
				try
				{
					if (results.IndexOfLineNumber(lineNumber) >= 0)
					{
						return lineNumber;
					}
				}
				catch (RecordNotFoundException)
				{
					// Not shown: try the next.
				}
			}

			return null;
		}

		public void GoToPreviousPin()
		{
			SearchFilterResults(
				$"Unable to find a pinned record in the search results.",
				() => _engine
					.Navigate
					.PreviousPin()
					.ToIndexUsing(_engine.Filter.Results));
		}

		public void GoToNextPin()
		{
			SearchFilterResults(
				$"Unable to find a pinned record in the search results.",
				() => _engine
					.Navigate
					.NextPin()
					.ToIndexUsing(_engine.Filter.Results));
		}

		public void GoToPreviousFlag()
		{
			SearchFilterResults(
				$"Unable to find a flagged record in the search results.",
				() => _engine
					.Navigate
					.PreviousFlag()
					.ToIndexUsing(_engine.Filter.Results));
		}

		public void GoToNextFlag()
		{
			SearchFilterResults(
				$"Unable to find a flagged record in the search results.",
				() => _engine
					.Navigate
					.NextFlag()
					.ToIndexUsing(_engine.Filter.Results));
		}

		public void GoToPreviousComment()
		{
			SearchFilterResults(
				$"Unable to find a comment in the search results.",
				() => _engine
					.Navigate
					.PreviousComment()
					.ToIndexUsing(_engine.Filter.Results));
		}

		public void GoToNextComment()
		{
			SearchFilterResults(
				$"Unable to find a comment in the search results.",
				() => _engine
					.Navigate
					.NextComment()
					.ToIndexUsing(_engine.Filter.Results));
		}

		#endregion

		#region Commands: Analysis
		private void SaveCommentSummary()
		{
			_engine.GenerateReport(ReportType.CommentSummary, _engine.SourceDirectory);
		}

		public Task Analyze(AnalysisType analysisType)
		{
			return AnalyzeInBackground((userDialog, cancellationToken) => _engine.Analyzer.Analyze(analysisType, userDialog, cancellationToken));
		}

		public Task Analyze(string customAnalyzerKey)
		{
			return AnalyzeInBackground((userDialog, cancellationToken) => _engine.Analyzer.Analyze(customAnalyzerKey, userDialog, cancellationToken));
		}

		/// <summary>
		/// Runs an analyzer on a background thread, so the window stays responsive (#996).
		/// </summary>
		/// <remarks>
		/// Its prompts are shown on the UI thread, with the records visible behind the modal prompt.
		/// Abort (Escape) stops it: the comments, pins and flags it changed so far stay (#1027).
		/// </remarks>
		private Task AnalyzeInBackground(Func<IUserDialog, CancellationToken, Results> analyze)
		{
			return RunInBackground("The analysis", operation =>
			{
				var userDialog = new UiThreadUserDialog(
					_dialogBox,
					_uiDispatcher,
					promptOpening: () => operation.IsShowingProgress = false,
					promptClosed: () => operation.IsShowingProgress = true);

				Results results = analyze(userDialog, operation.CancellationToken);

				_bulletinMediator.Post(new AnalysisCompleteBulletin(
					results.FlaggedRecords,
					results.Data));
			});
		}

		/// <summary>
		/// Runs work that reads the log, such as an analysis or a save, on a background thread, so the window stays responsive (#996, #1028).
		/// </summary>
		/// <remarks>
		/// Until the work finishes, the menus and the filter are disabled, and after <paramref name="progressDelay"/> the progress bar replaces the records,
		/// so the user cannot edit a comment or a pin that the work may be writing to, nor start other work.
		/// Work that only reads, such as a save, takes a delay, so quick work does not flicker; until then the records can still be clicked.
		/// Abort (Escape) cancels the operation's token, which the work reads (#1027). An unexpected error is logged and shown.
		/// Does nothing while another operation runs, such as a filter or an open.
		/// </remarks>
		/// <param name="name">What the work does, for the log and the error message, such as "The analysis".</param>
		/// <param name="work">Runs on a background thread, given the operation it runs in. It marshals anything it shows to the UI thread.</param>
		/// <param name="onSucceeded">
		/// Runs on the UI thread once the menus, the filter and the records are back, when the work finished without being aborted,
		/// such as going to the record found, which needs the records shown to take the focus.
		/// </param>
		/// <param name="progressDelay">How long the work runs before the progress bar replaces the records.</param>
		private Task RunInBackground(string name, Action<BusyState.Operation> work, Action onSucceeded = null, TimeSpan progressDelay = default)
		{
			// Without a log there is nothing to work on.
			BusyState.Operation operation = this.IsLogFileOpen
				? _busy.TryBegin(name, isShowingProgress: progressDelay <= TimeSpan.Zero, isFilterLocked: true, canCancel: true)
				: null;

			if (operation is null)
			{
				return Task.CompletedTask;
			}

			var hasFinished = false;
			Action<Exception> reportFailedRead = CreateFailedReadReporter();

			if (progressDelay > TimeSpan.Zero)
			{
				// Showing the progress bar once the operation has ended does nothing.
				_ = Task.Delay(progressDelay).ContinueWith(
					_ =>
					{
						if (!operation.Ended.IsCompleted)
						{
							_uiDispatcher.Invoke(() => operation.IsShowingProgress = true);
						}
					},
					TaskScheduler.Default);
			}

			this.BackgroundTask = Task.Run(() =>
			{
				try
				{
					work(operation);
					hasFinished = true;
				}
				catch (OperationCanceledException) when (operation.CancellationToken.IsCancellationRequested)
				{
					Log.Default.Write(LogSeverityType.Information, $"{name} was aborted by the user.");
				}
				catch (LogFileChangedException e)
				{
					// Shown once per log, as for any read of a log on disk that changed or is gone (#1028, #1247, #1295).
					Log.Default.Write(LogSeverityType.Warning, e, $"{name} stopped, because the log on disk changed or can no longer be read.");
					reportFailedRead(e);
				}
				catch (Exception e)
				{
					Log.Default.Write(LogSeverityType.Error, e, $"{name} could not be completed.");
					_uiDispatcher.Invoke(() => _dialogBox.ShowMessage($"{name} could not be completed.\r\n\r\n{e.Message}", "Error"));
				}
				finally
				{
					_uiDispatcher.Invoke(() =>
					{
						// Escape may be pressed after the work returned, and before this runs.
						var hasSucceeded = hasFinished && !operation.CancellationToken.IsCancellationRequested;

						operation.Dispose();

						if (hasSucceeded)
						{
							onSucceeded?.Invoke();
						}
					});
				}
			});

			return this.BackgroundTask;
		}

		private void ToggleIsPinned()
		{
			_engine.Selector.ToggleIsPinned();
		}

		public void UnpinAll()
		{
			_engine.Analyzer.UnpinAll();
		}

		public void RemoveAllFlags()
		{
			_engine.Analyzer.RemoveAllFlags();

			RefreshFilterResults();
		}

		public void RemoveComments(bool clearAll)
		{
			var message = "Remove all user comments from ALL records?";

			if (!clearAll)
			{
				message = "Remove user comments from selected records?";
			}

			MessageBoxResult userSelection = MessageBox.Show(
				 message,
				 "Confirmation",
				 MessageBoxButton.YesNo,
				 MessageBoxImage.Question);

			if (userSelection == MessageBoxResult.Yes)
			{
				_engine.Analyzer.RemoveComments(clearAll);
			}
		}

		#endregion
		protected virtual void RaiseRegionsChanged()
		{
			_hasUnsavedChanges = true;
			EventHandler threadSafeHandler = this.RegionsChanged;

			if (threadSafeHandler != null)
			{
				try
				{
					Log.Default.Write(
						LogSeverityType.Debug,
						$"Raising the {nameof(RegionsChanged)} event.");

					_uiDispatcher.Invoke(() => threadSafeHandler(this, EventArgs.Empty));

					_bulletinMediator.Post(new RegionsChangedBulletin
					{
						RegionCount = _engine.Regions.Regions.Length,
					});
				}
				catch (Exception exception)
				{
					Log.Default.Write(
						LogSeverityType.Error,
						exception,
						$"An unexpected error occurred while raising the {nameof(RegionsChanged)} event.");
				}
			}
		}

		protected virtual void RaiseBookmarksChanged()
		{
			_hasUnsavedChanges = true;
			EventHandler threadSafeHandler = this.BookmarksChanged;

			if (threadSafeHandler != null)
			{
				try
				{
					Log.Default.Write(
							LogSeverityType.Debug,
							$"Raising the {nameof(BookmarksChanged)} event.");

					_uiDispatcher.Invoke(() => threadSafeHandler(this, EventArgs.Empty));

					_bulletinMediator.Post(new BookmarksChangedBulletin
					{
						BookmarkCount = _engine.Bookmarks.Bookmarks.Length,
					});
				}
				catch (Exception exception)
				{
					Log.Default.Write(
							LogSeverityType.Error,
							exception,
							$"An unexpected error occurred while raising the {nameof(BookmarksChanged)} event.");
				}
			}
		}

		protected virtual void RaiseResultsChanged()
		{
			EventHandler threadSafeHandler = this.ResultsChanged;

			if (threadSafeHandler != null)
			{
				try
				{
					Log.Default.Write(
						LogSeverityType.Debug,
						$"Raising the {nameof(ResultsChanged)} event.");

					_uiDispatcher.Invoke(() => threadSafeHandler(this, EventArgs.Empty));
				}
				catch (Exception exception)
				{
					Log.Default.Write(
						LogSeverityType.Error,
						exception,
						$"An unexpected error occurred while raising the {nameof(ResultsChanged)} event.");
				}
			}
		}

		/// <summary>
		/// Applies the appropriate filter while managing UI updates. 
		/// </summary>
		/// <remarks>
		/// Generally, ViewModels can assume that they have access to the UI thread.
		/// In this case, work is being deferred to a background thread. As a result,
		/// the transition the UI & background threads has to be managed.
		/// Called on the UI thread, so the filter's operation begins before this returns: the menus are disabled at once (#1257).
		/// </remarks>
		/// <param name="filterType">The filter to apply, or the one <paramref name="runFilter"/> runs again.</param>
		/// <param name="filterCriteria">The filter to apply, or the one <paramref name="runFilter"/> runs again.</param>
		/// <param name="runFilter">Runs the filter on a background thread; by default it applies <paramref name="filterCriteria"/>. Refresh (F5) re-applies the current filter instead (#1245).</param>
		/// <returns>
		/// A task that completes with <see langword="true"/> once this filter's results are shown, and with <see langword="false"/>
		/// when it was aborted, failed, or replaced by a newer filter (#998).
		/// </returns>
		private Task<bool> FilterAsynchronously(FilterType filterType, IFilterCriteria filterCriteria, Func<Task<bool>> runFilter = null)
		{
			runFilter ??= () => _engine.FilterAsync(filterType, filterCriteria, CancellationToken.None);

			Log.Default.Write(
				LogSeverityType.Debug,
				$"Filter operation starting... QueuedFilters={_concurrentFilterCount}, FilterType={filterType}, {filterCriteria}");

			// Is a filter operation in progress?
			// ... yes: the new filter takes precedence, and shares its operation, which shows the progress bar until the last filter ends.
			var isReplacing = Interlocked.Increment(ref _concurrentFilterCount) > 1;
			_filterOperation ??= _busy.TryBegin("The filter", isShowingProgress: true, isFilterLocked: false);

			// Creating a background thread for processing.
			// ... Risk: any events raised by the Weevil library will execute on a background thead,
			// ... and not the UI thread which is required in order update/change the UI.
			Action<Exception> reportFailedRead = CreateFailedReadReporter();

			return Task.Run(async () =>
			{
				var isCounted = true;

				try
				{
					var areResultsShown = false;

					if (isReplacing)
					{
						Log.Default.Write(
							LogSeverityType.Warning,
							$"Filter operation in progress will be aborted.");

						_engine.Filter.Abort();
					}
					else
					{
						// Force UI to ensure that the screen has been refreshed
						// ... so that the user knows a filter operation is in progress.
						_uiDispatcher.Invoke(delegate () { }, DispatcherPriority.Render);
					}

					var wasFilterApplied = false;

					try
					{
						wasFilterApplied = await runFilter();
					}
					catch (InvalidExpressionException e)
					{
						var error = e.InnerException == null ? "Not specified." : e.InnerException.Message;

						var message =
							$"Filter operation could not be completed. The provided filter expression is invalid.\r\n\r\n" +
							$"Expression: {e.Expression}\r\n" +
							$"Error: {error}";

						Log.Default.Write(
							LogSeverityType.Error,
							message);

						_uiDispatcher.Invoke(() =>
						{
							MessageBox.Show(message, "Invalid Expression", MessageBoxButton.OK, MessageBoxImage.Error);
						});
					}
					catch (Exception e) when (e is IOException or UnauthorizedAccessException)
					{
						// A log on disk that changed, or is gone. The previous results stay, and the progress bar is hidden below (#1028).
						Log.Default.Write(LogSeverityType.Warning, e, "The filter could not read the log.");
						reportFailedRead(e);
					}
					catch (Exception e)
					{
						// Caught, so the progress bar is hidden below, rather than left running (#1028).
						Log.Default.Write(LogSeverityType.Error, e, "The filter could not be completed.");
						_uiDispatcher.Invoke(() => _dialogBox.ShowMessage($"The filter could not be completed.\r\n\r\n{e.Message}", "Error"));
					}

					// Last filter to execute?
					isCounted = false;

					if (Interlocked.Decrement(ref _concurrentFilterCount) == 0)
					{
						// Was the filter applied?
						// ... if not, then display the original 
						if (wasFilterApplied)
						{
							areResultsShown = true;
							_currentfilterType = filterType;
							_currentfilterCriteria = filterCriteria;

							Log.Default.Write(
								LogSeverityType.Information,
								$"Filter operation is displaying the filter results.");

							_uiDispatcher.Invoke(() =>
							{
								RefreshFilterResults();
								RaiseResultsChanged();
							});
						}
						else
						{
							_inclusiveFilter = _engine.Filter.Criteria.Include;
							_exclusiveFilter = _engine.Filter.Criteria.Exclude;

							Log.Default.Write(
								LogSeverityType.Information,
								$"Filter operation will display the previous filter values. Include=`{_inclusiveFilter}`, Exclude=`{_exclusiveFilter}`");

							_uiDispatcher.Invoke(() =>
							{
								// Note: We can't set the property directly, because we would trigger the filter operation again. 
														OnPropertyChanged(nameof(this.InclusiveFilter));
								OnPropertyChanged(nameof(this.ExclusiveFilter));
							});
						}

						Log.Default.Write(
							LogSeverityType.Debug,
							$"Filter operation is complete. The progress bar will now be hidden.");

						_uiDispatcher.Invoke(EndFilterOperationIfLast);
					}
					else
					{
						Log.Default.Write(
							LogSeverityType.Warning,
							$"Filter operation is complete. Current filter results will be ignored because another filter operation has started.");
					}

					return areResultsShown;
				}
				finally
				{
					// A throw outside the catches above, such as from a dispatcher shutting down, must not leave Weevil busy for good.
					if (isCounted)
					{
						Interlocked.Decrement(ref _concurrentFilterCount);
					}

					if (Volatile.Read(ref _concurrentFilterCount) == 0 && Volatile.Read(ref _filterOperation) != null)
					{
						_uiDispatcher.BeginInvoke(EndFilterOperationIfLast, DispatcherPriority.Normal);
					}
				}
			});
		}

		/// <summary>
		/// Ends the filters' operation, unless a filter asked for since then keeps it running. Call it on the UI thread.
		/// </summary>
		/// <remarks>
		/// The field is cleared before the operation ends, so a filter asked for as it ends begins a new one rather than joining an ended one.
		/// </remarks>
		private void EndFilterOperationIfLast()
		{
			if (Volatile.Read(ref _concurrentFilterCount) == 0)
			{
				BusyState.Operation operation = _filterOperation;
				_filterOperation = null;
				operation?.Dispose();
			}
		}

		/// <remarks>
		/// Selects only the record found, and scrolls to it and focuses it, whatever keys are held (#699, #768, #806).
		/// </remarks>
		private void SearchFilterResults(string userMessage, Func<int> search)
		{
			try
			{
				ShowFoundRecord(search.Invoke());
			}
			catch (RecordNotFoundException e)
			{
				Log.Default.Write(LogSeverityType.Warning, e, userMessage);
				MessageBox.Show(userMessage, "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		/// <summary>
		/// Selects only the record found, and scrolls to it and focuses it (#699, #768, #806).
		/// </summary>
		private void ShowFoundRecord(int position)
		{
			if (this.Rows != null && position >= 0 && position < this.Rows.Count)
			{
				_rowSelection.SelectOnly(this.Rows, position);
				OnSelectionChanged();
			}

			// An event, not a property: Metalama would ignore going to the same row again (#806).
			this.ActiveRecordIndex = position;
			this.ScrollRequested?.Invoke(this, position);
		}

		private void RefreshFilterResults()
		{
			_inclusiveFilter = _engine.Filter.Criteria.Include;
			_exclusiveFilter = _engine.Filter.Criteria.Exclude;

			_uiDispatcher.Invoke(() =>
			{
				// Note: We can't set the property directly, because we would trigger the filter operation again. 
				OnPropertyChanged(nameof(this.InclusiveFilter));
				OnPropertyChanged(nameof(this.ExclusiveFilter));

				// Read once, so the items and the rows show the same results. Each row measures its elapsed time in them (#1150).
				RecordList results = _engine.Filter.Results;

				this.VisibleItems = results;
				this.Rows = new RowList(results, _engine.Annotations, _engine.Selector, _uiDispatcher, CreateFailedReadReporter());

				OnPropertyChanged(nameof(this.VisibleItems));
			});

			_bulletinMediator.Post(new FilterChangedBulletin
			{
				SelectedRecordCount = _engine.Selector.SelectedCount,
				VisibleRecordCount = this.VisibleItems?.Count ?? 0,
				SeverityMetrics = _engine.Filter.GetMetrics(),
				ExecutionTime = _engine.Filter.FilterExecutionTime,
			});

			// Remember: filtering can impact the number of selected records.
			_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
		}

		private static SelectionChangedBulletin BuildSelectionChangedBulletin(ICoreEngine coreEngine)
		{
			// One read of the selection, so the count and the first line agree.
			RecordList selected = coreEngine.Selector.GetSelected();
			var selectedItemCount = selected.Count;
			var selectedTimePeriod = coreEngine.Selector.SelectionPeriod;

			var lineNumber = 0;
			var sectionName = string.Empty;
			var regionName = string.Empty;

			if (selected.Count > 0)
			{
				lineNumber = selected.GetLineNumber(0);

				coreEngine.Navigate.TableOfContents.TryGetSectionName(lineNumber, out sectionName);
				coreEngine.Regions.TryGetRegionName(lineNumber, out regionName);
			}

			return new SelectionChangedBulletin
			{
				LineNumber = lineNumber,
				SelectedRecordCount = selectedItemCount,
				SelectionPeriod = selectedTimePeriod,
				SectionName = sectionName,
				RegionName = regionName,
			};
		}

		private Dictionary<string, object> GetFilterConfiguration()
		{
			return this.FilterOptionsViewModel.Options.ToConfiguration();
		}

		private void AddRegion()
		{
			try
			{
				if (_engine.Selector.HasSelectionPeriod)
				{
					if (_dialogBox.TryShowUserPrompt("Create Region", "Name", @"^[a-zA-Z0-9\-]{1,12}$", "Must be 1 to 12 characters: letters, numbers, or hyphens.", out var regionName))
					{
						RecordList selected = _engine.Selector.GetSelected();
						var selectedLineNumbers = Enumerable.Range(0, selected.Count).Select(selected.GetLineNumber).ToArray();
						_engine.Regions.CreateFromSelection(regionName, selectedLineNumbers);
						RaiseRegionsChanged();
						_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
					}
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void RemoveRegion()
		{
			// One read of the selection, so the count and the line agree.
			RecordList selected = _engine.Selector.GetSelected();

			if (selected.Count == 1)
			{
				var selectedLineNumber = selected.GetLineNumber(0);

				_engine.Regions.Clear(selectedLineNumber);
				RaiseRegionsChanged();
				_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
			}
			else
			{
				MessageBox.Show("A single record must be selected in order to remove a region.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void RemoveAllRegions()
		{
			MessageBoxResult userSelection = MessageBox.Show(
				 "Remove all regions?",
				 "Confirmation",
				 MessageBoxButton.YesNo,
				 MessageBoxImage.Question);

			if (userSelection == MessageBoxResult.Yes)
			{
				_engine.Regions.Clear();
				RaiseRegionsChanged();
				_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
			}
		}

		private void RemoveBookmark()
		{
			// One read of the selection, so the count and the line agree.
			RecordList selected = _engine.Selector.GetSelected();

			if (selected.Count == 1)
			{
				var selectedLineNumber = selected.GetLineNumber(0);

				_engine.Bookmarks.Clear(selectedLineNumber);
				RaiseBookmarksChanged();
				_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
			}
			else
			{
				MessageBox.Show("A single record must be selected in order to remove a bookmark.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void RenameBookmark()
		{
			// One read of the selection, so the count and the line agree.
			RecordList selected = _engine.Selector.GetSelected();

			if (selected.Count == 1)
			{
				var selectedLineNumber = selected.GetLineNumber(0);

				if (_engine.Bookmarks.TryGetBookmark(selectedLineNumber, out var bookmark))
				{
					string newName = _dialogBox.ShowUserPrompt(
						"Rename Bookmark",
						"Name:",
						bookmark.Name);

					// If user cancels dialog, do not rename bookmark (standard Windows behavior)
					if (!string.IsNullOrWhiteSpace(newName))
					{
						_engine.Bookmarks.Rename(selectedLineNumber, newName);
						RaiseBookmarksChanged();
						_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
					}
				}
				else
				{
					MessageBox.Show("The selected record does not have a bookmark.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			else
			{
				MessageBox.Show("A single record must be selected in order to rename a bookmark.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void RemoveAllBookmarks()
		{
			MessageBoxResult userSelection = MessageBox.Show(
					 "Remove all bookmarks?",
					 "Confirmation",
					 MessageBoxButton.YesNo,
					 MessageBoxImage.Question);

			if (userSelection == MessageBoxResult.Yes)
			{
				_engine.Bookmarks.Clear();
				RaiseBookmarksChanged();
				_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
			}
		}

		private void SetBookmark(int slot)
		{
			try
			{
				RecordList selected = _engine.Selector.GetSelected();

				if (selected.Count == 1)
				{
					var selectedRecord = selected[0];
					var selectedLineNumber = selectedRecord.LineNumber;

					var defaultName = selectedRecord.HasCreationTime
						? selectedRecord.CreatedAt.ToString("HH:mm:ss")
						: "Bookmark";

					// Prompt user for bookmark name with default
					string bookmarkName = _dialogBox.ShowUserPrompt(
						"Create Bookmark",
						"Name:",
						defaultName);

					// If user cancels dialog, do not create bookmark (standard Windows behavior)
					if (!string.IsNullOrWhiteSpace(bookmarkName))
					{
						// Pass the slot (1-5) as the bookmark ID
						_engine.Bookmarks.Create(slot, bookmarkName, selectedLineNumber);

						RaiseBookmarksChanged();
						_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
					}
				}
				else
				{
					MessageBox.Show("A single record must be selected in order to create a bookmark.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void GoToBookmark(int slot)
		{
			// Use the bookmark ID (slot) to find the bookmark, instead of using array index
			if (_engine.Bookmarks.TryGetBookmarkById(slot, out var bookmark))
			{
				SearchFilterResults(
						$"Bookmark {slot} is not visible in the current results.",
						() => _engine
								.Navigate
								.GoTo(bookmark.Record.LineNumber, RecordSearchType.NearestNeighbor)
								.ToIndexUsing(_engine.Filter.Results));
			}
			else
			{
				MessageBox.Show($"Bookmark {slot} has not been set.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		public bool RegionStartsWith(IRecord record, out string regionName)
		{
			return _engine.Regions.TryStartsWith(record.LineNumber, out regionName);
		}

		public bool RegionEndsWith(IRecord record, out string regionName)
		{
			return _engine.Regions.TryEndsWith(record.LineNumber, out regionName);
		}

		public bool RegionContains(IRecord record)
		{
			return _engine.Regions.Contains(record.LineNumber);
		}

		public bool TryGetBookmarkName(IRecord record, out string bookmarkName)
		{
			return _engine.Bookmarks.TryGetBookmarkName(record.LineNumber, out bookmarkName);
		}

		public bool TryGetBookmark(IRecord record, out Bookmark bookmark)
		{
			return _engine.Bookmarks.TryGetBookmark(record.LineNumber, out bookmark);
		}
	}
}