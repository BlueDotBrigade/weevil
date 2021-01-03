namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.IO.Compression;
	using System.Linq;
	using System.Reflection;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Threading;
	using BlueDotBrigade.Weevil;
	using GongSolutions.Wpf.DragDrop;
	using PostSharp.Patterns.Model;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Analysis.LogSplitter;
	using BlueDotBrigade.Weevil.Configuration.Software;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using BlueDotBrigade.Weevil.Gui.Help;
	using BlueDotBrigade.Weevil.Gui.IO;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Navigation;
	using BlueDotBrigade.Weevil.Reports;
	using BlueDotBrigade.Weevil.Runtime.Serialization;
	using BlueDotBrigade.Weevil.Gui.Properties;
	using Directory = System.IO.Directory;
	using File = System.IO.File;
	using SelectFileView = BlueDotBrigade.Weevil.Gui.IO.SelectFileView;

	[NotifyPropertyChanged()]
	internal partial class FilterResultsViewModel : IDropTarget, INotifyPropertyChanged
	{
		private const string NewReleaseDetailsPath = @"ReleasedVersion.xml";
		private const string CompatibleFileExtensions = "Log Files (*.log, *.csv, *.txt)|*.log;*.csv;*.tsv;*.txt|Compressed Files (*.zip)|*.zip|All files (*.*)|*.*";

		private static readonly string HelpFilePath = Path.GetFullPath(EnvironmentHelper.GetExecutableDirectory() + @"\Doc\Help.html");
		private static readonly string ThirdPartyNoticesPath = Path.GetFullPath(EnvironmentHelper.GetExecutableDirectory() + @"\Licenses\ThirdPartyNoticesAndInformation.txt");

		private static readonly string ApplicationLogFilePath = @"C:\ProgramData\BlueDotBrigade\Weevil\Logs\";

		private static readonly TimeSpan DefaultUiResponsivenessPeriod = TimeSpan.FromSeconds(1);

		#region Fields & Object Lifetime

		private readonly Window _mainWindow;

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
		private readonly Dispatcher _uiDispatcher;

		private readonly DispatcherTimer initializationTimer;

		private readonly IDialogBoxService _dialogBox;

		private IEngine _engine;

		private readonly DragAndDropViewModel _dragAndDrop;

		public event PropertyChangedEventHandler PropertyChanged;

		private string _inclusiveFilter;
		private string _exclusiveFilter;

		private FilterCriteria _previousFilterCriteria;

		private int _concurrentFilterCount;

		private ITableOfContents _tableOfContents;

		public FilterResultsViewModel(Window mainWindow, Dispatcher uiDispatcher)
		{
			_mainWindow = mainWindow;
			_uiDispatcher = uiDispatcher;
			_dialogBox = new DialogBoxService(mainWindow);

			_engine = Engine.Surrogate;

			_previousFilterCriteria = FilterCriteria.None;

			this.IsLogFileOpen = Engine.IsRealInstance(_engine);
			this.IsCommandExecuting = false;

			this.IncludePinned = true;
			this.IsSameAsDisk = true;

			_inclusiveFilter = string.Empty;
			_exclusiveFilter = string.Empty;

			_concurrentFilterCount = 0;

			this.IsManualFilter = false;
			this.AreDetailsVisible = false;

			this.InclusiveFilterEnabled = false;
			this.ExclusiveFilterEnabled = false;

			this.InclusiveFilterHistory = new ObservableCollection<string>();
			this.ExclusiveFilterHistory = new ObservableCollection<string>();

			// ReSharper disable once PossibleNullReferenceException
			this.CurrentVersion = Assembly.GetEntryAssembly().GetName().Version;

			initializationTimer = new DispatcherTimer();
			initializationTimer.Tick += (sender, args) => OnInitialize();
			initializationTimer.Interval = TimeSpan.FromSeconds(2);
			initializationTimer.Start();

			_dragAndDrop = new DragAndDropViewModel();
			_dragAndDrop.DroppedFile += OnFileDropped;

			_tableOfContents = new TableOfContents();

			this.IsIndeterminate = true;
		}

		private static ApplicationInfo GetApplicationInfo()
		{
			ApplicationInfo result = ApplicationInfo.NotSpecified;

			var applicationInfoPath = NewReleaseDetailsPath;

#if DEBUG
			applicationInfoPath = Path.GetFullPath(@"..\..\..\..\..\Dep\Runtime\Debug\ReleaseNotes.xml");
#endif
			try
			{
				result = TypeFactory.LoadFromXml<ApplicationInfo>(applicationInfoPath);
				result.ChangeLogPath = Path.GetFullPath(result.ChangeLogPath);
				result.InstallerPath = Path.GetFullPath(result.InstallerPath);
			}
			catch (IOException e)
			{
				var message = $"Unable to determine the latest official release. Reason=`{e.Message}`";
				Log.Default.Write(LogSeverityType.Warning, message);
			}

			return result;
		}
		#endregion

		#region Properties

		public string SourceFilePath => _engine.SourceFilePath;

		public ApplicationInfo NewReleaseDetails { get; set; }

		public IList<IRecord> VisibleItems { get; private set; }

		public int AllRecordCount => _engine.Filter.Results.Length;

		public int VisibleRecordCount => this.VisibleItems?.Count ?? 0;

		public int SelectedRecordCount { get; set; }

		[SafeForDependencyAnalysis]
		public bool IsUpdateAvailable =>
				//Depends.On(this.NewReleaseDetails);

				//var isUpdateAvailable = false;

				//if (NewReleaseDetails != null)
				//{
				//	isUpdateAvailable = NewReleaseDetails.Version > this.CurrentVersion;
				//}

				//return isUpdateAvailable;
				false;
		public Version CurrentVersion { get; private set; }

		[SafeForDependencyAnalysis]
		public bool IsMenuEnabled
		{
			get
			{
				if (Depends.Guard)
				{
					Depends.On(this.IsLogFileOpen);
				}

				if (Depends.Guard)
				{
					Depends.On(this.IsCommandExecuting);
				}

				return this.IsLogFileOpen && !this.IsCommandExecuting;
			}
		}

		public bool IsLogFileOpen { get; private set; }

		public bool IsCommandExecuting { get; private set; }

		public int FlaggedRecordCount { get; private set; }
		public bool IsSameAsDisk { get; private set; }
		public bool IncludePinned { get; set; }
		public IDictionary<string, object> Metrics { get; set; }

		public ContextDictionary Context { get; set; }

		public bool IsManualFilter { get; set; }

		public bool IsFilterInProgress => _concurrentFilterCount >= 1;

		public bool AreDetailsVisible { get; set; }

		public bool InclusiveFilterEnabled { get; private set; }
		public bool ExclusiveFilterEnabled { get; private set; }

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

					// Automatic filtering?
					if (!this.IsManualFilter)
					{
						var filterCriteria = new FilterCriteria(value, _exclusiveFilter, GetFilterConfiguration());

						FilterAsynchronously(FilterType.RegularExpression, filterCriteria);
					}
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

					// Automatic filtering?
					if (!this.IsManualFilter)
					{
						var filterCriteria = new FilterCriteria(_inclusiveFilter, value, GetFilterConfiguration());

						FilterAsynchronously(FilterType.RegularExpression, filterCriteria);
					}
				}
			}
		}

		public ObservableCollection<string> InclusiveFilterHistory { get; }
		public ObservableCollection<string> ExclusiveFilterHistory { get; }

		public int ActiveRecordIndex { get; set; }

		public bool AlwaysHideDebugRecords { get; set; }

		public bool AlwaysHideTraceRecords { get; set; }

		public bool IsProcessingLongOperation { get; private set; }
		public int MaxProgress { get; private set; }
		public int LongOperationProgress { get; private set; }
		public bool IsIndeterminate { get; private set; }

		public bool CanChangeFilter => true;

		public bool AreRecordsSelected { get; private set; }

		[SafeForDependencyAnalysis]
		public IList<IRecord> SelectedItems => _engine.Selector.GetSelected();

		public TimeSpan ElapsedTime { get; private set; }

		public string CurrentHeading { get; private set; }

		public Action<object, EventArgs> ResultsChanged { get; internal set; }
		#endregion

		#region Event Handlers
		private void OnInitialize()
		{
			initializationTimer.IsEnabled = false;

			this.NewReleaseDetails = GetApplicationInfo();

			var args = Environment.GetCommandLineArgs();

			if (args.Length > 1)
			{
				if (File.Exists(args[1]))
				{
					OpenCompressed(args[1]);
				}
			}
		}

		private void OnFileDropped(object sender, DroppedFileEventArgs e)
		{
			OpenCompressed(e.FilePath);
		}

		protected void RaisePropertyChanged(string name)
		{
			this?.PropertyChanged(this, new PropertyChangedEventArgs(name));
		}
		#endregion

		#region Commands: General
		public async void Open()
		{
			var filePath = _dialogBox.ShowOpenFile(CompatibleFileExtensions);
			if (string.IsNullOrWhiteSpace(filePath))
			{
				Log.Default.Write(LogSeverityType.Debug, "Open file operation was aborted by the user.");
			}
			else
			{
				try
				{
					this.IsCommandExecuting = true;

					await OpenCompressed(filePath);
				}
				finally
				{
					this.IsCommandExecuting = false;
				}
			}
		}

		public async Task OpenCompressed(string sourceFilePath)
		{
			var fileInfo = new FileInfo(sourceFilePath);
			var isSourceFileCompressed = fileInfo.Extension.ToUpperInvariant() == ".ZIP";

			var temporarySourceFileName = string.Empty;

			if (isSourceFileCompressed)
			{
				var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
				ZipFile.ExtractToDirectory(sourceFilePath, tempFolder);
				var files = Directory.GetFiles(tempFolder);

				if (files.Length > 0)
				{
					temporarySourceFileName = await ShowListOfCompressedFiles(files);
				}
				if (!string.IsNullOrEmpty(temporarySourceFileName))
				{
					sourceFilePath = Path.Combine(tempFolder, temporarySourceFileName);
				}
			}

			try
			{
				await OpenAsync(sourceFilePath);
			}
			finally
			{
				if (isSourceFileCompressed && File.Exists(temporarySourceFileName))
				{
					File.Delete(temporarySourceFileName);
				}
			}
		}

		private Task<string> ShowListOfCompressedFiles(string[] files)
		{
			var selectFileViewModel = new SelectFileViewModel(files);
			var selectFileView = new SelectFileView(selectFileViewModel);
			var result = string.Empty;
			selectFileView.ShowDialog();
			result = selectFileViewModel.SelectedFilename;

			return Task.FromResult(result);
		}

		private void RefreshHistory(ObservableCollection<string> history, IList<string> newValues)
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

		public async Task OpenAsync(string sourceFilePath)
		{
			this.IsProcessingLongOperation = true;

			var openAsResult = new OpenAsResult();
			var wasFileOpened = false;

			try
			{
				IPlugin plugin = new PluginFactory().Create(sourceFilePath);

				if (plugin.CanOpenAs)
				{
					(bool, OpenAsResult) result = plugin.ShowOpenAs(
						_mainWindow,
						(path) => Engine.UsingPath(path),
						sourceFilePath);

					wasFileOpened = result.Item1;
					openAsResult = result.Item2;
				}
				else
				{
					wasFileOpened = true;
				}
			}
			catch (NotSupportedException e)
			{
				var sourceFilename = Path.GetFileName(sourceFilePath);
				var message =
					e.Message + Environment.NewLine + Environment.NewLine +
					"In order to open the file, you must first delete the sidecar:" + Environment.NewLine +
					$"{sourceFilename}.xml" + Environment.NewLine;
				MessageBox.Show(message, "Open Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
			}

			if (wasFileOpened)
			{
				await Task.Run(() =>
				{
					try
					{
						if (Engine.IsRealInstance(_engine))
						{
							_engine.Save(true);
						}

						Log.Default.Write($"File is being opened... Path={sourceFilePath}");

						_engine = Engine
							.UsingPath(sourceFilePath, openAsResult.Range.Minimum)
							.UsingContext(openAsResult.Context)
							.UsingRange(openAsResult.Range)
							.Open();

						Log.Default.Write("Updating filter history on the UI.");
						RefreshHistory(this.InclusiveFilterHistory, _engine.Filter.IncludeHistory);
						RefreshHistory(this.ExclusiveFilterHistory, _engine.Filter.ExcludeHistory);

						// BUG: where is un-register `HistoryChanged`?
						_engine.Filter.HistoryChanged += OnFilterHistoryChanged;

						this.Context = _engine.Context;

						RefreshFilterResults();

						this.InclusiveFilterEnabled = true;
						this.ExclusiveFilterEnabled = true;

						Log.Default.Write(
							LogSeverityType.Information,
							$"File has been opened. Path={sourceFilePath}");
					}
					catch (InvalidFileFormatException e)
					{
						var message = "Unknown File Format";
						Log.Default.Write(LogSeverityType.Error, e, message);
						MessageBox.Show(e.Message, message);
					}
					catch (Exception e)
					{
						var message = "Unable to open the log file.";
						Log.Default.Write(LogSeverityType.Error, e, message);
						MessageBox.Show(e.Message, message);
					}
					finally
					{
						this.IsProcessingLongOperation = false;
						this.IsLogFileOpen = Engine.IsRealInstance(_engine);
					}
				}).ContinueWith((x) =>
					{
						if (_engine.Navigator.TableOfContents.Sections.Count == 0)
						{
							_engine.Navigator.RebuildTableOfContents();
						}

						_tableOfContents = _engine.Navigator.TableOfContents;
					}
				);
			}
			else
			{
				this.IsProcessingLongOperation = false;
			}
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

		public void SaveState()
		{
			_engine.Save();
		}

		public void Reload()
		{
			try
			{
				this.IsCommandExecuting = true;


				ImmutableArray<IRecord> oldRecordSelection = _engine.Selector.ClearAll();

				_engine.Save();
				_engine.Reload();

				var newRecordSelection = _engine.Filter.Results.Where(a => oldRecordSelection.Any(b => b.LineNumber == a.LineNumber)).ToList();
				_engine.Selector.Select(newRecordSelection);

				RefreshFilterResults();
			}
			finally
			{
				this.IsCommandExecuting = false;
			}

			this.ResultsChanged?.Invoke(this, EventArgs.Empty);
		}

		public void ClipboardCopyRaw()
		{
			ClipboardHelper.CopyRawFromSelected(_engine, Settings.Default.AddLineNumberPrefix);
		}

		public void ClipboardCopyComment()
		{
			ClipboardHelper.CopyCommentFromSelected(_engine, Settings.Default.AddLineNumberPrefix);
		}

		public void ClipboardPaste(bool allowOverwrite)
		{
			ClipboardHelper.PasteToSelected(_engine, allowOverwrite);
			RefreshFilterResults();
		}

		public void Select(IList<IRecord> records)
		{
			if (!this.IsCommandExecuting)
			{
				if (records.Count == 0)
				{
					this.CurrentHeading = string.Empty;
				}
				else
				{
					this.CurrentHeading = _tableOfContents.GetSection(records[0].LineNumber);

					_engine.Selector.Select(records);
					this.SelectedRecordCount = _engine.Selector.Selected.Count;
					RefreshStatusBar();
				}
			}
		}

		public void UnSelect(IList<IRecord> records)
		{
			if (!this.IsCommandExecuting)
			{
				_engine.Selector.Unselect(records);
				this.SelectedRecordCount = _engine.Selector.Selected.Count;
				RefreshStatusBar();
			}
		}

		public void DragOver(IDropInfo dropInfo)
		{
			_dragAndDrop.Drag(dropInfo);
		}

		public void Drop(IDropInfo dropInfo)
		{
			_dragAndDrop.Drop(dropInfo);
		}

		public void Exit()
		{
			if (Engine.IsRealInstance(_engine))
			{
				_engine.Save(true);
			}
		}

		private void RefreshStatusBar()
		{
			_uiDispatcher.Invoke(() =>
			{
				RaisePropertyChanged(nameof(this.SourceFilePath));

				this.Metrics = _engine.Filter.GetMetrics();

				this.IsSameAsDisk = _engine.IsSameAsDisk;

				this.AreRecordsSelected = _engine.Selector.IsTimePeriodSelected;

				if (_engine.Selector.IsTimePeriodSelected)
				{
					this.ElapsedTime = _engine.Selector.TimePeriodOfInterest;
				}
				else
				{
					if (_engine.Count == _engine.Filter.Results.Length)
					{
						this.ElapsedTime = _engine.Metrics.RecordAndMetadataLoadDuration;
					}
					else
					{
						this.ElapsedTime = _engine.Filter.FilterExecutionTime;
					}
				}
			});
		}

		private void SaveSelected(FileFormatType fileFormatType)
		{
			var destinationFolder = Path.GetDirectoryName(_engine.SourceFilePath);

			try
			{
				_engine.Selector.SaveSelection(destinationFolder, fileFormatType);
			}
			catch (IOException e) when (e.HResult.Equals(-2147024864))
			{
				Log.Default.Write(LogSeverityType.Error, e, "Unable to save the selected records. The target file is being used by another process.");
				MessageBox.Show("Unable to save the selected records\r\nThe target file is being used by another process.");
			}
			catch (Exception e)
			{
				var message = "An unexpected error occurred while the records were being saved.";
				Log.Default.Write(LogSeverityType.Error, e, message);
				MessageBox.Show(message);
				throw;
			}
		}


		public void ShowSourceFile()
		{
			WindowsProcess.Start(WindowsProcessType.FileExplorer, Path.GetDirectoryName(_engine.SourceFilePath));
		}

		public void ShowApplicationLogFile()
		{
			WindowsProcess.Start(WindowsProcessType.FileExplorer, ApplicationLogFilePath);
		}

		public void ShowHelp()
		{
			var helpUrl = new Uri("file:///" + HelpFilePath);

			Debug.WriteLine(helpUrl);
			Process.Start("\"" + HelpFilePath + "\"");
		}

		public void ShowAbout()
		{
			try
			{
				var dialog = new AboutDialog(this.CurrentVersion, ThirdPartyNoticesPath, ThirdPartyNoticesPath)
				{
					Owner = _mainWindow,
				};

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

		private void SplitCurrentLog()
		{
			new LogFileSplitter(_engine.SourceFilePath).Run(_engine.Filter.Results);
		}
		#endregion

		#region Commands: Filtering

		public void ClearSelectedRecords()
		{
			_engine.Clear(ClearRecordsOperation.Selected);

			RefreshFilterResults();
			this.ResultsChanged?.Invoke(this, EventArgs.Empty);

			// HACK: As a developer using the API, how would I know to re-register for existing events. It's not intuitive.
			_engine.Filter.HistoryChanged += OnFilterHistoryChanged;
		}

		public void ClearUnselectedRecords()
		{
			_engine.Clear(ClearRecordsOperation.Unselected);

			RefreshFilterResults();
			this.ResultsChanged?.Invoke(this, EventArgs.Empty);

			// HACK: As a developer using the API, how would I know to re-register for existing events. It's not intuitive.
			_engine.Filter.HistoryChanged += OnFilterHistoryChanged;
		}

		public void ClearAfterSelectedRecord()
		{
			_engine.Clear(ClearRecordsOperation.AfterSelected);
			RefreshFilterResults();
			this.ResultsChanged?.Invoke(this, EventArgs.Empty);

			// HACK: As a developer using the API, how would I know to re-register for existing events. It's not intuitive.
			_engine.Filter.HistoryChanged += OnFilterHistoryChanged;
		}

		public void ClearBeforeAndAfterSelection()
		{
			_engine.Clear(ClearRecordsOperation.BeforeAndAfterSelected);
			RefreshFilterResults();
			this.ResultsChanged?.Invoke(this, EventArgs.Empty);

			// HACK: As a developer using the API, how would I know to re-register for existing events. It's not intuitive.
			_engine.Filter.HistoryChanged += OnFilterHistoryChanged;
		}

		public void ClearBeforeSelectedRecord()
		{
			_engine.Clear(ClearRecordsOperation.BeforeSelected);
			RefreshFilterResults();
			this.ResultsChanged?.Invoke(this, EventArgs.Empty);

			// HACK: As a developer using the API, how would I know to re-register for existing events. It's not intuitive.
			_engine.Filter.HistoryChanged += OnFilterHistoryChanged;
		}

		public void FilterManually(object[] filters)
		{
			_inclusiveFilter = filters[0].ToString();
			_exclusiveFilter = filters[1].ToString();

			var filterCriteria = new FilterCriteria(_inclusiveFilter, _exclusiveFilter, GetFilterConfiguration());

			FilterAsynchronously(FilterType.RegularExpression, filterCriteria);
		}

		public void FilterOrCancel(object[] filters)
		{
			if (this.IsFilterInProgress)
			{
				_engine.Filter.Abort();
			}
			else
			{
				_inclusiveFilter = filters[0].ToString();
				_exclusiveFilter = filters[1].ToString();

				var filterCriteria = new FilterCriteria(_inclusiveFilter, _exclusiveFilter, GetFilterConfiguration());

				FilterAsynchronously(FilterType.RegularExpression, filterCriteria);
			}
		}

		public void ToggleFilters()
		{
			if (_engine.Filter.Criteria.Equals(FilterCriteria.None))
			{
				FilterAsynchronously(FilterType.RegularExpression, _previousFilterCriteria);
			}
			else
			{
				_previousFilterCriteria = (FilterCriteria) _engine.Filter.Criteria;
				FilterAsynchronously(FilterType.RegularExpression, FilterCriteria.None);
			}
		}

		private void ReApplyFilters()
		{
			_engine.Filter.ReApply();
		}

		private void AbortFilter()
		{
			_engine.Filter.Abort();
		}
		#endregion

		#region Commands: Navigation

		public void GoToNextPin()
		{
			_engine.Navigator.Pinned.GoToNextPin();
			this.ActiveRecordIndex = _engine.Navigator.Pinned.ActiveIndex;
		}

		public void GoToPreviousPin()
		{
			_engine.Navigator.Pinned.GoToPreviousPin();
			this.ActiveRecordIndex = _engine.Navigator.Pinned.ActiveIndex;
		}

		#endregion

		#region Commands: Analysis
		private void SaveCommentSummary()
		{
			var destinationFolder = Path.GetDirectoryName(_engine.SourceFilePath);
			_engine.GenerateReport(ReportType.CommentSummary, destinationFolder);
		}

		public void AnalyzeUiResponsiveness()
		{
			var userInput = _dialogBox.ShowUserPrompt(
				"Input Required",
				"Elapsed greater than (ms):",
				DefaultUiResponsivenessPeriod.TotalMilliseconds.ToString("0.#"));

			if (int.TryParse(userInput, out var timePeriodInMs))
			{
				IDictionary<string, object> results = _engine.Analyzer.GetAnalyzer(AnalysisType.UiResponsiveness).Analyze(timePeriodInMs);
				this.FlaggedRecordCount = int.Parse(results["UnresponsiveUiCount"].ToString());

				RefreshFilterResults();
			}
			else
			{
				MessageBox.Show("Elapsed time is expected to be greater than zero(0).", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void DetectData()
		{
			IDictionary<string, object> results = _engine.Analyzer.GetAnalyzer(AnalysisType.ExtractRegExKvp).Analyze();
			this.FlaggedRecordCount = int.Parse(results["KeysFound"].ToString());
			RefreshFilterResults();
		}

		public void AnalyzeDataTransitions()
		{
			IDictionary<string, object> results = _engine.Analyzer.GetAnalyzer(AnalysisType.DataTransition).Analyze();
			this.FlaggedRecordCount = int.Parse(results["TransitionCount"].ToString());

			RefreshFilterResults();
		}

		public void AnalyzeDataTransitionsFallingEdge()
		{
			IDictionary<string, object> results = _engine.Analyzer.GetAnalyzer(AnalysisType.DataTransitionFallingEdge).Analyze();
			this.FlaggedRecordCount = int.Parse(results["TransitionCount"].ToString());

			RefreshFilterResults();
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

		/// <summary>
		/// Applies the appropriate filter while managing UI updates. 
		/// </summary>
		/// <remarks>
		/// Generally, ViewModels can assume that they have access to the UI thread.
		/// In this case, work is being deferred to a background thread. As a result,
		/// the transition the UI & background threads has to be managed.
		/// </remarks>
		private void FilterAsynchronously(FilterType filterType, FilterCriteria filterCriteria)
		{
			Log.Default.Write(
				LogSeverityType.Debug,
				$"Filter operation starting... QueuedFilters={_concurrentFilterCount}, FilterType={filterType}, {filterCriteria}");

			// Creating a background thread for processing.
			// ... Risk: any events raised by the Weevil library will execute on a background thead,
			// ... and not the UI thread which is required in order update/change the UI.
			Task.Run(async () =>
			{
				// Is a filter operation in progress?
				// ... yes: cancel it because new filter takes precedence
				if (this.IsFilterInProgress)
				{
					Log.Default.Write(
						LogSeverityType.Warning,
						$"Filter operation in progress will be aborted.");

					_engine.Filter.Abort();
				}

				var queuedFilters = Interlocked.Increment(ref _concurrentFilterCount);
				RaisePropertyChanged(nameof(this.IsFilterInProgress));

				// Force UI to ensure that the screen has been refreshed
				// ... so that the user knows a filter operation is in progress.
				_uiDispatcher.Invoke(delegate() { }, DispatcherPriority.Render);

				// First filter to execute?
				if (queuedFilters == 1)
				{
					Log.Default.Write(
						LogSeverityType.Debug,
						$"Filter operation is displaying the progress bar.");

					_uiDispatcher.Invoke(() =>
					{
						this.IsCommandExecuting = true;
						this.IsProcessingLongOperation = true;
					});
				}

				var wasFilterApplied = false;

				try
				{
					wasFilterApplied = await _engine.FilterAsync(filterType, filterCriteria, CancellationToken.None);
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

				queuedFilters = Interlocked.Decrement(ref _concurrentFilterCount);
				RaisePropertyChanged(nameof(this.IsFilterInProgress));

				// Last filter to execute?
				if (queuedFilters == 0)
				{
					// Was the filter applied?
					// ... if not, then display the original 
					if (wasFilterApplied)
					{
						Log.Default.Write(
							LogSeverityType.Information,
							$"Filter operation is displaying the filter results.");

						_uiDispatcher.Invoke(() =>
						{
							RefreshFilterResults();
							this.ResultsChanged?.Invoke(this, EventArgs.Empty);
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
							RaisePropertyChanged(nameof(this.InclusiveFilter));
							RaisePropertyChanged(nameof(this.ExclusiveFilter));
						});
					}

					Log.Default.Write(
						LogSeverityType.Debug,
						$"Filter operation is complete. The progress bar will now be hidden.");

					_uiDispatcher.Invoke(() =>
					{
						this.IsCommandExecuting = false;
						this.IsProcessingLongOperation = false;
					});
				}
				else
				{
					Log.Default.Write(
						LogSeverityType.Warning,
						$"Filter operation is complete. Current filter results will be ignored because another filter operation has started.");
				}
			});
		}

		private void RefreshFilterResults()
		{
			_inclusiveFilter = _engine.Filter.Criteria.Include;
			_exclusiveFilter = _engine.Filter.Criteria.Exclude;

			_uiDispatcher.Invoke(() =>
			{
				// Note: We can't set the property directly, because we would trigger the filter operation again. 
				RaisePropertyChanged(nameof(this.InclusiveFilter));
				RaisePropertyChanged(nameof(this.ExclusiveFilter));

				this.VisibleItems = _engine.Filter.Results;
				RaisePropertyChanged(nameof(this.VisibleItems));
			});

			RefreshStatusBar();
		}

		private Dictionary<string, object> GetFilterConfiguration()
		{
			var configuration = new Dictionary<string, object>();

			if (this.IncludePinned)
			{
				configuration.Add("IncludePinned", this.IncludePinned);
			}

			if (this.AlwaysHideDebugRecords)
			{
				configuration.Add("HideDebugRecords", this.AlwaysHideDebugRecords);
			}

			if (this.AlwaysHideTraceRecords)
			{
				configuration.Add("HideTraceRecords", this.AlwaysHideTraceRecords);
			}

			return configuration;
		}
	}
}