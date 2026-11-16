# Audit Scope: What Changed for Weevil 3.0

Stage 0 of the [Weevil 3.0 audit](Progress.md). Maps the code that changed since 2.x to the three epics, so later stages review only that code.

> Mapped on 2026-11-16 at `main` e3dee822. Regenerate with [`scope.py`](scope.py).

## Baselines

| Name | Commit | Use |
|---|---|---|
| Merge base | `baa70a9d` (2026-06-27, the 2.12 release) | Start of the 3.0 changes: `git log baa70a9d..main` |
| `Releases/2.x` | `aef130da` (2026-10-28) | The 2.x that users run. Baseline for behaviour and benchmarks. It has 54 fixes since the merge base |
| `main` | `e3dee822` (2026-11-16) | Weevil 3.0 under audit |

Since the merge base, `main` has 454 commits (merges excluded). `Src/` changed by 254 files, +17,160 / −3,183 lines.

The clone is shallow. Before using git history, run `git fetch --unshallow origin` and `git fetch origin "Releases/2.x:refs/remotes/origin/Releases/2.x"`.

## The Epics

| Epic | Goal | State on 2026-11-16 | Plan |
|---|---|---|---|
| #990 | Immutable core: records hold no mutable state; session state lives in stores; selection as ranges | Done | [990-ImmutableCore.md](../../../Plans/990-ImmutableCore.md) |
| #988 | Keep a log in much less memory: raw-byte content, record table, records on demand, saved index | Done | [988-IndexingService.md](../../../Plans/988-IndexingService.md) |
| #986 | Open a log in RAM or on disk; Open As dialog | In progress. Disk mode (#421) is done except the 20 GB PoC (#1024). The Open As dialog (#1157) has 1 of 12 pieces done (#1210, the file browser) | [986-DiskMode.md](../../../Plans/986-DiskMode.md), [1157-OpenAsDialog.md](../../../Plans/1157-OpenAsDialog.md) |
| #991 | Multiple views | Draft. **Out of scope**; checked only for whether the design leaves room for it | [991-MultipleViews.md](../../../Plans/991-MultipleViews.md) |

Commits by epic (a commit citing issues of several epics counts once, under all of them): #988 143, #986 78, #990 63, several 7, out of scope 163.

### Out-of-Scope Changes That Matter

These commits cite no epic issue, but change performance or behaviour that the audit measures. Only their effect on the audit is in scope.

| Issue | Effect |
|---|---|
| #969 | Opening and filtering stop forcing garbage collections; memory returns to the OS after opening |
| #976 | Each filter thread uses its own regular expression |
| #1240 | Opening collects once its work returns |
| #1086, #1087, #1091, #1103, #1105 | Memory budget tests, performance tests on the 10-million-record droid log, timing telemetry |

## Review Units

Stage 1 reviews one unit at a time. Each unit gets its own `Findings/<Unit>.md`.

Lines are the file's size today. "Epic churn" is lines added and deleted by epic commits.

### U1. Line Index and Parsing (#988)

Reads the file once, indexes lines, saves and reuses the index.

| File (`Src/BlueDotBrigade.Weevil.`…) | Lines | Epic churn |
|---|---|---|
| `Core/IO/LineIndex.cs` | 721 | 1510 |
| `Core/IO/IndexSection.cs` | 359 | 427 |
| `Core/IO/LineIndexFile.cs` | 321 | 373 |
| `Core/IO/LogLineReader.cs` | 329 | 375 |
| `Core/IO/SavedParse.cs` | 216 | 298 |
| `Core/IO/SavedTableReader.cs` | 279 | 279 |
| `Core/IO/GrownLogParser.cs` | 115 | 149 |
| `Core/Data/MultiLineRecordParser.cs` | 367 | 445 |

### U2. Record Representation (#988)

How a record is held: record table, raw-byte content chunks, records created on demand.

| File | Lines | Epic churn |
|---|---|---|
| `Core/Data/RecordTable.cs` | 582 | 684 |
| `Core/Data/InMemoryRecordRepository.cs` | 412 | 298 |
| `Core/Data/SerializedRecordRepository.cs` | 131 | 211 |
| `Core/Data/TableRecordList.cs` | 139 | 155 |
| `Core/Data/PositionRecordList.cs` | 34 | 34 |
| `Common/Data/Record.cs` | 191 | 198 |
| `Common/Data/RecordFacts.cs` | 72 | 88 |
| `Common/Data/RecordList.cs` | 270 | 296 |
| `Common/Data/ContentChunk.cs` | 215 | 289 |
| `Common/Data/ContentBuffer.cs` | 132 | 168 |
| `Common/Data/RecordCollectionExtensions.cs` | 451 | 182 |
| `TestTools/Data/R.cs`, `ColumnsOnlyRecordList.cs` | 168 | 76 |

### U3. Disk Mode and the Log File Source (#986)

Content read back from the file; the RAM-or-disk choice; a log that changed on disk.

