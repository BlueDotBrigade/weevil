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
	using System.Net;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Threading;
	using BlueDotBrigade.Weevil;
	using GongSolutions.Wpf.DragDrop;
	using PostSharp.Patterns.Model;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Analysis.LogSplitter;
	using BlueDotBrigade.Weevil.Configuration;
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
	using BlueDotBrigade.Weevil.Gui.Threading;
	using Directory = System.IO.Directory;
	using File = System.IO.File;
	using SelectFileView = BlueDotBrigade.Weevil.Gui.IO.SelectFileView;

	[NotifyPropertyChanged()]
	internal partial class FilterResultsViewModel : IDropTarget, INotifyPropertyChanged
	{
		private static readonly Uri NewReleaseUrl =
			new Uri(@"https://raw.githubusercontent.com/BlueDotBrigade/weevil/master/Doc/Notes/Release/NewReleaseNotification.xml");

		private static readonly Uri RegEx101Url = new Uri(@"https://regex101.com/r/EKCf6T/4");
		private const string CompatibleFileExtensions = "Log Files (*.log, *.csv, *.txt)|*.log;*.csv;*.tsv;*.txt|Compressed Files (*.zip)|*.zip|All files (*.*)|*.*";

		private static readonly string HelpFilePath = Path.GetFullPath(EnvironmentHelper.GetExecutableDirectory() + @"\..\Doc\Help.html");
		private static readonly string LicensePath = Path.GetFullPath(EnvironmentHelper.GetExecutableDirectory() + @"\..\Licenses\License.md");
		private static readonly string ThirdPartyNoticesPath = Path.GetFullPath(EnvironmentHelper.GetExecutableDirectory() + @"\..\Licenses\ThirdPartyNoticesAndInformation.txt");

		private static readonly string NewReleaseFilePath = @"C:\ProgramData\BlueDotBrigade\Weevil\Logs\";

		private static readonly ImmutableArray<IInsight> NoInsight = ImmutableArray.Create(new IInsight[0]);

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
		private readonly IUiDispatcher _uiDispatcher;

		private readonly DispatcherTimer initializationTimer;

		private readonly IDialogBoxService _dialogBox;

		private IEngine _engine;

		private readonly DragAndDropViewModel _dragAndDrop;

		public event PropertyChangedEventHandler PropertyChanged;

		private string _inclusiveFilter;
		private string _exclusiveFilter;

		private FilterCriteria _previousFilterCriteria;

		private string _findText;
		private bool _findIsCaseSensitive;

		private FilterType _currentfilterType;
		private IFilterCriteria _currentfilterCriteria;

		private int _concurrentFilterCount;

		private ITableOfContents _tableOfContents;

		private ImmutableArray<IInsight> _insights;

		public FilterResultsViewModel(Window mainWindow, IUiDispatcher uiDispatcher)
		{
			_mainWindow = mainWindow;
			_uiDispatcher = uiDispatcher;
			_dialogBox = new DialogBoxService(mainWindow);

			_engine = Engine.Surrogate;

			this.IsLogFileOpen = Engine.IsRealInstance(_engine);
			this.IsCommandExecuting = false;

			this.IncludePinned = true;

			_inclusiveFilter = string.Empty;
			_exclusiveFilter = string.Empty;

			_concurrentFilterCount = 0;
			_currentfilterCriteria = FilterCriteria.None;
			_previousFilterCriteria = FilterCriteria.None;

			_findText = string.Empty;
			_findIsCaseSensitive = false;

			this.IsManualFilter = false;
			this.IsFilterCaseSensitive = true;
			this.AreFilterOptionsVisible = false;
			this.IncludeDebugRecords = true;
			this.IncludeTraceRecords = true;

			this.IsFilterToolboxEnabled = false;

			this.InclusiveFilterHistory = new ObservableCollection<string>();
			this.ExclusiveFilterHistory = new ObservableCollection<string>();

			this.HasInsight = false;
			this.HasInsightNeedingAttention = false;
			this.InsightNeedingAttention = 0;

			this.WeevilVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(128, 128, 128);

			initializationTimer = new DispatcherTimer();
			initializationTimer.Tick += (sender, args) => OnInitialize();
			initializationTimer.Interval = TimeSpan.FromSeconds(2);
			initializationTimer.Start();

			_dragAndDrop = new DragAndDropViewModel();
			_dragAndDrop.DroppedFile += OnFileDropped;

			_tableOfContents = new TableOfContents();

			this.CustomAnalyzerCommands = new ObservableCollection<MenuItemViewModel>();

			_insights = NoInsight;
		}

		private static ApplicationInfo GetApplicationInfo()
		{
			ApplicationInfo result = ApplicationInfo.NotSpecified;

			try
			{
#if DEBUG
				var applicationInfoPath = Path.GetFullPath(@"..\..\..\..\..\Doc\Notes\Release\NewReleaseNotification.xml");
				Stream newReleaseStream = FileHelper.Open(applicationInfoPath);
#else
				Stream newReleaseStream = new WebClient().OpenRead(NewReleaseUrl);
#endif

				result = TypeFactory.LoadFromXml<ApplicationInfo>(newReleaseStream);
			}
			catch (Exception e) 
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

		public int AllRecordCount => _engine.Count;

		public int VisibleRecordCount => this.VisibleItems?.Count ?? 0;

		public int SelectedRecordCount => _engine.Selector.Selected.Count;

		[SafeForDependencyAnalysis]
		public bool HasInsight { get; private set; }

		[SafeForDependencyAnalysis]
		public bool HasInsightNeedingAttention { get; private set; }

		[SafeForDependencyAnalysis]
		public int InsightNeedingAttention { get; private set; }

		[SafeForDependencyAnalysis]
		public bool IsUpdateAvailable
		{
			get
			{
				Depends.On(this.NewReleaseDetails);

				var isUpdateAvailable = false;

				if (this.NewReleaseDetails != null)
				{
					isUpdateAvailable = this.NewReleaseDetails.LatestReleaseVersion > this.WeevilVersion;
				}

				return isUpdateAvailable;
			}
		}

		public Version WeevilVersion { get; private set; }

		[SafeForDependencyAnalysis]
		public bool IsMenuEnabled
		{
			get
			{
				if (Depends.Guard)
				{
					Depends.On(this.IsLogFileOpen, this.IsCommandExecuting);
				}

				return this.IsLogFileOpen && !this.IsCommandExecuting;
			}
		}

		public bool IsLogFileOpen { get; private set; }

		public bool IsCommandExecuting { get; private set; }

		public int FlaggedRecordCount { get; private set; }
		public bool HasBeenCleared => _engine.HasBeenCleared;
		public bool IncludePinned { get; set; }

		public IDictionary<string, object> Metrics { get; set; }

		public ContextDictionary Context { get; set; }

		public bool IsManualFilter { get; set; }

		public bool IsFilterCaseSensitive { get; set; }

		public bool IsFilterInProgress => _concurrentFilterCount >= 1;

		public bool AreFilterOptionsVisible { get; set; }

		public bool IsFilterToolboxEnabled { get; private set; }

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

		public bool IncludeDebugRecords { get; set; }

		public bool IncludeTraceRecords { get; set; }

		public bool IsProcessingLongOperation { get; private set; }

		public bool CanChangeFilter => true;

		public bool AreRecordsSelected { get; private set; }

		[SafeForDependencyAnalysis]
		public IList<IRecord> SelectedItems => _engine.Selector.GetSelected();

		public TimeSpan ElapsedTime { get; private set; }

		public string CurrentHeading { get; private set; }

		public ObservableCollection<MenuItemViewModel> CustomAnalyzerCommands { get; }

		public event EventHandler ResultsChanged;

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
			this.IsFilterToolboxEnabled = false;

			this.HasInsight = false;
			this.HasInsightNeedingAttention = false;
			this.InsightNeedingAttention = 0;

			var openAsResult = new OpenAsResult();
			var wasFileOpened = false;

			try
			{
				IPlugin plugin = new PluginFactory().Create(sourceFilePath);

				if (plugin.CanOpenAs)
				{
					(bool, OpenAsResult) result = plugin.ShowOpenAs(
						_mainWindow,
						LicensePath,
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

						_engine.Filter.HistoryChanged -= OnFilterHistoryChanged;
						_engine.Filter.HistoryChanged += OnFilterHistoryChanged;

						this.Context = _engine.Context;

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

						this.IsFilterToolboxEnabled = true;

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
						if (_engine.Navigate.TableOfContents.Sections.Count == 0)
						{
							_engine.Navigate.RebuildTableOfContents();
						}

						_tableOfContents = _engine.Navigate.TableOfContents;
					}
				).ContinueWith((x) =>
					{
						_insights = _engine.Analyzer.GetInsights();

						this.HasInsight = _insights.Length > 0;
						this.InsightNeedingAttention = _insights.Count(i => i.IsAttentionRequired);
						this.HasInsightNeedingAttention = this.InsightNeedingAttention > 0;
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
			this.IsCommandExecuting = true;
			this.IsProcessingLongOperation = true;
			this.IsFilterToolboxEnabled = false;

			// Creating a background thread for processing.
			// ... Risk: any events raised by the Weevil library will execute on a background thead,
			// ... and not the UI thread which is required in order update/change the UI.
			Task.Run(async () =>
			{
				try
				{
					ImmutableArray<IRecord> oldRecordSelection = _engine.Selector.ClearAll();

					_engine.Save();
					_engine.Reload();

					_engine.Filter.HistoryChanged -= OnFilterHistoryChanged;
					_engine.Filter.HistoryChanged += OnFilterHistoryChanged;

					var newRecordSelection = _engine.Filter.Results
						.Where(a => oldRecordSelection.Any(b => b.LineNumber == a.LineNumber)).ToList();
					_engine.Selector.Select(newRecordSelection);

					RefreshFilterResults();

					RaiseResultsChanged();
				}
				finally
				{
					_uiDispatcher.Invoke(() =>
					{
						this.IsCommandExecuting = false;
						this.IsProcessingLongOperation = false;
						this.IsFilterToolboxEnabled = true;
					});
				}
			});
		}

		public void ClipboardCopyRaw(bool readableCallstack)
		{
			IRecordFormatter formatter = readableCallstack
				? new SimpleCallStackFormatter() as IRecordFormatter
				: new RawRecordFormatter() as IRecordFormatter;

			ClipboardHelper.CopyRawFromSelected(_engine, Settings.Default.AddLineNumberPrefix, formatter);
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
					RefreshStatusBar();
				}
			}
		}

		public void UnSelect(IList<IRecord> records)
		{
			if (!this.IsCommandExecuting)
			{
				_engine.Selector.Unselect(records);
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
				RaisePropertyChanged(nameof(this.SelectedRecordCount));

				this.Metrics = _engine.Filter.GetMetrics();

				this.AreRecordsSelected = _engine.Selector.IsTimePeriodSelected;

				if (_engine.Selector.IsTimePeriodSelected)
				{
					this.ElapsedTime = _engine.Selector.TimePeriodOfInterest;
				}
				else
				{
					this.ElapsedTime = Metadata.ElapsedTimeUnknown;
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

		public void ShowFileExplorer()
		{
			WindowsProcess.Start(WindowsProcessType.FileExplorer, Path.GetDirectoryName(_engine.SourceFilePath));
		}

		public void ShowRegExTool()
		{
			Process.Start(RegEx101Url.ToString());
		}

		public void ShowApplicationLogFile()
		{
			WindowsProcess.Start(WindowsProcessType.FileExplorer, NewReleaseFilePath);
		}

		public void ShowHelp()
		{
			if (File.Exists(HelpFilePath))
			{
				var helpUrl = new Uri("file:///" + HelpFilePath);
				Debug.WriteLine(helpUrl);
				Process.Start("\"" + HelpFilePath + "\"");
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
				var dialog = new AboutDialog(this.WeevilVersion, LicensePath, ThirdPartyNoticesPath)
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

		private void ShowDashboard()
		{
			IPlugin plugin = new PluginFactory().Create(_engine.SourceFilePath);

			if (plugin.CanShowDashboard)
			{
				plugin.ShowDashboard(_mainWindow, this.WeevilVersion, _engine, _insights.ToArray());
			}
			else
			{
				_dialogBox.ShowDashboard(this.WeevilVersion, _engine, _insights);
			}
		}

		private void GraphData()
		{
			_dialogBox.ShowGraph(
				_engine.Selector.GetSelected(), 
				_inclusiveFilter,
				_engine.Analyzer.GetGraphPatternOptions());
		}

		private void ForceGarbageCollection()
		{
			GC.Collect(3, GCCollectionMode.Forced, true, true);
		}
		#endregion

		#region Commands: Filtering

		public void ClearRecords(ClearRecordsOperation operation)
		{
			_engine.Clear(operation);
			FilterAsynchronously(_currentfilterType, _currentfilterCriteria);

			RefreshFilterResults();
			RaiseResultsChanged();

			// HACK: As a developer using the API, how would I know to re-register for existing events. It's not intuitive.
			_engine.Filter.HistoryChanged -= OnFilterHistoryChanged;
			_engine.Filter.HistoryChanged += OnFilterHistoryChanged;
		}

		public void Filter()
		{
			var filters = new object[] { _inclusiveFilter, _exclusiveFilter };
			FilterManually(filters);
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
				FilterManually(filters);
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

		private void FindText()
		{
			if (_dialogBox.TryShowFind(_findText, out _findIsCaseSensitive, out var findNext, out _findText))
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

		private void FindNext()
		{
			if (!string.IsNullOrWhiteSpace(_findText))
			{
				SearchFilterResults(
					$"Unable to find the provided text in the search results.\r\n\r\nSearching for: {_findText}\r\nCase sensitive: {_findIsCaseSensitive}",
					() => _engine
						.Navigate
						.NextContent(_findText, _findIsCaseSensitive)
						.ToIndexUsing(_engine.Filter.Results));
			}
		}

		private void FindPrevious()
		{
			if (!string.IsNullOrWhiteSpace(_findText))
			{
				SearchFilterResults(
					$"Unable to find the provided text in the search results.\r\n\r\nSearching for: {_findText}\r\nCase sensitive: {_findIsCaseSensitive}",
					() => _engine
						.Navigate
						.PreviousContent(_findText, _findIsCaseSensitive)
						.ToIndexUsing(_engine.Filter.Results));
			}
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
								.GoTo(userValue, RecordSearchType.ClosestMatch)
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
									.GoTo(lineNumber, RecordSearchType.ClosestMatch)
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
			var destinationFolder = Path.GetDirectoryName(_engine.SourceFilePath);
			_engine.GenerateReport(ReportType.CommentSummary, destinationFolder);
		}

		public void Analyze(AnalysisType analysisType)
		{
			try
			{
				this.FlaggedRecordCount = -1;
				this.FlaggedRecordCount = _engine
					.Analyzer.Analyze(analysisType, _dialogBox);
				RaisePropertyChanged(nameof(this.FlaggedRecordCount));
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void Analyze(string customAnalyzerKey)
		{
			try
			{
				this.FlaggedRecordCount = -1;
				this.FlaggedRecordCount = _engine
					.Analyzer.Analyze(customAnalyzerKey, _dialogBox);
				RaisePropertyChanged(nameof(this.FlaggedRecordCount));
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
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

		protected virtual void RaiseResultsChanged()
		{
			EventHandler threadSafeHandler = this.ResultsChanged;

			if (threadSafeHandler != null)
			{
				try
				{
					_uiDispatcher.Invoke(() => threadSafeHandler(this, EventArgs.Empty));
				}
				catch (Exception exception)
				{
					Log.Default.Write(
						LogSeverityType.Error,
						exception,
						$"An unexpected error occured while raising the {nameof(ResultsChanged)} event.");
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
		/// </remarks>
		private void FilterAsynchronously(FilterType filterType, IFilterCriteria filterCriteria)
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

		private void SearchFilterResults(string userMessage, Func<int> search)
		{
			try
			{
				this.ActiveRecordIndex = search.Invoke();
			}
			catch (RecordNotFoundException e)
			{
				Log.Default.Write(LogSeverityType.Warning, e, userMessage);
				MessageBox.Show(userMessage, "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
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
				RaisePropertyChanged(nameof(this.AllRecordCount));
				RaisePropertyChanged(nameof(this.HasBeenCleared));
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

			configuration.Add("IsCaseSensitive", this.IsFilterCaseSensitive);

			if (!this.IncludeDebugRecords)
			{
				configuration.Add("HideDebugRecords", true);
			}

			if (!this.IncludeTraceRecords)
			{
				configuration.Add("HideTraceRecords", true);
			}

			return configuration;
		}
	}
}