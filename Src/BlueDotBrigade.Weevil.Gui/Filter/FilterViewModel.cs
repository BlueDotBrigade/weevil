﻿namespace BlueDotBrigade.Weevil.Gui.Filter
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
	using System.Reflection;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Threading;
	using BlueDotBrigade.Weevil;
	using GongSolutions.Wpf.DragDrop;
	using PostSharp.Patterns.Model;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Configuration;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using BlueDotBrigade.Weevil.Gui.Analysis;
	using BlueDotBrigade.Weevil.Gui.Help;
	using BlueDotBrigade.Weevil.Gui.IO;
	using BlueDotBrigade.Weevil.IO;
	using BlueDotBrigade.Weevil.Navigation;
	using BlueDotBrigade.Weevil.Reports;
	using BlueDotBrigade.Weevil.Runtime.Serialization;
	using BlueDotBrigade.Weevil.Gui.Properties;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.Utilities;
	using Directory = System.IO.Directory;
	using File = System.IO.File;
	using SelectFileView = BlueDotBrigade.Weevil.Gui.IO.SelectFileView;
	using System.Windows.Input;
	using PostSharp.Extensibility;
	using System.Net.Http;
	using Newtonsoft.Json.Linq;

	[NotifyPropertyChanged()]
	internal partial class FilterViewModel : IDropTarget, INotifyPropertyChanged
	{
		const string TsvFileName = "SelectedRecords.tsv";
		const string RawFileName = "SelectedRecords.log";

		private static readonly Uri NewReleaseUrl =
			new Uri(@"https://raw.githubusercontent.com/BlueDotBrigade/weevil/master/Doc/Notes/Release/NewReleaseNotification.xml");

		private static readonly Uri RegEx101Url = new Uri(@"https://regex101.com/r/EKCf6T/4");
		private const string CompatibleFileExtensions = "Log Files (*.log, *.csv, *.txt)|*.log;*.csv;*.tsv;*.txt|Compressed Files (*.zip)|*.zip|All files (*.*)|*.*";

		private static readonly string HelpFilePath = Path.GetFullPath(EnvironmentHelper.GetExecutableDirectory() + @"\..\Doc\Help.html");
		private static readonly string LicensePath = Path.GetFullPath(EnvironmentHelper.GetExecutableDirectory() + @"\..\Licenses\License.md");
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

		private readonly DragAndDropViewModel _dragAndDrop;

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

		public FilterViewModel(IUiDispatcher uiDispatcher, IBulletinMediator bulletinMediator)
		{
			_uiDispatcher = uiDispatcher;
			_dialogBox = new DialogBoxService();

			_bulletinMediator = bulletinMediator;

			_engine = Engine.Surrogate;

			this.IsLogFileOpen = false;
			this.CanOpenLogFile = true;

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

			this.WeevilVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(128, 128, 128);

			initializationTimer = new DispatcherTimer();
			initializationTimer.Tick += (sender, args) => OnInitialize();
			initializationTimer.Interval = TimeSpan.FromSeconds(2);
			initializationTimer.Start();

			_dragAndDrop = new DragAndDropViewModel();
			_dragAndDrop.DroppedFile += OnFileDropped;

			_tableOfContents = new TableOfContents();

			this.CustomAnalyzerCommands = new ObservableCollection<MenuItemViewModel>();
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
		#endregion

		#region Properties

		public IList<IRecord> VisibleItems { get; private set; }

		public Version WeevilVersion { get; private set; }

		[SafeForDependencyAnalysis]
		public bool IsMenuEnabled
		{
			get
			{
				if (Depends.Guard)
				{
					Depends.On(this.IsLogFileOpen, this.CanOpenLogFile);
				}

				return this.IsLogFileOpen && this.CanOpenLogFile;
			}
		}

		public bool IsLogFileOpen { get; private set; }

		public bool CanOpenLogFile { get; private set; }
		public bool IncludePinned { get; set; }
		public bool IsManualFilter { get; set; }

		public bool IsFilterCaseSensitive { get; set; }

		public bool IsFilterInProgress { get; private set; }

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

		[SafeForDependencyAnalysis]
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
					_bulletinMediator.Post(new SourceFileRemarksChangedBulletin
					{
						HasSourceFileRemarks = _engine.SourceFileRemarks.Any()
					});
					RaisePropertyChanged(nameof(this.SourceFileRemarks));
				}
			}
		}

		public int ActiveRecordIndex { get; set; }

		public bool IncludeDebugRecords { get; set; }

		public bool IncludeTraceRecords { get; set; }

		public bool IsProcessingLongOperation { get; private set; }

		[SafeForDependencyAnalysis]
		public IList<IRecord> SelectedItems => _engine.Selector.GetSelected();

		public ObservableCollection<MenuItemViewModel> CustomAnalyzerCommands { get; }

		public event PropertyChangedEventHandler PropertyChanged;

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

		private void OnFileDropped(object sender, DroppedFileEventArgs e)
		{
			OpenCompressedAsync(e.FilePath);
		}

		protected void RaisePropertyChanged(string name)
		{
			this?.PropertyChanged(this, new PropertyChangedEventArgs(name));
		}
		#endregion

		#region Commands: General
		public async void OpenAsync()
		{
			var filePath = _dialogBox.ShowOpenFile(CompatibleFileExtensions);
			if (string.IsNullOrWhiteSpace(filePath))
			{
				Log.Default.Write(LogSeverityType.Debug, "Open file operation was aborted by the user.");
			}
			else
			{
				await OpenCompressedAsync(filePath);
			}
		}

		public async Task OpenCompressedAsync(string sourceFilePath)
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
			this.CanOpenLogFile = false;
			this.IsProcessingLongOperation = true;
			this.IsFilterToolboxEnabled = false;

			var openAsResult = new OpenAsResult();
			var wasOpenRequested = false;

			try
			{
				IPlugin plugin = new PluginFactory().Create(sourceFilePath);

				if (plugin.CanOpenAs)
				{
					(bool, OpenAsResult) result = plugin.ShowOpenAs(
						LicensePath,
						(path) => Engine.UsingPath(path),
						sourceFilePath);

					wasOpenRequested = result.Item1;
					openAsResult = result.Item2;
				}
				else
				{
					wasOpenRequested = true;
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

			if (wasOpenRequested)
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
							.UsingPath(sourceFilePath, openAsResult.Range.Start.Value)
							.UsingContext(openAsResult.Context)
							.UsingRange(openAsResult.Range)
							.Open();

						this.FileOpened?.Invoke(this, EventArgs.Empty);

						_bulletinMediator.Post(new SourceFileOpenedBulletin
						{
							SourceFilePath = _engine.SourceFilePath,
							SourceFileLoadingPeriod = _engine.Metrics.SourceFileLoadingPeriod,
							Context = _engine.Context,
							TotalRecordCount = _engine.Count
						});

						_uiDispatcher.Invoke(() =>
						{
							this.SourceFileRemarks = _engine.SourceFileRemarks;
						});

						_bulletinMediator.Post(new SourceFileRemarksChangedBulletin
						{
							HasSourceFileRemarks = _engine.SourceFileRemarks.Any()
						});

						var selectedItem = _engine.Selector.Selected.FirstOrDefault().Value;

						_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));

						_bulletinMediator.Post(new AnalysisCompleteBulletin(0));

						Log.Default.Write("Updating filter history on the UI.");
						RefreshHistory(this.InclusiveFilterHistory, _engine.Filter.IncludeHistory);
						RefreshHistory(this.ExclusiveFilterHistory, _engine.Filter.ExcludeHistory);

						_engine.Filter.HistoryChanged -= OnFilterHistoryChanged;
						_engine.Filter.HistoryChanged += OnFilterHistoryChanged;

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
						_insights = _engine.Analyzer.GetInsights();

						_bulletinMediator.Post(new InsightChangedBulletin
						{
							HasInsight = _insights.Length > 0,
							InsightNeedingAttention = _insights.Count(i => i.IsAttentionRequired)
						});

						_uiDispatcher.Invoke(() =>
						{
							this.CanOpenLogFile = true;
							CommandManager.InvalidateRequerySuggested();
						});
					}
				);
			}
			else
			{
				this.CanOpenLogFile = true;
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

		public void SaveMetadata()
		{
			_engine.Save();
		}

		public void Reload()
		{
			this.CanOpenLogFile = false;
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

					_bulletinMediator.Post(new SourceFileOpenedBulletin
					{
						SourceFilePath = _engine.SourceFilePath,
						SourceFileLoadingPeriod = _engine.Metrics.SourceFileLoadingPeriod,
						Context = _engine.Context,
						TotalRecordCount = _engine.Count
					});

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
						this.CanOpenLogFile = true;
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

		public void ClipboardCopyLineNumbers()
		{
			ClipboardHelper.CopySelectedLineNumbers(_engine);
		}

		public void ClipboardCopyTimestamps()
		{
			ClipboardHelper.CopySelectedTimestamps(_engine);
		}

		public void ClipboardCopyComment()
		{
			ClipboardHelper.CopySelectedComments(_engine);
		}

		public void ClipboardPaste(bool allowOverwrite)
		{
			ClipboardHelper.PasteToSelected(_engine, allowOverwrite);
			RefreshFilterResults();
		}

		public void Select(IList<IRecord> records)
		{
			if (this.CanOpenLogFile)
			{
				_engine.Selector.Select(records);

				_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
			}
		}

		public void UnSelect(IList<IRecord> records)
		{
			if (this.CanOpenLogFile)
			{
				_engine.Selector.Unselect(records);

				_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
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

		private void SaveSelected(FileFormatType fileFormatType)
		{
			try
			{
				var destinationFolder = Path.GetDirectoryName(_engine.SourceFilePath);

				var destinationFilePath = fileFormatType == FileFormatType.Tsv ?
					Path.Combine(destinationFolder, TsvFileName) :
					Path.Combine(destinationFolder, RawFileName);

				_engine.Selector.SaveSelection(destinationFilePath, fileFormatType);
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
			WindowsProcess.Start(
				WindowsProcessType.FileExplorer,
				Path.GetDirectoryName(_engine.SourceFilePath));
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
					this.WeevilVersion,
					LicensePath,
					ThirdPartyNoticesPath,
					_engine.SourceFilePath);

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
				plugin.ShowDashboard(this.WeevilVersion, _engine, _insights.ToArray());
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
				_inclusiveFilter);
		}

		private void ForceGarbageCollection()
		{
			GC.Collect(3, GCCollectionMode.Forced, true, true);
		}
		#endregion

		#region Commands: Filtering

		public void ClearRecords(ClearOperation operation)
		{
			_engine.Clear(operation);

			_bulletinMediator.Post(new ClearRecordsBulletin
			{
				TotalRecordCount = _engine.Count
			});

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
				FilterAsynchronously(FilterType.RegularExpression, _previousFilterCriteria);
			}
			else
			{
				_previousFilterCriteria = (FilterCriteria)_engine.Filter.Criteria;
				FilterAsynchronously(FilterType.RegularExpression, FilterCriteria.None);
			}
		}

		private void Refresh()
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
				var results = _engine
					.Analyzer.Analyze(analysisType, _dialogBox);

				_bulletinMediator.Post(new AnalysisCompleteBulletin(
					results.FlaggedRecords,
					results.Data));
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
				Results results = _engine
					.Analyzer.Analyze(customAnalyzerKey, _dialogBox);

				_bulletinMediator.Post(new AnalysisCompleteBulletin(
					results.FlaggedRecords,
					results.Data));
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
		protected virtual void RaiseRegionsChanged()
		{
			EventHandler threadSafeHandler = this.RegionsChanged;

			if (threadSafeHandler != null)
			{
				try
				{
					Log.Default.Write(
						LogSeverityType.Debug,
						$"Raising the {nameof(RegionsChanged)} event.");

					_uiDispatcher.Invoke(() => threadSafeHandler(this, EventArgs.Empty));
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
			EventHandler threadSafeHandler = this.BookmarksChanged;

			if (threadSafeHandler != null)
			{
				try
				{
					Log.Default.Write(
							LogSeverityType.Debug,
							$"Raising the {nameof(BookmarksChanged)} event.");

					_uiDispatcher.Invoke(() => threadSafeHandler(this, EventArgs.Empty));
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
				this.IsFilterInProgress = _concurrentFilterCount >= 1;

				// Force UI to ensure that the screen has been refreshed
				// ... so that the user knows a filter operation is in progress.
				_uiDispatcher.Invoke(delegate () { }, DispatcherPriority.Render);

				// First filter to execute?
				if (queuedFilters == 1)
				{
					Log.Default.Write(
						LogSeverityType.Debug,
						$"Filter operation is displaying the progress bar.");

					_uiDispatcher.Invoke(() =>
					{
						this.CanOpenLogFile = false;
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
				this.IsFilterInProgress = _concurrentFilterCount >= 1;

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
						this.CanOpenLogFile = true;
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
			});

			_bulletinMediator.Post(new FilterChangedBulletin
			{
				SelectedRecordCount = _engine.Selector.Selected.Count,
				VisibleRecordCount = this.VisibleItems?.Count ?? 0,
				SeverityMetrics = _engine.Filter.GetMetrics(),
				ExecutionTime = _engine.Filter.FilterExecutionTime
			});

			// Remember: filtering can impact the number of selected records.
			_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
		}

		private static SelectionChangedBulletin BuildSelectionChangedBulletin(ICoreEngine coreEngine)
		{
			var selectedItemCount = coreEngine.Selector.Selected.Count;
			var selectedTimePeriod = coreEngine.Selector.SelectionPeriod;

			var lineNumber = 0;
			var sectionName = string.Empty;
			var regionName = string.Empty;

			if (coreEngine.Selector.Selected.Count > 0)
			{
				var selectedItem = coreEngine.Selector.Selected.First().Value;

				lineNumber = selectedItem.LineNumber;

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

		private void AddRegion()
		{
			try
			{
				if (_engine.Selector.HasSelectionPeriod)
				{
					if (_dialogBox.TryShowUserPrompt("Create Region", "Name", @"^[a-zA-Z0-9\-]{1,12}$", "Must be 1 to 12 characters: letters, numbers, or hyphens.", out var regionName))
					{
						var selectedLineNumbers = _engine.Selector.Selected.Keys.ToArray();
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
			if (_engine.Selector.Selected.Count == 1)
			{
				var selectedLineNumber = _engine.Selector.Selected.Single().Value.LineNumber;

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

		private void AddBookmark()
		{
			try
			{
				if (_engine.Selector.Selected.Count == 1)
				{
					if (_dialogBox.TryShowUserPrompt("Create Bookmark", "Name", @"^[a-zA-Z0-9\-]{1,12}$", "Must be 1 to 12 characters: letters, numbers, or hyphens.", out var bookmarkName))
					{
						var selectedLineNumber = _engine.Selector.Selected.Single().Value.LineNumber;
						_engine.Bookmarks.CreateFromSelection(bookmarkName, selectedLineNumber);
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

		private void RemoveBookmark()
		{
			if (_engine.Selector.Selected.Count == 1)
			{
				var selectedLineNumber = _engine.Selector.Selected.Single().Value.LineNumber;

				_engine.Bookmarks.Clear(selectedLineNumber);
				RaiseBookmarksChanged();
				_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
			}
			else
			{
				MessageBox.Show("A single record must be selected in order to remove a bookmark.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
				if (_engine.Selector.Selected.Count == 1)
				{
					var selectedLineNumber = _engine.Selector.Selected.Single().Value.LineNumber;

					var existing = _engine.Bookmarks.Bookmarks.FirstOrDefault(b => b.Name == slot.ToString());
					if (existing != null)
					{
						_engine.Bookmarks.Clear(existing.Record.LineNumber);
					}

					_engine.Bookmarks.CreateFromSelection(slot.ToString(), selectedLineNumber);

					RaiseBookmarksChanged();
					_bulletinMediator.Post(BuildSelectionChangedBulletin(_engine));
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
			var bookmark = _engine.Bookmarks.Bookmarks.FirstOrDefault(b => b.Name == slot.ToString());
			if (bookmark != null)
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
	}
}
