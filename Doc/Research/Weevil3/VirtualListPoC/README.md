# Virtual List Proof of Concept

Question for #1014 (step 4 of the [#990 plan](../../../Plans/990-ImmutableCore.md)): can a WPF `ListView` show 100 million rows with Weevil's row template, when a row view-model exists only while WPF needs it and the selection is stored as ranges?

Weevil 2.12 already virtualizes the grid, but only its controls: every record is an object in memory, and Ctrl+A puts every record in WPF's `SelectedItems` and in Core's selection. #988 removes the record objects, so the grid must work without them. The answer decides the row design for #1015 and #988.

## What It Does

A window like Weevil's filter results, over a synthetic log that exists only as arithmetic: each record is generated from its record number, so 100 million rows need no file.

- **The grid** is Weevil's `ListView` and row template: pin box, comment box, Elapsed, content with the region, bookmark and truncation tags. Every 50th record is a 6-line call stack and every 20th wraps, so rows have different heights.
- **The list** implements the non-generic `IList`, so WPF reads through it instead of copying it. It creates a row view-model only when WPF asks for a row, and finds a row's index by binary search (`IndexOf`). Rows are equal by line number.
- **The selection** is a list of ranges of positions in the filter results. WPF never selects a row: clicks and the navigation keys go to the window, which updates the ranges and tells each row on screen whether it is selected. Ctrl+A is one range.
- **A filter** keeps the selected records it still shows: two binary searches per range, or a merge of the two result lists when the old results had gaps.
- **Comments and pins** live in an annotation store kept by line number, like #1013's. A row writes to the store and refreshes later, on the UI thread, when the store reports a change to its line. It ignores the echo of its own comment, so the comment box never loses text still being typed.
- **Copy** stops at 100,000 records and points to Export.
- **Counters** in the status bar show how often WPF asked the list for a row, `IndexOf`, `Contains`, `Count`, a full enumeration or `CopyTo`, how many row view-models were created, the memory, and the longest time the window could not respond.

`--wpf-selection` switches to WPF's own selection (`SelectionMode="Extended"`), as Weevil 2.12 does: WPF keeps the selected rows, and the window mirrors them in a sorted dictionary as Weevil's `SelectionManager` does. This shows where that approach breaks.

## Run It

The project is self-contained: it does not reference any Weevil project and is not part of `Weevil-v2.sln`. It runs only on Windows, but also builds on Linux.

What the Windows PC needs:

