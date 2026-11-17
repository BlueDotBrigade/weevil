# Weevil 3.0: Independent Engineering Audit

An independent review of Weevil 3.0 (`main` at `e3dee822`) against Weevil 2.x. The question: are the three 3.0 epics implemented properly, does the new code capture its intent, and is it maintainable? Epic #986 is disk mode, #988 is less memory, and #990 is the immutable core.

> **Status:** Final. 2026-11-17. Detail and evidence for every finding are in [Findings/](Findings/). Measurements are in [Measurements/](Measurements/README.md), and the audit's record is in [Progress.md](Progress.md).

## 1. Executive Summary

**Weevil 3.0 is a real architectural improvement over 2.x, and the measurements back it.**

| Measured here, on Linux | 2.12.1 | 3.0 (`main`) |
|---|---|---|
| RAM to open the 4 GB log (peak) | 12.9 GB | 4.9 GB (62% less) |
| Time to open the 4 GB log | 90 s | 29 s |
| A filter on the 1 GB log | 2.0–3.5 s | 0.1–0.75 s (4–24× faster) |
| Reopen the 4 GB log | (parse again) | 8 s, from the saved table |
| A log larger than RAM | Can't be opened | Disk mode: 22 bytes per record at rest |
| Find on the 1 GB log | 0.45 s | 0.53 s (no faster) |

The new data model is sound:

- **Immutable records.** Records hold no state, so filter threads read without locks.
- **One home per annotation.** Every annotation is kept once, keyed by line number.
- **Selection in Core.** The selection is a set of ranges that Core owns.
- **The record table.** It is immutable and checksummed.
- **One switch for the text.** RAM and disk differ only behind it, and both modes return the same results everywhere they were compared.

**The code is clear in the small and implicit in the large.** Almost every new class says what it is for, why, and which issue made it. Tests grew by 78% and read as specifications. What is missing sits between classes:

- **Policies spread over several classes:** what an open saves, what Reload keeps, whether anything is running.
- **Protocols kept by convention:** a null source means RAM; seven busy flags.
- **Designs recorded only in finished plans:** three of the four largest have no note in `Doc/Design/`.

**One High finding (X-1):** F5 still filters on the UI thread. On a log too large for RAM, which is the case disk mode exists for, that thread reads the whole log from the drive or the network, with no progress and no way to cancel. The fix is small.

**No architectural decision needs reversing before 3.0 ships.** Six things should be fixed or decided first:

- F5 on the UI thread (X-1).
- The buffer churn of filters on disk (P-1).
- Clear failing in a read-only folder (U4-1).
- Telling users about the `.idx` file (U1-5).
- Four short design notes (X-4).
- The owner's call on whether changing storage mode should cost a full parse (P-2).

