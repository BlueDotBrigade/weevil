# Engine and Session

How an open log's engine is put together, which of its objects Clear and Reload keep, and where a record's state lives (#990). The plan and its decisions are in [990-ImmutableCore.md](../Plans/990-ImmutableCore.md); where the two differ, this note describes the code.

## The Objects

| Object | Holds | Lives |
|---|---|---|
| `Engine` (`IEngine`) | The current `CoreEngine`, and the events callers subscribe to | One per open log |
| `CoreEngine` | One view of the log: its scope, managers, line index, sidecar manager and context | Until Reload replaces it |
| `Session` | What the user changes in the log | As long as its `Engine` |
| `RecordState` | The view's flags and elapsed times | As long as its `Engine` |
| `RecordScope` | The records the view keeps | As long as its `CoreEngine` |
| Managers: filter, selection, navigation, analysis | The view's filter, selection, active record and analyzers | As long as their `CoreEngine` |

- **`Engine`** (Core) is the public facade: `Engine.UsingPath(path)` … `.Open()` builds one, and opening another log builds another. It forwards calls to its `CoreEngine`, which is internal, and runs Reload itself.
- **Events:** `Engine` forwards the filter's events as `FilterResultsChanged` and `FilterHistoryChanged`, and moves its subscription when Reload replaces the filter, so a caller subscribes once per open log (#1011).
- **`CoreEngine`** is built by `CoreEngineBuilder`, which reads the log and the sidecar. Its constructor creates the managers and applies an empty filter.
- **`Engine.Surrogate`** is an engine over an empty file, which the GUI holds until a log opens. `Engine.IsRealInstance` tells it apart.

## The Managers

Each is internal to Core, created by its `CoreEngine`, and replaced with it.

- **Filter** (`FilterManager`, `IFilter`): applies a filter to the scope, keeps the filter and its results, and replaces the view's elapsed times. It reads the bookmarks, regions and filter histories from the session.
- **Selection** (`SelectionManager`, `ISelect`): the selected records, as ranges of positions in the filter results, and the anchor.
- **Navigation** (`NavigationManager`, `INavigate`): the active record, a position in the filter results, and the navigators. It reads the table of contents from the session.
- **Analysis** (`AnalysisManager`, `IAnalyze`): runs an analyzer over the selection when at least two records are selected, otherwise over the results. An analyzer that reads values with regular expressions derives from `RegularExpressionAnalyzer`, which asks the user for them (#1258). Unpin All, Remove Comments, Remove All Flags and Flag Only are each one call on a store.
- After each filter, `CoreEngine` moves the selection and the active record to the new results before callers hear of them, because it subscribed to the filter first (#1015).

## The Session

`Session` (Core, internal) holds what the user changes in a log:

- **Annotations:** comments and pins, in an `AnnotationStore`: a byte per line and a dictionary of comments. Adding a comment pins its line; removing it unpins the line.
- **Bookmarks** and **regions:** `BookmarkManager` and `RegionManager`.
- **Filter history:** an include and an exclude `FilterHistory`, newest first, at most 32 values each. Only the filter adds to them; callers read them as `IReadOnlyList<string>` (#1109).
- **Remarks** about the whole log, and the **table of contents**.

`AnnotationStore`, `FlagSet` and `RecordState` are internal to Core, so only Core can load them without their rules or events, or clear every flag (#1293). Callers reach them through `IAnnotations` and `IRecordState`, in Common.

It is created when a log opens, sized from the line index, and loaded from the sidecar. Clear and Reload keep it, so Reload keeps unsaved bookmarks, regions, remarks and history, as it keeps comments and pins (#1107).

`Save` writes the session to the sidecar, but only the comments and pins of records in memory: the sidecar keeps what it had for other lines. So Clear saves first (#1004).

Callers reach the session through the engine: `Annotations`, `Bookmarks`, `Regions`, `SourceFileRemarks`, `Navigate.TableOfContents`, and the filter's `IncludeHistory` and `ExcludeHistory`.

## One Lock

- `Session` owns one lock and gives it to each store it creates. It serializes every write to the session: comments, pins, bookmarks, regions, remarks, the table of contents and both histories (decision 8).
- Nothing else takes it. `FlagSet` and the selection each have their own lock, and each filter waits for its turn ([Filtering.md](Filtering.md#cancellation-and-threading)). Clear and Reload take none.
- Reads never wait. Bookmarks, regions and the histories publish a new immutable array on each write; remarks and the table of contents are replaced whole. The annotation store writes each line's byte in place, publishes a larger array when it grows, and keeps comments in a `ConcurrentDictionary`.
- Each store is read on its own: there is no snapshot of the whole session.
- Events are raised after the lock is released, on the thread that wrote:
  - `IAnnotations.Changed`, once per change, with its line number, or `ManyLines` for a bulk change such as Unpin All (decision 4). It is not raised when nothing changed, nor when the sidecar is loaded. A writer may pass itself as `source`, so a grid row ignores its own writes.
  - `FilterHistory.Add` returns its changes, and the filter raises `HistoryChanged` for each.
  - Bookmarks, regions, remarks, the table of contents and the flags raise no events.

## Records Hold No State

- A `Record` (Common) is sealed and never changes. Records are created when read, and are equal when their line numbers are ([RecordTable.md](RecordTable.md#records-on-demand)).
- Comments and pins are kept by line number in the session; flags by line number in the view's `FlagSet`, a bit per line (decision 3). Clear changes positions, never line numbers, so neither moves.
- Flags are not saved: they last as long as the engine.
- Elapsed times belong to the view. Each filter replaces `RecordState.ElapsedTimes` with those of its results, calculated when read (#1150): by position with `ElapsedTimes.Measure`, or by line number with `Get`.
- `IRecordState` (Common) reads and writes one record's comment, pin and flag, and reads its elapsed time (decision 7). `RecordState` implements it by forwarding to the stores, and adds no lock. `IEngine.RecordState` returns it, and each analyzer receives it.
- Bulk changes call the stores directly, so many lines change under one lock: Unpin All, Remove Comments, Toggle Pin and Paste each raise at most one event. `SetFlagged(RecordList, bool)` flags many rows in one call.

## Clear and Reload

| | Clear | Reload |
|---|---|---|
| `Engine`, and its subscribers | Kept | Kept |
| `Session`, with every store | Kept | Kept |
| `RecordState`, with the flags | Kept | Kept |
| Storage mode | Kept | Kept |
| `CoreEngine` and its managers | Kept | Replaced |
| `RecordScope` | Narrowed | Replaced: the whole log |
| Filter | Reset to none | Applied again |
| Elapsed times | Replaced by that filter | Replaced by that filter |
| Selection | The selected records kept | The selected records still shown |

**Clear** narrows the view in place (#1018):

1. `Engine` saves the sidecar, then stops forwarding the filter's events until Clear ends. Where the sidecar can never be written, as in a read-only folder, it logs a warning and clears without saving, so unsaved comments and pins on the records cleared are not saved; any other failure to save stops Clear (#1246).
2. The records kept are chosen by the rules of the `ClearOperation`. The selection becomes the selected records kept, without an anchor, and the active record the last of them, or none.
3. `RecordScope.Narrow` replaces the list in one write, so a reader that reads it once sees the records before or after. The metrics are recounted and `HasBeenCleared` is set.
4. An empty filter is applied, not counted as the user's. `FilterResultsChanged` is not raised: read `Filter.Results` afterwards.
5. When records were removed, a blocking collection frees them before Clear returns ([RecordTable.md](RecordTable.md#records-on-demand)).

**Reload** reads the log again into a new `CoreEngine`:

1. It deletes the saved line index if the log changed on disk ([DiskMode.md](DiskMode.md#a-log-that-changed)).
2. It keeps the session, the record state, the storage mode, the filter, and the selection, anchor and active record by line number.
3. It swaps in an engine without records, with the same session and filter, so the old records are freed while the log is read (#1126). If reading fails, that engine stays, so Save and a retry still use this log.
4. The new engine reads the whole log from line 1: the start line, range and limit the log opened with are not kept (#1219). Its context is read from the sidecar again, or worked out by the plugin. The kept session is not loaded from the sidecar.
5. The selection is restored, then the filter applied again, not counted as the user's (#1185). For the same log, `@Elapsed` compares the elapsed times it compared before (#1224).
6. `FilterResultsChanged` is raised once. If that filter cannot read the log, Reload ends with an engine without records that keeps the filter, and throws.

## Threads

- Records, record lists and elapsed times never change, so any thread may read them.
- The session's stores and the flags may be written from any thread; their locks serialize the writes.
- One filter runs at a time: a new one stops the one running. `Apply`, `ReApply` and `Abort` may be called from any thread, and a filter raises its events on its own thread (#1248, [Filtering.md](Filtering.md#cancellation-and-threading)).
- `IAnnotations.Changed` handlers run on the writer's thread, which may be a background task. GUI rows queue their refresh on the UI thread rather than wait for it (decision 4).

Conventions that Core does not enforce:

- **One Clear or Reload at a time.** `Engine` takes no lock. The GUI runs each on a background task, as it does filters and F5 (#1245). Each, like an open, a filter or an analysis, is an operation of the GUI's one `BusyState`: while one runs, no other begins, and Open, the menus, the filter toolbox and the progress bar follow it (#1257). Filters are the exception: a filter asked for while another runs shares its operation, and every open locks the filter box so none is asked for meanwhile.
- **Read the managers from the engine each time.** Reload replaces the filter, selector, navigation, analyzer and records, as `IEngine`'s remarks say. `Annotations`, `RecordState`, `Bookmarks` and `Regions` stay the same objects.
- **Navigation takes no lock.** The selection, Clear and each filter move its active record.
- **Save before Reload,** as `IEngine.Reload` advises: Reload does not save, while Clear does. The GUI saves first; the CLI and PowerShell never reload (#1247). The GUI also saves before opening another log and before closing. Where the sidecar can never be written, as in a read-only folder (`FileHelper.IsNeverWritable`), each of these logs a warning and carries on, as Clear does (#1246, #1307). If the log has changes no save has written, opening another log and closing first ask the analyst whether to carry on and lose them, or stay, so the folder can be made writable and the log saved; Reload never asks, as it keeps them in memory.
- The CLI and PowerShell start no threads of their own.
