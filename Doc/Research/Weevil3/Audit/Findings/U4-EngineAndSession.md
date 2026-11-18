# U4: Engine Composition and Session State (#990)

Part of the [Weevil 3.0 audit](../Progress.md). This code opens a log and wires an engine over it, keeps the user's state (comments, pins, bookmarks, regions, remarks, filter history) in a session shared across Clear and Reload, and loads and saves that state in the XML sidecar.

> **Status:** Final. Reviewed on 2026-11-17 at `main` e3dee822, by one reviewer covering every lens.

## Summary

#990 delivered its core promise: records hold no state, every value that changes has one home keyed by line number, and Clear narrows the engine in place instead of rebuilding it. The session's stores share one write lock and mostly publish immutable values, which removed a lock per record from filtering and fixed a real 2.x race on the filter history. Each class says what it is for and cites its issue, and the price is size: the engine and its stores grew from about 1,500 to about 3,100 lines, sidecar code aside.

**The main concern is that the sidecar save still trusts the old file for lines outside the view.** So Clear now writes the sidecar first, which `IEngine.Clear` does not say, and fails in a folder Weevil cannot write to, where 2.x's Clear worked (U4-1). Second, **neither the read log nor the view has a type of its own**, so what Reload keeps is decided in five places, the first obstacle #991 will meet (U4-2). No design note describes any of this (U4-3), and `CoreEngineBuilder`, though cohesive around opening a log, does seven jobs in one 148-line method (U4-4). Existing sidecars still load, and the unit has no High finding.

## What Was Examined

Paths drop the `Src/BlueDotBrigade.Weevil.` prefix: `Core/Engine.cs` is `Src/BlueDotBrigade.Weevil.Core/Engine.cs`. Test and document paths are from the repo root.

- Core: `Engine.cs`, `EngineBuilder.cs`, `EngineExtensions.cs`, `CoreEngine.cs`, `CoreEngineBuilder.cs`, `Session.cs`, `BookmarkManager.cs`, `RegionManager.cs`, `Filter/FilterHistory.cs`, `Data/RecordScope.cs`, `Configuration/Sidecar/SidecarManager.cs`, `v2/SidecarLoader.cs`, `v1/LogMetadataLoader.cs` (diff). Callers: `Analysis/AnalysisManager.cs` (bulk changes), `Filter/FilterManager.cs` (history, elapsed times), `SelectionManager.cs` (Paste).
- Common: `IEngine.cs`, `ICoreEngine.cs`, `IEngineBuilder.cs`; `Data/AnnotationStore.cs`, `IAnnotations.cs`, `FlagSet.cs`, `ElapsedTimes.cs`, `RecordState.cs`, `IRecordState.cs`; `Navigation/TableOfContents.cs`, `Region.cs`, `Bookmark.cs`; `Common.csproj` (`InternalsVisibleTo`).
- GUI callers: `Gui/Filter/FilterViewModel.cs` (Open, Reload, ClearRecords, Exit).
- 2.x: `Core/Engine.cs`, `CoreEngine.cs`, `CoreEngineBuilder.cs`, `BookmarkManager.cs`, `Filter/FilterManager.cs`, `v2/SidecarLoader.cs`; `Common/Data/Metadata.cs`; `Gui/Filter/FilterViewModel.cs` (Reload, Clear).
- Documents: `Doc/Plans/990-ImmutableCore.md`; `Doc/Research/Weevil3/MutableStateInCore.md`, `EngineApi.md`; all of `Doc/Design/`; `Doc/README.md`; the core and common instructions.
- Tests: `EngineTests` (Clear, Reload, session), `AnnotationsShould`, `SidecarDataShould`, `AnnotationStoreTests`, `FlagSetTests`, `SidecarLoaderTests`, `SidecarManagerTests`, the two `[WorkItem(1126)]` tests; the CI disk run; the checked-in sidecars.
- SonarCloud's leads for U4. History: `git log baa70a9d..HEAD` for each file above.

## Strengths

- **Records hold no state, and every value that changes has one home.**
  - 2.x's `Metadata` (181 lines of setters on every record) is gone.
  - Comments and pins live in the session by line number. Flags live in the view, one bit per line. Elapsed times live in the view and are calculated when read (`Common/Data/AnnotationStore.cs:8-14`, `FlagSet.cs:7-12`, `ElapsedTimes.cs:6-17`).
  - Memory kept per record fell from 396 to 348 bytes (`Doc/Plans/990-ImmutableCore.md:89`).
