# Engine API: One Engine per View

Question for #990 and #991: how do callers open a log, work with it, and open more views of it, without ever handling Core's internal state?

> Notes from 2026-10-19. Proposed design; nothing here is implemented yet.

## Rules

- The root state, `LogSession`, is `internal` to Core. It holds the log, annotations, bookmarks, regions and the sidecar. Callers never see it, hold it or pass it. Tests reach it through `InternalsVisibleTo` in Debug builds.
- An `IEngine` is one view of a log. It keeps a private reference to its session, so no operation takes a token.
- `CreateView()` returns another `IEngine` that shares the same session.
- The name `IEngine` stays, so existing GUI, CLI and PowerShell callers keep compiling.

```
Engine.UsingPath(path).Open() ──► IEngine (view 1, own record scope) ─┐
                                                                      ├──► LogSession (internal): log, annotations,
engine.CreateView()           ──► IEngine (view 2, own record scope) ─┘    bookmarks, regions, sidecar
```

## Public API Sketch

```csharp
public interface IEngine : IDisposable
{
    // This view only
    IFilter Filter { get; }        // filter and results; the same object for the view's lifetime
    INavigate Navigate { get; }
    ISelect Selector { get; }
    IAnalyze Analyzer { get; }     // runs on this view's results; writes flags to the session
    int Count { get; }             // records in this view's scope
    bool HasBeenCleared { get; }   // true when the scope is not the whole log
    void Clear(ClearOperation clearOperation);   // narrows this view's scope only
    void RestoreRecords();         // the scope becomes the whole log again (#986)

    // Shared by every view of the log
    IAnnotations Annotations { get; }   // comments, pins, flags; replaces record.Metadata
    IBookmarkManager Bookmarks { get; }
    IRegionManager Regions { get; }
    string SourceFilePath { get; }
    void Reload();     // reloads the log for every view; each view keeps its scope and re-applies its filter
    void Save();

    IEngine CreateView();   // #991
}

public interface IAnnotations
{
    event EventHandler<AnnotationChangedEventArgs> Changed;   // raised for a change made in any view

    string GetComment(int lineNumber);
    void SetComment(int lineNumber, string comment);   // keeps today's rule: a comment pins the record
    bool IsPinned(int lineNumber);
    void Pin(int lineNumber);
    void Unpin(int lineNumber);
    bool IsFlagged(int lineNumber);
}
```

The builder keeps today's fluent style and gains the storage mode from #986:

```csharp
IEngine engine = Engine
    .UsingPath(@"C:\Logs\Application.log")
    .UsingStorage(StorageMode.Disk)   // #986; RAM is the default
    .Open();
```

## Sample

```csharp
_engine = await Task.Run(() => Engine.UsingPath(path).Open());

// Subscribe once: these objects live as long as the view
_engine.Filter.ResultsChanged += OnResultsChanged;
_engine.Annotations.Changed += OnAnnotationChanged;

await Task.Run(() => _engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria("Error|Timeout")));

IRecord nextPin = _engine.Navigate.NextPin();
_engine.Annotations.SetComment(nextPin.LineNumber, "Retry starts here");
_engine.Bookmarks.Create(1, "Retry", nextPin.LineNumber);

_engine.Selector.Select(nextPin);
_engine.Clear(ClearOperation.BeforeSelected);   // same engine afterwards: no re-subscribing

IEngine second = _engine.CreateView();          // its own scope and filter; shared annotations
second.Annotations.GetComment(nextPin.LineNumber);   // "Retry starts here"

second.Dispose();
_engine.Save();
_engine.Dispose();   // the last view closes the session
```

## Inside Core

```csharp
internal sealed class LogSession                 // the root state; never public
{
    internal LogSource Source { get; private set; }   // immutable; Reload replaces it
    internal AnnotationStore Annotations { get; }
    internal BookmarkManager Bookmarks { get; }
    internal RegionManager Regions { get; }
}

internal sealed class RecordScope                // the records one view includes; immutable
{
    private readonly ImmutableArray<RecordRange> _ranges;   // sorted, non-overlapping
}

public sealed class Engine : IEngine             // one view
{
    private readonly LogSession _session;        // shared
    private readonly FilterManager _filter;      // this view only
    private RecordScope _scope;                  // this view only

    public IEngine CreateView() => new Engine(_session);

    public void Clear(ClearOperation operation)
    {
        _scope = ClearRules.Apply(operation, _scope, _selection.GetSelected(), _session.Regions);
        _filter.ReApply();   // raises ResultsChanged on the same Filter object
    }
}
```

