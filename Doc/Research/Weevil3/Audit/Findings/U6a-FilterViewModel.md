# U6a: FilterViewModel (#990, #986)

Part of the [Weevil 3.0 audit](../Progress.md). `FilterViewModel` is the WPF application's main view-model: it opens, reloads and saves the log, runs the long operations, and drives the one view of the log that the window shows (filter box, grid rows and selection, Find, navigation, bookmarks and regions).

> **Status:** Final. Reviewed on 2026-11-17 at `main` e3dee822, by one reviewer covering every lens.

## Summary

The 3.0 work in `FilterViewModel` is careful: each addition names its issue and says why, and work that reads the log left the UI thread (analysis, export, split, Find, Clear, the graph's copy). The epics moved logic into Core rather than into this class: keeping the selection across Reload, flagging an insight's records, reading comments and pins through the stores. The one piece they added here that a headless caller would also need, the graph's `CopyWithText`, is already U2-1. Engine events are now subscribed once per open log, and moved only after an open succeeds. The cost is size and protocol: the class grew from 2,195 to 2,941 lines (#1028 alone churned 753), and none of the new parts got a type of its own. **Whether anything is running is a protocol of seven flags written by hand in eight methods**, with `CanOpenLogFile` as the lock, and 3.0 nearly doubled its write sites (U6a-1); the class also holds both the open log's lifetime and one view's state, which the GUI half of #991 will need apart (U6a-2). **F5 still filters on the UI thread**, so on disk it reads the log there: #1028 moved export, split and Find off the UI thread but missed it (U6a-3). SonarCloud's S8949 lead is noise, since the token it suggests would be wrong at all 11 sites (U6a-4), and the unit has no High finding.

## What Was Examined

- GUI: `Src/BlueDotBrigade.Weevil.Gui/Filter/FilterViewModel.cs` (all of it), `FilterViewModel.Commands.cs` (how commands are gated), `FilterView.xaml` (bindings and key gestures), `FilterView.xaml.cs` (the view's event handlers), `Input/UiBoundCommand.cs`, `Threading/IUiDispatcher.cs`, `Analysis/UiResponsivenessMonitor.cs`, `IO/IDialogBoxService.cs`, `IO/OpenAsAdvice.cs`.
- Core and Common, where the view-model depends on them: `Common/IEngine.cs` (events and lifetimes), `Core/Engine.cs` (event forwarding), `Core/CoreEngine.cs` (`Save`, `GenerateReport`), `Core/Filter/FilterManager.cs` (`ReApply`, `GetMetrics`), `Core/PluginFactory.cs`.
- 2.x: `Gui/Filter/FilterViewModel.cs` at `origin/Releases/2.x` (2,195 lines): `OpenAsync`, `Reload`, `ClearRecords`, `Analyze`, `GraphData`, `Refresh`, `RefreshFilterResults`; `Core/Engine.cs` (`Reload`, `Clear`).
- History: the 45 commits since `baa70a9d` that touch the file, and its churn per issue.
- Documents: `Doc/Plans/1028-OpenAs.md`, `986-DiskMode.md`, `990-ImmutableCore.md`, `991-MultipleViews.md`, `1027-StreamingReads.md`; `Doc/Design/DiskMode.md`; `.github/copilot-instructions.md`, `instructions/gui.instructions.md`, `review.instructions.md`; findings U2, U3, U4 and U5a.
- Tests: `Tst/BlueDotBrigade.Weevil.Gui-FeatureTests/Filter/FilterViewModelTests.cs` (37 tests), `Threading/UiDispatcherFake.cs`, the pausable and deferred dispatchers.
- Measurements: [Ops-1gb-main-disk-run2.txt](../Measurements/Ops-1gb-main-disk-run2.txt), [Ops-4gb-main-disk-run2.txt](../Measurements/Ops-4gb-main-disk-run2.txt), [Ops-1gb-2.12.1-run2.txt](../Measurements/Ops-1gb-2.12.1-run2.txt).
- SonarCloud: all 44 open issues on the file, read through its public API on 2026-11-17 (the analysed commit `7c6a061c` has the same GUI code as `e3dee822`).

## Strengths

- **Long work left the UI thread, through one documented runner.** `RunInBackground` says what it disables, when the progress bar appears, what Escape cancels and on which thread `onSucceeded` runs (`Src/BlueDotBrigade.Weevil.Gui/Filter/FilterViewModel.cs:2271-2286`). Its locals say which thread owns them (`:2318`, `:2353`). Analysis, export, split, Find and the graph's copy use it (`:2253-2269`, `:1321-1343`, `:1405-1411`, `:1847-1886`, `:1549-1556`). In 2.x an analysis ran inside the command handler (2.x `FilterViewModel.cs:1624-1639`). Clear (#1071), the Open As estimate and the deletion of extracted copies also run in the background (`:1584`, `:723-736`, `:607-614`).
- **Logic went to Core, not into the view-model.**
  - Reload keeps the selection in Core (`:1103`, #1062). 2.x restored it here with a quadratic `Where(... Any(...))` (2.x `:811-829`).
  - Insight navigation asks Core to flag the records still in memory (`:2098-2101`, #1013).
  - Comments, pins and elapsed times are read through Core (#1097), and the filter history belongs to the session (`:1114`, #1109).
  - What the epics added here is screen logic: the limits on Copy, Paste and Graph, the Open As window, the zip copy and the failed-read messages.
- **Subscriptions follow the log, not each operation.**
  - `IEngine` forwards the filter history, so the view-model subscribes once per open log. 2.x subscribed again after every Clear and Reload, under a `// HACK` comment (2.x `:824-825`, `:1057-1059`).
  - The subscriptions move only once the new engine is built, so a failed open leaves the open log subscribed (`:838-847`), and a test pins it (`Tst/BlueDotBrigade.Weevil.Gui-FeatureTests/Filter/FilterViewModelTests.cs:508`).
  - `IEngine` states that `Annotations` is the same object for the log's lifetime (`Src/BlueDotBrigade.Weevil.Common/IEngine.cs:36-39`), which is what keeps the second subscription valid across Clear and Reload.
  - `RowList` subscribes to nothing. The view-model forwards changes to whichever rows are current (`:995-999`), so a replaced `RowList` leaks nothing.
- **Memory is let go of on purpose.** Reload and Clear drop the rows before Core frees the records (`:1095-1101`, `:1590-1596`; #1126, #1139). Opening another log closes the dashboard, which held the previous engine (`:849-851`). The graph keeps its own array of at most 100,000 records (`:1519-1557`).
- **The open engine is read defensively.** Code that can run after the menus are back takes `_engine` once and says why (`:946-948`, `:1323`, `:1538`, `:1849`).
- **The storage mode is passed through, not tested.** The view-model hands the chosen `StorageMode` to the builder (`:788`, `:831-834`) and shows it (`:871`). The failed-read handling never asks the mode. The one test of it is the graph's (`:1543`, U3-7).
- **Comments say why.** Examples: the three reasons for the 100,000-record limit (`:66-68`); why `_logGeneration` stays a field (`:127-128`); why going to the found row is an event and not a property (`:2673`), the Metalama caveat in `.github/copilot-instructions.md`.
- **The GUI tests read as specifications.** `FilterViewModelTests` has 37 tests named Given/When/Then, most tied to an issue (`[WorkItem(1126)]`, "Regression: Issue #996"). A pausable dispatcher lets a test hold the UI thread and show that Clear does not need it (`FilterViewModelTests.cs:818-845`, `:1071-1105`). A deferred dispatcher specifies the queued failed-read path (`:194-242`). Eleven tests pin the zip copy's lifetime (U3).
- **Messages are moving behind the dialog service.** Eleven now go through `IDialogBoxService.ShowMessage`, "so a test that finds nothing gets no message box, which would wait for a click forever" (`:1873`). 2.x sent none through it (but see U6a-5).

## Findings

### U6a-1. "Is anything running?" is seven flags kept in step by hand, and `CanOpenLogFile` is the lock

- **Priority:** Medium
- **Kind:** Maintainability
- **New in 3.0?** Existing weakness, about doubled by 3.0
- **Evidence:**
  - **Seven flags.** `CanOpenLogFile`, `IsProcessingLongOperation` (the progress bar), `IsFilterToolboxEnabled`, `IsFilterInProgress` with `_concurrentFilterCount`, `_backgroundCancellation`, `AreInsightsReady` and `IsLogFileOpen` (`FilterViewModel.cs:125`, `:148`, `:287-291`, `:338-342`, `:448`). `IsMenuEnabled` and `IsDashboardEnabled` derive from them (`:278-300`).
  - **Written by hand in eight methods:**
    - `OpenCompressedAsync` (`:611`) and `ChooseStorageAsync` (`:704`, `:716`);
    - `OpenAsync` (`:771-774`, `:935-942`, `:990-991`);
    - `Reload` (`:1079-1081`, `:1133-1138`) and `ClearRecords` (`:1580-1582`, `:1621-1626`);
    - `AnalyzeInBackground` (`:2258-2259`) and `RunInBackground` (`:2300-2311`, `:2321`, `:2356-2365`);
    - `FilterAsynchronously` (`:2534`, `:2544-2548`, `:2591`, `:2631-2635`).

    `CanOpenLogFile` is written 15 times (2.x: 8), `IsProcessingLongOperation` 14 times (7), `IsFilterToolboxEnabled` 9 times (5).
  - **The name says one thing; the code means another.** `CanOpenLogFile` means "nothing is running". It gates a row click (`:1231`), a key (`:1265`) and a dropped file (`:510`, `:1296`). Through `IsMenuEnabled` (`:283`) it gates background work (`:2290`), the failed-read handler (`:1498`), insight navigation (`:2089`) and almost every command (`FilterViewModel.Commands.cs`).
  - **The gate is handed between methods by remark.** Open As closes it, and "the open that follows takes over the menus, and gives them back" (`:699-700`). If the zip's copy then fails to open, `OpenCompressedAsync` reopens it (`:610-611`).
  - **The operations differ.**
    - Open and background work raise `IsMenuEnabled` and ask WPF to requery the commands (`:940-941`, `:2364-2365`). Reload, Clear and a filter do not (`:1133-1138`, `:1621-1626`, `:2631-2635`).
    - Every operation closes the gate on the UI thread before its work starts, except a filter. A filter closes it from the worker, after `Task.Run` has started (`:2518`, `:2544-2548`), so in principle another command can start in that window. Not reproduced.
  - **Waiting for "idle" is polling.** The failed-read handler retries every 500 ms while the gate is closed (`:1497-1502`). The tests poll four different flags as "done" (23 `WaitUntil` calls, for example `FilterViewModelTests.cs:306`, `:532`, `:535`, `:551`), because Reload and a filter return `void`, while Open, Clear and background work return a `Task`.
  - **The gate also guards `_engine`.** Open replaces `_engine` and `_rowSelection` on a worker thread (`:842`, `:846`). The UI thread reads both with no lock and no `volatile`, which is safe only because the gate keeps the commands out meanwhile. Nothing says so. Only the newer fields state their owning thread (`:124`, `:132`).
- **Impact:** Each new long operation must copy the protocol, after reading it in up to eight places. A slip leaves the menus disabled, or lets two operations run against one engine. Neither would show in the tests: their default dispatcher runs "UI thread" callbacks inline on the calling thread (`Tst/BlueDotBrigade.Weevil.Gui-FeatureTests/Threading/UiDispatcherFake.cs:14-29`). The protocol grew with #996, #1027, #1028 and #1071. The GUI half of #991 needs one gate per log, not one per view-model.
- **Recommendation:** One small GUI type, for example `BusyState`, owned by the view-model and used on the UI thread:
  - `TryBegin(progressDelay)` closes the gate and returns a scope whose `Dispose` restores the flags, raises `IsMenuEnabled` and requeries the commands;
  - `IsIdle` replaces `CanOpenLogFile` in guards, and the bound properties stay as views of it;
  - `WhenIdle(Action)` replaces the 500 ms retry;
  - `Reload` and the filter return their `Task`, so tests await rather than poll.

  `RunInBackground` becomes its main user, and its Sonar complexity (17) falls. Do it after 3.0, before the GUI half of #991. **Complexity:** M.
- **Sceptic:** Not challenged (Medium)

### U6a-2. One class holds the open log's lifetime and one view of it, and the epics grew both halves without a seam

- **Priority:** Medium
- **Kind:** Architecture
- **New in 3.0?** Existing weakness, grown by 3.0
- **Evidence:**
  - **Size.** 2,941 lines, plus 375 lines and 86 commands in `FilterViewModel.Commands.cs`; 29 instance fields and about 108 methods. 2.x had 2,195 lines, 22 fields and about 89 methods. Epic commits churned 1,411 lines of the file. By issue: #1028 753, #1162 155, #996 123, #1064 104, and #1156 (no epic) 94.
  - **The log's half,** one per open log:
    - the zip copy (`:105-110`, `:544-679`) and Open As (`:694-761`);
    - opening, swapping and announcing the engine (`:769-993`);
    - the failed-read generations (`:127-134`, `:1457-1517`);
    - Reload, Save and Exit (`:1069-1141`, `:1307-1315`);
    - insights and the dashboard (`:152`, `:944-986`, `:1413-1427`);
    - the background runner (`:123-125`, `:450-454`, `:2271-2376`).
  - **The view's half:**
    - the filter box and its history (`:115-119`, `:142-144`, `:344-379`, `:1637-1735`, `:2499-2644`);
    - rows and selection (`:111`, `:267-272`, `:1218-1289`, `:2646-2708`);
    - Find (`:121`, `:135-140`, `:1740-1938`) and navigation (`:1940-2225`);
    - bookmarks and regions (`:2742-2940`).
  - **The halves call each other.** The failed-read handler reloads (`:1509`), and Reload drops this view's rows (`:1097-1101`). The open builds this view's `RowSelection` and refreshes its filter box and rows (`:846`, `:886-890`).
  - **Where a part did get a type, it reads well.** `ExtractedLog`, `OpenAsAdvice`, `RowList`, `RowSelection` and `UiThreadUserDialog` each own one thing and are tested alone (U3, U5a).
  - **#991.** Each view has its own filter, results and selection, and all views share the log (`Doc/Plans/991-MultipleViews.md:17-22`). Its GUI comes in the next milestone (`:11`). U4-2 finds the same missing split in Core.
- **Impact:** To change how a log opens, reloads or fails, a maintainer works in a 2,941-line file with all of the view's state in reach, and must first learn the busy protocol (U6a-1). For #991, a second view would either repeat the zip copy, the generations, Open As and the runner, or need this class split first.
- **Recommendation:** Extract along seams that already exist, one at a time, after 3.0. Each step moves code as it is:
  1. the zip half of `OpenCompressedAsync`, with `IsOpenFrom`, `ReplaceExtractedLogAsync`, `DeleteExtractedLogs` and `DeleteFolder`, into an owner of the extracted copies beside `ExtractedLog` (S);
  2. `CreateFailedReadReporter`, `ReportFailedRead`, `OnFailedRead` and the three generation fields into a failed-read handler that is given a Reload callback (S);
  3. `RunInBackground` and the busy state (U6a-1) (M);
  4. the seven `_find*` fields and `FindText` to `FindPreviousElapsedTime` into a small Find view-model (S).

  What remains of the log's half (`OpenAsync`, `Reload`, `SaveMetadata`, `Exit`, the insights) is then the part that #991's views share. Steps 1 to 3 carry over even if the next milestone rebuilds the GUI (`991-MultipleViews.md:11`). **Complexity:** M in total.
- **Sceptic:** Not challenged (Medium)

### U6a-3. F5 still filters on the UI thread; on disk that pass reads the log, and Escape cannot stop it

- **Priority:** Medium
- **Kind:** Architecture
- **New in 3.0?** Existing weakness, which now reads the log
- **Evidence:**
  - **The call.** `Refresh` runs `_engine.Filter.ReApply()` in the command handler (`FilterViewModel.cs:1711-1726`), which F5 is bound to (`Src/BlueDotBrigade.Weevil.Gui/Filter/FilterView.xaml:74`). `UiBoundCommand` runs a handler synchronously (`Gui/Input/UiBoundCommand.cs:102-119`). `ReApply` runs a full filter (`Src/BlueDotBrigade.Weevil.Core/Filter/FilterManager.cs:298-306`). 2.x did the same (2.x `:1136-1141`).
  - **3.0 touched it and left it there.** #1028 added the catch for a failed read (`:1717-1722`, commit d2a74c3f). Its plan lists export, split and Find as the UI-thread work to move (`Doc/Plans/1028-OpenAs.md:47-52`, `:177-187`), and names F5 only among the failed-read handlers (`:155`).
  - **Escape is unreachable.** `AbortFilterCommand` is enabled only by `IsFilterInProgress` or `_backgroundCancellation` (`FilterViewModel.Commands.cs:179`), and `Refresh` sets neither. The UI thread is blocked anyway.
  - **How long.** F5 runs one filter. Linux, warm page cache, OpsHarness medians:
    - the 4 GB log on disk: 0.41 to 3.30 s ([Ops-4gb-main-disk-run2.txt](../Measurements/Ops-4gb-main-disk-run2.txt));
    - the 1 GB log on disk: 0.12 to 0.88 s ([Ops-1gb-main-disk-run2.txt](../Measurements/Ops-1gb-main-disk-run2.txt));
    - the 1 GB log in 2.12.1, in RAM: 3.20 to 4.05 s ([Ops-1gb-2.12.1-run2.txt](../Measurements/Ops-1gb-2.12.1-run2.txt)).

    For the same log, F5 freezes the window for less time than in 2.x. But disk mode invites larger logs: its target is a 20 GB log that "filters in under 10 s" (`Doc/Plans/986-DiskMode.md:12`), and on a network drive each read crosses the network. Weevil's own monitor calls 1 s unresponsive (`Gui/Analysis/UiResponsivenessMonitor.cs:17`).
  - **Copy follows the same pattern.** Ctrl+C reads the text of up to 100,000 records one at a time on the UI thread, while the graph copies the same records in the background (`:1143-1161`, against `:1549-1556`). U2-1 found this, and this review confirms it.
- **Impact:** On a large log on disk, F5 freezes the window for seconds, shows no progress bar and cannot be cancelled. It breaks the UI-responsiveness rule (`.github/instructions/gui.instructions.md`, "UI Thread Safety") through an inherited path that #1028's inventory missed.
- **Recommendation:** Run `Refresh` on the filter's background path, for example a re-apply variant of `FilterAsynchronously` that calls `ReApply` on the worker. It then shows the progress bar, and Escape aborts it. Move Copy into `RunInBackground`, as U2-1 recommends. Before 3.0 if possible. **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U6a-4. SonarCloud's S8949 (11 calls) is not a problem; the two timed waits beside it are

- **Priority:** Low
- **Kind:** Intent
- **New in 3.0?** New design for 8 of the calls (`:591`, `:614`, `:625`, `:649`, `:724`, `:1500` twice, `:1584`); Existing weakness for 3 (`:1086`, `:2151` twice)
- **Evidence:**
  - **What Sonar asks.** Each issue says "Pass the 'this._backgroundCancellation.Token' to this method". It picked that field only because it is the class's one `CancellationTokenSource`. The field belongs to the one `RunInBackground` call in progress, and is null otherwise (`:123-125`, `:2304`, `:2359`).
  - **Each site:**
    - `:591` and `:724` run during an open, when the field is null, so the suggested token would throw;
    - `:614`, `:625` and `:649` delete extracted copies, which must run even after Escape;
    - `:1086` (Reload) and `:1584` (Clear) wrap Core calls that take no token, and a token on `Task.Run` only skips the start;
    - `:1500` (twice) retries a failed read, which an unrelated Escape must not drop;
    - `:2151` (twice) waits before going to an insight's record.
  - **The real smell is the two timed waits.** `:1500` polls every 500 ms until the gate opens (U6a-1). `:2151` guesses that 500 ms is enough for a filter to apply, and no filter is started at all (see Minor). The second is inherited.
  - **Two ways to cancel.** A filter is stopped by `Filter.Abort()` and is given `CancellationToken.None` (`:2555`, as in 2.x). Background work is stopped by a token. `AbortFilter` does both (`:1731-1735`). Open, Reload and Clear cannot be stopped (U1, Minor).
- **Impact:** None from the calls themselves. Following Sonar's advice would skip cleanup and break opens. The 11 issues overstate the file's debt.
- **Recommendation:** Close the 11 issues as "won't fix", citing the field's lifetime, or pass `CancellationToken.None` where cleanup must run, so the code says so. Replace the two timed waits as part of U6a-1. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U6a-5. Two-thirds of the messages still bypass the dialog service, so their paths can't be tested

- **Priority:** Low
- **Kind:** Maintainability
- **New in 3.0?** Existing weakness, reduced by 3.0, with one new instance
- **Evidence:**
  - **The rule, as 3.0 states it.** A message goes "through the dialog service, so a test that finds nothing gets no message box, which would wait for a click forever" (`:1873`, commit 856e369e).
  - **The count.** 11 messages use `_dialogBox.ShowMessage`. 24 call `MessageBox.Show` directly (`:806`, `:1376`, `:1398`, `:1614`, `:1952`, `:1993`, `:2012-2030`, `:2115`, `:2404`, `:2572`, `:2658`, `:2759-2913`). 2.x called it directly 29 times.
  - **One is new in 3.0.** Clear's failure message (`:1614`, #1071, commit 17952a36) was written on 2026-10-23, before the rule.
  - **Untested as a result:** Clear failing, an invalid filter expression (`:2572`), a "Not Found" from navigation (`:2658`), the insight navigation's message (`:2115`), and Remove Comments' confirmation (`:2404`). No test reaches them.
- **Impact:** These paths can only be checked by hand, in the running application.
- **Recommendation:** Send the rest through `IDialogBoxService`. It has `ShowMessage`; add a yes/no question (`Gui/IO/IDialogBoxService.cs:19-48`). **Complexity:** S.
- **Sceptic:** Not challenged (Low)

## Minor

- **Insight navigation never applies its filter (seen in passing; inherited).** It sets `InclusiveFilter` "(this will trigger FilterAsynchronously via the property setter)" (`:2144-2148`), but the setter only stores the text (`:344-359`), and nothing listens for the change. After 500 ms it goes to the first related line in the results already shown (`:2151-2163`). This dates from 2025 (commit 82d91467), and 2.x is the same. Only the busy case is tested (`FilterViewModelTests.cs:478-506`). Read, not run.
- **`VisibleItems` outlived its binding.** The grid binds to `Rows` since #1057. `VisibleItems` is now read by the status count (`:2701`) and by tests, yet its change is still raised by hand (`:2695`), and the remark on `Rows` still defines it by `VisibleItems` (`:269-270`).
- **Dead state, inherited.** `NoInsight` is unused (`:78`, Sonar S1144), and `_tableOfContents` is written but never read (`:150`, `:208`, `:958`, S4487).
- **`FileOpened` is raised on a worker thread** (`:853`), unlike `ResultsChanged`, `RegionsChanged` and `BookmarksChanged`, which are marshalled (`:2429`, `:2458`, `:2487`). Its one handler only sets a flag (`FilterView.xaml.cs:112-115`). Inherited.
- **Plugin detection on the UI thread.** `PluginFactory.Create` loads the third-party plugins and has each one check the log (`Core/PluginFactory.cs:21-39`), before the first `await` in `OpenAsync` (`:781`). The estimate just before it was moved off the UI thread because reading the log "takes a moment on a network drive" (`:723`). Inherited.
- **A failure before the open's `try` leaves the menus disabled.** `OpenAsync` closes the gate (`:771-774`), then catches only `NotSupportedException` around plugin detection (`:779-807`). Any other exception there leaves the progress bar shown. Seen in passing; inherited.
- **Fire-and-forget tasks.** Reload and a filter discard their `Task` (`:1086`, `:2518`). An exception outside their inner catches, for example from `RefreshFilterResults` (`:1127`), is never observed, shown or logged. Inherited.
- **Duplication, inherited.** `OnFilterHistoryChanged` repeats one switch for the two histories (`:1008-1064`, Sonar complexity 20). The six `GoToPrevious…`/`GoToNext…` methods and the four comment and elapsed-time searches differ only in one call and one message (`:1888-1938`, `:2167-2225`).
- **`ClipboardLimit` also limits Paste and Graph** (`:66-69`, `:1187-1193`), which its name does not suggest.
- **Clear's "keep the filter shown" rule lives in the GUI.** Core's Clear resets the filter (`IEngine.cs:55-61`). The GUI shows every kept record, then filters again (`:1606`, `:1627-1631`). 2.x did the same, while Reload keeps the filter in Core. The CLI and PowerShell do not Clear, so nothing is duplicated yet.
- **The tests specify order and flags, not threads.** The default `UiDispatcherFake` runs "UI thread" callbacks inline on the calling thread (`Threading/UiDispatcherFake.cs:14-29`). `WaitUntil` returns silently when its 10 s run out (`FilterViewModelTests.cs:1059-1067`), so a hang shows up as a wrong assertion. F5 and the positive path of insight navigation have no test.
- **SonarCloud's complexity lead.** Of the six methods over 15, one is new: `RunInBackground` (17, `:2287`). `OpenAsync` (35, `:769`) grew from the 2.x method. `OnFilterHistoryChanged` (20), `FindText` (17), `TryExtractSourceTarget` (16) and `FilterAsynchronously` (21) are inherited. So are the regular expressions without a timeout (S6444, `:2060`, `:2071`) and the `GC.Collect` of the diagnostic Force Garbage Collection command (S1215, `:1568`).
- **Doc drift.**
  - No design note describes the GUI's threading: the busy gate, `RunInBackground`, or which thread writes `_engine`. Only code remarks and the dated #1028 plan do.
  - `.github/instructions/gui.instructions.md` says to marshal with `IUiDispatcher.Invoke()`. 3.0 also queues with `BeginInvoke` on purpose (`:1475`, `:1882`), and the instructions don't say when to use which.
  - The same file says "changes that cause delays will be caught in testing". The monitor only logs a warning at run time, at most once a minute (`UiResponsivenessMonitor.cs:17`, `:47-52`), and no test uses it.
  - `Doc/Plans/986-DiskMode.md:34` names `FilterViewModel.OpenAsync(string)` as the open. That overload now skips Open As and serves tests (`:763-766`, U3 Minor); the open is `OpenAsync(string, StorageMode?)` (`:769`).
  - Code comments: `:1084-1085` and `:2513-2515` warn that events "will execute on a background thead" (inherited), and `:2144` (above).

## To Measure in Stage 2

- **F5 on a large log on disk (U6a-3).** Time `Filter.ReApply()` with a regex filter on the 4 GB log on disk, with a warm page cache and after dropping it. That is how long the window freezes. The 20 GB log and a network share are for the owner, on Windows.
- **Copy of 100,000 records on disk (U2-1).** Time the formatting in `ClipboardHelper.CopyRawFromSelected` for 100,000 records selected in the 4 GB log on disk, against the same records read through one `ContentBuffer`, as `CopyWithText` does.
- **Ctrl+S and Exit on the UI thread.** `SaveMetadata` and `Exit` save the sidecar inside the command handler (`:1069-1072`, `:1307-1312`), and a save opens the log to compute byte offsets (`Core/CoreEngine.cs:438-456`). Time a save with 1,000 and 100,000 pinned records. This is U1's sidecar measurement, read as a freeze of the window.
- **Peak memory when opening a second log.** Open builds the new engine while the old one is still held (`:836-842`), as `986-DiskMode.md:34` intends. Open a 1 GB log in RAM, then another 1 GB log in RAM, and compare the peak working set with the Open As estimate shown for the second.
- **Plugin detection on a network share.** Time `PluginFactory.Create` (`:781`) against an SMB path, on the owner's Windows PC.

## Open Questions

- **Will the GUI half of #991 build on `FilterViewModel`,** or replace it with the MaterialDesign GUI the plan mentions (`991-MultipleViews.md:11`)? The answer decides how much of U6a-2 is worth doing first.
- **Should F5 and Copy move off the UI thread before 3.0 ships (U6a-3)?**
- **Is a plugin's own Open As window meant to follow Weevil's?** A log that a plugin handles now shows two windows in a row (`:702-761`, then `:783-793`). Does #1157 replace both?
- **Should Clear keep the filter in Core, as Reload does?** The GUI's second filter after Clear (`:1627-1631`) would then go, and the CLI, PowerShell and tests would get the same behaviour.
