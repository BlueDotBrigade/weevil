# Record Grid

How the record grid, the WPF list of a log's filter results, shows millions of rows by creating a row only when WPF asks for it (#1015). The design came from the [virtual list proof of concept](../Research/Weevil3/VirtualListPoC/README.md), and decision 4 of the [#990 plan](../Plans/990-ImmutableCore.md#decisions) sets how changes reach rows.

## The Parts

- **`RecordListView`** (`Gui.Controls`) is the grid: a `ListView` with a `GridView`, declared in `FilterView.xaml`. Its `ItemsSource` is `FilterViewModel.Rows`.
- **`RowList`** (`Gui.Filter`) is the filter results as the grid sees them. It implements the non-generic `IList`, so WPF reads through it instead of copying every item.
- **`RowViewModel`** (`Gui.Filter`) is one row. It raises `PropertyChanged` by hand, not through Metalama's `[Observable]`, because its pin and comment change in Core's store, which Metalama cannot see.

## Millions of Rows

- **Virtualization:** the grid sets `VirtualizingStackPanel.IsVirtualizing`, `VirtualizationMode="Recycling"` and `ScrollViewer.CanContentScroll`, so WPF builds controls only for the rows on screen, and scrolls by row.
- **Rows on demand:** `RowList[position]` creates a row only when WPF asks for it. `Count` is the results' count, so nothing is read up front.
- **A weak cache:** a row is cached by position while something holds it, so WPF gets the same instance back, but the cache never keeps a row alive. Dead entries are purged as the cache grows.
- **One record per row:** `GetRow` reads the record once, because a list over the record table creates a new record on each read ([RecordTable.md](RecordTable.md#records-on-demand)).
- **Without a record:** everything else reads the results by position, and creates no record:
  - `IndexOf`, `IndexOfLineNumber` and `LineNumberAt` read `RecordList.GetLineNumber`. `IndexOf` is a binary search, because WPF finds a row with it;
  - the elapsed time is `ElapsedTimes.Measure(results, position)`, which reads only `GetCreatedAt` and `GetLineNumber`, once, when the row is created (#1150).
- **Text** comes from the row's record, `Record.Content`, each time a converter asks; the grid does not use `RecordList.ReadContent`. On disk, each read reads the record's 64 KB blocks ([DiskMode.md](DiskMode.md#reading-a-log-on-disk)).
- **Equality:** rows are equal by line number, because a row may be created more than once for the same record.
- **A new list per filter:** `FilterViewModel.RefreshFilterResults` reads `Filter.Results` once, on the UI thread, and gives it to both `VisibleItems` and a new `RowList`. A `RowList` never changes, so it raises no collection events.
- **Known limitation:** after a long run of rows without a timestamp, `Measure` walks back over the run on the UI thread: about 20 ms for 10 million rows (#1150).

## What Core Owns, and What the GUI Owns

- **Core** keeps every fact about a record, by line number:
  - the records: the filter results, a `RecordList`;
  - comments and pins: `IAnnotations`, in the session, shared by every view;
  - flags: a `FlagSet` per view, which the grid does not show;
  - the selection and its anchor: `ISelect`, stored as ranges of positions, but read and changed by line number;
  - bookmarks and regions, which the converters read through `FilterViewModel`.
- **The GUI** keeps only what the screen needs: the rows, each row's copy of `IsSelected` and its elapsed time, the refresh queue, the caret (`RowSelection.CaretLineNumber`) and the placeholders.
- A row stores no pin or comment: `IsPinned` and `Comment` read and write the annotation store.
- WPF never selects a row (#1064). `RecordListView` reports presses, `FilterView` passes them and the selection keys to `FilterViewModel`, and `RowSelection` turns them into calls to `ISelect`.

## How Changes Reach Rows

Rows are created and refreshed only on the UI thread. Changes arrive on any thread, so a lock in `RowList` guards its cache and its queue.

Comments and pins follow decision 4: a row learns of a change later, and the thread that made it never waits for the UI thread.

1. The annotation store raises `Changed` on the thread that made the change, after releasing its lock. A change to many lines, such as Unpin All, raises one event for `AnnotationChangedEventArgs.ManyLines`.
2. `FilterViewModel.OnAnnotationsChanged`, subscribed once per open log (#1011), calls `RowList.QueueRefresh` on that thread.
3. `QueueRefresh` adds the change to the queue and returns. The first change since the last refresh queues `RefreshChangedRows` on the UI thread with `BeginInvoke`, at `Background` priority, never `Invoke`.
4. On the UI thread, the list takes the queue and the rows still held, then refreshes them outside its lock, because the bindings read the store. More than 1,000 changes, or one change to many lines, refresh every row still held, once.
5. `RowViewModel.Refresh` ignores the row's own writes, and raises `Comment` only when the store's comment differs from the one shown. The comment box sends its text 250 ms after the last key, so reading it back could erase text still being typed.

The selection has no Core event. The GUI changes it on the UI thread, through `RowSelection`, then calls `RowList.RefreshSelection`. That updates `IsSelected` on the rows still held, and raises `SelectionRefreshed` with those that changed, which the grid passes on to screen readers (#1075). A new row reads `IsSelected` from `ISelect.Selected`.

Bookmarks and regions are not on the row: when they change, `FilterView` calls `Items.Refresh()`, which replaces the controls of the rows on screen.

## What WPF Must Not Do

Walking `RowList` with `GetEnumerator` or `CopyTo` creates a row, and a record, for every result. Weevil never calls either; they complete `IList`. `GetEnumerator` is lazy, so a call costs nothing until it is walked: in the proof of concept, WPF called it once and read no rows. No test catches a walk.

The grid also avoids what would read every row:

- **WPF's selection:** in the proof of concept, Ctrl+A with WPF's selection created all 1 million rows. The grid uses `SelectionMode.Multiple`, so moving focus does not select, and unselects anything WPF selects. The selected style binds the row's `IsSelected`, not the container's.
- **Text search** is off: it would compare every row with the letter typed.
- **UI Automation:** `RecordListViewAutomationPeer` reports at most 1,000 selected rows, and finds the next selected row in Core's selection, because WPF's search would create a row and a peer for every record.
- **Sorting, grouping or filtering** the grid's view would read every row. The grid does none; filters run in Core.

## Placeholders

When a log open on disk changes or can no longer be read, a row shows a placeholder instead of its text (#1028):

- `ChangedLogPlaceholder`, "⚠ Log changed: press Ctrl+F5", when the read throws `LogFileChangedException`;
- `UnreadableLogPlaceholder`, "⚠ Log can't be read", for another `IOException` or an `UnauthorizedAccessException`.

`RowViewModel.ReadContent` catches the exception, because WPF does not catch a converter's exception, so Weevil would end. The converters, and `ToString`, the row's name for screen readers, read through it. The row reports the failure to the callback `RowList` gave it, and `FilterViewModel` queues its handling on the UI thread, so a read that fails while the grid draws never shows a message from inside that read. In RAM mode the read cannot fail.

What Weevil then does, once per log, is in [DiskMode.md](DiskMode.md#a-log-that-changed).

## Tests

`Tst/BlueDotBrigade.Weevil.Gui-FeatureTests` holds them: `RowListTests`, `RowViewModelTests`, `RowSelectionTests` and `SelectingRecords.feature` in `Filter/`, the placeholder tests in `FilterViewModelTests`, and screen readers in `Controls/RecordListViewAutomationPeerTests`.
