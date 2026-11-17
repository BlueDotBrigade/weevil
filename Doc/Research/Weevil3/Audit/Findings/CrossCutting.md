# Cross-Cutting Assessment

Stage 3 of the [Weevil 3.0 audit](../Progress.md). This stage asks how the units fit together. It covers patterns that recur across them, RAM against disk mode as a whole, the room left for #991, and whether the added complexity pays for itself. It builds on the unit findings (U1 to U7) and on [Performance.md](Performance.md), and does not repeat their evidence.

> **Status:** Final. Written and challenged on 2026-11-17 at `main` e3dee822.

## Summary

**3.0 is a real architectural improvement, and the measurements back it.**

- **Measured gains.** On the 4 GB log, opening needs 62% less memory at the peak than 2.12.1 and is about 3 times faster. Filters are 4 to 24 times faster on the 1 GB log. A reopen skips the parse. A log no longer has to fit in RAM at all.
- **A sounder data model.** Records hold no state. The table, chunks and lists never change, so filter threads need no lock. Every user annotation has one home, keyed by line number. The selection is a set of ranges that Core owns.
- **The code grew.** Core grew from 8,700 to 14,500 lines, Common from 6,100 to 8,400, the GUI from 9,700 to 12,300, and the tests from 16,000 to 28,500. For what it buys, the growth is justified.

**The code is clear in the small and implicit in the large.** Almost every class says what it is for, why, and which issue made it; reviewers in every unit said so. What is missing sits between classes:

- **Policies spread over several classes.** Examples: what an open saves (U1-1), what Reload keeps (U4-2), whether anything is running (U6a-1).
- **Protocols kept by convention.** Examples: a null source means RAM (U1-2); seven busy flags (U6a-1); a selection refresh the view-model must remember (U6b-2).
- **Designs described only in finished plans.** `Doc/README.md` says a finished plan's result belongs in `Doc/Design/`. Three of the four largest 3.0 designs have no note there: the engine and session, filtering, and the grid. The fourth, the open sequence and its `.idx`, is only partly covered by `RecordTable.md`.

**Disk mode hides the mode for the text, but not for cost and failure.** The same `IRecord.Content` returns the same text in both modes. On disk, though, each read opens the log and can throw. That difference reaches the UI thread through two paths inherited from 2.x, F5 and Copy. Both read the log there in disk mode, against the repository's own rule (X-1, the one High finding of this stage).

**#991 will find the data model ready and the object model not.** The log, a view of it and the session have no types of their own. That's the first thing multiple views will need (X-3).

## Findings

### X-1. Disk mode turned two inherited UI-thread paths into file I/O: F5 and Copy

