# U6b: The Grid Over Core (#990)

Part of the [Weevil 3.0 audit](../Progress.md). This code shows the filter results in the WPF grid without a record object per row: it creates a row view-model only for a row that WPF asks for, turns clicks and keys into changes to Core's selection, shows Core's pins, comments and selection on the rows, and reports that selection to screen readers.

> **Status:** Final. Reviewed on 2026-11-17 at `main` e3dee822, by one reviewer covering every lens.

## Summary

The grid is one of the clearest parts of 3.0. Four small classes replace 2.x's record-bound rows and WPF's own selection. `RowList` is the results as WPF sees them. `RowViewModel` is one row, reading and writing the annotation store. `RowSelection` turns clicks and keys into Core calls and keeps only the caret. `RecordListView` and its two automation peers form a list that never selects a row and tells screen readers what Core selected. Each class says what it is for and cites its issue. The split is plain: Core owns the selection, the anchor, annotations and elapsed times; the GUI owns the caret, the row cache and when rows refresh.

Against 2.x, which kept WPF's selection and Core's in step through code-behind and selected every record again after each filter, there is now one selection, and a large selection no longer freezes a filter. The design rests on a proof of concept measured at 100 million rows, and the cost of a screen of rows on disk was measured before a cache was ruled out.

**The concerns are about guarding and documenting the design, not the code.** The property that lets the grid scale, that WPF never walks the list, was counted in the proof of concept but is neither tested nor detected in the product, and 3.0 added paths the proof of concept never ran (U6b-1). A row's state reaches the screen by three routes. The selection's route is a call the view-model must remember, and the scenarios cannot tell when it is missed (U6b-2). No design note describes the grid, and the GUI instructions contradict three of its deliberate choices (U6b-3). The unit has no High finding.

## What Was Examined

- **GUI:** `Filter/RowList.cs`, `RowSelection.cs`, `RowViewModel.cs`, `FilterView.xaml` (the grid, row template and triggers), `FilterView.xaml.cs`; `Controls/RecordListView.cs`, `RecordListViewItem.cs`, `RowPressedEventArgs.cs`, `RecordListViewAutomationPeer.cs`, `RecordItemAutomationPeer.cs`; `ClipboardHelper.cs`; `IO/UiThreadUserDialog.cs`; `Converters/ContentConverter.cs`, `ContentToTruncationIconConverter.cs`, `BookmarkStringConverter.cs`. In `FilterViewModel.cs`, the grid's call sites: rows, presses, keys, refreshes, clipboard and navigation.
- **Core and Common, where the grid depends on them:** `ISelect.cs`, `Data/IAnnotations.cs`, `Data/AnnotationStore.cs` (events, and the rule that a comment pins its record), `SelectionManager.Selected`, `Collections/SelectedRecords.cs`, `Data/ContentChunk.cs` and `IO/LogFileSource.cs` (how a read works on disk).
- **2.x:** `Gui/Filter/FilterView.xaml(.cs)`, `FilterListViewBehavior.cs`, `ClipboardHelper.cs`.
- **History:** the 25 commits since `baa70a9d` that touch the unit's files (#1057, #1064, #1075, #1077, #1097, #1137, #1139, #1150, #1028, #996, #1162), and the #1064 review fixes (commit f6931b0c).
- **Documents:** `Doc/Research/Weevil3/VirtualListPoC/README.md`, its `VirtualRowList.cs` and `AutoRun.cs`; `Doc/Plans/990-ImmutableCore.md` (decisions 4 and 5, step 5), `1025-ContentFromFile.md`, `1028-OpenAs.md` (Screen rows), `991-MultipleViews.md`; `Doc/Design/`; `Doc/README.md`; `Doc/Guides/Testing/TestingStrategy.md`; `.github/copilot-instructions.md` and the gui, review and tests instructions; findings U2, U4, U5a and U6a.
- **Tests:** `RowListTests`, `RowSelectionTests`, `RowViewModelTests`, `RecordListViewAutomationPeerTests`, `UiThreadUserDialogTests`, `ContentConverterTests`, `ContentToTruncationIconConverterTests`, `SelectingRecords.feature` and its steps, `PinNavigationTests`, `CommentNavigationTests`, the grid's tests in `FilterViewModelTests`, `FilterViewPaletteTests`; `ScreenRowsBenchmarks`; `Tst/Disk.runsettings` and the CI workflow.
- **SonarCloud:** the open issues on the unit's ten source files, read through its API on 2026-11-17. Progress.md lists no lead for this half of U6.

