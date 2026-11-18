# Filtering

How Core decides which records a filter shows, tests them in parallel, and publishes the results.

## Entry Points

`FilterManager` (`BlueDotBrigade.Weevil.Filter`, Core) implements `IFilter`, which the GUI, the CLI and PowerShell reach through `IEngine.Filter`.

- `Apply(filterType, criteria)`: a filter the analyst asked for, counted in telemetry.
- `ReApply()`: the current filter again, as F5 does.
- `Abort()`: stops the filter running.
- Core also applies filters it does not count: the engine's first, which keeps every record; Clear's reset; and Reload's, which applies the filter again.

## Building the Strategy

A filter first builds a `FilterStrategy` from its `FilterCriteria`: include text, exclude text and a configuration dictionary.

1. **Aliases:** `FilterAliasExpander` replaces each `||`-separated part that equals an alias, ignoring case and surrounding spaces, with its value: `#Warning` becomes `@Severity=Warning`. The core extension gives the aliases (`ICoreExtension.GetFilterAliases`).
2. **Expressions:** each part becomes an `IExpression`. `ExpressionBuilder` tries the built-in monikers (`@Pinned`, `@Elapsed`, `@Severity` …), then the extension's monikers, then plain text or a regular expression, as `FilterType` says.
3. **Include and exclude:** each side is a `LogicalOrOperation`, which matches when any of its expressions matches. `HideDebugRecords` and `HideTraceRecords` add severity tests to the exclude side.

- Plain text and regular expressions are case-sensitive unless `IsCaseSensitive` is `false`.
- A regular expression is compiled, and each thread gets its own copy, so threads do not share its runner. An invalid one throws `InvalidExpressionException` here, before any record is read.
- `@Elapsed` calculates the elapsed times it compares here, so no worker waits for them.

`FilterStrategy.CanKeep(record, content)` then decides:

| Include | Exclude | Keeps |
|---|---|---|
| None | None | Every record |
| Some | None | Records an include expression matches |
| None | Some | Records no exclude expression matches |
| Some | Some | Records an include expression matches and no exclude expression matches |

## Pinned and Bookmarked Records

- `IncludePinned` and `IncludeBookmarks`, set to `true` in the criteria's configuration, keep every pinned or bookmarked record, whatever the include and exclude sides say, hidden severities included.
- They apply only when the filter has an expression. Without one, every record is kept anyway.
- Both are off unless set. The GUI's filter options turn both on by default; the CLI and PowerShell set neither.
- `@Pinned` and `@Bookmark` are different: ordinary expressions that match those records.

## Running the Filter

`FilterAndCalculateMetrics` tests every record the view keeps (`RecordScope.Records`, which Clear narrows).

- **Ranges:** `RecordRanges.Split` cuts the positions into ranges, and `Parallel.ForEach` hands them out one at a time, so no worker holds several while the others wait.
  - In RAM, about 4 ranges per core, of about the same size.
  - On disk, a range starts at each content chunk's first record, so one worker reads each chunk, once. A log with at most half as many chunks as cores is the exception: each chunk's range is split, and each part's worker reads the whole chunk.
- **One `ContentBuffer` per worker,** in its `FilterMapReduceState`, because a buffer is not thread-safe.
- **Reading text:** a worker creates one record per position, because `CanKeep` takes a record. Expressions read its text with `content.Read(record)`, never `record.Content`:
  - the buffer reads the record once, however many expressions test it (#1131), and decodes the log's bytes into memory it reuses, so no string is allocated (#1133);
  - on disk, it reads a whole chunk once two records in a row come from it (#1027), while `record.Content` reads the record's blocks from the log on every call.
- **Marking:** a kept record sets its position in a `bool[]` as long as the scope. Each worker counts its own; the counts are summed when the loop ends.
- **Results:** a filter that keeps every record returns the scope's list itself. Otherwise the marked positions, in order, go to `TableRecordList.Select`, which returns a list over the same record table: 4 bytes per result, and no record kept (#1140).
- **Metric collectors** (`ICoreExtension.GetRecordCounters`, such as `SeverityMetrics`) are reset first, then count each kept record from every worker at once, so a collector must be thread-safe.

## What a Filter Publishes

A filter that was not stopped publishes, under one lock: the results (`Results`), the strategy (`FilterStrategy`, which the analyzers reuse), the filter type and criteria, a copy of the collectors' metrics (`GetMetrics`), and the results' elapsed times (`ElapsedTimes`). Then it adds to the history, raises `ResultsChanged`, and sets `FilterExecutionTime`.

### Elapsed Times

- `IFilter.ElapsedTimes` gives each result the time since the previous result with a timestamp, calculated when read (#1150).
- `@Elapsed` compares the elapsed times of the results shown before the filter, not those of the records it tests (#1016). A record the previous filter hid has none, so it never matches.
- F5 compares again the times the filter compared when it was applied, so `@Elapsed` keeps its results (#1224). Reload does too, if the log kept its length and line count. `Apply`, even of the same filter, compares the results shown now (#1231).
- Clear's reset compares none.

### History

- A shown filter puts its include text first in `IncludeHistory`, and its exclude text first in `ExcludeHistory`, unless blank. A value already there moves to the front; at most 32 are kept (`MaxFilterHistory`).
- Each change raises `HistoryChanged`, whose sender is the history that changed.
- The histories belong to the session, so they are saved in the sidecar and survive Reload (#1109).

### Results Changed and the Selection

- `ResultsChanged` carries the results, after the history's events.
- `CoreEngine` subscribes first. So before anyone else hears of the results, `SelectionManager.KeepRecordsShownBy` keeps the selected records they still show and drops the others, and navigation then works over them (#1015).
- A stopped or failed filter changes neither the results nor the selection.
- Reload restores the selection, then its filter narrows it (#1062). Clear narrows the selection itself before its reset, and `Engine` does not pass that reset's `ResultsChanged` on.
- `Engine.FilterResultsChanged` passes on the current engine's events, so a subscriber survives Reload (#1011).

## Disk Against RAM

On disk, the ranges follow the chunks and a read can fail. A failed read throws its own exception, not the workers' `AggregateException`, and the filter publishes nothing. What then happens is in [DiskMode.md](DiskMode.md#a-log-that-changed).

## Cancellation and Threading

`Apply`, `ReApply` and `Abort` can be called from any thread (#1248).

- **Each run has its own cancellation.** A new run cancels the newest run before it, and becomes the newest. `Abort` cancels the newest run; with none running, it does nothing.
- **One run works at a time** (`_runGate`). A run builds its strategy first, so its expressions compile while the run it replaced stops, then waits for its turn. A run cancelled while it waits never starts.
- **A cancelled run stops** at its workers' next record, and throws `FilterAbortedException`. A newer filter cancels the older one even if its own expression turns out to be invalid.
- **A filter is either stopped or shown.** A run publishes only if it was not cancelled, under the lock that `Abort` and a new run take to cancel it. Once shown, it stays shown.
- **Events are raised during the filter's turn,** on the thread that called `Apply`, before it returns, so the newest filter's results are always heard last.
  - A handler that applies a filter gets an `InvalidOperationException`, rather than waiting for itself.
  - A handler must not wait for a thread that may be applying a filter, such as a UI thread that applies filters: each would wait for the other.
- **`FilterExecutionTime`** runs from `Apply` until the `ResultsChanged` handlers return, including the wait for the filter it replaced.

The GUI calls `Apply` on a thread-pool thread (`FilterViewModel.FilterAsynchronously`), then shows the results through the UI dispatcher.