- **Priority:** High, for F5. Copy alone would be Medium.
- **Kind:** Architecture
- **New in 3.0?** An existing weakness that disk mode makes worse. Both paths ran on the UI thread in 2.x, and F5 already froze the window there for seconds on a large log. On disk, both now read the log on the UI thread.
- **Evidence:**
  - **The rule.** "Preserve UI responsiveness; do not block the UI thread" (`.github/copilot-instructions.md`, Architectural Rules). The review rules flag "any code placed on the UI thread that performs I/O, filtering, or other long-running operations" (`.github/instructions/review.instructions.md`).
  - **F5 (U6a-3).** `Refresh` runs `_engine.Filter.ReApply()` inside the command handler (`Src/BlueDotBrigade.Weevil.Gui/Filter/FilterViewModel.cs:1711-1726`; `Gui/Input/UiBoundCommand.cs:102-119`). `ReApply` runs a full filter (`Src/BlueDotBrigade.Weevil.Core/Filter/FilterManager.cs:298-306`). On disk, a filter that tests text reads the whole log; with no filter, F5 reads nothing (`Core/Filter/FilterStrategy.cs:133-137`). The window has no progress bar while it runs, and Escape can't stop it.
  - **What the timings show.** From the page cache, a filter on the 4 GB log takes 0.41–3.3 s on disk and 0.44–3.1 s in RAM ([Ops-4gb-main-disk-run2.txt](../Measurements/Ops-4gb-main-disk-run2.txt), [Ops-4gb-main-ram-run2.txt](../Measurements/Ops-4gb-main-ram-run2.txt)). 2.12.1 took 3.2–4.1 s on the 1 GB log in RAM ([Ops-1gb-2.12.1-run2.txt](../Measurements/Ops-1gb-2.12.1-run2.txt)). So with a warm cache, the F5 freeze is the inherited one, and it lasts as long in RAM as on disk. Disk mode adds the cost of reading the drive. Open As suggests Disk only when the log's estimate plus 1 GB exceeds the available memory (`Gui/IO/OpenAsAdvice.cs:49-54`). A log well past that, the case disk mode is for (`Doc/Plans/986-DiskMode.md:12-13`), can't stay in the page cache, so every F5 with a text filter reads it from the drive or the network. That cost has not been measured.
  - **Copy (U2-1).** Ctrl+C formats the selection one record at a time through `record.Content` on the UI thread (`Gui/ClipboardHelper.cs:22-49`; `FilterViewModel.cs:1143-1161`). On disk, each read opens the log and reads and hashes at least one 64 KB block (`Src/BlueDotBrigade.Weevil.Common/IO/LogFileSource.cs:154-183`). Copy refuses more than 100,000 records. The limit was set so the window wouldn't freeze while the clipboard fills (`FilterViewModel.cs:66-69`, `:1193-1206`). At that limit, with the records spread over the log, Copy takes 0.03–0.09 s in RAM, 0.9–1.0 s on disk for the 1 GB log and 1.2–1.3 s for the 4 GB log ([Copy-100k.txt](../Measurements/Copy-100k.txt)). A usual selection of a few rows takes milliseconds. One `ContentBuffer` saves little for spread records on the 4 GB log (1.0 s against 1.2), so the gain would come from leaving the UI thread, not from the buffer.
  - **Why they are there.** #1027 left Copy out on purpose: "A selection is usually small; selecting every record of a large log on disk is slow, as it is in RAM" (`Doc/Plans/1027-StreamingReads.md:93`). At the limit, though, disk is 10 or more times slower than RAM. #1028 gave F5 its failed-read catch (`Doc/Plans/1028-OpenAs.md:155`, commit d2a74c3f), but moved only export, split and Find off the UI thread (`:47-52`, `:177-187`). It records no reason for leaving F5 behind. The graph's copy of the same records already runs in the background through one `ContentBuffer` (`FilterViewModel.cs:1436-1455`, `:1549-1556`).
  - **Smaller paths of the same kind** (from U1, U6a, U6b and U7):
    - Ctrl+S and Exit save the sidecar in the handler, and a save opens the log to find each annotated line's byte offset (`FilterViewModel.cs:1069-1072`, `:1307-1312`; `Src/BlueDotBrigade.Weevil.Core/CoreEngine.cs:441-456`).
    - Each realized grid row reads its text two or three times. #1028 measured that design at 1.9 ms a screen.
    - `Location.Parse` checks the disk on the UI thread (U7-1), but nothing uses it yet.
- **Impact:**
  - **Measured here.** Every figure above is from Linux, with the log in the page cache and at most 4 GB. They are the best case, and for F5 they match RAM mode.
  - **Expected where disk mode is used.** Disk mode exists for logs too large for RAM. Its target is a 20 GB log filtered in under 10 s on NVMe, and several times slower on a SATA SSD or a network drive (`986-DiskMode.md:13`, `:74`). An F5 freezes the window for as long as that filter takes. Open As warns when Disk is chosen for a log on a network drive, but allows it (`OpenAsAdvice.cs:76-78`). Copy opens the file once for each record, so on a network share each of up to 100,000 opens crosses the network. Weevil's own monitor treats a 1 s delay as unresponsive (`Gui/Analysis/UiResponsivenessMonitor.cs:17`).
  - **The result:** the window freezes with no progress bar and no way to cancel, in exactly the setting disk mode was built for. F5 carries the risk. Copy takes at most about 1.3 s on a local drive.
- **Recommendation:**
  - **F5.** Before 3.0 ships, run it through the filter's background path, so it shows progress and Escape aborts it. Use a re-apply variant of `FilterAsynchronously` that calls `ReApply` on the worker. Its `FilterAsync` calls `Apply` (`Gui/Filter/FilterExtensions.cs:20`), which would change what F5 does: it would measure new elapsed times instead of keeping those its filter compared (#1224), and it would record telemetry.
  - **Copy.** This can wait until after 3.0. Move it into `RunInBackground` with a snapshot of the selection, as the graph does, and set the clipboard in `onSucceeded`, on the UI thread. Per U2-1, move `CopyWithText` into Core first, so the CLI and PowerShell share it.
  - **Ctrl+S and Exit.** Move the sidecar save off the UI thread once it saves from the store (U4-1).
  - **Prevent recurrences.** Add one GUI feature test that opens a log on disk and checks that F5 shows the progress bar.
  - **Complexity:** S for F5 and Copy.