## Clear

The log on disk, its index and its content never change. Clear changes only what one view includes: its **record scope**, a short list of record-number ranges. Filtering, navigation and the record count work inside the scope.

| | Today | Proposed |
|---|---|---|
| What Clear does | Builds a new engine from the records left in memory | Narrows the view's scope |
| Other views | Not applicable | Unaffected: two views can clear different records |
| Line numbers | Unchanged | Unchanged |
| Undo | Re-open the log | `RestoreRecords()` (#986) |
| Comments and pins on cleared records | Unsaved changes appear to be lost on save, because the old sidecar values win (from reading `SidecarLoader.PackSidecar`; not tested) | Kept and saved: they live in the session |
| Events | The GUI subscribes again | Same engine: no re-subscribing |

Every clear operation becomes a range rule, with today's rules:

| Operation | Scope afterwards |
|---|---|
| `BeforeSelected` | From the first selected record to the end |
| `AfterSelected` | From the start to the last selected record |
| `BeforeAndAfterSelected` | From the first to the last selected record |
| `BetweenSelected` | Everything except the records between the first and last selected ones |
| `Selected` | Everything except the selected records |
| `Unselected` | Only the selected records |
| `BeyondRegions` | Only the records inside regions |

## Freeing RAM

In RAM mode, content is held in 16 MB chunks (#988), about 128 K records each at 131 bytes per record. A chunk that no view's scope includes is released. The log stays on disk, so a released chunk is read back from the file when a view needs it again. RAM mode becomes a cache of the log, not the only copy.

```
Log on disk:   [chunk 0][chunk 1][chunk 2][chunk 3][chunk 4][chunk 5]
View 1 scope:                    ████████████████████████████████████   (cleared before chunk 2)
View 2 scope:  ███████████████████████████                              (cleared after chunk 3)
Combined:      ████████████████████████████████████████████████████████  → nothing released
After view 2 closes:             ████████████████████████████████████   → chunks 0 and 1 released
```

- **Comparing views is cheap.** Scopes are short sorted range lists, merged whenever a view clears, restores, opens or closes.
- **Releasing is safe during a filter.** The chunk table is immutable: releasing publishes a new table. A running filter keeps the old one and finishes; the garbage collector frees the chunk afterwards.
- **Memory returns to Windows** after one compacting garbage collection, as Clear does today.
- **Limits:** only whole chunks are released, so up to about 16 MB stays at each edge of a kept range. `Clear Selected` removes scattered records and frees nothing.
- **The record table and annotation byte stay,** about 14 bytes per record. Line numbers, navigation and the sidecar need them.

Delivered in three steps, so the release never depends on multiple views:

| Epic | What it adds |
|---|---|
| #988 | With one view, Clear releases chunks outside the scope. Cleared records return after Reload, as today. |
| #986 | A released chunk is read back from the file, so `RestoreRecords()` works. RAM and disk mode share one content store with two caching rules: RAM keeps every chunk a scope includes, disk keeps only the chunks being read. |
| #991 | Only chunks outside every view's scope are released; a new view reads back what it needs. |

## Shared or Per View

| Shared: in the session | Per view: in the engine |
|---|---|
| Log content, line index, record table | Record scope (Clear) |
| Comments, pins, flags (flags: decision for #991) | Filter, results, elapsed times |
| Bookmarks, regions, remarks, table of contents | Selection, active record, navigation |
| Filter history, context, sidecar save, Reload | Analyzer runs (their flags go to the session) |

## Rejected: A Public Log Object

`LogFile.Open(path).CreateView()` makes it obvious what is shared, but the caller then holds the root state, there are two types to learn, and every `IEngine` caller breaks.

## Status

Proposed, 2026-10-19. Clear per view is agreed. Still open: whether `IEngine` becomes `IDisposable` with the last view closing the session; what a new view starts with; whether Reload from one view reloads every view; and whether the GUI offers a Restore command.

The plans for [#990](../../Plans/990-ImmutableCore.md), [#988](../../Plans/988-IndexingService.md), [#986](../../Plans/986-DiskMode.md) and [#991](../../Plans/991-MultipleViews.md) follow this design.
