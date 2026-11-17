# U5a: Selection (#990)

Part of the [Weevil 3.0 audit](../Progress.md). This code keeps the records selected in one view as ranges of positions in its filter results, carries them across a filter, a Clear and a Reload, and acts on them: save, pin and comment.

> **Status:** Final. Reviewed on 2026-11-17 at `main` e3dee822, by one reviewer covering every lens.

## Summary

The range design is sound, and the code says why it exists: the selection is a set of position ranges, built in one pass and replaced rather than edited, so Select All is one range and the quadratic re-selection that froze 2.x is gone. Keeping the selection across a filter, a Clear and a Reload moved from the GUI into Core, as the architecture rules ask, and each path explains in a remark why it differs from the others. Callers see line numbers, never positions, so the GUI stays correct while a filter replaces the results under it. `SelectionManager` is cohesive around one view's selection; its 655 lines come mostly from keeping the 2.x API beside the 3.0 one, not from tangled logic, and SonarCloud finds no complexity issue in the unit.

**The main concern is the public contract, not the implementation.** `ISelect` keeps 2.x record-based members that no production code calls beside the 3.0 line-range members, and the two halves treat a hidden line, the anchor and the active record differently (U5a-1); `Selected` keeps the 2.x dictionary shape, so readers copy or scan to find the first, last or next selected line (U5a-2). Both are Medium, and cheap to fix while 3.0 already breaks plugin compatibility. The worst case, one range per record, is reachable by removing a filter after Select All; its cost in the product is unmeasured, and the product's remap does more work per range than the proof of concept cited as its evidence (U5a-3). The unit has no High finding.

## What Was Examined

- **Core:** `SelectionManager.cs`; `Collections/PositionRanges.cs`, `SelectedRecords.cs`, `SelectedRecordList.cs`. Callers: `CoreEngine.cs` (construction, Clear, the filter handler), `Engine.cs` (Reload), `Analysis/AnalysisManager.cs`, `Data/InMemoryRecordRepository.cs`, `Data/TableRecordList.cs`, `Navigation/NavigationManager.cs`, `Filter/FilterManager.cs` (event order).
- **Common:** `ISelect.cs`, `Navigation/RecordSearch.cs`, `Data/AnnotationStore.cs` (how pins are stored).
- **GUI:** `Filter/RowSelection.cs`, the selection members of `Filter/RowList.cs`, `ClipboardHelper.cs`, the selection call sites in `Filter/FilterViewModel.cs`, `Controls/RecordListViewAutomationPeer.cs`.
- **2.x:** `SelectionManager.cs`, `ISelect.cs`, `CoreEngine.cs`, `RegionManager.cs`, and `FilterViewModel.cs` (selecting again after Reload).
- **Documents:** `Doc/Plans/990-ImmutableCore.md` (decision 5, steps 4, 5 and 8, Compatibility); `Doc/Research/Weevil3/VirtualListPoC/README.md` and its `RangeSelection.cs`; `Doc/Plans/991-MultipleViews.md`; `Doc/Design/RecordTable.md`; the core, common and tests instructions.
- **Tests:** `PositionRangesTests`, `SelectedRecordsTests`, `SelectedRecordListTests`, `SelectionManagerTest` (unit); `SelectionManagerTests` (performance); `Selecting.feature`, `Commenting.feature`, `Pinning.feature`, `Filtering.feature:260-281`; `EngineTests` (Clear and Reload); `ClearingShould`; the GUI's `RowSelectionTests` (skimmed); `Tst/Disk.runsettings`.
- **SonarCloud:** the open issues of the five files, read through its API on 2026-11-17. Progress.md lists no lead for them.

## Strengths

