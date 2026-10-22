#nullable enable

namespace Weevil.VirtualListPoC;

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

internal sealed record FilterOption(FilterKind Kind, string Name);

internal sealed record FilterTimings(double FilterMilliseconds, double KeepSelectionMilliseconds, int ResultCount, long SelectedCount, int RangeCount);

internal sealed record CopyResult(long RecordCount, int CharacterCount, bool WasRefused, double Milliseconds);

/// <summary>
/// The window's state: the rows, the selection, the annotations, and the counters shown in the status bar.
/// </summary>
/// <remarks>
/// With the range selection, the view sends every click and key to this class, which keeps the selection as ranges
/// and tells each row on screen whether it is selected. With WPF's own selection, WPF keeps it, and this class
/// mirrors it in a sorted dictionary of rows, as Weevil 2.12's <c>SelectionManager</c> does.
/// </remarks>
internal sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
	public const int CopyLimit = 100_000;

	public const int SeededPinRecord = 40;

	private readonly Dispatcher _dispatcher;
	private readonly int _recordCount;
	private readonly Dictionary<int, (int Id, string Name)> _bookmarks = new Dictionary<int, (int Id, string Name)>();
	private readonly Dictionary<int, string> _regionStarts = new Dictionary<int, string>();
	private readonly Dictionary<int, string> _regionEnds = new Dictionary<int, string>();
	private readonly SortedDictionary<int, RowViewModel> _wpfSelection = new SortedDictionary<int, RowViewModel>();
	private readonly object _pendingPadlock = new object();
	private readonly List<AnnotationChangedEventArgs> _pendingChanges = new List<AnnotationChangedEventArgs>();
	private readonly DispatcherTimer _statusTimer;

	private RangeSelection _selection = new RangeSelection();
	private int _anchor = -1;
	private int _caret = -1;
	private int _wpfCaretLine;
	private int _activeRecordIndex = -1;
	private bool _isBusy;
	private bool _isRefreshQueued;
	private FilterKind _selectedFilter = FilterKind.AllRecords;
	private string _lastAction = string.Empty;
	private string _rowsText = string.Empty;
	private string _selectionText = string.Empty;
	private string _lookupsText = string.Empty;
	private string _memoryText = string.Empty;

	public MainViewModel(Options options)
	{
		_dispatcher = Dispatcher.CurrentDispatcher;
		_recordCount = options.RowCount;
		this.UsesWpfSelection = options.UsesWpfSelection;

		this.Annotations = new AnnotationStore();
		this.Annotations.Changed += OnAnnotationsChanged;
		this.Rows = new VirtualRowList(this.Annotations, position => !this.UsesWpfSelection && _selection.Contains(position));
		this.UiDelay = new UiDelayMonitor(_dispatcher);

		this.ApplyFilterCommand = new RelayCommand(async () => await ApplyFilterAsync(this.SelectedFilter));
		this.CopyCommand = new RelayCommand(() => Copy(isInteractive: true));
		this.UnpinAllCommand = new RelayCommand(() => Measure("Unpin all", () => this.Annotations.UnpinAll() + " pins removed"));
		this.RemoveCommentsCommand = new RelayCommand(() => Measure("Remove comments", () => this.Annotations.RemoveComments() + " comments removed"));
		this.NextCommentCommand = new RelayCommand(() => GoToNextComment());
		this.NextPinCommand = new RelayCommand(() => GoToNextPin());
		this.ResetCountersCommand = new RelayCommand(ResetCounters);

		SeedAnnotations();

		_statusTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, (_, _) => RefreshStatus(), _dispatcher);
		_statusTimer.Start();
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// Asks the view to bring a row into view and give it keyboard focus.
	/// </summary>
	public event EventHandler<int>? ScrollRequested;

	/// <summary>
	/// With WPF's own selection, asks the view to select these rows again after a filter, as Weevil 2.12's
	/// <c>FilterView.OnResultsChanged</c> does.
	/// </summary>
	public event EventHandler<IReadOnlyList<RowViewModel>>? WpfSelectionRestoreRequested;

	public bool UsesWpfSelection { get; }

	public AnnotationStore Annotations { get; }

	public VirtualRowList Rows { get; }

	public UiDelayMonitor UiDelay { get; }

	public RangeSelection Selection => _selection;

	public int Caret => _caret;

	public long SelectedCount => this.UsesWpfSelection ? _wpfSelection.Count : _selection.Count;

	public string ModeText => this.UsesWpfSelection
		? "Selection: WPF's own (SelectionMode=Extended), as Weevil 2.12"
		: "Selection: ranges";

	public IReadOnlyList<FilterOption> FilterOptions { get; } = Enum.GetValues<FilterKind>()
		.Select(k => new FilterOption(k, SyntheticLog.Describe(k)))
		.ToArray();

	public FilterKind SelectedFilter
	{
		get => _selectedFilter;
		set => SetField(ref _selectedFilter, value, nameof(this.SelectedFilter));
	}

	public bool IsBusy
	{
		get => _isBusy;
		private set => SetField(ref _isBusy, value, nameof(this.IsBusy));
	}

	/// <summary>
	/// With WPF's own selection: the row navigation asks the view to show, through Weevil's <c>FilterListViewBehavior</c>.
	/// Like a Metalama <c>[Observable]</c> property, it raises no event when the value does not change.
	/// </summary>
	public int ActiveRecordIndex
	{
		get => _activeRecordIndex;
		private set => SetField(ref _activeRecordIndex, value, nameof(this.ActiveRecordIndex));
	}

	public string LastAction
	{
		get => _lastAction;
		private set => SetField(ref _lastAction, value, nameof(this.LastAction));
	}

	public string RowsText
	{
		get => _rowsText;
		private set => SetField(ref _rowsText, value, nameof(this.RowsText));
	}

	public string SelectionText
	{
		get => _selectionText;
		private set => SetField(ref _selectionText, value, nameof(this.SelectionText));
	}

	public string LookupsText
	{
		get => _lookupsText;
		private set => SetField(ref _lookupsText, value, nameof(this.LookupsText));
	}

	public string MemoryText
	{
		get => _memoryText;
		private set => SetField(ref _memoryText, value, nameof(this.MemoryText));
	}

	public ICommand ApplyFilterCommand { get; }

	public ICommand CopyCommand { get; }

	public ICommand UnpinAllCommand { get; }

	public ICommand RemoveCommentsCommand { get; }

	public ICommand NextCommentCommand { get; }

	public ICommand NextPinCommand { get; }

	public ICommand ResetCountersCommand { get; }

	public void Dispose()
	{
		_statusTimer.Stop();
		this.UiDelay.Dispose();
	}

	/// <summary>
	/// Shows every record. Called once the window exists, so the first layout is part of what is measured.
	/// </summary>
	public double Open()
	{
		var stopwatch = Stopwatch.StartNew();
		this.Rows.Reset(SyntheticLog.Filter(FilterKind.AllRecords, _recordCount));
		this.LastAction = $"Opened {_recordCount:N0} rows in {stopwatch.Elapsed.TotalMilliseconds:N0} ms";
		RefreshStatus();
		return stopwatch.Elapsed.TotalMilliseconds;
	}

	public void Click(int position, ModifierKeys modifiers)
	{
		var control = (modifiers & ModifierKeys.Control) != 0;
		var shift = (modifiers & ModifierKeys.Shift) != 0;

		Measure("Click", () =>
		{
			if (shift)
			{
				if (_anchor < 0)
				{
					_anchor = position;
				}

				if (!control)
				{
					_selection.Clear();
				}

				_selection.Add(_anchor, position);
			}
			else if (control)
			{
				_selection.Toggle(position);
				_anchor = position;
			}
			else
			{
				_selection.SelectOnly(position);
				_anchor = position;
			}

			_caret = position;
			OnSelectionChanged();
			return SelectionSummary();
		});
	}

	/// <summary>
	/// Moves the keyboard cursor, as the arrow keys, Page Up, Page Down, Home and End do.
	/// </summary>
	public void MoveCaret(int position, ModifierKeys modifiers, string keyName)
	{
		var count = this.Rows.Results.Length;
		if (count == 0)
		{
			return;
		}

		position = System.Math.Clamp(position, 0, count - 1);
		var control = (modifiers & ModifierKeys.Control) != 0;
		var shift = (modifiers & ModifierKeys.Shift) != 0;

		Measure(keyName, () =>
		{
			if (shift)
			{
				if (_anchor < 0)
				{
					_anchor = _caret >= 0 ? _caret : position;
				}

				_selection.Clear();
				_selection.Add(_anchor, position);
			}
			else if (!control)
			{
				_selection.SelectOnly(position);
				_anchor = position;
			}

			_caret = position;
			OnSelectionChanged();
			return SelectionSummary();
		});

		this.ScrollRequested?.Invoke(this, position);
	}

	public void SelectAll()
	{
		Measure("Ctrl+A", () =>
		{
			_selection.SelectAll(this.Rows.Results.Length);
			OnSelectionChanged();
			return SelectionSummary();
		});
	}

	public void ToggleCaret()
	{
		if (_caret >= 0)
		{
			Measure("Ctrl+Space", () =>
			{
				_selection.Toggle(_caret);
				_anchor = _caret;
				OnSelectionChanged();
				return SelectionSummary();
			});
		}
	}

	/// <summary>
	/// Mirrors WPF's selection in a sorted dictionary of rows, as Weevil 2.12's <c>FilterView.ListView_OnSelectionChanged</c> does.
	/// </summary>
	public void OnWpfSelectionChanged(IList added, IList removed)
	{
		foreach (RowViewModel row in removed)
		{
			_wpfSelection.Remove(row.LineNumber);
		}

		foreach (RowViewModel row in added)
		{
			_wpfSelection[row.LineNumber] = row;
			_wpfCaretLine = row.LineNumber;
		}
	}

	public async Task<FilterTimings?> ApplyFilterAsync(FilterKind filter)
	{
		if (this.IsBusy)
		{
			return null;
		}

		this.IsBusy = true;
		try
		{
			var oldResults = this.Rows.Results;
			RangeSelection selection = _selection;
			var caretRecord = _caret >= 0 && _caret < oldResults.Length ? oldResults[_caret] : -1;
			var anchorRecord = _anchor >= 0 && _anchor < oldResults.Length ? oldResults[_anchor] : -1;
			var wpfSelected = _wpfSelection.Values.ToList();

			var stopwatch = Stopwatch.StartNew();
			var newResults = await Task.Run(() => SyntheticLog.Filter(filter, _recordCount));
			var filterMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

			stopwatch.Restart();
			RangeSelection kept = this.UsesWpfSelection
				? new RangeSelection()
				: await Task.Run(() => RangeSelection.Remap(selection, oldResults, newResults));
			var keepMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

			_selection = kept;
			_caret = PositionOf(newResults, caretRecord);
			_anchor = PositionOf(newResults, anchorRecord);
			this.Rows.Reset(newResults);

			if (this.UsesWpfSelection)
			{
				// Resetting the list cleared WPF's selection; Weevil 2.12 then selects the remembered records again.
				stopwatch.Restart();
				_wpfSelection.Clear();
				var rows = new List<RowViewModel>();
				foreach (RowViewModel row in wpfSelected)
				{
					var position = this.Rows.FindPosition(row.RecordNumber);
					if (position >= 0)
					{
						rows.Add(this.Rows.GetRow(position));
					}
				}

				this.WpfSelectionRestoreRequested?.Invoke(this, rows);
				keepMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
			}

			OnSelectionChanged();

			var timings = new FilterTimings(filterMilliseconds, keepMilliseconds, newResults.Length, this.SelectedCount, _selection.RangeCount);
			this.LastAction = $"{SyntheticLog.Describe(filter)}: {newResults.Length:N0} rows; filter {filterMilliseconds:N0} ms, " +
				$"keep the selection {keepMilliseconds:N0} ms; {SelectionSummary()}";
			return timings;
		}
		finally
		{
			this.IsBusy = false;
		}
	}

	public CopyResult Copy(bool isInteractive)
	{
		var stopwatch = Stopwatch.StartNew();
		var count = this.SelectedCount;

		if (count > CopyLimit)
		{
			var refused = new CopyResult(count, 0, WasRefused: true, stopwatch.Elapsed.TotalMilliseconds);
			this.LastAction = $"Copy refused: {count:N0} records selected, more than {CopyLimit:N0}";
			if (isInteractive)
			{
				MessageBox.Show(
					$"Copy is limited to {CopyLimit:N0} records, and {count:N0} are selected.\n\nUse Export to save them to a file.",
					"Copy",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}

			return refused;
		}

		var builder = new StringBuilder();
		IEnumerable<int> recordNumbers = this.UsesWpfSelection
			? _wpfSelection.Values.Select(r => r.RecordNumber)
			: _selection.EnumeratePositions().Select(p => this.Rows.Results[p]);
		foreach (var recordNumber in recordNumbers)
		{
			builder.Append(SyntheticLog.GetContent(recordNumber)).Append("\r\n");
		}

		var text = builder.ToString();
		try
		{
			Clipboard.SetText(text);
		}
		catch (ExternalException)
		{
			// Another program holds the clipboard; the time to build the text is what matters here.
		}

		var result = new CopyResult(count, text.Length, WasRefused: false, stopwatch.Elapsed.TotalMilliseconds);
		this.LastAction = $"Copied {count:N0} records ({text.Length:N0} characters) in {result.Milliseconds:N0} ms";
		return result;
	}

	public int GoToNextComment()
	{
		return GoToNext(this.Annotations.GetCommentedLinesFrom(GetCurrentLine()), "comment");
	}

	public int GoToNextPin()
	{
		return GoToNext(this.Annotations.GetPinnedLinesFrom(GetCurrentLine()), "pinned record");
	}

	public void ResetCounters()
	{
		LookupCounters counters = this.Rows.Counters;
		counters.Indexer = counters.IndexOf = counters.Contains = counters.Count = 0;
		counters.Enumerations = counters.EnumeratedRows = counters.CopyTo = counters.RowsCreated = 0;
		this.UiDelay.Reset();
		RefreshStatus();
	}

	public bool TryGetBookmark(RowViewModel row, out int id, out string name)
	{
		var found = _bookmarks.TryGetValue(row.LineNumber, out (int Id, string Name) bookmark);
		(id, name) = found ? bookmark : (0, string.Empty);
		return found;
	}

	public bool RegionStartsWith(RowViewModel row, out string name)
	{
		return _regionStarts.TryGetValue(row.LineNumber, out name!);
	}

	public bool RegionEndsWith(RowViewModel row, out string name)
	{
		return _regionEnds.TryGetValue(row.LineNumber, out name!);
	}

	public void RefreshStatus()
	{
		LookupCounters counters = this.Rows.Counters;
		this.RowsText = $"Rows: {this.Rows.Results.Length:N0}";
		this.SelectionText = this.UsesWpfSelection
			? $"Selected: {this.SelectedCount:N0}"
			: $"Selected: {this.SelectedCount:N0} in {_selection.RangeCount:N0} range(s)";
		this.LookupsText = $"WPF asked for: rows {counters.Indexer:N0}, IndexOf {counters.IndexOf:N0}, Contains {counters.Contains:N0}, " +
			$"Count {counters.Count:N0}, enumerations {counters.Enumerations:N0} ({counters.EnumeratedRows:N0} rows), CopyTo {counters.CopyTo:N0} | rows created {counters.RowsCreated:N0}";
		using var process = Process.GetCurrentProcess();
		this.MemoryText = $"Managed {GC.GetTotalMemory(false) / 1_048_576.0:N0} MB, working set {process.WorkingSet64 / 1_048_576.0:N0} MB | " +
			$"longest UI pause {this.UiDelay.LongestMilliseconds:N0} ms";
	}

	private static int PositionOf(int[] results, int recordNumber)
	{
		if (recordNumber < 0)
		{
			return -1;
		}

		var position = Array.BinarySearch(results, recordNumber);
		return position >= 0 ? position : -1;
	}

	private int GetCurrentLine()
	{
		if (this.UsesWpfSelection)
		{
			return _wpfCaretLine;
		}

		return _caret >= 0 && _caret < this.Rows.Results.Length ? SyntheticLog.GetLineNumber(this.Rows.Results[_caret]) : 0;
	}

	private int GoToNext(int[] lineNumbers, string what)
	{
		var target = -1;
		foreach (var lineNumber in lineNumbers)
		{
			target = this.Rows.FindPositionOfLine(lineNumber);
			if (target >= 0)
			{
				break;
			}
		}

		if (target < 0)
		{
			this.LastAction = $"No {what} in the results";
			return -1;
		}

		if (this.UsesWpfSelection)
		{
			// Weevil 2.12: the view scrolls, focuses the row and collapses WPF's selection (FilterListViewBehavior).
			this.ActiveRecordIndex = target;
		}
		else
		{
			_selection.SelectOnly(target);
			_anchor = target;
			_caret = target;
			OnSelectionChanged();
			this.ScrollRequested?.Invoke(this, target);
		}

		this.LastAction = $"Next {what}: line {SyntheticLog.GetLineNumber(this.Rows.Results[target]):N0}";
		return target;
	}

	private void OnSelectionChanged()
	{
		if (!this.UsesWpfSelection)
		{
			foreach (RowViewModel row in this.Rows.GetLiveRows())
			{
				row.IsSelected = _selection.Contains(row.Position);
			}
		}
	}

	private string SelectionSummary()
	{
		return this.UsesWpfSelection
			? $"{this.SelectedCount:N0} selected"
			: $"{this.SelectedCount:N0} selected in {_selection.RangeCount:N0} range(s)";
	}

	private void Measure(string action, Func<string> work)
	{
		var stopwatch = Stopwatch.StartNew();
		var summary = work();
		this.LastAction = $"{action}: {stopwatch.Elapsed.TotalMilliseconds:N2} ms; {summary}";
	}

	/// <summary>
	/// Changes arrive on the writer's thread. They are queued, and the rows on screen refresh later on the UI thread,
	/// so a writer never waits for the grid (decision 4 of the #990 plan).
	/// </summary>
	private void OnAnnotationsChanged(object? sender, AnnotationChangedEventArgs e)
	{
		lock (_pendingPadlock)
		{
			_pendingChanges.Add(e);
			if (_isRefreshQueued)
			{
				return;
			}

			_isRefreshQueued = true;
		}

		_dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(RefreshChangedRows));
	}

	private void RefreshChangedRows()
	{
		AnnotationChangedEventArgs[] changes;
		lock (_pendingPadlock)
		{
			changes = _pendingChanges.ToArray();
			_pendingChanges.Clear();
			_isRefreshQueued = false;
		}

		ILookup<int, RowViewModel> rowsByLine = this.Rows.GetLiveRows().ToLookup(r => r.LineNumber);
		foreach (AnnotationChangedEventArgs change in changes)
		{
			IEnumerable<RowViewModel> rows = change.LineNumber == AnnotationChangedEventArgs.AllLines
				? rowsByLine.SelectMany(g => g)
				: rowsByLine[change.LineNumber];
			foreach (RowViewModel row in rows)
			{
				row.Refresh(change.Source);
			}
		}
	}

	private void SeedAnnotations()
	{
		// Lines 10 and 20 are the manual test for #806. The two pins check that Ctrl+Plus crosses the whole log.
		this.Annotations.SetComment(10, "Comment on line 10", this);
		this.Annotations.SetComment(20, "Comment on line 20", this);
		this.Annotations.SetPinned(SyntheticLog.GetLineNumber(SeededPinRecord), true, this);
		this.Annotations.SetPinned(SyntheticLog.GetLineNumber(System.Math.Max(0, _recordCount - 1_000)), true, this);

		_bookmarks[SyntheticLog.GetLineNumber(100)] = (1, "Retry starts here");
		_regionStarts[SyntheticLog.GetLineNumber(1_000)] = "Startup";
		_regionEnds[SyntheticLog.GetLineNumber(2_000)] = "Startup";
	}

	private void SetField<T>(ref T field, T value, string propertyName)
	{
		if (!EqualityComparer<T>.Default.Equals(field, value))
		{
			field = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}

internal sealed class RelayCommand : ICommand
{
	private readonly Action _execute;

	public RelayCommand(Action execute)
	{
		_execute = execute;
	}

	public event EventHandler? CanExecuteChanged
	{
		add { }
		remove { }
	}

	public bool CanExecute(object? parameter) => true;

	public void Execute(object? parameter) => _execute();
}