- **One write lock, visible where it is created.** `Session` creates the lock and hands it to every store (`Core/Session.cs:20-32`), and each store's remark says so (`Core/BookmarkManager.cs:8-10`, `RegionManager.cs:10-12`, `Filter/FilterHistory.cs:12-14`).
  - Bookmarks, regions and the filter history publish immutable arrays, so filters read them without a lock.
  - 2.x took a lock for every bookmark lookup, which a filter does per record (`Releases/2.x`, `Core/BookmarkManager.cs:89-91`).
  - 2.x's filter history was a `List` that the filter thread changed while the GUI and the save read it (2.x `Core/Filter/FilterManager.cs:37`, `:84`). Callers now get an `IReadOnlyList<string>` that stays the same object (`FilterHistory.cs:9-17`).
- **A bulk change takes one lock and raises one event.**
  - The event is raised after the lock is released, with the writer as `source`, so a row can ignore its own edit (`Common/Data/IAnnotations.cs:11-68`; `AnnotationStore.cs:100-120`, `:171-242`).
  - The lines to change are found in parallel before the lock is taken (`Core/Analysis/AnalysisManager.cs:30-39`, `:208-215`; `Core/SelectionManager.cs:636-643`).
  - `AppendComment` is one locked step, where 2.x read the comment and then wrote it (`AnnotationStore.cs:81-97`).
