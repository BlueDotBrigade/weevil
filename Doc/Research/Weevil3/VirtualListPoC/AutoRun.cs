#nullable enable

namespace Weevil.VirtualListPoC;

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

/// <summary>
/// One line of the report.
/// </summary>
/// <param name="Milliseconds">From the start of the action until the window has finished its layout.</param>
/// <param name="Passed">Null when the step has no target.</param>
/// <param name="GcPauseMilliseconds">How long the garbage collector paused every thread, the UI thread included; NaN when not measured.</param>
internal sealed record Step(string Name, double Milliseconds, string Target, bool? Passed, LookupCounters Lookups, double ManagedMegabytes, string Result, double GcPauseMilliseconds = double.NaN);

/// <summary>
/// Runs every scenario of #1014 through the same methods the keys and the mouse call, then writes a Markdown report.
/// </summary>
/// <remarks>
/// Each time runs from the start of an action until the dispatcher reaches <c>ContextIdle</c>, after layout and
/// rendering, so it is how long the window could not respond.
/// </remarks>
internal sealed class AutoRun
{
	private const double InstantMilliseconds = 100;
	private const double ResponsiveMilliseconds = 250;
	private const double KeepSelectionMilliseconds = 1_000;
	private const double SelectAllMegabytes = 10;
	private const int MaximumLiveRows = 1_000;

	private readonly MainWindow _window;
	private readonly MainViewModel _viewModel;
	private readonly Options _options;
	private readonly List<Step> _steps = new List<Step>();
	private readonly List<string> _notes = new List<string>();
	private DateTime _started;

	public AutoRun(MainWindow window, MainViewModel viewModel, Options options)
	{
		_window = window;
		_viewModel = viewModel;
		_options = options;
	}

	private int RowCount => _viewModel.Rows.Results.Length;

	public async Task RunAsync()
	{
		_started = DateTime.Now;
		try
		{
			AddStep(new Step(
				$"Open: build the results and show the first rows",
				_window.OpenMilliseconds,
				string.Empty,
				null,
				new LookupCounters(),
				ManagedMegabytes(collect: false),
				$"{this.RowCount:N0} rows"));

			if (_viewModel.UsesWpfSelection)
			{
				await RunWithWpfSelectionAsync();
			}
			else
			{
				await RunWithRangeSelectionAsync();
			}
		}
		catch (Exception exception)
		{
			_notes.Add("**The run stopped early:** " + exception.GetType().Name + ": " + exception.Message);
		}

		SaveReport(isFinished: true);
	}

	/// <summary>
	/// Adds a step and saves the report at once, so a crash or a hang in a later step keeps the steps before it.
	/// </summary>
	private void AddStep(Step step)
	{
		_steps.Add(step);
		SaveReport(isFinished: false);
	}

	private void SaveReport(bool isFinished)
	{
		try
		{
			File.WriteAllText(_options.ReportPath, BuildReport(isFinished));
		}
		catch (IOException exception)
		{
			_notes.Add("Could not save the report: " + exception.Message);
		}
	}

	private async Task RunWithRangeSelectionAsync()
	{
		var last = this.RowCount - 1;
		var random = new Random(1014);

		foreach (var fraction in new[] { 0.25, 0.5, 0.75, 1.0, 0.0 })
		{
			await MeasureAsync(
				$"Drag the scroll bar to {fraction:P0}",
				() => _window.ScrollToPosition((int)(fraction * last)),
				() => $"top row: line {TopLine():N0}",
				ResponsiveTarget());
		}

		await MeasureRepeatedAsync("Page Down, 100 times from the middle", 100, async () =>
		{
			if (_viewModel.Caret < 0 || _viewModel.Caret > last - 1_000)
			{
				_viewModel.Click(this.RowCount / 2, ModifierKeys.None);
			}

			var page = System.Math.Max(1, (int)(_window.ScrollViewer?.ViewportHeight ?? 20) - 1);
			_viewModel.MoveCaret(_viewModel.Caret + page, ModifierKeys.None, "Page Down");
			await IdleAsync();
		});

		await MeasureRepeatedAsync("Scroll a random row into view, 1,000 times", 1_000, async () =>
		{
			var position = random.Next(this.RowCount);
			_window.RowList.ScrollIntoView(_viewModel.Rows.GetRow(position));
			await IdleAsync();
		});

		await MeasureAsync("Click a row", () => _viewModel.Click(10, ModifierKeys.None), SelectionResult, InstantTarget());

		await MeasureAsync("Ctrl+A", _viewModel.SelectAll, SelectionResult, InstantTarget(), measuresMemory: true, maximumMegabytes: SelectAllMegabytes);

		_viewModel.Click(10, ModifierKeys.None);
		await MeasureAsync("Shift+End from row 10", () => _viewModel.MoveCaret(last, ModifierKeys.Shift, "Shift+End"), SelectionResult, InstantTarget());

		_viewModel.Click(0, ModifierKeys.None);
		await MeasureAsync(
			"Ctrl+click 1,000 random rows",
			() =>
			{
				for (var i = 0; i < 1_000; i++)
				{
					_viewModel.Click(random.Next(this.RowCount), ModifierKeys.Control);
				}
			},
			SelectionResult,
			ResponsiveTarget());

		await RunFiltersAsync();

		await MeasureIndexOfAsync(random);

		await RunNavigationAsync();

		await RunCommentEditingAsync();

		await RunBulkChangeAsync();

		await RunCopyAsync();

		await CountLiveRowsAsync();
	}