- **A decision made on measurements.** The proof of concept showed WPF's own selection, as 2.x used it, growing with the square of the count: 8,000 rows took 8 s to select again after a filter. Ranges at 100 million rows met 21 of 23 targets (`Doc/Research/Weevil3/VirtualListPoC/README.md#results`). `PositionRanges` cites that decision (`Src/BlueDotBrigade.Weevil.Core/Collections/PositionRanges.cs:16-25`).
- **A value that is replaced, not changed.** A `PositionRanges` is built only by its static methods through a private `Append` (`PositionRanges.cs:60-153`, `:309-325`). `SelectedRecords` reads one moment of the selection (`Collections/SelectedRecords.cs:13-16`), so a reader never sees a half-changed set.
- **Constant time where 2.x was linear.** Select All is one range (`SelectionManager.cs:478`). The count is a field (`PositionRanges.cs:35`). The selection period reads only the first and last records (`SelectionManager.cs:97-113`), where 2.x walked `Values.Last()` of a sorted dictionary after every change (2.x `SelectionManager.cs:77-78`).
- **Logic moved into Core.** Core keeps the selection across a filter (`CoreEngine.cs:423-428`), a Clear (`:376-395`) and a Reload (`Engine.cs:197-210`). In 2.x the GUI kept it across a Reload with a quadratic `Where(... Any(...))` (2.x `Gui/Filter/FilterViewModel.cs:811-829`). The GUI now maps keys and clicks to Core calls and keeps only the caret (`Gui/Filter/RowSelection.cs:6-13`), a thin layer of 207 lines.
- **Positions inside, line numbers outside.** Every public member takes or returns line numbers. A click on a row of the old results is still right while a filter swaps them, because the grid asks by line number (`Gui/Filter/RowList.cs:93`, `:192`).
- **Remarks give the reason.** Clear cannot use `Remap` (`SelectionManager.cs:528-535`). Clear sets the selection before its filter publishes (`CoreEngine.cs:386-389`). Reload restores before it filters (`Engine.cs:209`). A remap checks the log because the log may have shrunk (`PositionRanges.cs:228`).
- **No record created.** Line numbers and timestamps are read by position (#1137, #1140), and tests prove it with a list that cannot create records (`Tst/BlueDotBrigade.Weevil.Core-UnitTests/Collections/PositionRangesTests.cs:105-116`, `SelectedRecordsTests.cs:28`, `SelectedRecordListTests.cs:27`).
- **Bulk pin and comment are one store call,** with the lines found in parallel first (`SelectionManager.cs:604-648`), as `.github/instructions/core.instructions.md` asks.
- **No storage-mode check.** Nothing in this unit asks whether the log is in RAM or on disk. The selection scenarios name no mode, so CI runs them in both (`Tst/Disk.runsettings`, `.github/workflows/dotnet.yml:65`).
- **Specifications that read as requirements.** `Selecting.feature` has 12 scenarios tagged `@Requirement:1062` covering ranges, the anchor, Reload and navigation. `Filtering.feature:260-281` states the 2026-10-22 decision. `Union` and `Except` are checked against a `SortedSet` over 2,000 random changes (`PositionRangesTests.cs:44-77`).
- **Room for #991.** Each engine, which is one view, has its own `SelectionManager`, as the #991 plan expects (`Doc/Plans/991-MultipleViews.md:21`). `Snapshot` holds the selection by line number and no records (`SelectionManager.cs:650-653`), which is the shape a copy between views needs.
- **Low complexity.** SonarCloud raises 13 issues on the five files: none on complexity, 12 on style, and one real (an unread field; see Minor).
- **A proportionate size.** Core's selection grew from 388 lines in 2.x (`SelectionManager` and `ISelect`) to 1,307. That buys ranges, the anchor, carry-over in Core instead of the GUI, bulk annotation in one store call, and no records created.

## Findings

### U5a-1. `ISelect` has two ways to select, with different rules

- **Priority:** Medium
- **Kind:** Intent
- **New in 3.0?** New design. The 2.x members and their text are inherited; the split is new.
- **Evidence:**
  - **Two halves.** The 3.0 members take line numbers: `SelectOnly`, `SelectRange`, `Toggle`, `SelectAll` and `AnchorLineNumber` (`Src/BlueDotBrigade.Weevil.Common/ISelect.cs:19-56`). The GUI uses only these (`Gui/Filter/RowSelection.cs:52-203`). The 2.x members take records: `Select` and `Unselect` (three overloads each) and `ClearAll` (`ISelect.cs:58-75`, `:86`). **No code in `Src/` calls them.** About 50 test call sites in 10 files do. These include the step "selecting the records on lines 1 to 4", which specifies how a filter keeps the selection (`Tst/BlueDotBrigade.Weevil.Core-FeatureTests/Filter/FilteringSteps.cs:183-190`, used by `Filtering.feature:263`). `Selecting.feature` uses `SelectOnly` for the same action (`Selection/SelectingSteps.cs:14-16`), so one action has two steps.
  - **Eight answers to "the filter hides this line":**

    | Member | Result |
    |---|---|
    | `Select(int)`, `Unselect(int)` | Throws `RecordNotFoundException` from `RecordSearch.IndexOf` (`Common/Navigation/RecordSearch.cs:119-122`). The `index >= 0` test that follows can never be false (`Core/SelectionManager.cs:255`, `:319`) |
    | `Select(IRecord)` | Changes nothing, then throws from `NavigationManager.SetActiveLineNumber` (`SelectionManager.cs:273-281`; `Navigation/NavigationManager.cs:62-67`) |
    | `Select(IList<IRecord>)` | Skips hidden records; throws only if the last one is hidden (`SelectionManager.cs:296-308`) |
    | `Unselect(IRecord)`, `Unselect(IList<IRecord>)` | Ignored, silently |
    | `Toggle(int)` | Throws, from its own guard (`:448-452`) |
    | `SelectOnly(a, b)`, `SelectRange(a, b)` | Selects what is shown between; a hidden `a` still becomes the anchor (`:501-507`) |

  - **The anchor.** `SelectOnly`, `SelectRange` and `Toggle` set it (`:507`, `:459`). `SelectAll` keeps it (`:472-488`). `Select`, `Unselect` and `ClearAll` ignore it. Clear forgets it (`:564`), which no `ISelect` remark mentions: the remark covers only a filter (`ISelect.cs:22-23`).
  - **The active record.** `Select(IRecord)` always moves it (`SelectionManager.cs:281`). `Select(IList)` moves it to the last record given (`:304-308`). `SelectOnly` and `SelectRange` move it only when the line is shown (`:515`, `:520-526`). `Toggle` always moves it (`:467`). `SelectAll`, `Unselect` and `ClearAll` never do.
  - **The documents describe 2.x.** `ISelect.cs:58-72` is word for word the 2.x text: "added to the 'items of interest' list", and an unqualified `RecordNotFoundException`. The 3.0 rule, "a record that the filter hides is not selected", is written only on the internal class (`SelectionManager.cs:263-265`, `:286-288`).
  - **A name clash, inherited.** `ClearAll()` unselects. In the domain, Clear removes records from the view: `IEngine.Clear(ClearOperation.Selected)` removes the selected records.
- **Impact:** Someone writing a test, a script or a plugin reads `ISelect`, picks `Select(int)` as the documented way, and gets rules the GUI never exercises. The anchor specifications (`@Requirement:1062`) cover only the other half. #991 will copy selections between views and must keep these rules, which today have to be read out of eight method bodies.
- **Recommendation:** Make the line-number members the only API. 3.0 already gives up plugin compatibility (`Doc/Plans/990-ImmutableCore.md:110`).
  - Remove the `IRecord` and `IList<IRecord>` overloads and `ClearAll`, or mark them `[Obsolete]`. Add `UnselectAll` if a caller needs one.
  - Move the test calls to `SelectOnly(n, n)`, and merge the two steps.
  - State in `ISelect` one rule for a hidden line, and delete the dead tests.
  - Write the selection's rules in a short `Doc/Design/Selection.md`: positions inside and line numbers outside; what a filter, a Clear and a Reload keep; the anchor; the active record; the worst case. Today they are spread over the dated plan, the proof of concept's README and code remarks. `Doc/Design` gives the selection one line (`RecordTable.md:34`).

  **Complexity:** S for the document, M for the API (mechanical).
- **Sceptic:** Not challenged (Medium)

### U5a-2. `Selected` keeps the 2.x dictionary, so readers copy or scan to ask simple questions

- **Priority:** Medium
- **Kind:** Maintainability
- **New in 3.0?** New design. The type is from 2.x; the adapter that keeps it is new.
- **Evidence:**
  - **An adapter that refuses most of its interface.** `SelectedRecords` (140 lines) exists so that `ISelect.Selected` stays an `IDictionary<int, IRecord>` (`Core/Collections/SelectedRecords.cs:9-17`). Six of its members throw `NotSupportedException` (`:39`, `:81-89`), and it nests a second read-only collection for `Keys` and `Values` (`:103-138`). `Values` creates every selected record (`:34`).
  - **The ranges answer first, last and contains directly** (`PositionRanges.cs:44-55`), and a search over the ranges answers "next after". The dictionary exposes none of these, so callers scan:
    - **Create Region** copies every selected line number to find the smallest and the largest (`Gui/Filter/FilterViewModel.cs:2750`), and `RegionManager` then sorts them (`Core/RegionManager.cs:99-103`). This is inherited from 2.x (2.x `FilterViewModel.cs:2004`).
    - **UI Automation's next selected row** walks `Keys` from the first selected line on every call (`Gui/Filter/RowList.cs:199-210`; `Gui/Controls/RecordListViewAutomationPeer.cs:107`, `:122`).
    - **The selection bulletin** reads `Count`, `SelectionPeriod` and `Keys.First()` in three calls, which can see three different moments (`FilterViewModel.cs:2710-2721`).
  - **Positions rebuilt in the GUI.** The GUI turns line numbers back into positions with a copy of Core's search (`RowList.cs:127-145` against `PositionRanges.cs:286-304`), since `LowerBound` is internal.
  - **Readers must learn which members are cheap.** One caller is commented "The count first, so a large selection creates no records (#1139)" (`FilterViewModel.cs:2008`). `Selected.Single().Value.LineNumber` creates a record to read its line number (`:2767`, `:2799`, `:2815`).
- **Impact:** The range representation is hidden, which is right, but behind a type that suggests a mutable map of records. The operations that ranges make cheap cost a full copy or a scan, and each reader has to discover which members are safe at 100 million rows. Copy, Paste and Graph stop at 100,000 records, so most of these costs are small today. Create Region and UI Automation have no limit.
- **Recommendation:** Replace `Selected` with a small read-only snapshot taken in one read, for example `Selection`. It would offer `Count`, `Contains(lineNumber)`, `FirstLineNumber`, `LastLineNumber`, `NextLineNumberAfter(lineNumber)`, `LineNumbers`, `Records` (what `GetSelected` returns), `Period` and `AnchorLineNumber`. Let `RegionManager` take the first and last line. `SelectedRecords` then goes. Make this change together with U5a-1, as one change to `ISelect`. **Complexity:** M (about 20 GUI call sites).
- **Sceptic:** Not challenged (Medium)

### U5a-3. One range per record: reachable, and its cost in the product is unmeasured

- **Priority:** Low
- **Kind:** CPU
- **New in 3.0?** New design
- **Evidence:**
  - **How it arises.** A filter drops the records it hides from the selection, and a later filter never adds them back (decided 2026-10-22, `Doc/Plans/990-ImmutableCore.md:76`). So Select All under a filter, followed by removing the filter, leaves one range for each run of adjacent selected records: one range per record when none are adjacent. The proof of concept took this as its worst case: 10 million ranges, about 80 MB and 1.5 s on a 4-vCPU Linux VM (`Doc/Research/Weevil3/VirtualListPoC/README.md:106`, `:210`).
  - **Each later change copies every range.** `Toggle`, `SelectRange`, `Select` and `Unselect` build a new set with `Union` or `Except` (`SelectionManager.cs:155-158`, `:455-457`, `:505`), in a list that starts with no capacity (`PositionRanges.cs:30-33`). A Ctrl+click runs this on the UI thread (`RowSelection.cs:52`). `ISelect` says that the cost of `SelectRange` "does not depend on the number of records" (`ISelect.cs:43`). That holds only while there are few ranges.
  - **Each `GetSelected` copies the ranges** into two arrays (`Collections/SelectedRecordList.cs:18-33`), and each read of a position is a binary search over them (`:47-62`).
  - **The remap after a filter costs more than the proof of concept's:**
    - **More searches.** It does four binary searches per range, two of them over the whole scope, starting from position 0 each time (`PositionRanges.cs:219-230`). The proof of concept did two, and found a range with no gaps by arithmetic on record numbers (`VirtualListPoC/RangeSelection.cs:178-187`). The product compares line numbers, which have gaps, so it has to search the scope. The results already hold record numbers (`Data/TableRecordList.cs:17-18`, `:48`), but `RecordList` does not expose them.
    - **It holds the lock.** The remap runs under the selection's lock (`SelectionManager.cs:223-239`). Every read of `Selected` takes that lock (`:78`), including each row that WPF creates (`RowList.cs:93`).
    - **It cannot be cancelled.** The proof of concept's remap could be (`RangeSelection.cs:165`, `:173`); the product's cannot.
  - **No measurement in the product.**
    - The performance test times only a save of the whole log (`Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/SelectionManagerTests.cs:33-48`).
    - The unit timing tests use 300,000 records (`Tst/BlueDotBrigade.Weevil.Core-UnitTests/SelectionManagerTest.cs:73-102`).
    - `PositionRanges` cites the proof of concept as its evidence (`PositionRanges.cs:21-22`), but that was measured over `int` arrays and records made by arithmetic.
- **Impact:** Most selections are a few ranges. Even the worst case keeps far less memory than 2.x did: 8 bytes per range, against a sorted-dictionary node and a record for each selected record. The cost falls on a user who selects under a filter, removes the filter, then keeps working on a large log with Ctrl+click or more filters. How large that cost is, nobody knows.
- **Recommendation:** Measure first (see To Measure). Now, correct the `ISelect` remark. If the measurement shows pauses:
  - give the set builders a capacity;
  - remap outside the lock, then swap the result in;
  - let the remap read record numbers when both lists share one table, as the proof of concept did.

  **Complexity:** S for the remark and capacities, M for the remap.
- **Sceptic:** Not challenged (Low)

### U5a-4. The selection's state is four fields kept in step by convention

- **Priority:** Low
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **Four fields that change together** (`SelectionManager.cs:40-43`), under one lock, in eight methods. `_selectionPeriod` is derived from the other two in constant time (`:97-113`), yet it is stored, and recomputed at `:162`, `:231`, `:425`, `:458`, `:479`, `:506`, `:563` and `:578`.
  - **Nothing says what the lock protects.** `Selected` takes it (`:78`). `HasSelectionPeriod`, `SelectionPeriod` and `AnchorLineNumber` (`:85-89`), `SaveSelection` (`:367`) and `SetActiveLineIfShown` (`:522`) read without it. The class remark says readers never see a set being changed (`:22`). That is true of the ranges, but the ranges mean nothing without `_visibleRecords`, a second field.
  - **Three copies of "the current results".** `FilterManager.Results`, the selection's `_visibleRecords`, and `NavigationManager`'s data source each hold them. `CoreEngine` updates the last two one after the other (`CoreEngine.cs:426-427`). `SetActiveLineIfShown` checks the selection's copy, then `NavigationManager.SetActiveLineNumber` looks the line up in its own copy and throws if the line is missing (`SelectionManager.cs:520-526`; `Navigation/NavigationManager.cs:62-67`). A click handled between those two lines on the filter's thread could therefore throw. This is plausible, not verified.
  - **Navigation inside the selection.** The selection moves the active record itself (`SelectionManager.cs:281`, `:307`, `:429`, `:467`, `:515`). The 2.x TODO already calls this coupling "a code smell" (`CoreEngine.cs:181`), and 3.0 deepened it.
- **Impact:** Each new way to change the selection must remember the period, the anchor and the lock. Readers outside Core cannot take a consistent view (U5a-2). #991 needs a value it can copy between views, and `Snapshot` is only half of one.
- **Recommendation:** Keep one immutable value (ranges, results and anchor), replaced in one write. Compute the period when it is read. Readers take the value without a lock. Return the line that the active record should move to, and let `CoreEngine` tell navigation, as Clear already does (`CoreEngine.cs:389-395`). **Complexity:** S to M.
- **Sceptic:** Not challenged (Low)

### U5a-5. Four binary searches by line number, with three conventions

- **Priority:** Low
- **Kind:** Maintainability
- **New in 3.0?** New design. `RecordSearch` is inherited.
- **Evidence:**
  - **Three conventions for "not found":**
    - `PositionRanges.LowerBound` returns the count (`PositionRanges.cs:286-304`).
    - `PositionRanges.IndexOfLineNumber` returns -1 (`:271-280`), and `SelectionManager.FindPosition` repeats it without the hint for where to start (`SelectionManager.cs:123-130`).
    - `RecordSearch.IndexOfLineNumber` throws (`Common/Navigation/RecordSearch.cs:31-33`, `:119-122`). This is the search that `Select(int)` and `Unselect(int)` call (`SelectionManager.cs:253`, `:317`).
  - **A copy in the GUI.** The GUI has its own copy of `LowerBound` (`RowList.cs:127-145`).
  - **A general search on a set type.** `LowerBound` lives on "a set of positions", yet Clear's region rule (`Data/InMemoryRecordRepository.cs:279`) and the dictionary adapter (`Collections/SelectedRecords.cs:96`) use it as a general search.
- **Impact:** The search that throws is why selecting by line number behaves differently (U5a-1). A reader looking for "find a record by its line" finds four answers.
- **Recommendation:** Put one public search on `RecordList` in Common, for example `LowerBound(lineNumber, from)`, and `IndexOfLineNumber(lineNumber)` returning -1. Core and the GUI both use it. `PositionRanges` keeps the set operations and `Remap`. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U5a-6. `Remap` has examples but no oracle, and its precondition is only a comment

- **Priority:** Low
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **Uneven test coverage.** `Union` and `Except` are checked against a `SortedSet` (`PositionRangesTests.cs:44-77`). `Remap`, the subtlest method in the unit, has five examples (`:79-143`), plus engine tests for Clear and Reload (`Tst/BlueDotBrigade.Weevil.Core-UnitTests/EngineTests.cs:74-115`, `:759-772`).
  - **The shortcut rests on a comment.** The shortcut selects every new result in a range's span. It is right only when the old results are a subset of `allRecords`, and that is stated once, in a parameter comment (`PositionRanges.cs:172`). The line-number overload's documentation leaves it out (`:195`). Clear avoids `Remap` for exactly this reason (`SelectionManager.cs:534`).
  - **The shrink check is not obvious.** It tests only the last line, though its comment says "both ends" (`PositionRanges.cs:228-232`). That is enough when a log only shrank, because a missing first line implies a missing last line, but the code does not say so.
- **Impact:** A future caller can break the precondition and select records that were never selected, and no test would notice. #991, copying a selection to a view with another scope, is such a caller.
- **Recommendation:** Add one randomized test. Build a random scope with gaps in its line numbers, random old and new subsets of it, and a random selection. The expected result is the selected old lines that the new results show. Assert the precondition in Debug builds, and explain at `:228` why the last line is enough. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

## Minor

- **Unread field.** `_reapplyCurrentFilter` is stored and never read (`SelectionManager.cs:35`, `:64`; passed at `CoreEngine.cs:184`); SonarCloud S4487. Inherited from 2.x.
- **`HasSelectionPeriod` is a count test.** It means "two or more selected", not "a period is known" (`SelectionManager.cs:85`), and its callers use it that way (`Analysis/AnalysisManager.cs:174`; `FilterViewModel.cs:2746`). Inherited.
- **`SaveSelection` with no or one record selected saves every record in the scope,** not the filter results (`SelectionManager.cs:367-369`). Inherited from 2.x; `ISelect` does not say so (`ISelect.cs:76-85`).
- **The filter's time leaves out keeping the selection.** `FilterManager` stops its stopwatch (`Filter/FilterManager.cs:362`) before it raises the event that runs the remap (`:396`; `CoreEngine.cs:426`). So the status bar's filter time (`FilterViewModel.cs:2703`) and the telemetry miss it. `KeepRecordsShownBy` logs only a count, at Trace (`SelectionManager.cs:241-246`).
- **Clear forgets the anchor** (`SelectionManager.cs:564`), and no test or `ISelect` remark says so.
- **Toggle Pin has no limit.** On a large selection it runs on the UI thread (`FilterViewModel.cs:2378-2381`), with one `int` per selected line (`SelectionManager.cs:607`). Paste stops at 100,000 records (`FilterViewModel.cs:1211`); Toggle Pin does not. Memory is no reason to limit it, since a pin costs one byte per line (`Common/Data/AnnotationStore.cs:17-24`), but time may be.
- **Reload's snapshot stores one `int` per selected record** (`SelectionManager.cs:390-407`), held while the log is read again (`Engine.cs:249-263`). For Select All on the 4 GB harness log that is 120 MB, about 2.5% of its 4.7 GB working set ([Memory-Main-Run2.md](../Measurements/Memory-Main-Run2.md)).
- **Test names.** `SelectionManagerTest` (`Tst/BlueDotBrigade.Weevil.Core-UnitTests/SelectionManagerTest.cs`) breaks the `{ClassUnderTest}Tests` rule and keeps three method names from before the convention (`:17`, `:31`, `:46`). Inherited from 2.x. The performance project's `SelectionManagerTests` tests only `SaveSelection`.
- **Doc drift.** `Doc/Design/RecordTable.md:34` says the selection is a record list over the view's table. It is a set of position ranges over the filter results; only `GetSelected` returns a list.

## To Measure in Stage 2

- **The worst-case remap in the product (U5a-3).**
  - **Setup.** Open the 1 GB harness log (7,547,742 records), in RAM and on disk. Filter to about every other record, for example with a regular expression on an even `sequence=`, then Select All.
  - **Measure.** Remove the filter. Record the time of the `Filter.Apply` call minus `FilterExecutionTime`, which is the remap, and the number of ranges afterwards.
  - **Compare** with the proof of concept's 1,527 ms for 10 million ranges on a 4-vCPU Linux VM.
- **Changes to a fragmented selection (U5a-3).** On that selection, time one `Toggle` and one `SelectRange`, with the bytes allocated by each. Then time `GetSelected()` and one pass over its records, against a contiguous selection of the same count.
- **The UI waiting on the selection's lock (U5a-3, U5a-4).** Windows only, so a gap for the owner. In the GUI, with the same selection, filter again while scrolling, and record the longest delay that `UiResponsivenessMonitor` reports.
- **Reload with Select All.** On the 4 GB log, compare the peak working set during a Reload with every record selected against one with none selected (Minor: the snapshot).
- **Toggle Pin with Select All.** On the 4 GB log, time `ToggleIsPinned`, which the GUI runs on the UI thread.

## Open Questions

- **Should the 2.x members of `ISelect` survive into 3.0?** These are `Select` and `Unselect` of records, and `ClearAll`. Nothing in `Src/` calls them, and 3.0 already breaks plugin compatibility.
- **How often do users select under a filter, then remove the filter?** The answer decides whether U5a-3 matters.
- **Is "no or one record selected saves the whole scope" still the intended behaviour of Save?**
- **Should Toggle Pin have Paste's 100,000 limit?** Paste's reason is memory, which does not apply to pins; the time on the UI thread might.