- **The comment rule has one home and a test for each case.** Adding a comment pins a record, and removing it unpins the record (`AnnotationStore.cs:122-147`; `Tst/BlueDotBrigade.Weevil.Common-UnitTests/Data/AnnotationStoreTests.cs:10-69`).
- **Clear no longer rebuilds the engine.**
  - It narrows the scope in place (`Core/CoreEngine.cs:359-421`; `Core/Data/RecordScope.cs:24-27`), so subscribers survive.
  - Comments explain the order of its steps: the selection is set before the results (#1015, `:386-388`), and the work is split so a Debug build frees its temporaries (`:361-363`).
  - The 2.x builder's second mode, building an engine from another, is gone.
- **Reload fails safely.**
  - It releases the old records before reading the log again (#1126).
  - A failed read leaves an engine for the same log with the same filter, so a retry works (`Core/Engine.cs:195-265`).
  - `IEngine.Reload` documents both (`Common/IEngine.cs:63-72`), and tests pin the retry on disk and after a second change (`Tst/BlueDotBrigade.Weevil.Core-UnitTests/EngineTests.cs:253-361`).
- **Sidecars stay compatible by construction.**
  - 3.0 changed only the loader and the manager. The serialized classes in `Configuration/Sidecar/v2` and schema version 4.2 (`v2/SidecarLoader.cs:150`) are as in 2.x.
  - The tests open eleven checked-in sidecars written by earlier versions (`Tst/**/.Daten/**/*.log.xml`).
  - Every entry now loads, including lines not in memory (`SidecarLoader.cs:61-67`), which fixed #1004 (`Tst/BlueDotBrigade.Weevil.Core-FunctionalTests/SidecarDataShould.cs:80-131`).
  - The save now records each line's byte offset from the line index, and it copies a location rather than changing it (`Locate`, `SidecarLoader.cs:180-192`).
- **Tests read as specifications.**
  - `EngineTests` names state the behaviour, cite the bug with `[WorkItem]`, and open with one sentence of why.
  - Clear and Reload are tested in both modes, by `DataRow` (`EngineTests.cs:722-757`) or by CI's disk run (`.github/workflows/dotnet.yml:59-65`).
  - Flags across Clear and Reload have their own tests (`Tst/BlueDotBrigade.Weevil.Core-FunctionalTests/AnnotationsShould.cs:43`, `:55`, `:83`, `:98`).
- **Room for #991.** `RecordState` is the seam the plan wanted. A new view is a new `RecordState` over the same `AnnotationStore`, with its own `FlagSet` (`Common/Data/RecordState.cs:24-28`). Every comment and pin event carries its source.

## Findings

### U4-1. The sidecar save trusts the old file for lines outside the view, so Clear saves first, and Clear now fails where the sidecar cannot be written

- **Priority:** Medium
- **Kind:** Correctness
- **New in 3.0?** Regression (Clear's save). The merge rule is an existing weakness that 3.0 kept.
- **Evidence:**
  - **Clear saves.** `Core/Engine.cs:176-180` saves the sidecar before every Clear (e5980d7e, #1004, 2026-10-20). That fix came a week before the session existed. 2.x's Clear rebuilt the engine and wrote nothing (`Releases/2.x`, `Core/Engine.cs:142-145`).
  - **The contract does not say so.** `Common/IEngine.cs:54-61` describes Clear without the save. Only a GUI remark mentions it (`Gui/Filter/FilterViewModel.cs:1575`).
  - **A failed save stops the Clear.**
    - `SidecarManager.Save` skips only the temporary folder and catches nothing (`Core/Configuration/Sidecar/SidecarManager.cs:140-161`). The XML is written with `FileMode.Create` (`Common/Runtime/Serialization/TypeFactory.cs:31`).
    - In a folder Weevil cannot write to, Clear throws before it narrows, and the GUI shows "Unable to clear the records." (`FilterViewModel.cs:1611`).
    - Reload and opening the next log also save first, in both 2.x and 3.0 (`FilterViewModel.cs:1093`, `:820`; 2.x `:813`, `:584`). So read-only folders were already poorly supported, and Clear now joins them.
  - **Why the save is needed.** `PackSidecar` keeps the old file's entry for every line outside the view's scope. It adds only lines inside the scope: "annotations on cleared lines are not saved" (`Core/Configuration/Sidecar/v2/SidecarLoader.cs:214-246`). Without the save first, an unsaved edit on a cleared line would be lost (#1004).
  - **The plan meant the store to be the source.** The plan says: "The store loads every sidecar entry, including lines not in memory …, so the sidecar can be saved from the store without losing them" (`Doc/Plans/990-ImmutableCore.md:60`). The store does load every entry (`SidecarLoader.cs:61-67`), but the save does not use it.
  - **The rule leaks into other contracts.**
    - `IEngine.Reload` tells callers to save first, because a save after a failed read keeps only what the file holds (`IEngine.cs:68-69`; the empty-scope branch, `SidecarLoader.cs:206-213`).
    - Unpin All touches only records in memory, "as before". Remove All Flags touches every line, because a cleared line's flag "would come back on Reload" (`Core/Analysis/AnalysisManager.cs:30-47`). Under the current merge, unpinning a cleared line in the store would never reach the file.
  - **Each Clear also pays for a save.** It copies the sidecar to a backup and rewrites it (`SidecarManager.cs:151-155`, `:163-174`). It also reopens the log to find the byte offset of every annotated line (`Core/CoreEngine.cs:441-454`).
  - **Tests pin the behaviour, not the mechanism.** The three #1004 tests save explicitly after the Clear (`SidecarDataShould.cs:80-131`), so they would also pass with a save from the store. No test clears a log whose sidecar cannot be written.
- **Impact:**
  - Users: Clear stops working in a read-only folder, such as a shared evidence folder, which 2.x handled.
  - Maintainers: a public method writes a file without saying so, and the save bends the stores' rules for cleared lines.
  - #991: with two views, saving from a view that cleared a line would drop an edit made to that line in the other view.
- **Recommendation:**
  - Save annotations from the store: every line it holds, plus the old file's entries for lines it never loaded, if that case exists (see Open Questions).
  - Then remove the save from `Engine.Clear` and the "save first" caveat from `IEngine.Reload`.
  - If that has to wait, say on `IEngine.Clear` that it saves, and let a failed save log a warning instead of stopping the Clear.
  - Add a test that clears a log in a folder the sidecar cannot be written to.

  **Complexity:** S to M.
- **Sceptic:** Not challenged (Medium)

### U4-2. The session holds the user's state but not the log, and a view has no type, so what Reload keeps is decided in five places

- **Priority:** Medium
- **Kind:** Architecture
- **New in 3.0?** New design
- **Evidence:**
  - **The plan's model.** The session "holds the log, annotations, bookmarks, regions and the sidecar". An `IEngine` is one view of it, with its own scope, filter and selection (`Doc/Plans/990-ImmutableCore.md:22`; `Doc/Research/Weevil3/EngineApi.md:95-101`, `:175-182`).
  - **The code's model.**
    - `Session` holds only the user's state: comments and pins, bookmarks, regions, remarks, the table of contents and the filter histories (`Core/Session.cs:18-72`).
    - The log as read sits in `CoreEngine`: records, line index, source, sidecar manager, plugin extension, context, encoding and file metrics. The view's filter, selection and navigation sit beside it (`Core/CoreEngine.cs:24-54`).
  - **Three lifetimes, no names.**
    - `Engine` lives as long as the log is open.
    - `CoreEngine` lives for one read of the log, since Reload builds another.
    - `RecordState` lives as long as the log, yet it holds view state: the flags and the elapsed times of the current results (`Common/Data/RecordState.cs:16`, `:38-47`). Its remarks say it "only forwards to the stores" (`:11`), and decision 7 says it "adds no state" (`990-ImmutableCore.md:32`).
    - The elapsed times the last filter compared live in `FilterManager`, which has the shorter lifetime (`Core/Filter/FilterManager.cs:105`, `:110`, `:353`, `:359`).
  - **What Reload keeps, and where that is decided:**
    - storage mode, selection, filter type and criteria: `Engine.Release` (`Core/Engine.cs:249-265`);
    - the elapsed times an `@Elapsed` filter compared, only for the same log: `Engine.Reload` and `CoreEngine.HasReadSameLogAs` (`Engine.cs:199-207`; `CoreEngine.cs:204-209`);
    - session, flags and telemetry recorder: `CoreEngineBuilder.UsingStateOf` (`Core/CoreEngineBuilder.cs:167-179`), plus `isKeepingSession` branches in `BuildEngine` (`:351-353`, `:403`, `:423`);
    - the old log's identity while Reload reads: `CoreEngine.WithoutRecords` passes 11 of the constructor's 15 arguments straight through. The empty engine it returns carries the old metrics, line index and compared elapsed times to the new one (`CoreEngine.cs:218-256`; read back at `Engine.cs:200-207`);
    - not kept: the range, start line, region or section the log was opened with, and a context the caller gave (`Engine.cs:254-257`). 2.x did the same.
  - **The release is implicit.**
    - The shared `RecordState.ElapsedTimes` refers to the old results until a filter replaces it (`FilterManager.cs:359`).
    - Reload frees the old records before it parses (#1126) only because the empty engine runs a filter over no records, which overwrites that reference (`CoreEngine.cs:167`, `:253`). The comments on `WithoutRecords` mention only the compared elapsed times (`:211-222`).
    - Both #1126 tests check only after Reload returns (`EngineTests.cs:207-219`; `Tst/BlueDotBrigade.Weevil.Gui-FeatureTests/Filter/FilterViewModelTests.cs:543-558`). By then the new engine has replaced the elapsed times anyway, so neither test would notice the old records surviving the parse.
  - **Churn.** Reload's carry-over needed #1126 (three commits), #1185 and #1224 (two commits on 2026-11-15), the last two after the epic closed.
- **Impact:**
  - To change what Reload keeps, a maintainer reads and edits up to four classes and must know which object outlives which.
  - For #991, `CreateView()` would have to copy the log's state out of a `CoreEngine`, as `WithoutRecords` does. Reload would have to rebuild each view's `CoreEngine`, since each holds the log.
  - The save walks one view's scope (U4-1).
- **Recommendation:** Before #991, give the two lifetimes types.
  - A read of the log becomes one immutable object that the session holds and Reload replaces: records, line index, source, sidecar manager, extension, context and metrics.
  - A view is built over the session: scope, filter with both sets of elapsed times, selection, navigation and flags. `Engine` keeps one view.
  - Reload replaces the session's log, then narrows each view to it and applies its filter again.
  - `UsingStateOf`, `WithoutRecords` and `isKeepingSession` then go.

  Not before 3.0: behaviour does not change. **Complexity:** M to L.
- **Sceptic:** Not challenged (Medium)

### U4-3. No design note describes the engine and its session; the finished plan is the only description, and it differs from the code

- **Priority:** Medium
- **Kind:** Intent
- **New in 3.0?** New design
- **Evidence:**
  - **Nothing in `Doc/Design`.** It has notes for disk mode, the record table, insight reports, telemetry and versioning. None covers the engine, the session or the view.
  - **The plan is marked Done** (`Doc/Plans/990-ImmutableCore.md:3`). `Doc/README.md:7` and `:45` say a finished plan's result is described in `Design/`.
  - **The plan differs from the code on four points:**
    - the session "holds the log … and the sidecar" (`990-ImmutableCore.md:22`); it holds neither (U4-2);
    - `RecordState` "adds no state" (`:32`); it keeps the view's elapsed times (`Common/Data/RecordState.cs:16`, `:43-47`);
    - the sidecar "can be saved from the store" (`:60`); it is saved from the view's scope and the old file (U4-1);
    - "each store publishes values that do not change" (`:39`, repeated in `Core/Session.cs:14-15`); the comment and pin store and the flags change their arrays in place (`Common/Data/AnnotationStore.cs:288-295`; `FlagSet.cs:118-120`).
  - **The research design differs too, by a later decision.** `EngineApi.md:26` keeps the filter "the same object for the view's lifetime". The code replaces the filter on Reload and forwards its events instead (`Common/IEngine.cs:8-12`; plan step 1, `990-ImmutableCore.md:49-53`). Research is dated, so this is expected, but no current document says which design won.
  - **The instructions say nothing about session state.** `core.instructions.md` has only the bulk-change rule (`.github/instructions/core.instructions.md:57`). It does not cover the lock, whether new state belongs to the session or the view, events after the lock, or what Clear and Reload keep.
  - **What Clear and Reload keep is spread out.** It is in `IEngine`'s remarks (`IEngine.cs:36-72`) and in tests in three classes across two projects (`EngineTests`, `AnnotationsShould`, `SidecarDataShould`). The remarks leave out Clear's save (U4-1), and that Reload reads the whole log whatever part was opened.
- **Impact:** The next person to add state, or to start #991, must rebuild the model from a dated plan that is wrong on four points, and from remarks that repeat one of them.
- **Recommendation:**
  - Write `Doc/Design/EngineAndSession.md`, about a page. Give a table of every piece of state, with its owner and its lifetime (the open log, one read, or a view). Add the write lock and each store's read rule, the events and their threads, what Clear and Reload keep, and the sidecar save rule.
  - Fix `Session.cs:14-15`.
  - Point to the note from `core.instructions.md`.

  **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U4-4. `CoreEngineBuilder` is the whole open procedure in one 148-line method, and each option is written three times

- **Priority:** Low
- **Kind:** Maintainability
- **New in 3.0?** New design, on a shape inherited from 2.x
- **Evidence:**
  - **What `BuildEngine` does** (`Core/CoreEngineBuilder.cs:306-453`):
    - detects the encoding (`:334`) and picks the plugin (`:341-342`);
    - loads the line index (`:348`);
    - creates the session and flags, or reuses them on Reload (`:351-353`);
    - resolves the start line, range, region or section (`:355-356`, `:488-557`);
    - reuses the saved table, or parses the log and saves the table (`:358-397`; see U1-1);
    - loads the sidecar and the plugin's default histories (`:401-432`);
    - resolves the context (`:435`, `:587-608`).

    `Build` adds the forced collection and the open's log entry (`:283-300`, `:464-483`). Nineteen locals are declared at the top, as in 2.x (`:308-332`).
  - **Reload goes through it.** With `UsingStateOf`, `BuildEngine` still reads the sidecar and the plugin's default histories, then keeps only the context (`:401-432`).
  - **Opening a region or section reads the sidecar twice.** The first read finds the range (`:526`, `:547`), each time a full `SidecarManager.Load` (`Core/Configuration/Sidecar/SidecarManager.cs:124-138`). The second fills the session (`:401`).
  - **Each option exists three times.** It is declared in `IEngineBuilder`, forwarded by `EngineBuilder` and implemented in `CoreEngineBuilder` (`Common/IEngineBuilder.cs:11-65`; `Core/EngineBuilder.cs:21-69`; `CoreEngineBuilder.cs:110-237`). The region and section remarks are copied word for word (`IEngineBuilder.cs:31-52`; `CoreEngineBuilder.cs:196-226`). 2.x had the same shape; 3.0 added three options to it.
  - **Size.** The builder is 374 lines in 2.x, including Clear's path, and 610 lines in 3.0 without it. Clear moved to `CoreEngine.Narrow`.
  - **What is good.**
    - The builder is cohesive around one use case, and every step is commented with its issue.
    - `Build` and `TryReopen` are separate methods, so a collection can free their temporaries (`:240-248`, `:302-305`).
    - Errors for an unknown region or section list the names available (`:531-535`, `:550-553`).
- **Impact:** Every change to opening, such as the rest of #1157, U1-1's reuse rules or #991, edits `BuildEngine`. A reader has to keep seven steps and a Reload flag in mind at once.
- **Recommendation:**
  - Split `BuildEngine` into its named steps: resolve the line range (reading the sidecar once), read the records (U1-1's single home), load the session's state (first open only), and resolve the context.
  - Let Reload pass what it keeps as one value (U4-2).
  - Keep the option docs on `IEngineBuilder` only, and use `<inheritdoc/>` elsewhere.

  **Complexity:** M.
- **Sceptic:** Not challenged (Low)

### U4-5. Every session store writes under the one lock, but reads and change events differ by store, and the remarks hide it

- **Priority:** Low
- **Kind:** Intent
- **New in 3.0?** New design
- **Evidence:**
  - **Writes are consistent.** `Session`'s constructor hands its lock to every store (`Core/Session.cs:25-32`), and each store takes it for every write (for example `Core/BookmarkManager.cs:43`, `Core/RegionManager.cs:105`, `Core/Filter/FilterHistory.cs:58`, `Common/Data/AnnotationStore.cs:70`, `Session.cs:55`).
  - **Reads follow three patterns.**
    - Bookmarks, regions and the filter history publish a new `ImmutableArray` (`BookmarkManager.cs:153-156`; `RegionManager.cs:250-253`; `FilterHistory.cs:86-89`). Remarks and the table of contents are replaced whole (`Session.cs:50-72`).
    - The comment and pin store changes a byte array and a `ConcurrentDictionary` in place (`AnnotationStore.cs:258-296`). Each line is safe to read, but the comment text and its bit change in two steps, so a reader can briefly see one without the other.
    - The flags belong to the view. They have their own lock and change in place (`Common/Data/FlagSet.cs:17`, `:76-88`, `:118-120`). `FlagOnly` clears every word before it sets the new flags.
  - **The remarks blur this.** `Session.cs:14-15` says every store "publishes values that do not change". `FlagSet.cs:11` and `AnnotationStore.cs:12` use the same words, "Writes are serialized by one lock", about two different locks.
  - **Change events follow three patterns.**
    - The comment and pin store raises `Changed` itself, after the lock (`AnnotationStore.cs:47`, `:75-78`).
    - Filter-history changes are raised by the view's filter, from what `FilterHistory.Add` returns (`FilterHistory.cs:56`; `Core/Filter/FilterManager.cs:243-250`).
    - Bookmarks, regions, remarks and the table of contents raise nothing.
- **Impact:**
  - Today the cost is small: the GUI refreshes after its own commands, and no reader needs a consistent view across stores.
  - A maintainer adding a store has three models to choose from and no rule.
  - For #991, a second view would hear about comments and pins, but not about a bookmark, region or filter-history entry added in the first.
- **Recommendation:**
  - In `Session`'s remarks, say which stores publish snapshots and which change in place, and why: copying one byte per line on every write would cost too much.
  - In `FlagSet`, say that its lock belongs to the view.
  - When #991 starts, have the stores raise one session change event after the lock, as the comment store does.

  **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U4-6. The session's stores are public types in Common, so plugins can reach their load and bulk methods

- **Priority:** Low
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **Public, but used only by Core.**
    - `AnnotationStore`, `FlagSet` and `RecordState` are public in Common (`Common/Data/AnnotationStore.cs:15`, `FlagSet.cs:13`, `RecordState.cs:14`).
    - Only Core creates or calls them: `Session`, `CoreEngineBuilder`, `CoreEngine`, the sidecar loaders, `AnalysisManager`, `FilterManager`, `SelectionManager` and three navigators.
    - The GUI, CLI, PowerShell and plugins use only `IAnnotations` and `IRecordState`.
  - **What that exposes.**
    - `AnnotationStore.Load` sets a comment and pin without the comment rule and without an event (`AnnotationStore.cs:244-255`). Its constructor accepts any object as the write lock (`:39-45`).
    - `RecordState` exposes the concrete stores and a settable `ElapsedTimes` (`RecordState.cs:30-47`).
    - An analyzer receives an `IRecordState` (`Common/Analysis/IRecordAnalyzer.cs:35`) that is a `RecordState`, so a cast reaches `Flags.Clear()` or `Annotations.Load`.
  - **Why they are in Common.** Common's `InternalsVisibleTo` is Debug-only and names only its own unit tests (`Common/BlueDotBrigade.Weevil.Common.csproj:54-58`). A type that Core needs must therefore be public there. Decision 7 makes `IRecordState` public, but says nothing about its implementations (`Doc/Plans/990-ImmutableCore.md:32`).
- **Impact:** Plugin authors see methods that bypass the store's rules, and any change to the stores' shape is a change to public API.
- **Recommendation:** Move `AnnotationStore`, `FlagSet` and `RecordState` to Core as internal types, beside `Session`, and move their tests to Core's unit tests. Common keeps `IAnnotations`, `IRecordState` and `ElapsedTimes`. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U4-7. `IEngine` does not say which members belong to the log and which to the view, and one of its events has no caller

- **Priority:** Low
- **Kind:** Intent
- **New in 3.0?** New design, on the 2.x `ICoreEngine`
- **Evidence:**
  - **One flat list.** `IEngine` adds seven members (`Common/IEngine.cs:13-72`) to the 2.x `ICoreEngine` (`Common/ICoreEngine.cs:9-54`).
    - The log's members and the view's are mixed. The log's are annotations, bookmarks, regions, remarks, context, Save and Reload. The view's are records, filter, selector, navigation, analyzer, record state and Clear.
    - The research design grouped them as "this view only" and "shared by every view" (`Doc/Research/Weevil3/EngineApi.md:23-43`).
  - **Lifetimes are given member by member.**
    - The remarks say Reload replaces the filter, selector, navigation, analyzer and records (`IEngine.cs:8-12`), and that `Annotations` and `RecordState` stay (`:36-47`).
    - `Regions` and `Bookmarks` also stay, because they come from the session (`Core/CoreEngine.cs:299-301`), but nothing says so.
  - **An event with no caller.**
    - `FilterResultsChanged` (`IEngine.cs:15-24`) has no subscriber outside two tests (`Tst/BlueDotBrigade.Weevil.Core-UnitTests/EngineTests.cs:46`, `:135`). The GUI never subscribed to it, nor to the filter's own event in 2.x.
    - Yet `Engine.Clear` unsubscribes and subscribes again around every Clear, so that this event does not report Clear's reset (`Core/Engine.cs:182-192`).
- **Impact:** A caller learns member by member, from remarks, what survives Reload. For #991, `CreateView()` returns another `IEngine`, and callers will need to know which members the views share.
- **Recommendation:**
  - Order and document `IEngine`'s members in two groups, the log's and the view's, and say once what Reload replaces.
  - Remove `FilterResultsChanged` and Clear's re-subscription, or name the caller they are kept for.

  **Complexity:** S.
- **Sceptic:** Not challenged (Low)

## Minor

- **Stale summary.** `SidecarManager.Load`'s summary still describes a method that 3.0 removed: "Determines if the sidecar file path is in a temporary directory." (`Core/Configuration/Sidecar/SidecarManager.cs:26-28`).
- **Published values are immutable only by convention.**
  - `Section` and `RelatesTo` have public setters (`Common/Navigation/Section.cs:33-45`; `Common/Data/RelatesTo.cs:16-19`).
  - `Region` and `Bookmark` keep the `RelatesTo` object they are given (`Common/Region.cs:42-47`; `Common/Bookmark.cs:43-48`).
  - Nothing changes these objects today, and `PackSidecar` takes copies on purpose (`SidecarLoader.cs:180-192`).
- **Mutable context.** `ContextDictionary` is a `Dictionary` that the engine, the filter and plugins share (`Common/ContextDictionary.cs:17`). Inherited from 2.x.
- **Reading the history by index.** `FilterHistory.Count` and its indexer each read the array again (`Core/Filter/FilterHistory.cs:33-35`). A loop by index can fail if `Load` shrinks the history between the two reads. Enumerating it is safe.
- **No concurrency test for the comment store.** No test reads the comment and pin store while another thread writes to it. `FlagSet` has such a test (`Tst/BlueDotBrigade.Weevil.Common-UnitTests/Data/FlagSetTests.cs:77-84`).
- **The static constructor opens a log.** The first use of `Engine` creates `EmptyFile.log` in the user's data folder and opens it as any log is opened, forced collection included (`Core/Engine.cs:34-75`; `CoreEngineBuilder.cs:295`). The surrogate pattern is inherited from 2.x.
- **Leftovers in the builder, inherited from 2.x.**
  - `LatestVersion` is unused (`CoreEngineBuilder.cs:34`).
  - `UsingRange` logs "a maximum record limit" (`:189-191`).
  - `UsingLimit`'s message says "greater than or equal to zero" for a check of `> 0` (`:151-154`).
  - An empty `#region Event Handlers` sits in `CoreEngine.cs:431-432`.
- **The v1 and v2 loaders differ.** A v1 label is applied through `SetComment`, so it pins its record, and only records in memory get it (`Core/Configuration/Sidecar/v1/LogMetadataLoader.cs:47-66`). A v2 entry loads exactly, for every line (`v2/SidecarLoader.cs:61-67`). Both behaviours are kept from 2.x.
- **SonarCloud leads,** all inherited from 2.x:
  - `SidecarLoader.cs:57` (cognitive complexity 23) is `Load`'s nested null checks. 3.0 lowered its complexity by removing a lookup per record.
  - `:62` and `:70` (possible null `_sidecar`) are false positives: `TryLoad` always sets `_sidecar` (`:28-55`), and the `?.` at `:59` is what makes Sonar suspect it. The real inconsistency is that `CommonData` is dereferenced at `:62` and `:70`, and only then checked for null at `:75`.
  - `:194` (cognitive complexity 36) is `PackSidecar`: the 2.x merge, with records replaced by line numbers. See U4-1 for the merge itself.

## To Measure in Stage 2

- **Clear in RAM, including its sidecar save (U4-1).** On the 4 GB log in RAM, run Clear Before Selected with 0, 1,000 and 100,000 pinned records. Record the total time and the time of the save alone. Compare with the same Clear on 2.12.1, which writes nothing. U1 covers the save on its own, and U2 covers Clear's memory.
- **The pause of the forced collections.** Clear and every open or Reload run a blocking, compacting, aggressive collection (`Core/CoreEngine.cs:368`; `Core/CoreEngineBuilder.cs:295`), which pauses every thread, the UI's included. Record `GC.GetTotalPauseDuration()` around Clear and around an open of the 4 GB log in RAM.
- **Reload's time and peak working set, against 2.12.1.** #1126 says Reload no longer holds two copies of the log.
  - On the 1 GB and 4 GB logs in RAM, compare the peak during Reload with the peak during a first open, on `main` and on 2.12.1.
  - On `main`, split Reload's time between the parse and its two filter passes: the engine's default filter, then the filter applied again (`CoreEngine.cs:167`; `Engine.cs:215`).
- **Are the old records freed before Reload parses? (U4-2)** While Reload parses the 1 GB log, check from another thread a weak reference to the old record table, or sample the managed heap. The existing tests look only after Reload returns.

## Open Questions

- **Should the store be the sidecar's source of truth, as step 3 of the plan intended?** Is there a reason for the current rule, under which the file wins for lines outside the view? Two Weevil instances saving the same sidecar would be one.
- **Should a sidecar that cannot be written stop Clear, Reload or opening the next log?** In 2.x it stopped Reload and the next open, but not Clear.
- **Should Reload keep how the log was opened (range, region, section, a caller's context)?** The #1157 plan covers what the GUI shows after a partial open (`Doc/Plans/1157-OpenAsDialog.md:63`). `IEngine.Reload` says only that it reads the log again. Reloading in RAM a large log that was opened in part could need far more memory than the open did.
- **Is `IEngine.FilterResultsChanged` kept for a planned caller?**
- **For #991:** will the read log move into the session, and will Reload reload every view?