	private async Task RunWithWpfSelectionAsync()
	{
		var last = this.RowCount - 1;
		foreach (var fraction in new[] { 0.5, 1.0, 0.0 })
		{
			await MeasureAsync(
				$"Drag the scroll bar to {fraction:P0}",
				() => _window.ScrollToPosition((int)(fraction * last)),
				() => $"top row: line {TopLine():N0}",
				ResponsiveTarget());
		}

		await MeasureAsync(
			"Ctrl+A (WPF's SelectAll)",
			() => _window.RowList.SelectAll(),
			() => $"{_window.RowList.SelectedItems.Count:N0} selected",
			InstantTarget(),
			measuresMemory: true,
			maximumMegabytes: SelectAllMegabytes);

		await MeasureAsync("Copy", () => _viewModel.Copy(isInteractive: false), () => _viewModel.LastAction, string.Empty);

		await MeasureReselectionAsync();

		await CountLiveRowsAsync();

		_notes.Add("Shift+End and Ctrl+Shift+Plus (#806) are not run automatically with WPF's own selection: WPF reads the real state of the Shift and Ctrl keys. Try them by hand.");
	}

	/// <summary>
	/// After each filter, Weevil 2.12 selects the remembered records again with one <c>SelectedItems.Add</c> each
	/// (<c>FilterView.OnResultsChanged</c>). With 500,000 rows to select again, that froze the window on 2026-10-22,
	/// so this measures doubling counts instead, and stops after the first that takes 5 s or more.
	/// </summary>
	private async Task MeasureReselectionAsync()
	{
		for (var count = 1_000; count <= this.RowCount; count *= 2)
		{
			_window.RowList.UnselectAll();
			await IdleAsync();
			var rows = Enumerable.Range(0, count).Select(_viewModel.Rows.GetRow).ToList();

			await MeasureAsync(
				$"Select {count:N0} rows again after a filter, one Add each (Weevil 2.12)",
				() =>
				{
					foreach (RowViewModel row in rows)
					{
						_window.RowList.SelectedItems.Add(row);
					}
				},
				() => $"{_window.RowList.SelectedItems.Count:N0} selected",
				ResponsiveTarget());

			if (_steps[^1].Milliseconds >= 5_000)
			{
				_notes.Add($"Selecting rows again stopped at {count:N0}: it took {_steps[^1].Milliseconds / 1_000:N1} s. Weevil 2.12 does this after every filter, for every selected row.");
				break;
			}
		}

		_window.RowList.UnselectAll();
		await IdleAsync();
	}

	private async Task RunFiltersAsync()
	{
		_viewModel.SelectAll();
		await MeasureFilterAsync(FilterKind.EverySecondRecord, "Filter: every 2nd record, all rows selected");
		await MeasureFilterAsync(FilterKind.EveryTenthRecord, "Filter: every 10th record, every 2nd record selected");
		await MeasureFilterAsync(FilterKind.AllRecords, "Filter: all records, every 10th record selected (the worst case)", measuresMemory: true);

		_viewModel.Click(this.RowCount / 3, ModifierKeys.None);
		await MeasureFilterAsync(FilterKind.WarningsAndAbove, "Filter: warnings and above, one row selected");

		_viewModel.Click(0, ModifierKeys.None);
		await MeasureFilterAsync(FilterKind.AllRecords, "Filter: all records, one row selected");
	}

