# Mutable State in Core

Question for #990: which data in `Core` changes after a log is opened, and what changes it?

> Notes from 2026-10-18, taken from the code at commit 42db9aa0. Line numbers will drift; search for the member names.

## Summary

- **Records are immutable.** Line number, timestamp, severity and content never change (`Record.cs`).
- **`Metadata` is the mutable part of a record.** Every record has one, with six values. Parsers, the sidecar loader, the filter, analyzers and the GUI all write to it.
- **Managers hold the rest:** bookmarks, regions, selection, filter results and history, the active record, the table of contents and the file remarks.
- **Clear and Reload replace the whole `CoreEngine`** (`Engine.cs:142-155`). Clear passes the same record objects to the new engine, so their `Metadata` (comments, pins, flags) carries over. The GUI subscribes to `Filter.HistoryChanged` again after Open, Reload and Clear (`FilterViewModel.cs:636`, `826`, `1060`).

## Per-Record State: `Metadata`

| Value | Set when the log opens | Changed by the user (GUI) | Changed by Core | Saved in sidecar | Read by |
|---|---|---|---|---|---|
| `IsMultiLine` | Parser: `MultiLineRecordParser.cs:193` | No | No | No | `@MultiLine` filter |
| `WasGeneratedByUi` | TSV parser, when the thread ID is 1: `TsvRecordParser.cs:53` | No | No | No | `@UiThread` filter, `TimeGapUiAnalyzer` |
| `Comment` | Sidecar: `SidecarLoader.cs:74` (v1: `LogMetadataLoader.cs:60`) | Grid text box (`FilterView.xaml:330`, two-way); Paste (`ClipboardHelper.cs:123`); Remove comments (`AnalysisManager.cs:45`) | Analyzers append text with `UpdateUserComment` | Yes | `@Comment` filter, comment navigation, comment report |
| `IsPinned` | Sidecar: `SidecarLoader.cs:75` | Grid check box (`FilterView.xaml:321`); Toggle pin (`SelectionManager.cs:330`); Unpin all (`AnalysisManager.cs:29`) | Adding a comment pins the record; clearing it unpins (`Metadata.cs:61`, `68`) | Yes | Filter (pinned records stay visible), `@Pinned` filter, pin navigation |
| `IsFlagged` | No | Remove all flags (`AnalysisManager.cs:37`); opening an insight (`FilterViewModel.cs:1469-1488`) | 13 analyzers in `Core/Analysis` and `Common/Analysis` clear and set flags on the records they analyze | No | `@Flagged` filter, flag navigation |
| `ElapsedTime` | Calculated for every record: `CoreEngine.cs:141` | No | Every filter: reset on hidden records (`FilterManager.cs:151`), recalculated between visible records (`FilterManager.cs:324`) | No | `@Elapsed` filter, elapsed-time navigation, grid column |

Analyzers run on the selected records when the selection spans a period of time, otherwise on the filter results (`AnalysisManager.cs:152-154`).

## Session State: Managers

| State | Where | Set when the log opens | Changed by the user (GUI) | Changed by Core | Saved in sidecar |
|---|---|---|---|---|---|
| Bookmarks | `BookmarkManager._bookmarks` (`List`) | Sidecar | Create, Rename, Clear | No | Yes |
| Regions | `RegionManager._regions` (`List`), plus a pending start line | Sidecar | MarkStart, MarkEnd, CreateFromSelection, Clear | No | Yes |
| File remarks | `CoreEngine._sourceFileRemarks` | Sidecar | Remarks text box (`FilterViewModel.cs:341`) | No | Yes |
| Table of contents | `NavigationManager._tableOfContents` | Sidecar | Rebuild table of contents: the plugin re-reads the log (`NavigationManager.cs:65`) | No | Yes |
| Filter history | `FilterManager._includeHistory`, `_excludeHistory` | Sidecar, else the plugin's defaults | Each filter adds its criteria | No | Yes |
| Context | `CoreEngine._context` (`Dictionary`) | Sidecar, else the plugin | No | No (no writers found) | Yes |
| Line index | `LineIndex` | Loaded from the `.idx` file, built or extended | No | No | In the `.idx` file |
| Engine instance | `Engine._coreEngine` | Built | Clear, Reload | No | No |

## View State: Filter, Selection, Navigation

None of this is saved. In a design with several views of one log, each view would own its own copy.

| State | Where | Changed by |
|---|---|---|
| Current filter, strategy and results | `FilterManager._currentFilter`, `_latestFilterStrategy`, `_latestFilterResults` | Apply, ReApply; Reload re-applies the filter |
| Filter abort flag and execution time | `FilterManager._abortFilterOperation`, `_filterExecutionTime` | Abort; each filter |
| Metric counts | The extension's metric collectors (severity counts, timestamp anomalies) | Reset and counted on every filter (`FilterManager.cs:113`, `146`). Clear reuses the extension. |
| Selection | `SelectionManager._selectedRecords` (`SortedDictionary` of record objects), `_selectionPeriod` | Select, Unselect, ClearAll |
| Active record | `ActiveRecord._dataSource`, `_activeIndex` | New filter results (`CoreEngine.cs:296`); selection (`SelectionManager.cs:138`) |

## Threads

- The GUI opens, reloads and filters on background tasks (`FilterViewModel.cs:578`, `809`, `1798`). The grid's bindings write `Comment` and `IsPinned` on the UI thread.
- `Metadata` has no locking. The filter reads `Comment` and `IsPinned` on worker threads while the grid may be changing them.
- `Metadata` raises `PropertyChanged` on whichever thread writes it, including the `Parallel.For` loops in `AnalysisManager`.

## Side Effects Found While Listing

These come from reading the code; none has been tested.

- **An aborted filter can erase elapsed times that are still shown.** The filter resets `ElapsedTime` on each record it hides, before it checks for an abort (`FilterManager.cs:151`, `166`). After an abort the previous results stay on screen, but some of their records have already lost their elapsed time.
- **The `@Elapsed` filter depends on the filter before it.** It reads elapsed times calculated between the previous filter's visible records. Records hidden by the previous filter have no elapsed time, so they never match. This is intended: elapsed time is the time between two visible records (confirmed 2026-10-19).
- **Flags have two owners.** Analyzers clear the flags on the records they analyze, and opening an insight clears every flag, so each replaces the other's flags.

## What This Means for #990

Grouped by how the data behaves:

| Kind | Values | Fits in |
|---|---|---|
| Facts from parsing; never change | `IsMultiLine`, `WasGeneratedByUi`, line index, context | The immutable part, shared by every view |
| User annotations; rare, saved in the sidecar | `Comment`, `IsPinned`, bookmarks, regions, remarks, table of contents, filter history | A session store keyed by line number |
| Analysis results; may cover most records, not saved | `IsFlagged` | A bit per record, owned by the view (decided 2026-10-19) |
| Depends on the current filter | `ElapsedTime`, filter results, metric counts, selection, active record | The view, or calculated when needed |

Behaviour that a new store must keep:

- Adding a comment pins the record, and clearing it unpins it.
- Annotations survive Clear.
- The GUI needs an object that stays the same across Open, Clear and Reload, so it subscribes once. See [ImmutableCoreLibrary.md](../../LessonsLearned/ImmutableCoreLibrary.md).