- **Sceptic:** Revised: the priority stays High, because of F5. The warm-cache F5 timings are as long in RAM and were longer in 2.12.1, so they show the inherited freeze, not a disk-mode cost. The disk-mode cost is reading a log larger than the page cache from the drive, which has not been measured. Copy is a choice #1027 recorded, its limit holds it to about 1.3 s, and on its own it is Medium; Open As does not suggest Disk for a network log; and the F5 fix must call `ReApply`, not `Apply`, to keep #1224's behaviour.

### X-2. The mode is hidden for the text, not for cost and failure

- **Priority:** Medium
- **Kind:** Architecture
- **New in 3.0?** New design
- **Evidence:**
  - **What is hidden.** Reading text goes through `ContentChunk`, so filters, Find, analyzers, export, the CLI and PowerShell never ask the mode (U3, Strengths). Every timed filter returns the same results in both modes (U5b).
  - **What leaks:**
    - **Failure.** A read can throw only on disk, and `IRecord.Content` doesn't say so (U2-1, U3-1).
    - **Cost.** The easy loop costs a file open per record on disk (U2-1). A filter churns 16 MB buffers on disk (P-1). Changing mode costs a full parse (U1-7, P-2).
    - **Production of the table.** "No source means RAM" is read back from a null (U1-2). Outside Core, the mode is tested as one of two values (U3-7).
- **Impact:** Code that is correct and fast in RAM can be slow or can throw on disk, and nothing in the types warns its author. That's how X-1 happened. A plugin or script author who reads only `IRecord` has no way to know.
- **Recommendation:** Make the difference part of the contract, not a paragraph in `DiskMode.md`:
  - **Document `IRecord.Content` once:** on disk it reads the log and can throw an `IOException`, and many records are read through a `ContentBuffer` (U2-1).
  - **Name the failure:** one exception type for "the log could not be read" (U3-1).
  - **Name the parse result:** a parse returns one value that carries its table, source and mode (U1-2).
  - **Narrow the identity:** compare the mode in a saved parse only where it shapes the table (U1-7).
  - **Complexity:** S each.
- **Sceptic:** Not challenged (Medium)

### X-3. #991 will find the data model ready and the object model not

- **Priority:** Medium
- **Kind:** Architecture
- **New in 3.0?** New design
- **Evidence:**
  - **Ready:**
    - The record table, the content chunks and the line index are immutable and can be shared by views (U1, U2).
    - Annotations live in the session's stores, keyed by line number (U4).
    - The selection and its anchor live in Core as ranges (U5a).
    - The grid's rows read the stores, not the records (U6b).
  - **Not ready:**
    - **No types.** There is no type for "a read of the log" or for "a view". The session holds the user's state but not the log. The log sits inside `CoreEngine` beside one view's filter and selection, and what Reload keeps is decided in five places (U4-2).
    - **The GUI matches it.** `FilterViewModel` holds both the open log's lifetime and one view's state (U6a-2).
    - **The sidecar save walks one view's scope,** which is why Clear saves first (U4-1).
    - **Shared flags.** A filter is stopped by a shared flag that an overlapping filter resets (U5b-2).
    - **Retention.** A record keeps its whole 16 MB chunk alive, so a window that keeps records keeps an old log's content (U2-2).
- **Impact:** Each of these is fine for one view. With two, each becomes a design change: `CreateView()` would have to copy the log's state out of a `CoreEngine`, and Reload would have to rebuild every view. The cost of #991 is mostly here, not in the data model.
- **Recommendation:**
  - **Before starting #991, not before 3.0:**
    - Give the read log and the view their own types (U4-2).
    - Save the sidecar from the store (U4-1).
    - Give each filter its own cancellation (U5b-2).
    - Split `FilterViewModel` along the seams U6a-2 names.
  - **Order.** U4-1 is the cheapest and removes a 3.0 regression too, so do it first.
  - **Complexity:** M to L in total.
- **Sceptic:** Not challenged (Medium)

### X-4. Design knowledge lives in finished plans, while `Doc/Design` lags