	private async Task MeasureFilterAsync(FilterKind filter, string name, bool measuresMemory = false)
	{
		await IdleAsync();
		var before = ManagedMegabytes(collect: measuresMemory);
		LookupCounters counters = _viewModel.Rows.Counters.Snapshot();
		_viewModel.UiDelay.Reset();
		TimeSpan gcPause = GC.GetTotalPauseDuration();
		var stopwatch = Stopwatch.StartNew();

		FilterTimings? timings = await _viewModel.ApplyFilterAsync(filter);
		await IdleAsync();

		var milliseconds = stopwatch.Elapsed.TotalMilliseconds;
		var gcMilliseconds = (GC.GetTotalPauseDuration() - gcPause).TotalMilliseconds;
		LookupCounters lookups = _viewModel.Rows.Counters.Snapshot().Subtract(counters);
		var after = ManagedMegabytes(collect: measuresMemory);
		var pause = _viewModel.UiDelay.LongestMilliseconds;

		if (timings is null)
		{
			_notes.Add($"{name}: skipped, because a filter was still running.");
			return;
		}

		var result = $"{timings.ResultCount:N0} rows; filter {timings.FilterMilliseconds:N0} ms, keep the selection {timings.KeepSelectionMilliseconds:N0} ms; " +
			$"{timings.SelectedCount:N0} selected" + (_viewModel.UsesWpfSelection ? string.Empty : $" in {timings.RangeCount:N0} range(s)") +
			$"; longest UI pause {pause:N0} ms" + (measuresMemory ? $"; {after - before:N0} MB more" : string.Empty);

		AddStep(new Step(
			name,
			milliseconds,
			$"keep the selection < {KeepSelectionMilliseconds:N0} ms; UI pause < {ResponsiveMilliseconds:N0} ms",
			timings.KeepSelectionMilliseconds < KeepSelectionMilliseconds && pause < ResponsiveMilliseconds,
			lookups,
			after,
			result,
			gcMilliseconds));
	}

	private async Task MeasureIndexOfAsync(Random random)
	{
		await IdleAsync();
		RowViewModel[] rows = Enumerable.Range(0, 1_000).Select(_ => _viewModel.Rows.GetRow(random.Next(this.RowCount))).ToArray();
		const int calls = 1_000_000;

		var stopwatch = Stopwatch.StartNew();
		long found = 0;
		for (var i = 0; i < calls; i++)
		{
			found += _viewModel.Rows.IndexOf(rows[i % rows.Length]) >= 0 ? 1 : 0;
		}

		var nanoseconds = stopwatch.Elapsed.TotalMilliseconds * 1_000_000 / calls;
		AddStep(new Step(
			$"IndexOf, {calls:N0} calls over {this.RowCount:N0} rows",
			stopwatch.Elapsed.TotalMilliseconds,
			"< 1 µs per call",
			nanoseconds < 1_000 && found == calls,
			new LookupCounters { IndexOf = calls },
			ManagedMegabytes(collect: false),
			$"{nanoseconds:N0} ns per call"));

		GC.KeepAlive(rows);
	}