All are small. See [section 7](#7-prioritised-recommendations).

## 2. How the Audit Was Done

- **Scope.** 454 commits since the 2.12 release were mapped to the three epics. 163 belong to none and were left out ([Scope.md](Scope.md)). The epic code was split into nine review units.
- **Review.** Each unit was reviewed through five lenses: intent, maintainability, architecture, runtime, and tests and documents. U1 had two reviewers; the others had one, after usage limits interrupted the first runs. Fresh-context sceptics challenged every High finding: U1-1 was lowered to Medium, and X-1 was revised but stayed High.
- **Measurement.** Weevil 2.12.1 (NuGet) and `main` were compared on the same Linux machine with the repo's memory harness, plus four small probes written for the audit. Logs were synthetic, of 1 GB and 4 GB.
- **Limits.**
  - Every figure is from Linux, with the log in the page cache. 4 GB is the largest log this environment can open.
  - The WPF application wasn't run.
  - 20 GB logs, cold reads, network shares and Windows itself are listed as gaps for the owner ([Performance.md](Findings/Performance.md#not-measured-for-the-owner-on-windows)).

| Findings | High | Medium | Low |
|---|---|---|---|
| Units U1–U7 ([Findings/](Findings/)) | 0 | 29 | 29 |
| Performance (P-1 to P-5) | 0 | 2 | 3 |
| Cross-cutting (X-1 to X-7) | 1 | 5 | 1 |

## 3. Architecture

**What 3.0 changed.** 2.x held each record as a mutable object with its text as a string. 3.0 splits the log by owner:

- **The log as read:** a line index, a record table of 22 bytes per record, and the text as the file's raw bytes in 16 MB chunks, held in RAM or read back from the file.
- **The session:** comments, pins, bookmarks, regions and filter history, under one write lock.
- **The view:** its scope, filter results as positions, flags, elapsed times and the selection as ranges.

**What works.**

- **Immutable data.** The table, the chunks and the lists never change, so readers need no locks (U2).
- **One home per value.** Every value that changes has one place, keyed by line number (U4).
- **One reader of the log.** `LogFileSource`, a small class, owns every read back from the file. It holds no handle between reads and checks every 64 KB block, so a changed log is detected, not misread (U3).
- **Selection in Core.** It moved from the GUI into Core, as the layering rule asks (U5a).
- **The grid.** Four small classes give a list that WPF never walks, with Core owning the selection (U6b).

**What is weak.**

| Finding | Concern |
|---|---|
| X-2 | **The mode is hidden for the text, not for cost and failure.** On disk, `IRecord.Content` opens the log and can throw, and its contract says neither. The parse result travels as a null that means RAM (U1-2, U2-1, U3-1) |
| X-3, U4-2 | **The log, a view and the session have no types of their own.** The log sits inside `CoreEngine` beside one view, and what Reload keeps is decided in five places. That is the first obstacle #991 will meet |
| U1-1 | **What an open reads, reuses and saves** is decided across two classes through flags on `LineIndex` |
| X-5 | **Which thread owns what is a GUI convention.** A filter is stopped by a shared 2.x flag; the GUI's busy state is seven flags kept in step by hand |

**RAM against disk.**

- **Results.** Both modes return the same results for every filter and encoding tested.
- **Memory.** Disk mode keeps its promise at rest, but not while filtering (P-1).
- **Speed.** From the page cache, disk mode filters in 1.0 to 1.4 times RAM's time.
- **Adding a mode.** A third mode would leave readers untouched, but would change the producers of the table and the GUI's two-value checks.
- **Verdict.** The two-mode design suits large logs. The gaps (X-1, P-1, X-2) are small.

**Room for #991.** The data model is ready: shared immutable tables, stores keyed by line number, and selection in Core. The object model isn't (X-3). The work before #991:

- give the read log and the view their own types (U4-2);
- save the sidecar from the store (U4-1);
- give each filter its own cancellation (U5b-2);
- split `FilterViewModel` along its existing seams (U6a-2).

## 4. Code Quality

**Better than 2.x.**

- **Records are immutable.** 2.x's settable `Metadata` is gone, and with it the half-written records an aborted filter could leave.
- **Fewer locks and races.** Filtering no longer takes a lock for each record. The filter history is no longer changed by the filter's thread while the GUI reads it.
- **Classes explain themselves.** Each states its purpose and cites its issue, and the plans record decisions with measurements.
- **Tests are specifications.** They grew from 16,000 to 28,500 lines, with oracles: the line scanners are checked against `StreamReader`, and reopen against a fresh parse.
- **The grid is one of the clearest parts.** It replaced 2.x's two selections, kept in step by code-behind.

**Worse, or not yet better.**

| Finding | Concern |
|---|---|
| X-4 | **Designs only in finished plans.** `Doc/README.md` says a finished plan's result belongs in `Doc/Design/`. The engine and session, filtering and the grid have no note there, and the open sequence is only partly covered. Reviewers in five of seven units rebuilt a design from code |
| U6a-1, U6a-2 | **`FilterViewModel` grew from 2,195 to 2,941 lines with no new seams.** Whether anything is running is seven flags set by hand in eight methods |
| U5b-1 | **The analyzers are near-copies.** Every 3.0 change to the record model was made by hand in 10 to 14 files |
| X-7, U5a-1, U4-6 | **2.x shapes kept beside 3.0's,** although 3.0 already breaks plugins. `ISelect` has two ways to select with different rules, and the session's stores are public in Common |
| U6b-1 | **The grid's key property is unguarded.** WPF must never walk the row list, and no test or check detects it if it does |

**Size against benefit.** Core grew from 8,700 to 14,500 lines, Common from 6,100 to 8,400 and the GUI from 9,700 to 12,300. The growth pays for itself in the parsing, record, engine and grid code. It doesn't in `FilterViewModel`, where it went into one class, or in the analyzers, where duplication multiplied it ([CrossCutting.md](Findings/CrossCutting.md#complexity-against-benefit)).

## 5. CPU Efficiency

**Measured (Linux, warm cache):**

- **Filters.** Every timed filter is 4 to 24 times faster than 2.12.1 on the 1 GB log, with the same results. On disk, they take 1.0 to 1.4 times RAM's time.
- **Opening.** About 3 times faster. A reopen from the saved table takes 2.1–2.4 s in RAM and 1.3–1.5 s on disk for the 1 GB log, against about 7.5 s to parse.
- **Saving the `.idx`.** It cost no measurable time on a first open, less than the repo's documents claim.
- **Find.** It got no faster: 0.53 s against 0.45 s on the 1 GB log, and on the 4 GB log it takes longer than a whole filter (P-5, U5b-3).

**Findings.**

| Finding | Priority | Concern |
|---|---|---|
| P-2, U1-7 | Medium | **Changing storage mode costs a full parse:** 6.0–6.8 s against 1.3–2.4 s for a same-mode reopen. The mode is part of a saved parse's identity, although for UTF-8 and native UTF-16 the table is the same in both modes |
| X-1 | High | **F5 filters on the UI thread.** With a warm cache it freezes the window about as long as 2.x did. On disk, past the page cache, it reads the drive (unmeasured) |
| (no id) | Documented trade-off | **Each filter creates one `Record` per row:** 48 bytes, about 0.1 s per 7.5 million rows |

**Unverified.** The 20 GB target ("filtered in under 10 s on NVMe", #1024). Linear extrapolation from the 4 GB log gives 6.5–16 s per filter on 4 vCPUs, before any disk reads.

## 6. RAM Efficiency

**Measured.**

| Mode | Retained after opening | Peak while opening |
|---|---|---|
| 2.12.1 | 3.03 GB per GB of log | 3.21 GB per GB |
| RAM | 1.16 GB per GB of log | 1.22 GB per GB |
| Disk | 22 bytes per record (0.69 GB for 30 million records) | (not separated) |

All three agree with the repo's own figures.

**Findings.**

| Finding | Priority | Concern |
|---|---|---|
| P-1, U3-2 | Medium | **A filter on disk churns through 16 MB buffers.** `Parallel.ForEach` replaces its thread-local state, and with it the content buffer: 18 states for 4 workers on the 1 GB log, 47 on the 4 GB log. Each filter on disk allocated 0.4–1.3 GB of large `byte[]` and forced full collections. The working set grew towards the log's size. With the managed heap capped at 1 GB, two of eight filters on the 4 GB log ran out of memory, although its table needs 0.67 GB. A prototype per-filter pool made the large-object allocation constant (about 200 MB) and stopped the full collections, provided the buffers are sized to the chunk |
| P-3, X-6 | Low / Medium | **The RAM figures describe opening, not use.** After eight filters and a Find, the 4 GB log's working set in RAM grew from 4.7 GB to 6.5 GB at the peak. The budget tests and the Open As advice use the at-rest figure. Its 1 GB reserve covered this at 4 GB; larger logs are unmeasured |
| U2-2 | Medium | **A record keeps its whole 16 MB chunk alive,** so in RAM a window that keeps records (the graph) keeps an old log's content |
| U1-5 | Medium | **Every full open writes a `.idx` beside the log,** about 22 bytes per record (167 MB for the 1 GB log). Users aren't told, and nothing can turn it off |

## 7. Prioritised Recommendations

Complexity: S is hours, M is days, L is a week or more. "Owner" marks a decision rather than a fix.

### Before 3.0 Ships

| # | Recommendation | Findings | Benefit | Complexity |
|---|---|---|---|---|
| 1 | Run F5 through the filter's background path, calling `ReApply` on the worker, so it shows progress and Escape stops it. Add a GUI test for a log on disk | X-1, U6a-3 | No uncancellable freeze on large logs on disk | S |
| 2 | Pool content buffers per filter, sized to the chunk and capped at the worker count. Add an allocation budget to the disk filter test | P-1, U3-2 | Large-object allocation per filter becomes constant; no full collections; disk mode works with less memory | S |
| 3 | Stop Clear failing where the sidecar can't be written: at least log a warning, and say on `IEngine.Clear` that it saves | U4-1 | Removes a regression from 2.x in read-only folders | S |
| 4 | Say in Help and the change log what the `.idx` is, how large it gets, and that it is safe to delete | U1-5 | No surprise files beside users' logs | S |
| 5 | Write four short design notes: opening a log, the engine and session, filtering, the grid. Fix the instructions that contradict them | X-4, U1-1, U4-3, U5b-5, U6b-3 | Every later change starts from one page instead of five files | S each |
| 6 | Document on `IRecord.Content` that on disk it reads the log and can throw; name the failure in one exception type | X-2, U2-1, U3-1 | Plugin and script authors can see the cost and the failure | S |
| 7 | **Owner:** should the storage mode stay part of a saved parse's identity for UTF-8 and native UTF-16? | P-2, U1-7 | Switching to Disk costs a reopen, not a parse | S |
| 8 | If 3.0's API is published, remove the 2.x members no production code calls, and make internal the store methods meant only for Core | X-7, U5a-1, U4-6 | A smaller public API to support in 3.x | S |
| 9 | Fix the temporary-folder check and add its test | P-4 | No `.idx` or sidecar saved for a log directly in `%TEMP%` | S |

### After 3.0

| # | Recommendation | Findings | Complexity |
|---|---|---|---|
| 10 | Move Copy to the background through one `ContentBuffer`, with `CopyWithText` in Core | X-1, U2-1 | S |
| 11 | Before the remaining #1157 pieces build on it: keep `Location.Parse`'s disk check off the UI thread, and guard the generic control's boundary | U7-1, U7-2 | S |
| 12 | One cancellation model in Core (a token per call); one busy state in the GUI | X-5, U5b-2, U6a-1 | M |
| 13 | Give the open-and-save policy one home; one header type and one line-break scanner for the `.idx` | U1-1, U1-3, U1-4 | M |
| 14 | Share the analyzers' common prologue and their near-copies | U5b-1 | M |
| 15 | Guard the grid's scaling property with a test or a debug check | U6b-1 | S |
| 16 | Add a memory budget for a filter in each mode; state the peak in use in `DiskMode.md` | X-6, P-3 | S |

### Before #991 Starts

| # | Recommendation | Findings | Complexity |
|---|---|---|---|
| 17 | Save the sidecar from the store; then remove Clear's save | U4-1, X-3 | S–M |
| 18 | Give a read of the log and a view their own types; Reload replaces the log and re-applies each view | U4-2, X-3 | M–L |
| 19 | Split `FilterViewModel` along the seams U6a-2 names | U6a-2, X-3 | M |

### Measurements for the Owner (Windows)

These are the gaps this environment couldn't cover:

- a 20 GB log on disk, from NVMe and from SATA (#1024);
- disk mode with a cold cache, and under real memory pressure (P-1);
- a log on a network share;
- F5 and Copy in the WPF application on a large log on disk;
- 2.12.1's filters at 4 GB.

The tools and commands are in [Measurements/README.md](Measurements/README.md) and [Performance.md](Findings/Performance.md#not-measured-for-the-owner-on-windows).

## 8. Final Assessment

| Question | Answer |
|---|---|
| **1. Is 3.0 a meaningful architectural improvement over 2.x?** | **Yes.** Immutable records, one home per value, an immutable checksummed table, selection in Core, and a second storage mode behind one reader. These are real structural gains, not refactoring for its own sake |
| **2. Has code quality and maintainability improved?** | **Improved in the small; debt in the large.** Classes, comments and tests are better than 2.x. The debt is in cross-class policies and protocols, designs recorded only in plans, a view-model that grew without seams, and duplicated analyzers. Writing the four design notes (recommendation 5) would recover most of the maintainability cost for little effort |
| **3. Have CPU and RAM efficiency improved?** | **Improved, and measured** on Linux up to 4 GB: 62% less memory at the peak, opening 3 times faster, filters 4–24 times faster, reopen 3–4 times faster. Find did not improve. Disk mode at 20 GB, from a cold cache or a network share, remains unverified |
| **4. Are the RAM and disk architectures appropriate for large logs?** | **Yes.** The foundation is sound and the two modes agree. Three small gaps remain: F5 reads the log on the UI thread (X-1); filters on disk churn buffers (P-1); a read's cost and failure are not in the contract (X-2) |
| **5. What should be reconsidered before release?** | No architectural decision needs reversing. Fix or decide recommendations 1–7: F5, the disk buffers, Clear in a read-only folder, the `.idx` documentation, the design notes, the `IRecord.Content` contract, and whether the storage mode belongs in a saved parse's identity |