- **Priority:** Medium
- **Kind:** Maintainability
- **New in 3.0?** New design (documentation debt of the epics)
- **Evidence:**
  - **The rule.** A finished plan's result belongs in `Doc/Design/`, and the plan "can then be deleted" (`Doc/README.md`, Plans).
  - **Epic #990 is Done; its design has no note.** No design note describes the engine and session; the plan is the only description, and it differs from the code (U4-3).
  - **Epic #988 is Done; its result is partly documented.** `RecordTable.md` describes the file layout but not what each open writes (U1-1). Users aren't told about the `.idx` (U1-5).
  - **Filtering has no note,** and four comments describe 2.x behaviour (U5b-5).
  - **The grid has no note,** and the GUI instructions contradict three of its deliberate choices (U6b-3).
  - **The instructions lag too.** `.github/instructions/core.instructions.md` still sizes logs at "hundreds of thousands of records" (U1, Minor).
- **Impact:** The code's local clarity is excellent, but the global designs (what an open saves, what Reload keeps, which thread owns what, how the grid scales) are reconstructed from five to seven files or from a dated plan. The plans record decisions well, but `Doc/README.md` calls them "a dated record", not the current design. Reviewers in five of seven units had to rebuild a design from code.
- **Recommendation:** Before 3.0 ships, write four short notes in `Doc/Design/`:
  1. **Opening a log:** the open paths and what each reads and writes (U1-1).
  2. **The engine and its session:** lifetimes, what Clear and Reload keep, the session's lock (U4-3).
  3. **Filtering:** threads, buffers, cancellation, the pinned and bookmarked override (U5b-5).
  4. **The grid:** what Core owns, what the GUI owns, why WPF must never walk `RowList` (U6b-3).

  Fix the instructions that contradict the code. Each note takes about an hour, and every later change starts from it. **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### X-5. Who runs what, on which thread, is a GUI convention

- **Priority:** Medium
- **Kind:** Maintainability
- **New in 3.0?** Existing weakness, widened by 3.0
- **Evidence:**
  - **No Core contract.** Nothing in Core says which thread raises events, or which calls may overlap (U5b-2).
  - **Two ways to stop work.** A filter is still stopped by 2.x's shared flag, while #1027 gave the other long operations their own token (U5b-2).
  - **Seven flags in the GUI.** Whether anything is running is decided by seven flags set by hand in eight methods, with `CanOpenLogFile` as the lock (U6a-1).
  - **Stores that differ.** The session's stores share one write lock but differ in how reads and change events work (U4-5).
  - **3.0 added background work:** Find, Clear, analysis, export and split (U6a). That gave the convention more to coordinate.