	/// <summary>
	/// The manual test for #806: with comments on lines 10 and 20 and line 5 selected, Ctrl+Shift+Plus three times
	/// goes to line 10, then 20, then wraps to 10 again, and only that row is selected each time.
	/// </summary>
	private async Task RunNavigationAsync()
	{
		_window.ScrollToPosition(0);
		_viewModel.Click(_viewModel.Rows.FindPositionOfLine(5), ModifierKeys.None);
		await IdleAsync();

		var visited = new List<string>();
		var passed = true;
		var stopwatch = Stopwatch.StartNew();
		foreach (var expectedLine in new[] { 10, 20, 10 })
		{
			_viewModel.GoToNextComment();
			await IdleAsync();

			var caretLine = SyntheticLog.GetLineNumber(_viewModel.Rows.Results[_viewModel.Caret]);
			var isFocused = _window.GetContainer(_viewModel.Caret)?.IsKeyboardFocusWithin == true;
			visited.Add($"line {caretLine}, {_viewModel.SelectedCount} selected" + (isFocused ? string.Empty : ", **not focused**"));
			passed &= caretLine == expectedLine && _viewModel.SelectedCount == 1 && isFocused;
		}

		AddStep(new Step(
			"#806: Ctrl+Shift+Plus three times, from line 5",
			stopwatch.Elapsed.TotalMilliseconds,
			"lines 10, 20, 10; one row selected and focused each time",
			passed,
			new LookupCounters(),
			ManagedMegabytes(collect: false),
			string.Join("; ", visited)));

		var pinPosition = _viewModel.Rows.FindPositionOfLine(SyntheticLog.GetLineNumber(MainViewModel.SeededPinRecord));
		_viewModel.Click(pinPosition, ModifierKeys.None);
		await IdleAsync();
		var farLine = SyntheticLog.GetLineNumber(System.Math.Max(0, this.RowCount - 1_000));
		await MeasureAsync(
			$"Ctrl+Plus from the pin on line {SyntheticLog.GetLineNumber(MainViewModel.SeededPinRecord)} to a pin near the end of the log",
			() => _viewModel.GoToNextPin(),
			() => $"line {SyntheticLog.GetLineNumber(_viewModel.Rows.Results[_viewModel.Caret]):N0}",
			ResponsiveTarget(),
			check: () => SyntheticLog.GetLineNumber(_viewModel.Rows.Results[_viewModel.Caret]) == farLine);
	}

	/// <summary>
	/// Types into a real comment box, whose binding sends the text 250 ms after the last change, and checks that
	/// nothing overwrites text still being typed.
	/// </summary>
	private async Task RunCommentEditingAsync()
	{
		_window.ScrollToPosition(0);
		await IdleAsync();

		const int position = 3;
		TextBox? box = _window.FindCommentBox(position);
		RowViewModel row = _viewModel.Rows.GetRow(position);
		if (box is null)
		{
			_notes.Add("Comment editing: skipped, because the comment box of row 3 was not on screen.");
			return;
		}

		box.Focus();
		var stopwatch = Stopwatch.StartNew();
		var typed = string.Empty;

		// Fast typing: one change every 50 ms, so the binding sends nothing until the pause.
		foreach (var character in "Fast typing ")
		{
			typed += character;
			box.Text = typed;
			await Task.Delay(50);
		}

		await Task.Delay(400);

		// Slow typing: one change every 300 ms, so each change reaches the store, and its echo comes back between key presses.
		foreach (var character in "then slow")
		{
			typed += character;
			box.Text = typed;
			await Task.Delay(300);
		}

		await Task.Delay(400);
		await IdleAsync();

		var stored = _viewModel.Annotations.GetComment(row.LineNumber);
		var pinnedWhileTyping = row.IsPinned;
		var typingKept = box.Text == typed && stored == typed && pinnedWhileTyping;

		_viewModel.Annotations.SetComment(row.LineNumber, "Changed elsewhere", source: null);
		await IdleAsync();
		var changeShown = box.Text == "Changed elsewhere";

		_viewModel.Annotations.SetComment(row.LineNumber, string.Empty, source: null);
		_window.RowList.Focus();
		await IdleAsync();

		AddStep(new Step(
			"Type a comment, fast then slow",
			stopwatch.Elapsed.TotalMilliseconds,
			"no typed text lost; a change from elsewhere is shown",
			typingKept && changeShown,
			new LookupCounters(),
			ManagedMegabytes(collect: false),
			$"typed \"{typed}\", stored \"{stored}\", pinned {pinnedWhileTyping}; change from elsewhere shown: {changeShown}"));
	}

	private async Task RunBulkChangeAsync()
	{
		var step = System.Math.Max(1, this.RowCount / 100_000);
		for (var record = 0; record < this.RowCount; record += step)
		{
			_viewModel.Annotations.SetPinned(SyntheticLog.GetLineNumber(record), true, source: null);
		}

		_window.ScrollToPosition(0);
		await IdleAsync();
		await MeasureAsync(
			$"Unpin All, {_viewModel.Annotations.PinCount:N0} pins",
			() => _viewModel.Annotations.UnpinAll(),
			() => $"{_viewModel.Rows.GetLiveRows().Count(r => r.IsPinned)} rows on screen still pinned",
			ResponsiveTarget(),
			check: () => !_viewModel.Rows.GetLiveRows().Any(r => r.IsPinned));
	}

