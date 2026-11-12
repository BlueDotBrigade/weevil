# 1028: Choose RAM or Disk When Opening a Log

> **Status:** In progress
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Step 5 of the [#986 plan](986-DiskMode.md#steps): the analyst chooses RAM or Disk when opening a log. Task #1028, under user story #421.

## Goal

- After the Windows file dialog, Weevil shows a small Open As window offering RAM or Disk. The default is already chosen from the log's size and the computer's free memory.
- For a zip, Open As comes after the Select File list, so its default uses the size of the chosen log.
- Warnings appear in that window, not in message boxes:
  - RAM chosen for a log that may not fit;
  - Disk chosen for a log on a network drive.
- Reload (Ctrl+F5) keeps the mode and never asks again.
- The CLI and PowerShell open in RAM, unless an optional parameter asks for Disk.
- In disk mode, a read that fails never ends Weevil. A log that was truncated, replaced or corrupted shows one warning and reloads.
- Export, split and the search for text run off the UI thread, so the window stays responsive while a log on disk is read, and Escape stops them.

## Where Things Stand

Read from the code on 2026-11-11.

- **Core already opens a log on disk**, through the internal `EngineBuilder.UsingStorage(StorageMode)`.
  - Nothing outside Core's tests can call it: `IEngineBuilder` has no such member, and `InternalsVisibleTo` lists only test projects.
  - The engine keeps the mode, and Reload, Clear and a failed Reload keep it. `IEngine` does not say which mode it is in.
- **Six ways to open a log in the GUI.** All of them reach `FilterViewModel.OpenCompressedAsync(string)`:
  - the Open button, Ctrl+O and the menu's Open… show the Windows file dialog first;
  - drag-and-drop skips it, and so does a log passed on the command line (opening from Explorer). The command line is read 2 s after the main window appears.
  - A zip is extracted, and then the Select File list is shown. The chosen log goes on to `OpenAsync(string)`.
  - In `OpenAsync`, a plugin that can show its own Open As window shows it (`IPlugin.ShowOpenAs`). Neither built-in plugin can. The plugin is given a `CreateEngineBuilder` delegate and returns an `OpenAsResult` (context and range). The GUI then builds the engine itself from that result.
  - There is no recent-files list, and no single-instance forwarding.
- **Reload** never goes through an open. It calls `IEngine.Reload()`, which keeps the mode.
- **No code estimates the memory a log needs.**
  - `LineIndex` (internal) stores the log's line count, and the length it indexed, in the `.idx` header. `LineIndex.TryRead` validates the whole file: it hashes the `.idx` and the first and last 4 KB of the log.
  - The performance tests measure 1.22 GB of working set per GB of a UTF-8 log in RAM, and 22 bytes per record on disk.
- **`ComputerSnapshot`** is a GUI class. `Create()` runs WMI queries for the processor and the operating system, including free memory (`FreePhysicalMemory`). Nothing checks for a network drive.
- **Reads that can fail on disk, and that nothing catches today**, are listed in the [#1025 plan](1025-ContentFromFile.md#what-still-reads-a-log):
  - the grid's row converters, whose exception ends Weevil;
  - a filter started from the filter box, which leaves the progress bar running;
  - what runs after a log opens;
  - Find, the analyzers, export, split, the clipboard, the graph, the dashboard's Refresh, and `RowViewModel.ToString`.
  - The GUI never mentions `LogFileChangedException`.
- **The row converters** are XAML resources bound to the row (`RowViewModel`). They read the record's content on the UI thread: through `record.Content`, about 7 µs per record on disk.
  - The [#1025 plan](1025-ContentFromFile.md#decisions) left a cache for screen rows "to measure in #1028".
- **Export, split and Find run on the UI thread:**
  - `SaveSelected` calls `ISelect.SaveSelection`; it shows a message, then rethrows, so a second, generic message follows.
  - `SplitCurrentLog` runs `LogFileSplitter`.
  - `SearchFilterResults` calls `INavigate.NextContent` and the other navigations: pins, bookmarks, comments, elapsed time and Go To.
  - Core takes a cancellation token for export and for the search for text (#1178), but the GUI passes none.
  - Analyses already run off the UI thread (`AnalyzeInBackground`), with a `CancellationTokenSource` that Escape cancels.
- **The CLI** (Cocona) has `Filter` and `Insight` commands. Each opens with `Engine.UsingPath(logPath)`.
  - `Filter` writes its results with `File.WriteAllLines`, which reads each record as it writes.
  - Neither command catches an exception: an unhandled one ends the process with its HResult.
- **PowerShell** has `Get-Records` and `Get-Insight`.
  - `Get-Records` writes `engine.Filter.Results` to the pipeline as records, which read their content later, when the pipeline uses them.
  - Both cmdlets report any exception as a non-terminating error.

## Design

### Core: choosing the mode, and estimating memory

Public additions (decision 1):

- **`IEngineBuilder.UsingStorage(StorageMode)`**, the method that is internal today.
  - Only Core implements `IEngineBuilder`; plugins are only given one. So it is added as an ordinary member, and breaks no plugin.
- **`IEngine.StorageMode`**, read-only. The engine already keeps the mode through Reload and Clear, so the GUI need not keep its own copy: the status bar reads it from the engine, and so can a plugin's dashboard.
- **`Engine.EstimateMemory(string sourceFilePath)`** returns the memory the log would need in RAM, as a `StorageUnit`.
  - The estimate is the log's size plus 34 bytes per record: about 30 for the record table and 4 for a filter's temporary results (the #986 plan's estimate).
  - **The record count:**
    - If the log has an `.idx`, the count is the line count in its header, read without validating the rest of the file. That is good enough for an estimate. If the log has grown past the length the index covers, the count is scaled up in proportion.
    - Otherwise, the count is the log's size divided by the average line length in its first megabyte. That average can err either way.
    - Lines are never fewer than records, so a line count errs high.
  - **What it reads:** a few bytes of the `.idx` and up to 1 MB of the log, shared for reading, writing and deletion, as every read of the log is. On a network drive that takes a noticeable moment, so the GUI runs it off the UI thread.
  - Its limits are stated in its remarks:
    - The estimate covers the content, not the window.
    - The log that is open now is still in memory when the free memory is read. Reopening a large log may therefore default to Disk. The [#986 plan](986-DiskMode.md#decisions) explains why Open keeps it.
- **Reload after a change, for the CLI and PowerShell.** Both run one command and stop, so they recover the same way: catch `LogFileChangedException`, call `Reload()`, and try once more. If the second try fails, the error is reported as it is today.
  - The helper is written once, in Core, and used by both, so it is tested once.
  - Its shape (decision 1): an extension method on `IEngine` that runs a function, and on a change reloads and runs it once more.

### The Open As window

- **A small modal window**, in the same style as `GoToDialog` and `AnalysisDialog`:
  - owned by the main window, `NoResize`, centred on its owner;
  - Open is the default button, and Escape cancels.
  - `IDialogBoxService` gains `TryShowOpenAs(...)`, so tests can substitute it. The window only displays what it is given.
- **What it shows:**
  - the log's name and size;
  - **RAM:** "Faster. Needs about X GB of the Y GB free."
  - **Disk:** "Keeps about 22 bytes per record in memory, and reads the log each time it is needed."
- **The default and the warnings come from a small function in the GUI.** It takes the estimate, the free memory, the chosen mode and whether the log is on a network drive, and returns the default and the warnings. Tests call it directly.
- **Default:** RAM when the estimate fits in the free memory, less a reserve of 1 GB; otherwise Disk (decision 2).
- **Warnings**, shown under the choice and updated when it changes:
  - **RAM, and the estimate does not fit:** "This log needs about X GB of memory, and Y GB is free. Weevil may become slow, or run out of memory. Disk keeps only the record table in memory."
  - **Disk, and the log is on a network drive:** "This log is on a network drive. On disk, every filter reads it over the network: expect it to be several times slower than on a local drive."
    - The check is `DriveInfo.DriveType == Network` for the log's drive, or a UNC path.
    - A zipped log is read from its local copy, so it never shows this warning.
  - Neither warning blocks the open.
- **Cancel** opens nothing, and the current log stays open. For a zip, the extracted copy is deleted, as a cancelled Select File list does today (#1156).
- **Order on every open:**
  1. Windows file dialog, unless the log was dropped or passed on the command line;
  2. Select File, for a zip;
  3. Weevil's Open As;
  4. the plugin's own Open As window, if it has one.
  - The delegate given to `ShowOpenAs` builds with the chosen mode, as does the GUI's own `Engine.UsingPath(...)` chain.
- **The "Open As" log entry:** the GUI writes its own entry with the estimate, the free memory, the default and the mode chosen, so the rule can be tuned from real use.
  - Core's "File loading complete." entry already records the mode.
  - The values do not go into the engine's context, which the sidecar saves.
- **Status bar:** shows "RAM" or "Disk" with the open log's details (`SourceFileOpenedBulletin`), from `IEngine.StorageMode`.

### Every open shows Open As

Drag-and-drop and opening from Explorer show Open As too (decision 3).

- The analyst is at the screen in every case, and one rule is easier to learn.
- A log passed on the command line is opened once the main window is up, as it already is, so the window has an owner.
- Dropping several files still opens only the first, as today.

### Free memory

A static method beside `ComputerSnapshot` reads the available physical memory through `GlobalMemoryStatusEx` (`ullAvailPhys`). That is free and standby pages, the figure Task Manager shows as Available (decision 2).

- It answers in microseconds. `ComputerSnapshot.Create()` runs WMI queries that take a noticeable moment, so the open doesn't call it.
- The #986 plan asked whether WMI's `FreePhysicalMemory` reports the same figure. `GlobalMemoryStatusEx` is documented to, so the question no longer matters.
- The owner checks once on Windows that the window's "Y GB free" matches Task Manager's Available.

### Reads that can fail on disk

Each reader listed in the [#1025 plan](1025-ContentFromFile.md#what-still-reads-a-log) handles a failed read. In RAM mode none of them can fail, so RAM behaves as today.

**One handler in `FilterViewModel`** decides what a failed read does.

- **Deferred:** it always queues its work with `IUiDispatcher.BeginInvoke`, so a read that fails during layout, or on a worker thread, never shows a message or reloads from inside that read.
- **Once per log:**
  - Each open and each Reload starts a new generation of the log. Rows, the graph and background work carry the generation they read.
  - The handler acts once per generation, and ignores a failure from an older one, such as a row still on screen from before Reload.
  - Comparing paths would not work: Reload keeps the path.
- **A log that changed** (`LogFileChangedException`): one warning, "The log changed on disk, so Weevil reloads it", then Reload, as Ctrl+F5 does. The handler never acts while a Reload or an open runs.
- **If Reload fails** because the log changed again, Core shows the log without records and keeps the filter (DiskMode.md, step 6). The GUI shows that message, and does not reload again by itself.
- **Another read failure** (the log was deleted, or access was denied): one error message naming the log, and no automatic Reload. Later failures from the same generation show nothing, so scrolling a log that is gone does not repeat the message.

**Readers that cannot pass an exception on** catch it, show a placeholder, and report the failure to the handler.

- These are the row converters, `RowViewModel.ToString`, and the graph's bindings.
- **How they report:**
  - Each row is given a callback when it is built, as it is given `IAnnotations` today, and reads its content through a safe method on `RowViewModel`.
  - The converters, which are bound to the row, call that method.
  - So no Core event is needed (decision 4).
- **The placeholder:** "⚠ Log changed: press Ctrl+F5" for a change, or "⚠ Log can't be read" otherwise.

**Readers that run a command** catch the exception, stop, and report the failure to the handler.

- These are Find, the analyzers, export, split, the clipboard, a filter from the filter box, the dashboard's Refresh, and what runs after a log opens.
- The progress bar and menus come back. Export shows one message, not two.
- **What runs after a log opens** takes the engine once, rather than reading `_engine` on every statement. If it fails, the previous zip's copy is still released.
- **The graph** copies the text of its records when it opens. It then never reads a log, so a graph left open after another log opens still works. Giving its window an owner stays a separate choice.

### Screen rows

Step 2 measures how long the grid takes to draw a screen of rows from a log on disk, on a local SSD: about 50 rows, up to 5 reads each, through `record.Content`. The measurement is a screen case in the #1024 harness that opens a handle per read, as the [#1025 plan](1025-ContentFromFile.md#steps) asks.

- If a screen takes longer than 50 ms, step 2 keeps the content of the rows on screen in the row, read once through a `ContentBuffer` when the rows are built.
- Otherwise, no cache, as the [#1025 plan](1025-ContentFromFile.md#decisions) chose.
- A network drive is slower either way. The warning in Open As says so.

**Measured on 2026-11-12: no cache.** `ScreenRowsBenchmarks` (Core-PerformanceTests) reads 50 rows of the 1 million-record reference log, 5 times each, through Weevil's own disk mode rather than the #1024 harness. Run with the short job, in Release on Linux, 4 cores, with the log in the operating system's cache:

| Storage | A screen of rows |
|---|---|
| RAM | 0.018 ms |
| Disk | 1.9 ms: about 7.6 µs per read |

From a local SSD, each row adds the drive's latency for one 64 KB block, about 0.1 ms, since its other four reads find the block in the operating system's cache: about 5 ms a screen, a tenth of the 50 ms threshold. On a network drive, each of the 250 reads opens the log over the network, which is where a screen could pass 50 ms; the Open As warning covers it.

### Export, split and the search for text off the UI thread

All three follow `AnalyzeInBackground`:

- they run in `await Task.Run(...)`, with a `CancellationTokenSource` that Escape cancels;
- the menus are disabled while they run;
- the rest of the method, such as selecting the found record and scrolling to it, resumes on the UI thread after the `await`.

**Which searches move:** only `NextContent` and `PreviousContent` (Find, F3 and Shift+F3), the searches that read content. Pins, bookmarks, comments, elapsed time and Go To read no content, and stay as they are.

The search for text in RAM usually takes milliseconds. The progress bar appears only after a short delay, so a quick F3 doesn't flicker.

### The CLI and PowerShell

- **CLI:** `--storage Ram|Disk` on `Filter` and `Insight`. The default is Ram, and the help text gets an example.
  - Each command's work goes through the Core helper, so a changed log is reloaded once.
  - `Filter` writes its results to a temporary file, and moves it into place once complete, as the GUI's export does. A failure then leaves no partial `.Results` file.
- **PowerShell:** `-Storage Ram|Disk` on `Get-Records` and `Get-Insight`, with Ram as the default.
  - The help says that with `-Storage Disk`, the records `Get-Records` writes read their content when the pipeline uses them. A log that changes before then fails those reads.
  - The cmdlet still reloads once if the filter itself finds a change.
- The parameter's name is decision 5.

## Decisions for the Owner

All five were taken as recommended on 2026-11-11, when the owner approved this plan.

1. **Public API.**
   - Chosen: `IEngineBuilder.UsingStorage`, `IEngine.StorageMode`, `Engine.EstimateMemory` and the reload-and-retry helper, as described above.
   - Alternative: leave out `IEngine.StorageMode`. The GUI then remembers the mode it asked for and passes it on, and a plugin's dashboard can't tell.
2. **The default rule** (proposed in the #986 plan on 2026-11-07). Chosen:
   - RAM when the log's size plus 34 bytes per record fits in the available physical memory, less 1 GB;
   - available memory read with `GlobalMemoryStatusEx`;
   - the reserve tuned once #1141 measures the peak while opening.
3. **Drag-and-drop and opening from Explorer.**
   - **A (chosen):** they show Open As, like every other open.
   - **B:** they open with the default mode, without asking; the status bar shows which.
4. **No `IEngine.SourceFileChanged` event** (the question left open in #1028).
   - Chosen: none. The rows report to the GUI's handler through a callback.
   - A plugin that swallows the exception loses only its own read: the GUI's next read finds the change.
   - The event can be added later, if a plugin needs it.
5. **The parameter's name.**
   - Chosen: `--storage` in the CLI and `-Storage` in PowerShell, matching `StorageMode`.
   - The alternative, `--mode`, is shorter but says less.

## Steps

One pull request per step, in order. The GUI builds only on Windows, so for steps 2 and 3, CI's Windows build is the first compiler, and the owner checks them in Weevil.

1. **Core, the CLI and PowerShell.**
   - What it adds:
     - the public API (decision 1);
     - the memory estimate;
     - the reload-and-retry helper;
     - `--storage` and `-Storage`;
     - the CLI's results written through a temporary file.
   - Tests:
     - Core unit tests for the estimate: from an `.idx`, from an `.idx` of a log that has grown since, from the first megabyte, and for an empty log.
     - Core unit tests for the helper: it reloads once on a change, and the second failure reaches the caller.
     - Engine tests: `IEngine.StorageMode` reports the mode, and Reload keeps it.
     - CLI and PowerShell functional tests: `--storage Disk` and `-Storage Disk` open the log on disk and find the same records as RAM.
   - This step runs on Linux.
   - Done on 2026-11-11 (#1183). A damaged `.idx` header that claims more lines than bytes is ignored, and the CLI's save deletes its temporary file without hiding why it stopped (found in review).
2. **The GUI's reads are safe, and export, split and the search for text run off the UI thread.**
   - What it adds:
     - the failed-read handler, the rows' safe read, and the graph's copy of its text. Done on 2026-11-12, in its own pull request. Background work reloads only for a log that changed: another `IOException`, such as a save to a locked file, is shown as that work's error;
     - the screen-row measurement, and the cache if it is needed. Measured: no cache is needed (see Screen rows);
     - export, split and the search for text in the background, with Escape. Done first, in its own pull request: the found record is shown once the records and menus are back, and the progress bar waits 250 ms (found in review). Their messages go through `IDialogBoxService`, so a test never waits on a message box.
     - `OpenAsync` gains an internal mode parameter, so tests can open a log on disk before the window exists.
   - Tests in `Gui-FeatureTests`:
     - a row whose read fails shows its placeholder, and the handler acts once;
     - a failure from an older generation is ignored;
     - a log on disk that is truncated reloads once, and not again while Reload runs;
     - a log on disk that is deleted shows one message while its rows are scrolled;
     - export, split and the search for text run in the background, and Escape cancels them.
3. **The Open As window.**
   - What it adds:
     - the window, the default, the warnings and the order on every open;
     - the delegate given to plugins;
     - the status bar;
     - the "Open As" log entry.
   - Documentation: the Help, DiskMode.md, and the #986 plan.
   - Tests in `Gui-FeatureTests`:
     - each way of opening shows Open As, and opens in the mode chosen;
     - Cancel keeps the current log;
     - a zip shows Select File first, and its default uses the chosen log's size;
     - Reload doesn't ask.
     - The function that chooses the default and the warnings is tested directly.
   - The owner checks on Windows:
     - the window and its warnings;
     - the free memory against Task Manager;
     - a large log opened on disk.

## Not in This Plan

- Replacing the Windows file dialog with Weevil's own browser: #1157, step 6 of the #986 plan.
- Whether a plugin's own options join Weevil's window: moved to #1157.
- Letting a plugin choose the mode: `OpenAsResult` carries no mode, and nothing asks for one.
- The number of reader threads: #1024's measurements on NVMe and SATA.
- A mode passed on Weevil's own command line, to skip Open As: only if someone asks for it.