- **To run a built copy:** the .NET 10 Desktop Runtime, x64, which Weevil 2.12 also needs. If Weevil runs on the PC, it is already there. Otherwise install it from the [.NET 10 download page](https://dotnet.microsoft.com/download/dotnet/10.0) (".NET Desktop Runtime", Windows x64), or with `winget install Microsoft.DotNet.DesktopRuntime.10`.
- **To run from the source code:** the .NET 10 SDK, which includes that runtime.

A built copy is made with:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

Copy `publish`, `RunAll.cmd` and this README to the PC, keeping `publish` as a folder next to `RunAll.cmd` (or put the files of `publish` next to it), then double-click `RunAll.cmd`. It runs both automatic runs, one after the other, and leaves `results-ranges.md` and `results-wpf-selection.md` next to it. Each report is saved after every step, so if a run crashes or hangs, the report still holds the steps before it. From the source code, `RunAll.cmd` uses `dotnet run` instead.

Or run it yourself:

```powershell
cd Doc\Research\Weevil3\VirtualListPoC

# The automatic run: a few minutes, then the window closes and results.md holds the report.
dotnet run -c Release -- --auto

# The same with WPF's own selection, on 1 million rows.
dotnet run -c Release -- --auto --wpf-selection --report results-wpf-selection.md

# Try it by hand.
dotnet run -c Release
```

### Options

| Option | Meaning |
|---|---|
| `--rows <1-300000000>` | Rows in the list. Default 100,000,000, or 1,000,000 with `--wpf-selection`. |
| `--wpf-selection` | Use WPF's own selection, as Weevil 2.12 does. |
| `--auto` | Run every scenario, save the report, then close. |
| `--report <path>` | Where `--auto` saves the report. Default `results.md`. |
| `--stay` | Keep the window open after `--auto`. |

### Experiments

| Question | Command |
|---|---|
| How far does WPF's own selection go? Ctrl+A may take minutes, or run out of memory. | `dotnet run -c Release -- --auto --wpf-selection --rows 10000000` |
| Does the range selection behave the same on a smaller log? | `dotnet run -c Release -- --auto --rows 1000000` |

## Targets

A step in the report meets its target, or does not. The row design works at 100 million rows if:

| Action | Target |
|---|---|
| Ctrl+A, Shift+End, a click | Under 100 ms |
| Selecting every row | Under 10 MB more memory |
| Keeping the selection after a filter | Under 1 s, with the window never blocked for 250 ms or more |
| Dragging the scroll bar, Page Down, scrolling a row into view | The window never blocked for 250 ms or more |
| `IndexOf` | Under 1 µs per call |
| Row view-models alive | Under 1,000: about the rows on screen, not the rows in the list |
| Comment editing | No typed text lost; a change made elsewhere is shown |
| #806 | Ctrl+Shift+Plus three times from line 5 goes to lines 10, 20, then 10, with only that row selected and focused |
| Copy | Refused above 100,000 records |

WPF should also never enumerate the list or call `CopyTo`: either would create all 100 million rows. The report's notes give both counts.

With `--wpf-selection`, Ctrl+A is expected to miss its targets; the report shows by how much.

## By Hand

Some things only a person can judge. Run `dotnet run -c Release` and check:

1. **Scrolling feels smooth.** Drag the scroll bar from top to bottom and back, use the mouse wheel, and hold Page Down. Rows of different heights should not make the scroll bar jump.
2. **Click selection.** Click, Shift+click, Ctrl+click and Ctrl+Shift+click behave as in Weevil. The status bar shows the selected count and the number of ranges.
3. **Keyboard selection.** Up, Down, Page Up, Page Down, Home and End move the selection; with Shift they extend it from the last clicked row; with Ctrl they move the focus only, and Ctrl+Space then selects the focused row. Ctrl+A selects all 100 million rows at once.
4. **#806.** Click line 5, then press Ctrl+Shift+Plus three times: lines 10, 20, then 10 again, each time with only that row selected. Do it quickly too. Ctrl+Plus jumps to a pin near the end of the log.
5. **Comment editing.** Type in a comment box, quickly and slowly: no characters disappear, and the record is pinned. Click the pin box: it toggles without selecting the row. Select all, then type in a comment: the selection stays.
6. **Filtering with a large selection.** Ctrl+A, then filter on every 2nd record, then every 10th, then all records: the right rows stay selected, and the window says how long keeping the selection took.
7. **Copy.** Select a few rows and press Ctrl+C, then paste into Notepad. Ctrl+A then Ctrl+C shows a message pointing to Export.
8. **WPF's own selection.** Repeat steps 3 and 4 with `--wpf-selection` (1 million rows), and note what breaks.

## Limits

- **Selection after a filter:** a selected record that the new filter hides leaves the selection; nothing brings it back when a later filter shows it again. Today, Core keeps hidden records in its selection, but the grid's `SelectionChanged` probably removes them when the results change. #1015 must confirm which behaviour Weevil keeps.
- **The worst case for ranges:** every 10th record selected, then the filter removed, makes one range per selected record: 10 million ranges, about 80 MB and 1.5 s on a 4-vCPU VM (see [Results](#results)).
- **Elapsed time** is calculated when a row is created, from the previous result's timestamp. The #990 plan calculates it for all results after each filter; on screen, both give the same value.
- **No Core:** filtering is arithmetic on record numbers, so its time is not Weevil's. Only the time to keep the selection is comparable.
- **Not copied from Weevil:** the context menu, the filter boxes, `ListViewBehavior.AutoScrollToCurrentItem`, and `BookmarkSymbol` (one symbol for every bookmark).
- **Row view-models** raise `PropertyChanged` by hand instead of with Metalama's `[Observable]`, so the project needs no Metalama. Metalama would suppress an event when the value does not change, which the rows never rely on.
- The automatic run calls the same methods as the keys and the mouse, but not through real key presses. WPF reads the real state of Shift and Ctrl, so the #806 check with `--wpf-selection` needs a person.

## Copied Weevil Files

`WeevilCopies/` holds unmodified copies taken from `main` at `1b128ae` (2026-10-21, after #1013 merged). They behave as in release 2.12.1 (`Releases/2.x` at `e8f2e91`): of every Weevil file copied or adapted here, only two differ from that release, `FilterListViewBehavior.cs` in a comment and `SimpleCallStackFormatter` in reading `IsMultiLine` from the record instead of `Metadata` (#1012).

| File | Copied from |
|---|---|
| `ApplicationStyles.xaml` | `Src/BlueDotBrigade.Weevil.Gui/Themes/` |
| `FilterListViewBehavior.cs` | `Src/BlueDotBrigade.Weevil.Gui/Filter/`; used only with `--wpf-selection` |
| `TextBoxBehavior.cs` | `Src/BlueDotBrigade.Weevil.Windows/Controls/` |
| `TimeSpanExtensions.cs` | `Src/BlueDotBrigade.Weevil.Common/` |

Changed copies:

| File | Copied from | Change |
|---|---|---|
| `MainWindow.xaml` | The `ListView` in `FilterView.xaml` | Binds `IsPinned`, `Comment` and `ElapsedTime` on the row instead of `Metadata`; `CaretBrush` instead of `MdixComboBoxBehavior.CaretBrush`, which sets it; the tags find the view-model on the `Window`; the selected-row trigger reads the row's `IsSelected` unless WPF's own selection is on. |
| `RowConverters.cs` | The 8 converters the row template uses, and the two content formatters | They take a `RowViewModel` instead of an `IRecord`, as #1015 must do. |
| `App.xaml` | `Src/BlueDotBrigade.Weevil.Gui/App.xaml` | Without the styles for controls this window does not have. |

## Self-Test

`SelfTest/` checks the parts that need no window, on any operating system: the synthetic log, the ranges, keeping a selection after a filter, `IndexOf`, and the comment rules. It ends with timings at 100 million rows.

```powershell
cd Doc\Research\Weevil3\VirtualListPoC\SelfTest
dotnet run -c Release
```

## Results

### Status (2026-10-22)

Built and self-tested on Linux. **Not yet run on Windows**: the decision waits for `RunAll.cmd` and the by-hand checks on a Windows PC.

### Self-Test: 2026-10-22

On a 4-vCPU Linux VM with .NET 10, at 100,000,000 rows:

| Action | Time | Ranges after |
|---|---|---|
| Select all | < 0.01 ms | 1 |
| Keep the selection: all records selected, filter to every 2nd | < 1 ms | 1 |
| Keep the selection: every 2nd selected, filter to every 10th | 100 ms | 1 |
| Keep the selection: every 10th selected, filter removed (the worst case) | 1,527 ms | 10,000,000 |
| `IndexOf` | 127 ns per call | |