	private async Task RunCopyAsync()
	{
		_viewModel.Click(0, ModifierKeys.None);
		_viewModel.MoveCaret(MainViewModel.CopyLimit / 2 - 1, ModifierKeys.Shift, "Shift+Down");
		await MeasureAsync($"Copy {MainViewModel.CopyLimit / 2:N0} rows", () => _viewModel.Copy(isInteractive: false), () => _viewModel.LastAction, string.Empty);

		_viewModel.SelectAll();
		await MeasureAsync(
			"Copy every row",
			() => _viewModel.Copy(isInteractive: false),
			() => _viewModel.LastAction,
			$"refused above {MainViewModel.CopyLimit:N0}",
			check: () => this.RowCount <= MainViewModel.CopyLimit || _viewModel.LastAction.StartsWith("Copy refused", StringComparison.Ordinal));

		_viewModel.Click(0, ModifierKeys.None);
	}

	private async Task CountLiveRowsAsync()
	{
		await IdleAsync();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		var live = _viewModel.Rows.CountLiveRows();
		LookupCounters total = _viewModel.Rows.Counters;

		AddStep(new Step(
			"Row view-models alive after a full garbage collection",
			0,
			$"< {MaximumLiveRows:N0}",
			live < MaximumLiveRows,
			total.Snapshot(),
			ManagedMegabytes(collect: false),
			$"{live:N0} alive; {total.RowsCreated:N0} created during the run"));

		_notes.Add($"WPF called GetEnumerator {total.Enumerations:N0} time(s) and walked {total.EnumeratedRows:N0} rows with it, and called CopyTo {total.CopyTo:N0} time(s), during the run. Walking the whole list would create every row.");
		if (!_viewModel.UsesWpfSelection)
		{
			_notes.Add($"WPF selected a row on its own {_window.WpfSelectionsUndone:N0} time(s), and the range selection undid it.");
		}

		using var process = Process.GetCurrentProcess();
		_notes.Add($"At the end: managed heap {ManagedMegabytes(collect: false):N0} MB, working set {process.WorkingSet64 / 1_048_576.0:N0} MB. " +
			$"The filter results alone take 4 bytes per row: {this.RowCount * 4.0 / 1_048_576:N0} MB with no filter.");
	}

	private async Task MeasureAsync(
		string name,
		Action action,
		Func<string> describe,
		string target,
		Func<bool>? check = null,
		bool measuresMemory = false,
		double maximumMegabytes = double.MaxValue)
	{
		await IdleAsync();
		var before = ManagedMegabytes(collect: measuresMemory);
		LookupCounters counters = _viewModel.Rows.Counters.Snapshot();
		TimeSpan gcPause = GC.GetTotalPauseDuration();
		var stopwatch = Stopwatch.StartNew();

		action();
		await IdleAsync();

		var milliseconds = stopwatch.Elapsed.TotalMilliseconds;
		var gcMilliseconds = (GC.GetTotalPauseDuration() - gcPause).TotalMilliseconds;
		LookupCounters lookups = _viewModel.Rows.Counters.Snapshot().Subtract(counters);
		var after = ManagedMegabytes(collect: measuresMemory);
		var result = describe() + (measuresMemory ? $"; {after - before:N1} MB more" : string.Empty);

		bool? passed = null;
		if (target.Length > 0)
		{
			var limit = target.StartsWith("< ", StringComparison.Ordinal)
				? double.Parse(target.Substring(2).Split(' ')[0], NumberStyles.Number, CultureInfo.InvariantCulture)
				: double.MaxValue;
			passed = milliseconds < limit && (after - before) < maximumMegabytes && (check?.Invoke() ?? true);
		}

		if (maximumMegabytes < double.MaxValue)
		{
			target += $"; < {maximumMegabytes:N0} MB more";
		}

		AddStep(new Step(name, milliseconds, target, passed, lookups, after, result, gcMilliseconds));
	}