| File | Lines | Epic churn |
|---|---|---|
| `Common/IO/LogFileSource.cs` | 257 | 341 |
| `Common/IO/LogFileSourceBuilder.cs` | 101 | 115 |
| `Common/IO/LogFileChangedException.cs` | 64 | 64 |
| `Core/IO/DiskWriter.cs` | 191 | 165 |
| `Core/IO/MemoryEstimator.cs` | 87 | 103 |
| `Core/Data/RecordRanges.cs` | 85 | 123 |
| `Core/Navigation/ContentNavigator.cs` | 78 | 112 |
| `Gui/IO/ExtractedLog.cs`, `OpenAsAdvice.cs`, `OpenAsDialog.xaml(.cs)` | 409 | 467 |
| `Cli/Filter/FilterCommands.cs`, `PowerShell/GetRecordsCmdlet.cs` | 215 | 147 |

### U4. Engine Composition and Session State (#990)

The engine, its builder, and the stores that replaced mutable records.

| File | Lines | Epic churn |
|---|---|---|
| `Core/CoreEngineBuilder.cs` | 611 | 835 |
| `Core/CoreEngine.cs` | 491 | 336 |
| `Core/Engine.cs`, `EngineBuilder.cs`, `EngineExtensions.cs` | 418 | 227 |
| `Common/IEngine.cs`, `IEngineBuilder.cs` | 141 | 105 |
| `Core/Session.cs` | 96 | 112 |
| `Common/Data/AnnotationStore.cs`, `IAnnotations.cs` | 376 | 492 |
| `Common/Data/FlagSet.cs`, `ElapsedTimes.cs` | 318 | 416 |
| `Common/Data/RecordState.cs`, `IRecordState.cs` | 119 | 119 |
| `Core/BookmarkManager.cs`, `RegionManager.cs`, `Filter/FilterHistory.cs` | 504 | 385 |
| `Core/Configuration/Sidecar/SidecarManager.cs`, `v2/SidecarLoader.cs` | 516 | 302 |

### U5. Filtering, Selection, Navigation and Analyzers (#990, #988)

The consumers of the new record model.

| File | Lines | Epic churn |
|---|---|---|
| `Core/SelectionManager.cs` | 655 | 786 |
| `Core/Collections/PositionRanges.cs` | 350 | 479 |
| `Core/Collections/SelectedRecords.cs`, `SelectedRecordList.cs` | 204 | 238 |
| `Common/ISelect.cs` | 99 | 65 |
| `Core/Filter/FilterManager.cs` | 421 | 344 |
| `Core/Filter/FilterStrategy.cs` | 227 | 34 |
| `Core/Navigation/*` (Navigation, Comment, ElapsedTime) | 467 | 225 |
| `Common/Navigation/RecordSearch.cs` | 197 | 125 |
| `Core/Analysis/**` (11 analyzers and `AnalysisManager`) | ~1,900 | ~600 |
| `Common/Analysis/**` (6 analyzers and metrics) | ~760 | ~470 |

### U6. GUI Over the New Core (#990, #986)

Row view-models, the selection the grid owns, the 100-million-row list.

| File | Lines | Epic churn |
|---|---|---|
| `Gui/Filter/FilterViewModel.cs` | **2,942** | 1,411 |
| `Gui/Filter/RowList.cs` | 358 | 444 |
| `Gui/Filter/RowSelection.cs` | 207 | 221 |
| `Gui/Filter/RowViewModel.cs` | 193 | 211 |
| `Gui/Filter/FilterView.xaml.cs` | 393 | 264 |
| `Gui/Controls/*` (list view and automation peers) | 432 | 514 |
| `Gui/ClipboardHelper.cs`, `Gui/IO/UiThreadUserDialog.cs` | 268 | 214 |

### U7. Open As Dialog and File Browser (#1157, in progress)

Only #1210, the generic file browser, is merged. Reviewed lightly: architecture and intent only, since most of #1157 is still to come.

| File | Lines |
|---|---|
| `Windows/Dialogs/Storage/ContainerReader.cs` | 520 |
| `Windows/Dialogs/FileBrowserModel.cs` | 369 |
| `Windows/Dialogs/FileBrowser.xaml(.cs)` | 409 |
| `Windows/Dialogs/Storage/Location.cs` | 258 |
| `Windows/Dialogs/OpenAsDialog.xaml(.cs)` | 203 |
| Other `Windows/Dialogs/**` | 254 |

## First Observations

These guide the review; they are not findings yet.

- **#988 carries the most change.** `Core/IO` alone is 3,679 lines of churn, all epic work. `LineIndex.cs` changed by twice its own size.
- **`FilterViewModel.cs` is 2,942 lines,** and half of its epic churn comes from #986 and #990. Check in U6 whether the epics added screen logic that belongs in Core.
- **There are two Open As dialogs:** `Gui/IO/OpenAsDialog` (#1028) and `Windows/Dialogs/OpenAsDialog` (#1157). The second is meant to replace the first. Check in U3 and U7 that the overlap is temporary and tracked.
- **Analyzer changes are mostly mechanical.** Most analyzers changed by 35–65 lines, spread across all three epics. Sample them; do not review each.
