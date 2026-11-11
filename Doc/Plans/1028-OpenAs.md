# 1028: Choose RAM or Disk When Opening a Log

> **Status:** Draft
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
- Export and Find run off the UI thread, so the window stays responsive while a log on disk is read, and Escape stops them.

## Where Things Stand

Read from the code on 2026-11-11.

- **Core already opens a log on disk**, through the internal `EngineBuilder.UsingStorage(StorageMode)`. Nothing outside Core's tests can call it: `IEngineBuilder` has no such member, and `InternalsVisibleTo` lists only test projects. The engine keeps the mode, and Reload, Clear and a failed Reload keep it. `IEngine` does not say which mode it is in.
- **Six ways to open a log in the GUI.** All of them reach `FilterViewModel.OpenCompressedAsync(string)`:
  - the Open button, Ctrl+O and the menu's Open… show the Windows file dialog first;
  - drag-and-drop, and a log passed on the command line (opening from Explorer), skip it.
  - A zip is extracted, and then the Select File list is shown. The chosen log goes on to `OpenAsync(string)`.
  - In `OpenAsync`, a plugin that can show its own Open As window (`IPlugin.ShowOpenAs`) shows it. Neither built-in plugin can.
  - There is no recent-files list, and no single-instance forwarding.
- **Reload** never goes through an open; it calls `IEngine.Reload()`, which keeps the mode.
- **No code estimates the memory a log needs.** `LineIndex` (internal) stores the log's line count in the `.idx` header. The performance tests measure 1.22 GB of working set per GB of a UTF-8 log in RAM, and 22 bytes per record on disk.
- **`ComputerSnapshot`** is a GUI class. It reads installed and free memory through WMI (`Win32_OperatingSystem.FreePhysicalMemory`). It has no network-drive check, and nor does anything else.
- **Reads that can fail on disk, and that nothing catches today**, are listed in the [#1025 plan](1025-ContentFromFile.md#what-still-reads-a-log):
  - the grid's row converters, whose exception ends Weevil;
  - a filter started from the filter box, which leaves the progress bar running;
  - what runs after a log opens;
  - Find, the analyzers, export, the clipboard, the graph, the dashboard's Refresh, and `RowViewModel.ToString`.
  - The GUI never mentions `LogFileChangedException`.
- **Export and Find run on the UI thread.** `SaveSelected` calls `ISelect.SaveSelection`, and `SearchFilterResults` calls `INavigate.NextContent` and the related methods.
  - Core takes a cancellation token for both (#1178), but the GUI passes none.
  - Analyses already run off the UI thread (`AnalyzeInBackground`), with a `CancellationTokenSource` that Escape cancels.
- **The CLI** (Cocona) has `Filter` and `Insight` commands. Each opens with `Engine.UsingPath(logPath)`. Neither catches an exception: an unhandled one ends the process with its HResult.
- **PowerShell** has `Get-Records` and `Get-Insight`.
  - `Get-Records` writes `engine.Filter.Results` to the pipeline as records, which read their content later, when the pipeline uses them.
  - Both cmdlets report any exception as a non-terminating error.

## Design

### Core: choosing the mode, and estimating memory

Public additions, for the owner to approve (decision 1):

- **`IEngineBuilder.UsingStorage(StorageMode)`**, the method that is internal today.
  - Only Core implements `IEngineBuilder`; plugins are given one. So the member is added as an ordinary member, not a default implementation.
- **`IEngine.StorageMode`**, read-only. The status bar shows it, and a plugin can tell why reads are slower.
- **`Engine.EstimateMemory(string sourceFilePath)`** returns the memory the log would need in RAM, as a `StorageUnit`.
  - The estimate is the log's size plus 34 bytes per record: about 30 for the record table and 4 for a filter's temporary results (the #986 plan's estimate).
  - The record count is the line count in a valid `.idx`. Otherwise it is the log's size divided by the average line length in its first megabyte. Lines are never fewer than records, so the estimate errs high.
  - It reads at most the `.idx` header and 1 MB of the log, shared for reading, writing and deletion, as every read of the log is.
- **Reload after a change, for the CLI and PowerShell.** Both run one command and stop, so they recover the same way: catch `LogFileChangedException`, call `Reload()`, and try once more. If the second try fails, the error is reported as it is today.
  - The helper is written once, in Core, and used by both, so it is tested once.
  - Its public shape is part of decision 1. Recommended: an extension method on `IEngine` that runs a function, and on a change reloads and runs it once more.

### The Open As window

- A small modal window, in the same style as `GoToDialog` and `AnalysisDialog`:
  - code-behind, owned by the main window, `NoResize`, centred on its owner;
  - Open is the default button, and Escape cancels.
  - `IDialogBoxService` gains `TryShowOpenAs(...)`, so tests can substitute it.
- **What it shows:**
  - the log's name and size;
  - two choices: **RAM**, "faster; needs about X GB of the Y GB free", and **Disk**, "keeps about 22 bytes per record in memory; reads the log each time it is needed".
- **Default:** RAM when the estimate fits in the free memory, less a reserve of 1 GB; otherwise Disk (decision 2).
- **Warnings**, shown under the choice and updated when it changes:
  - **RAM, and the estimate does not fit:** "This log needs about X GB of memory, and Y GB is free. Weevil may become slow, or run out of memory. Disk keeps only the record table in memory."
  - **Disk, and the log is on a network drive:** "This log is on a network drive. On disk, every filter reads it over the network: expect it to be several times slower than on a local drive." The check is `DriveInfo.DriveType == Network` for the log's drive, or a UNC path. A zipped log is read from its local copy, so it never shows this warning.
  - Neither warning blocks the open.
- **Cancel** opens nothing, and the current log stays open. For a zip, the extracted copy is deleted, as a cancelled Select File list does today (#1156).
- **Order on every open:**
  1. Windows file dialog, unless the log was dropped or passed on the command line;
  2. Select File, for a zip;
  3. Weevil's Open As;
  4. the plugin's own Open As window, if it has one.
  - The builder that `ShowOpenAs` receives carries the chosen mode.
- **"Open" record:** the open's log entry already records the mode. It also gets the estimate, the free memory, the default and whether the user changed it, so the rule can be tuned from real use.
- **Status bar:** shows "RAM" or "Disk" with the open log's details (`SourceFileOpenedBulletin`).

### Every open shows Open As

Drag-and-drop and opening from Explorer show Open As too (decision 3). The analyst is at the screen in every case, and one rule is easier to learn. Dropping several files still opens only the first, as today.

### Free memory

`ComputerSnapshot` gains a read of the available physical memory through `GlobalMemoryStatusEx` (`ullAvailPhys`): free and standby pages, the figure Task Manager shows as Available (decision 2).

- It answers in microseconds, where the WMI query takes a noticeable moment.
- The #986 plan asked whether WMI's `FreePhysicalMemory` reports the same figure. `GlobalMemoryStatusEx` is documented to, so the question no longer matters.
- The owner checks once on Windows that the window's "Y GB free" matches Task Manager's Available.

### Reads that can fail on disk

Each reader listed in the [#1025 plan](1025-ContentFromFile.md#what-still-reads-a-log) handles a failed read. In RAM mode none of them can fail, so RAM behaves as today.

- **A log that changed** (`LogFileChangedException`): one warning, "The log changed on disk, so Weevil reloads it", then Reload, as Ctrl+F5 does.
  - The warning and Reload happen once per change, and never while a Reload or an open runs. A flag in `FilterViewModel` marks the change, and Reload or an open clears it.
  - If Reload fails because the log changed again, Core shows the log without records and keeps the filter (DiskMode.md, step 6). The GUI shows that message, and does not reload again by itself.
- **Another read failure** (the log was deleted, or access was denied): an error message naming the log, with no automatic Reload.
- **Readers that cannot pass an exception on** catch it, show a placeholder, and post a `LogFileChangedBulletin` (GUI only) that `FilterViewModel` handles with the rule above.
  - These are the row converters, `RowViewModel.ToString`, and the graph's bindings.
  - The placeholder is "⚠ Log changed: press Ctrl+F5" for a change, or "⚠ Log can't be read" otherwise.
  - So no Core event is needed (decision 4).
- **Readers that run a command**, such as Find, the analyzers, export, the clipboard, a filter from the filter box, the dashboard's Refresh and what runs after a log opens, catch the exception, stop, and follow the same rule.
  - The progress bar and menus come back.
  - Export shows one message, not two.
- **What runs after a log opens** takes the engine once, rather than reading `_engine` on every statement. If it fails, the previous zip's copy is still released.
- **The graph** copies the text of its records when it opens. It then never reads a log, so a graph left open after another log opens still works. Giving its window an owner stays a separate choice.

### Export and Find off the UI thread

Both follow `AnalyzeInBackground`:

- they run in `Task.Run`, with a `CancellationTokenSource` that Escape cancels;
- the menus are disabled while they run;
- results come back through `IUiDispatcher`.

Find in RAM usually takes milliseconds. The progress bar appears only after a short delay, so a quick F3 doesn't flicker.

### The CLI and PowerShell

- **CLI:** `--storage Ram|Disk` on `Filter` and `Insight`. The default is Ram, and the help text gets an example. Each command's work goes through the Core helper, so a changed log is reloaded once.
- **PowerShell:** `-Storage Ram|Disk` on `Get-Records` and `Get-Insight`, with Ram as the default.
  - The help says that with `-Storage Disk`, the records `Get-Records` writes read their content when the pipeline uses them. A log that changes before then fails those reads.
  - The cmdlet still reloads once if the filter itself finds a change.
- The parameter name is decision 5.

## Decisions for the Owner

1. **Public API.**
   - Recommended: `IEngineBuilder.UsingStorage`, `IEngine.StorageMode`, `Engine.EstimateMemory` and the reload-and-retry helper, as described above.
   - Alternative: leave out `IEngine.StorageMode`; the GUI then remembers the mode it asked for, and the status bar can't show the mode a plugin chose.
2. **The default rule** (proposed in the #986 plan on 2026-11-07, to confirm now). Recommended:
   - RAM when the log's size plus 34 bytes per record fits in the available physical memory, less 1 GB;
   - available memory read with `GlobalMemoryStatusEx`;
   - the reserve tuned once #1141 measures the peak while opening.
3. **Drag-and-drop and opening from Explorer.**
   - **A (recommended):** they show Open As, like every other open.
   - **B:** they open with the default mode, without asking; the status bar shows which.
4. **No `IEngine.SourceFileChanged` event** (the question left open in #1028).
   - Recommended: none. The GUI's bulletin serves its own readers, such as the row converters.
   - A plugin that swallows the exception loses only its own read: the GUI's next read finds the change.
   - The event can be added later, if a plugin needs it.
5. **The parameter's name.**
   - Recommended: `--storage` in the CLI and `-Storage` in PowerShell, matching `StorageMode`.
   - The alternative, `--mode`, is shorter but says less.

## Steps

One pull request per step, in order. The GUI builds only on Windows, so for steps 2 and 3 CI's Windows build is the first compiler, and the owner checks them in Weevil.

1. **Core, the CLI and PowerShell.**
   - What it adds:
     - the public API (decision 1);
     - the memory estimate;
     - the reload-and-retry helper;
     - `--storage` and `-Storage`.
   - Tests:
     - Core unit tests for the estimate: from a valid `.idx`, from the first megabyte, and an empty log.
     - Core unit tests for the helper: it reloads once on a change, and the second failure reaches the caller.
     - Engine tests: `IEngine.StorageMode` reports the mode, and Reload keeps it.
     - CLI and PowerShell functional tests: `--storage Disk` finds the same records as RAM, and an unknown value is refused with a message.
   - This step runs on Linux.
2. **The GUI's reads are safe, and export and Find run off the UI thread.**
   - What it adds:
     - the failed-read handling above, and the graph's copy of its text;
     - export and Find in the background, with Escape.
   - Disk mode can't be chosen yet, but `WEEVIL_TEST_STORAGE=Disk` opens a Debug GUI on disk to try it.
   - Tests in `Gui-FeatureTests`:
     - a converter returns its placeholder when the read fails, and posts the bulletin once;
     - a changed log reloads once, and not again while Reload runs;
     - export and Find run in the background, and Escape cancels them;
     - a cancelled export keeps an earlier file.
3. **The Open As window.**
   - What it adds:
     - the window, the default, the warnings and the order on every open;
     - the builder passed to plugins;
     - the status bar;
     - the "Open" record.
   - Documentation: the Help, DiskMode.md, and the #986 plan.
   - Tests in `Gui-FeatureTests`:
     - each way of opening shows Open As, and opens in the mode chosen;
     - Cancel keeps the current log;
     - a zip shows Select File first, and its default uses the chosen log's size;
     - Reload doesn't ask.
   - The default rule and the warnings are tested with a substituted `IDialogBoxService` and free-memory figure.
   - The owner checks on Windows:
     - the window and its warnings;
     - the free memory against Task Manager;
     - a large log opened on disk.

## Not in This Plan

- Replacing the Windows file dialog with Weevil's own browser: #1157, step 6 of the #986 plan.
- Whether a plugin's own options join Weevil's window: moved to #1157.
- The number of reader threads: #1024's measurements on NVMe and SATA.
- A mode passed on Weevil's own command line, to skip Open As: only if decision 3 picks A and someone asks for it.