	private async Task MeasureRepeatedAsync(string name, int times, Func<Task> action)
	{
		await IdleAsync();
		LookupCounters counters = _viewModel.Rows.Counters.Snapshot();
		TimeSpan gcPause = GC.GetTotalPauseDuration();
		var longest = 0.0;
		var total = Stopwatch.StartNew();
		for (var i = 0; i < times; i++)
		{
			var stopwatch = Stopwatch.StartNew();
			await action();
			longest = System.Math.Max(longest, stopwatch.Elapsed.TotalMilliseconds);
		}

		LookupCounters lookups = _viewModel.Rows.Counters.Snapshot().Subtract(counters);
		AddStep(new Step(
			name,
			longest,
			ResponsiveTarget() + " (the longest)",
			longest < ResponsiveMilliseconds,
			lookups,
			ManagedMegabytes(collect: false),
			$"average {total.Elapsed.TotalMilliseconds / times:N1} ms, longest {longest:N1} ms",
			(GC.GetTotalPauseDuration() - gcPause).TotalMilliseconds));
	}

	private string SelectionResult()
	{
		return _viewModel.UsesWpfSelection
			? $"{_viewModel.SelectedCount:N0} selected"
			: $"{_viewModel.SelectedCount:N0} selected in {_viewModel.Selection.RangeCount:N0} range(s)";
	}

	private int TopLine()
	{
		var top = (int)(_window.ScrollViewer?.VerticalOffset ?? 0);
		top = System.Math.Clamp(top, 0, this.RowCount - 1);
		return SyntheticLog.GetLineNumber(_viewModel.Rows.Results[top]);
	}

	private Task IdleAsync()
	{
		return _window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle).Task;
	}

	private static string InstantTarget() => $"< {InstantMilliseconds:N0} ms";

	private static string ResponsiveTarget() => $"< {ResponsiveMilliseconds:N0} ms";

	private static double ManagedMegabytes(bool collect)
	{
		return GC.GetTotalMemory(collect) / 1_048_576.0;
	}

	private string BuildReport(bool isFinished)
	{
		var processor = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", null) as string;
		var memory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1_073_741_824.0;

		var report = new StringBuilder();
		report.AppendLine("# Virtual list proof of concept (#1014)");
		report.AppendLine();
		report.AppendLine("| | |");
		report.AppendLine("|---|---|");
		report.AppendLine($"| Rows | {this.RowCount:N0} |");
		report.AppendLine($"| Selection | {(_viewModel.UsesWpfSelection ? "WPF's own (`--wpf-selection`), as Weevil 2.12" : "Ranges")} |");
		report.AppendLine($"| Computer | {processor?.Trim() ?? "Unknown processor"}, {Environment.ProcessorCount} logical processors, {memory:N1} GB of RAM |");
		report.AppendLine($"| Windows | {RuntimeInformation.OSDescription} |");
		report.AppendLine($"| .NET | {RuntimeInformation.FrameworkDescription} |");
		report.AppendLine($"| Run | {_started.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} |");
		report.AppendLine();

		if (!isFinished)
		{
			report.AppendLine("**Unfinished:** the run is still going, or it stopped during the step after the last one below.");
			report.AppendLine();
		}

		var failed = _steps.Count(s => s.Passed == false);
		var checkedSteps = _steps.Count(s => s.Passed.HasValue);
		report.AppendLine(failed == 0
			? $"All {checkedSteps} steps with a target met it."
			: $"**{failed} of {checkedSteps} steps with a target missed it.**");
		report.AppendLine();

		report.AppendLine("| Step | Time (ms) | Target | Met | GC pause (ms) | WPF asked for rows | IndexOf | Rows created | Managed heap (MB) | Result |");
		report.AppendLine("|---|---:|---|---|---:|---:|---:|---:|---:|---|");
		foreach (Step step in _steps)
		{
			var met = step.Passed switch { true => "yes", false => "**no**", null => string.Empty };
			report.AppendLine(
				$"| {step.Name} | {step.Milliseconds:N1} | {step.Target} | {met} | {(double.IsNaN(step.GcPauseMilliseconds) ? string.Empty : step.GcPauseMilliseconds.ToString("N1", CultureInfo.InvariantCulture))} | " +
				$"{step.Lookups.Indexer:N0} | {step.Lookups.IndexOf:N0} | " +
				$"{step.Lookups.RowsCreated:N0} | {step.ManagedMegabytes:N0} | {step.Result.Replace("|", "\\|")} |");
		}

		report.AppendLine();
		report.AppendLine("## Notes");
		report.AppendLine();
		foreach (var note in _notes)
		{
			report.AppendLine("- " + note);
		}

		return report.ToString();
	}
}