## Strengths

- **One selection, owned by Core.**
  - 2.x kept WPF's `SelectedItems` and Core's dictionary in step through code-behind, and selected each record again after a filter (2.x `Gui/Filter/FilterView.xaml.cs:91-158`). The proof of concept measured that as quadratic: 8,000 rows took 8 s (`Doc/Research/Weevil3/VirtualListPoC/README.md:190-199`).
  - Now WPF never selects a row (`Src/BlueDotBrigade.Weevil.Gui/Controls/RecordListView.cs:12-27`). A click becomes a Core call by line number (`Gui/Filter/RowSelection.cs:42-60`), and the row only shows Core's answer (`Filter/RowViewModel.cs:57-71`, `Filter/FilterView.xaml:305-309`).
- **A clear split, stated where it is used.**
  - `RowSelection` says that Core keeps the selection and the anchor, and the GUI keeps only the caret, as a line number so that it survives a filter (`RowSelection.cs:9-13`).
  - `RowViewModel` says that the pin and the comment live in the store (`RowViewModel.cs:10-13`).
  - Elapsed times come from Core (`Filter/RowList.cs:94-96`). Paste moved into Core: 2.x looped over `Metadata` in the GUI, 3.0 makes one `ISelect.SetComment` call (`Gui/ClipboardHelper.cs:118-124`).