- **Impact:** Each new background operation must learn the protocol from the code and repeat it by hand. A mistake shows up as a race rather than a compile error. Multiple views (#991) multiply the number of concurrent operations.
- **Recommendation:**
  - **One cancellation model for every long operation in Core:** a token per call, replacing the shared abort flag (U5b-2).
  - **One busy state in the GUI,** for example a single "current operation" object that both enables commands and owns the token (U6a-1).
  - **Document the threading contract** in the engine note (X-4).
  - **Complexity:** M.
- **Sceptic:** Not challenged (Medium)

### X-6. Memory is budgeted for opening, not for use

- **Priority:** Medium
- **Kind:** RAM
- **New in 3.0?** New design
- **Evidence:**
  - **What is budgeted.** The budget tests measure memory kept after opening (`Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/EngineTests.cs:48-90`). The documents and the Open As window quote the same figures (Performance, Claims Checked).
  - **What happens in use:**
    - On disk, a filter churns 16 MB buffers (P-1).
    - In RAM, the 4 GB log's working set grew from 4.7 GB to 6.5 GB at the peak over eight filters and a Find (P-3).
    - A record keeps its 16 MB chunk alive (U2-2).
    - Opening a second log builds the new engine while the old one is held (U6a, To Measure).
- **Impact:** The memory goal of #988 and #986 is met at rest and verified by tests. Nothing guards it while the user works, which is where P-1 hid. The advice that chooses between RAM and Disk rests on the at-rest figure.
- **Recommendation:**
  - **Add one budget for a filter in each mode,** as allocated bytes per record, so P-1 cannot return.
  - **State the peak in use** beside the at-rest figure in `DiskMode.md` and Help.
  - **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### X-7. 3.0 already breaks plugins, so it is the time to drop the 2.x shapes it kept

- **Priority:** Low
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **The break is already accepted.** External plugins need not stay compatible (`Doc/Research/Weevil3/Feasibility.md`, Answer), and `IRecord` lost `Metadata`.
  - **2.x shapes kept anyway:**
    - `ISelect` keeps 2.x's record-based members beside the 3.0 line-range members, with different rules (U5a-1).
    - `Selected` keeps 2.x's dictionary shape (U5a-2).
  - **New public surface in Common:**
    - The session's stores, so plugins can reach their load and bulk methods (U4-6).
    - `RecordFacts` exposes the table's storage bit (U2-4).
    - `IEngine` mixes the log's members with the view's, and one of its events has no caller (U4-7).
- **Impact:** Every public member kept now has to be supported, or broken again, in 3.x or 4.0. Plugin authors meet two ways to do the same thing.
- **Recommendation:**
  - **Before 3.0's API is published,** remove what no production code calls (U5a-1).
  - **Make internal** the store methods meant only for Core (U4-6).
  - **Decide `RecordFacts`'s storage bit:** hide it, or document it (U2-4).
  - **Complexity:** S.
- **Sceptic:** Not challenged (Low)

## RAM and Disk Mode as a Whole

| Question | Answer | Evidence |
|---|---|---|
| Is the mode behind one abstraction? | For reading text, yes. For cost, failure and producing the table, no | X-2 |
| Do both modes give the same results? | Yes, for every filter timed, every encoding tested, and reopen against parse | U2, U5b, U1 |
| Does disk mode keep its memory promise? | At rest, yes: 22 bytes per record. While filtering, no: it churns 16 MB buffers and needs the table plus 0.5–0.8 GB | Performance, P-1 |
| Is disk mode fast enough? | From the page cache, 1.0 to 1.4 times RAM's time for filters, 1.4 times for Find. Cold reads, 20 GB and network shares are unmeasured | Performance |
| Is a changed log handled? | Yes. Every 64 KB block is checked, a change is reported once, and Reload recovers | U3 |
| Can a third mode be added? | Readers would not change. The producers of the table and the GUI's two-value checks would | U1-2, U3-7 |
| Is the choice between modes sound? | RAM's estimate is accurate for opening. Disk's own need is never estimated, and switching mode costs a full parse | U3-5, P-2, P-3 |

**Verdict.** The two-mode design is appropriate for large logs. Its foundation is sound: an immutable table, a small reader that checks every block, and one switch for the text. The cost of disk mode is pushed to the readers, and three places don't account for it yet:

- reads on the UI thread (X-1);
- the buffer churn while filtering (P-1);
- the failure contract (X-2).

All three are small changes.

## Complexity Against Benefit

| Area | Growth | What it buys | Justified? |
|---|---|---|---|
| Line index, parsing, saved table (U1) | About 9 times 2.x's parse code | Byte offsets, raw-byte content, reopen 3 to 4 times faster, disk mode | Yes. The policy needs one home (U1-1) |
| Record representation (U2) | About 2.5 times | 1.22 GB per GB of log instead of 3.25 | Yes |
| Engine and session (U4) | About 2 times | Immutable records, one home per value, lock-free reads | Yes. The lifetimes need types (U4-2) |
| Selection (U5a) | Grew | One selection as ranges; no quadratic re-select | Yes. The 2.x API kept beside it should go (X-7) |
| `FilterViewModel` (U6a) | 2,195 to 2,941 lines | Background work, Open As, disk-mode failures | Partly. The growth went into one class without seams (U6a-2) |
| Grid (U6b) | Four small classes | 100 million rows; one selection | Yes. It's one of the clearest parts |
| Analyzers (U5b) | Mechanical changes to near-copies | Nothing new | No. The copies multiplied the cost of every 3.0 change (U5b-1) |

## What Improved Over 2.x, and What Didn't

- **Improved:**
  - **Immutability.** Records hold no state. 2.x had settable `Metadata`, and a filter that was aborted could leave records half-written.
  - **Locking.** Filtering no longer takes a lock for each record. The filter history is no longer a list that the filter's thread changed while the GUI and the sidecar save read it.
  - **Selection.** It lives in Core, and a large selection no longer freezes a filter.
  - **Speed and memory.** Measured, not estimated.
  - **Tests.** They grew by 78% and read as specifications, with oracles for parsing and reopening.
- **Not improved, or new weaknesses:**
  - **Implicit cross-class protocols:** X-4, X-5, U1-1, U4-2.
  - **UI-thread reads on disk:** X-1.
  - **Analyzer duplication:** U5b-1.
  - **One regression in a read-only folder:** Clear now saves the sidecar first and fails there (U4-1).

## Open Questions

- **Is F5 on the UI thread deliberate?** If so, the decision isn't recorded (X-1).
- **Should the session own the read log before #991, or as its first step?** (X-3)