- **Decision 4 is implemented and tested.** `QueueRefresh` never waits for the UI thread, and turns any number of changes into one queued refresh. Past 1,000 changes, or for a bulk change, it refreshes every row still held instead (`RowList.cs:225-262`, `:293-335`). Four tests specify this with a deferred dispatcher (`Tst/BlueDotBrigade.Weevil.Gui-FeatureTests/Filter/RowListTests.cs:64-141`).
- **Typing is protected.** A row ignores its own writes, reads the comment back only when it changed, and keeps its record pinned while the box holds text (#1066) (`RowViewModel.cs:79-97`, `:141-160`). The tests name the failures they prevent (`Filter/RowViewModelTests.cs:24-35`, `:77-114`).
- **WPF's quirks are explained where they are used.**
  - `SelectionMode.Multiple`, because moving the focus then selects nothing; text search off, because it would compare every row (`RecordListView.cs:15-18`).
  - A row reports a press before it takes focus, so Shift+click extends from the previous caret (`Controls/RecordListViewItem.cs:34`).
  - Rows are equal by line number, because WPF finds them with `IndexOf` (`RowViewModel.cs:13`).
  - The cache never keeps a row alive (`RowList.cs:13-17`).
- **Accessibility survived the loss of WPF's selection.**
  - The peers report Core's selection, and `GetSelection` stops at 1,000 rows (`Controls/RecordListViewAutomationPeer.cs:12-22`).
  - They answer a search for selected rows themselves instead of WPF's linear search (`:136-176`).
  - A row peer keeps its line number, so an off-screen row is still reported as selected (#1077) (`Controls/RecordItemAutomationPeer.cs:12-15`).
  - A screen reader changes the selection as a click would (`:38-57`).
- **A failed read on disk cannot end Weevil.** A converter's exception would end the application. Instead, the row shows a placeholder and reports the failure, and the view acts on it once per log (`RowViewModel.cs:99-121`). This is tested on a real log on disk that was deleted, and on one that was truncated (`Filter/FilterViewModelTests.cs:195-242`).
- **Costs measured before decisions.**
  - #1028 measured a screen of 50 rows, read 5 times each, at 1.9 ms on disk from the page cache, and decided not to cache rows (`Doc/Plans/1028-OpenAs.md:160-175`; `Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/Benchmarks/ScreenRowsBenchmarks.cs`).
  - The walk back for elapsed time over rows without a timestamp is documented with its cost: about 20 ms for 10 million rows (`RowList.cs:94-95`).
- **`UiThreadUserDialog` is a small decorator.** It shows each analyzer prompt on the UI thread and always reports the prompt closed (`Gui/IO/UiThreadUserDialog.cs:7-12`, `:121-140`). Its failure path is tested (`IO/UiThreadUserDialogTests.cs:54`), and the window's closing handler says why it must not wait for the analyzer (`Filter/FilterView.xaml.cs:81-82`).
- **Tests read as specifications.** `RowSelectionTests` names each key rule in 16 tests, for example a caret that the filter hides (`Filter/RowSelectionTests.cs:158`). `SelectingRecords.feature` covers the journeys, tagged `@Requirement:1064`, `@Requirement:1015` and `@Bug:806` (`Filter/SelectingRecords.feature:8-43`).
- **Proportionate to what it buys.**
  - About 1,190 lines replace about 175 in 2.x: `FilterListViewBehavior` (109 lines) and the selection code-behind.
  - They buy rows without record objects, one selection, refreshes that never block, protected typing, a screen-reader view of a selection that WPF no longer holds, and failed-read safety.
  - The peers' 265 lines are the price of bypassing WPF's selection, and the code says so (`RecordListView.cs:40-46`).
- **Room for #991.** Each piece belongs to one view: a `RowList` over one engine's results and selection, a `RowSelection` per engine. A row ignores only its own writes, so another view's row for the same line still refreshes (`RowViewModel.cs:149-160`). The wiring lives in `FilterViewModel` (U6a-2), and bookmarks and regions need a Core event (U6b-2, U4-5).
- **SonarCloud finds little:** 11 open issues on ten files, one about complexity (see Minor).

## Findings

### U6b-1. The grid scales only while WPF never walks `RowList`; the proof of concept counted that, the product neither tests nor detects it

- **Priority:** Medium
- **Kind:** Architecture
- **New in 3.0?** New design
- **Evidence:**
  - **The invariant.** A row view-model exists only for a row that WPF asks for (`Src/BlueDotBrigade.Weevil.Gui/Filter/RowList.cs:10-17`). If WPF enumerated or copied the list, `GetEnumerator` and `CopyTo` would create a row, and its record, for every result. The remark says so: "WPF should not need this: walking the list creates a row for every result" (`:264-281`).
  - **The proof of concept measured it.** Its list counted enumerations, rows walked and `CopyTo` calls (`Doc/Research/Weevil3/VirtualListPoC/VirtualRowList.cs:18-20`, `:226-240`), and each report printed them (`AutoRun.cs:481`). "WPF never walked the list" is one of its results (`README.md:86`, `:189`).
  - **The product has no counter and no test of it.** `RowList` counts nothing. The only test that builds a `RecordListView` gives it 3 rows and never lays it out (`Tst/BlueDotBrigade.Weevil.Gui-FeatureTests/Controls/RecordListViewAutomationPeerTests.cs:65-73`). The other grid tests use at most 5,000 rows, without WPF.
  - **3.0 runs paths that the proof of concept never ran:**
    - Every bookmark or region change calls `ListView.Items.Refresh()` (`Filter/FilterView.xaml.cs:117-143`). The proof of concept never refreshed its items (no `Items.Refresh` in `VirtualListPoC/*.cs`) and left bookmarks out (`README.md:109`).
    - The automation peers. The list's peer handles only a search for selected rows itself, because, in its own words, "WPF's search would create a row and a peer for every record" (`Controls/RecordListViewAutomationPeer.cs:137`). A search by any other property is passed to that same WPF search (`:155-158`), and each row peer reads its record's text for its name (`Controls/RecordItemAutomationPeer.cs:25`).
  - **A small change could break it.** A sort, group or filter on the grid's collection view would make WPF keep its own copy of the items. 2.x set `IsSynchronizedWithCurrentItem="True"`; `RecordListView` now sets it to false, with no remark (`Controls/RecordListView.cs:25`).
- **Impact:** Whether `Items.Refresh()` or a screen reader's search walks the list today is not known. If one does, the window freezes for as long as it takes to create a row for every result: after a bookmark, or while a screen reader runs. Only someone using a large log would see it. Any later change to the grid's XAML, its collection view or the WPF version carries the same risk, and no test would fail.
- **Recommendation:**
  - Restore the proof of concept's counters as one cheap guard: count rows created, enumerations and `CopyTo` calls in `RowList`, and log one warning when an enumeration passes, say, 10,000 rows.
  - Add one STA test that lays out a `RecordListView` over a few million results, scrolls to the end, calls `Items.Refresh()`, asks the peer for the selection and searches it by name, then asserts that fewer than 1,000 rows were created.
  - State the invariant in the grid's design note (U6b-3).

  **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U6b-2. A row's state reaches the screen by three routes, and the selection's route is a call the view-model must remember

- **Priority:** Medium
- **Kind:** Maintainability
- **New in 3.0?** New design for the selection; Existing weakness for bookmarks and regions
- **Evidence:**
  - **Pins and comments: Core tells the rows.** `IAnnotations.Changed` (`Src/BlueDotBrigade.Weevil.Common/Data/IAnnotations.cs:17`) reaches `FilterViewModel.OnAnnotationsChanged` (`Gui/Filter/FilterViewModel.cs:995-999`), then `RowList.QueueRefresh`. That never waits, and the rows refresh later on the UI thread (`RowList.cs:225-262`, `:293-335`), as decision 4 asks (`Doc/Plans/990-ImmutableCore.md:29`). Unpin All and Remove Comments rely on this route alone (`FilterViewModel.cs:2383-2386`, `:2412`).
  - **Selection: the view-model tells the rows.**
    - `ISelect` raises no event (`Common/ISelect.cs`). `RowSelection` changes Core's selection and refreshes nothing.
    - Each of its three callers then calls `OnSelectionChanged` (`FilterViewModel.cs:1242`, `:1274`, `:2670`). That refreshes the rows still held and posts the status bar's bulletin (`:1285-1289`; `RowList.cs:149-185`). `RefreshSelection`'s summary states the rule: "Call it on the UI thread after the selection changes."
    - After a filter, a Clear or a Reload, the rows are rebuilt instead (`FilterViewModel.cs:2693`).
  - **Bookmarks and regions: the view-model tells the view.** The view-model raises its own events after its own commands (`:2752`, `:2770`, `:2790`, `:2802`, `:2828`, `:2854`, `:2884`). The view then rebuilds every row on screen with `Items.Refresh()` (`FilterView.xaml.cs:117-143`). The tags find a row's bookmark through the whole `FilterViewModel`, which each converter gets as a second binding (`FilterView.xaml:377-388`, `:409-431`; `Converters/BookmarkStringConverter.cs:25-45`). This is inherited from 2.x (2.x `FilterView.xaml.cs:79-89`).
  - **A 2.x route lingers.** Paste rebuilds every row through `RefreshFilterResults` (`FilterViewModel.cs:1214`), although the store's event already refreshes them.
  - **The scenarios cannot see a missed selection refresh.**
    - The step "line number N will be selected" reads `GetRow(...).IsSelected` (`Tst/BlueDotBrigade.Weevil.Gui-FeatureTests/Filter/SelectingRecordsSteps.cs:73-77`, `:91-96`). A row that nothing holds is created afresh from Core's selection (`RowList.cs:84-93`), so the step checks Core, not the rows on screen.
    - In the `@Bug:806` scenario, row 10 was created in a Given step, so the step checks the refresh only if no garbage collection ran in between.
    - `RowListTests` test `RefreshSelection` on its own (`Filter/RowListTests.cs:143-178`).
- **Impact:**
  - Today only `RowSelection` changes the selection from the GUI, and all three callers comply.
  - The cost falls on the next selection command, such as selecting a region's records or every pinned record. It must know to call `OnSelectionChanged`. If it doesn't, the rows on screen and the status bar stay stale until the next filter, and no test fails.
  - A plugin can select through the public `IEngine.Selector`, with no effect on screen, as in 2.x.
  - For #991, a bookmark or region added in one view, or a selection copied into another, will not reach that view's grid. U4-5 finds the same gap in Core's stores.
- **Recommendation:**
  - Give `ISelect` a change event, raised after its lock, and let `RowList` refresh from it as it does from annotations: queued, on the UI thread. `OnSelectionChanged` and its three calls then go, and the bulletin can follow the same event.
  - When #991 starts, raise the session's bookmark and region changes the same way (U4-5), and give rows their tags, so that a change refreshes the rows on screen instead of rebuilding them.
  - Make the scenario step check rows that were held before the change.
  - Drop the `RefreshFilterResults` after Paste.

  **Complexity:** S for the selection event; M with bookmarks and regions.
- **Sceptic:** Not challenged (Medium)

### U6b-3. No design note describes the grid, and the GUI instructions contradict three of its deliberate choices

- **Priority:** Medium
- **Kind:** Intent
- **New in 3.0?** New design
- **Evidence:**
  - **The plan is done; its design note was never written.** `Doc/README.md:45` says that a finished plan's result is described in `Design/`, after which the plan can be deleted. #990 is done (`Doc/Plans/990-ImmutableCore.md:3`). `Doc/Design/` never mentions the grid; `RecordTable.md:34` says only that the selection is a record list.
  - **Where the reasons live now:**
    - the #990 plan: decisions 4 (rows never block) and 5 (ranges, the Copy limit), and step 5, with its dated decisions on dragging, Ctrl+Shift+arrow, typed letters and hidden records (`990-ImmutableCore.md:29-30`, `:69-78`);
    - the proof of concept's README, a dated record whose Limits section predates the decision on hidden records (`VirtualListPoC/README.md:105`);
    - code remarks, which are good, but each covers one class of eight files.
  - **The instructions say the opposite of the design.** An agent that follows them would undo it:

    | Instruction | What the grid does on purpose |
    |---|---|
    | "Do not manually implement `INotifyPropertyChanged` in ViewModels; rely on the `[Observable]` attribute" (`.github/instructions/gui.instructions.md:15`; also `.github/copilot-instructions.md:36`) | `RowViewModel` raises `PropertyChanged` by hand, "because the values change in the store, which Metalama cannot see" (`Gui/Filter/RowViewModel.cs:12`, `:188-191`) |
    | "Use `IUiDispatcher.Invoke()` to marshal results back to the UI thread" (`gui.instructions.md:22`; `.github/instructions/review.instructions.md:23`) | Rows refresh through `BeginInvoke`, never through a synchronous `Invoke` (decision 4; `RowList.cs:225-262`). U6a records the same drift for the view-model |
    | "Do not replace bindings with code-behind event handlers" (`gui.instructions.md:35`) | #1064 replaced two behaviour bindings (`ActiveRecordIndex`, `AutoScrollToCurrentItem`) and WPF's selection with four code-behind handlers (`FilterView.xaml:249-252`; `FilterView.xaml.cs:156-208`) |

  - **The review rules miss the grid's invariants:** WPF must not walk `RowList` (U6b-1); a row ignores its own writes (`RowViewModel.cs:145-147`); a selection change must refresh the rows (U6b-2).
  - **Agents do most of the work.** Agents wrote 445 of the 476 commits since `baa70a9d` (`git log --format=%an`), and all 16 that touch `RowList`, `RowSelection`, `RowViewModel` or `Controls/`. Each reads the instructions first.
- **Impact:** The grid is the part of 3.0 most likely to be "fixed" by someone who reads the rules but not the plan. Once the plan is deleted, as `Doc/README.md` allows, the reasons survive only in code remarks spread over eight files.
- **Recommendation:**
  - Write a design note for the grid, for example `Doc/Design/Grid.md`. It should cover:
    - the four classes and what each owns, and what Core owns;
    - the invariants: WPF reads only `Count`, the indexer and `IndexOf`; rows are equal by line number; rows never wait for the UI thread; a row ignores its own writes; WPF never selects a row;
    - the three refresh routes (U6b-2);
    - the dated decisions on keys and the mouse;
    - the measurements behind them: the proof of concept and #1028's screen of rows.
  - Add the exceptions to the three instructions, pointing to the note, and add the invariants to `review.instructions.md`.

  **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U6b-4. The #699 navigation tests assert `ActiveRecordIndex`, which nothing on screen has read since #1064

- **Priority:** Low
- **Kind:** Maintainability
- **New in 3.0?** Regression in what the tests guard; the product is unaffected
- **Evidence:**
  - **The binding went with the behaviour.** In 2.x the grid bound `local:FilterListViewBehavior.ActiveRecordIndex="{Binding ActiveRecordIndex, Mode=OneWay}"` (2.x `Gui/Filter/FilterView.xaml`). #1064 removed both (commit f91a9b6e). The property is still public with a setter (`Gui/Filter/FilterViewModel.cs:402-405`). Only `ShowFoundRecord` writes it (`:2674`), and only tests read it.
  - **The keys and the scroll now use other state.** The arrow keys move from the caret (`Filter/RowSelection.cs:25`, `:114`), and the view scrolls on `ScrollRequested` (`FilterViewModel.cs:2675`; `Filter/FilterView.xaml.cs:210-213`).
  - **The tests named for #699 check the old property.**
    - `PinNavigationTests` cites #699: "After 'Next Pinned' navigation, pressing the down arrow jumped to an unexpected record because `ActiveRecordIndex` was not reflecting the correct position". It asserts `ActiveRecordIndex` (`Tst/BlueDotBrigade.Weevil.Gui-FeatureTests/Filter/PinNavigationTests.cs:25-35`, `:57`, `:61`).
    - `CommentNavigationTests` does the same (`CommentNavigationTests.cs:9-27`, `:39`).
    - A Find test finds its row through it (`FilterViewModelTests.cs:188`).
  - **What guards #699 now.** The caret is tested only inside `RowSelection` (`RowSelectionTests.cs:200-210`), not through the navigation commands. If `ShowFoundRecord` stopped moving the caret, #699 would return, and the tests named for it would still pass.
  - **Rules:** a regression test must fail before the fix, and no public API may exist only for tests (`.github/instructions/tests.instructions.md`, Regression Tests and Test Configuration).
- **Impact:** Small. Readers believe #699 is guarded, and a public property survives only for tests.
- **Recommendation:** Assert what the user relies on: after Next Pin, `CaretPosition` is the pin's row, Down selects the row after it, and `ScrollRequested` names the pin's row. Then remove `ActiveRecordIndex`. The `@Bug:806` scenario already works this way (`Filter/SelectingRecords.feature:24-33`). **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U6b-5. The view's own rules (press order, focus and screen-reader events) are checked only by hand

- **Priority:** Low
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **Review found these rules, and three of the five fixes live in the view.** The #1064 fix commit (f6931b0c) lists five review findings. Three are in code that no test reaches:
    - after a filter, the caret's row is scrolled into view (`Gui/Filter/FilterView.xaml.cs:145-148`);
    - the caret follows keyboard focus into a row, and a row reports a press before it takes focus (`:178-187`; `Controls/RecordListViewItem.cs:27-38`);
    - focus returns to the grid only if the user has not moved it (`FilterView.xaml.cs:189-208`).

    The other two (Alt, and Shift with no anchor) are in `RowSelection`, and are tested (`Tst/BlueDotBrigade.Weevil.Gui-FeatureTests/Filter/RowSelectionTests.cs:171`, `:184`).
  - **More view rules without a test:**
    - keys typed in a comment box skip the grid (`FilterView.xaml.cs:166-170`);
    - a refresh gives focus back (`:133-143`);
    - the safety net that unselects whatever WPF selects (`Controls/RecordListView.cs:73-82`).
  - **No test builds the view.** The GUI tests call `FilterViewModel.PressRow` and `PressKey` directly (`Filter/SelectingRecordsSteps.cs:55`, `:98-101`), as the proof of concept's automatic run did (`Doc/Research/Weevil3/VirtualListPoC/README.md:111`).
  - **The peers have three tests,** one each for reporting the selection, Add to selection and an off-screen row (`Controls/RecordListViewAutomationPeerTests.cs:21-63`). Untested:
    - the events raised when the selection changes (`Controls/RecordListViewAutomationPeer.cs:67-95`, Sonar complexity 17, the unit's only complexity issue);
    - `Select` and `RemoveFromSelection` (`Controls/RecordItemAutomationPeer.cs:38-41`, `:51-57`);
    - the exceptions for a disabled grid and for a row that a filter hid (`:72`, `:81`);
    - the hand-off of other searches to WPF (`RecordListViewAutomationPeer.cs:153-158`).
  - **The testing strategy puts these at the GUI level:** "focus handling, keyboard shortcuts, accessibility semantics" (`Doc/Guides/Testing/TestingStrategy.md:53`).
- **Impact:** A WPF upgrade or a template change breaks these rules silently. A screen-reader user is the least likely to report it.
- **Recommendation:**
  - Test `Select`, `RemoveFromSelection` and the two exceptions in the existing peer-test style, which needs no window.
  - For the events and the view's focus rules, either add one STA test class that lays out the view, or keep a short manual checklist in the grid's design note (U6b-3), run before each release, like the proof of concept's "By Hand" list (`VirtualListPoC/README.md:90-101`).

  **Complexity:** S.
- **Sceptic:** Not challenged (Low)

## Minor

- **Reads per row (corrects U2-1).** A row that WPF shows reads its record's text three times, not twice: once in `ContentConverter`, and twice in the truncation converter, once through `ContentConverter` and once to compare (`Gui/Converters/ContentConverter.cs:34`; `ContentToTruncationIconConverter.cs:73`, `:76`). It is formatted twice. Each automation peer reads it once more for its name (`Controls/RecordItemAutomationPeer.cs:25`). #1028's benchmark assumed five reads per row, so its decision not to cache still holds (`Doc/Plans/1028-OpenAs.md:168-175`).
- **Converters still accept a record.** `RowViewModel.ToRecord(object)` and `ReadContent(object)` let each converter take a row or a record (`Filter/RowViewModel.cs:123-139`). The grid is the converters' only user (`Filter/FilterView.xaml:32-36`), and it always passes a row. Only the converter tests pass records (`Converters/ContentConverterTests.cs:13-24`, `ContentToTruncationIconConverterTests.cs:12-31`).
- **Copy Line Numbers creates records.** It creates every selected record, up to 100,000, to read its line number (`Gui/ClipboardHelper.cs:77-80`, `:97`). `Selected.Keys` gives the line numbers without records, as #1139 did for Copy Comments (`:57-61`). Copy on the UI thread is U2-1 and U6a-3; this review confirms both.
- **Clipboard formats are untested.** `ClipboardHelper` calls `System.Windows.Clipboard` directly, so its output formats have no test. Inherited.
- **Dead code.** `ListViewBehavior` (`Src/BlueDotBrigade.Weevil.Windows/Controls/ListViewBehavior.cs`, public, 86 lines) has had no user since #1064 removed `AutoScrollToCurrentItem` from the grid.
- **Two namespaces depend on each other.** `RecordListView` and both peers know `RowList` and `RowViewModel` (`Controls/RecordListView.cs:10`, `:62-70`; `Controls/RecordListViewAutomationPeer.cs:41`), while `FilterView` uses the control. The control serves only this grid, so this costs nothing yet.
- **A stale automation name.** A peer keeps the name it read when it was created (`RecordItemAutomationPeer.cs:25`). A row first named while its log could not be read keeps the placeholder as its name.
- **Small inconsistencies.** `IsSynchronizedWithCurrentItem = false` has no remark (`RecordListView.cs:25`). `RowList` silently treats null results as empty, while its other arguments throw (`Filter/RowList.cs:43`). `RowViewModel.Comment`'s getter records what it returned, a side effect explained at `RowViewModel.cs:32` (`:81`).
- **Inherited code-behind.**
  - `FilterView.xaml.cs` has an unused `using System.Data.Common` (`:4`) and a summary naming `FilterResultsView.xaml` (`:21`).
  - A private `UpdateLayout` hides `UIElement.UpdateLayout` (`:313`, Sonar S4015).
  - The `DataContextChanged` handler casts with `as` and then dereferences (`:32-53`).
- **Polling tests.** `PinNavigationTests` and `CommentNavigationTests` poll with `Thread.Sleep` for up to 5 s (`PinNavigationTests.cs:42-46`). Inherited style.
- **SonarCloud.** 11 open issues on the ten files, read on 2026-11-17. None is a defect:
  - one about complexity (`RecordListViewAutomationPeer.cs:67`, 17);
  - two nested ternaries (`:85`; `RowSelection.cs:195`);
  - a loop that returns on its first pass (`RecordListViewAutomationPeer.cs:124`), used as "first or none";
  - the hidden `UpdateLayout`;
  - six style issues.

## To Measure in Stage 2

- **The product grid against the proof of concept's targets (U6b-1).** On Windows, open the 10-million-record droid log in the GUI, in RAM and on disk. Run the proof of concept's targets: Ctrl+A, Shift+End, dragging the scroll bar, Page Down, a filter with everything selected, then removing the filter. Count the `RowViewModel`s created, with a memory snapshot or a temporary counter. The proof of concept measured synthetic text; the product's grid has not been measured. This is for the owner.
- **Paths the proof of concept never ran (U6b-1).** With 10 million results, add a bookmark and a region, then run Narrator and select all. Count the rows created and time the UI thread for each.
- **A screen of rows from a network share on disk.** #1028 measured 1.9 ms per screen from the page cache on Linux. On Windows, time Page Down on a large log opened on disk from an SMB share. Each row opens the log three times, and each automation peer once more. This is for the owner.
- **A screen reader's `GetSelection` after Ctrl+A on disk.** It creates up to 1,000 rows and reads 1,000 records' text on the UI thread (`RecordListViewAutomationPeer.cs:21-22`, `:100-110`). Time it locally and from an SMB share.

## Open Questions

- **Should `ISelect` raise a change event (U6b-2)**, now or with #991?
- **Will the next milestone's MaterialDesign GUI (#991) keep `RowList`, `RowSelection` and the peers?** If it will, U6b-2 and U6b-3 are worth doing first.
- **Is screen-reader support a requirement, with a target reader** (Narrator, NVDA or JAWS)? Which searches do those readers make? A search by name goes to WPF's own search (U6b-1).
- **What is the largest result count the GUI must support for 3.0?** The proof of concept targeted 100 million rows; the product has been tested at 10 million records in Core only.
