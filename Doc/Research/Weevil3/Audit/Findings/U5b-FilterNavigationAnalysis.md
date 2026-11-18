# U5b: Filtering, Navigation and Analyzers (#990, #988)

Part of the [Weevil 3.0 audit](../Progress.md). This code decides which records a view shows, moves the active record (Find, Go To, next comment, flag, pin or elapsed time), and runs the analyzers and insights that flag and comment records.

> **Status:** Final. Reviewed on 2026-11-17 at `main` e3dee822, by one reviewer covering every lens.

## Summary

Filtering keeps its 2.x shape and its semantics, and it is much faster. On the 1 GB log, every timed filter is 4 to 30 times faster than 2.12.1 in RAM, and 3 to 30 times faster on disk. All eight timed filters return the same counts in 2.12.1, in RAM and on disk. The 3.0 changes that matter are small, and their reasons are written beside them. An expression reads text through a `ContentBuffer`, so RAM and disk sit behind one type. Results are positions, not records. `@Elapsed` compares elapsed times that never change once created, where 2.x read fields on the records that an aborted filter had already overwritten. Navigation reads by line number or position, and creates only the record it finds.

The analyzers changed mechanically, so they kept 2.x's near-copies: rising and falling edges, first and last occurrence, the two time-gap analyzers, and a 30-line prologue repeated in ten analyzers. As a result, every 3.0 change to the record model was made in 10 to 14 analyzer files, and #1150's record-free style reached only 3 of the 13 analyzers (U5b-1). **The main architectural concern is that the threading model is a GUI convention.** A filter is still stopped by 2.x's shared flag, which an overlapping filter resets, while #1027 gave every other long operation its own token. No Core contract says which thread raises events, or which calls may overlap (U5b-2). Find stays on one thread, as decided, and on the 4 GB log it is now slower than a whole filter (U5b-3). The unit has no High finding.

## What Was Examined

- **Core filter:** `Filter/FilterManager.cs`, `FilterStrategy.cs`, `FilterMapReduceState.cs`, `Expressions/**` (the builder, every moniker, plain text). Callers: `CoreEngine.cs` (construction, the results handler, Clear), `Engine.cs` (event forwarding), `DefaultCoreExtension.cs` (metric collectors), `Data/RecordRanges.cs` (as used).
- **Core navigation:** `Navigation/NavigationManager.cs`, `ActiveRecord.cs`, and the content, comment, flag, pin, elapsed-time and timestamp navigators.
- **Core and Common analysis:** `AnalysisManager.cs`, `AnalyzerExpressionHelper.cs`, `Timeline/AnalysisHelper.cs`.
  - Sampled analyzers: `DetectRisingEdgeAnalyzer` and `DetectFallingEdgeAnalyzer`, `FirstOccurrenceAnalyzer` and `LastOccurrenceAnalyzer`, `DetectDataAnalyzer` and `StatisticalAnalyzer` (Core), and `TimeGapAnalyzer`, `TimeGapUiAnalyzer` and `OutOfOrderTimestampsAnalyzer` (Common).
  - Diffs against `Releases/2.x` of `StableValueRunsAnalyzer` and `ThresholdCrossingsAnalyzer`.
  - `IRecordAnalyzer`, `IAnalyze`, `IMetricCollector`, `SeverityMetrics`, `TemporalAnomalyMetrics`, `CriticalErrorsInsight`, `InsightBase`.
- **Common:** `Filter/IFilter*.cs`, `IFilterStrategy.cs`, `Expressions/IExpression.cs`, `LogicalOrOperation.cs`, `Regular/RegularExpression.cs`, `Navigation/RecordSearch.cs`. As used here: `Data/ContentBuffer.cs`, `ElapsedTimes.cs`, `IRecordState.cs`, `RecordState.cs`, `FlagSet.cs` and `AnnotationStore.cs`.
- **GUI, as caller:** `Filter/FilterViewModel.cs` (starting and aborting a filter, Find, `RunInBackground`, the annotation handler), `Filter/FilterExtensions.cs`.
- **2.x:** `FilterManager.cs`, `FilterStrategy.cs`, the expressions, `ContentNavigator.cs`, `ActiveRecord.cs`, `RecordSearch.cs`, `SeverityMetrics.cs`, `Record.cs` (`HasContent`), `Metadata.cs` (comments and pins), the analyzers above, and `FilterViewModel.cs` (overlapping filters).
- **Documents:** `Doc/Design/DiskMode.md`, `RecordTable.md`, `InsightReport.md`. The plans 990 (elapsed time), 988 (line 44), 1027 (Abort), 1150 (record-free reads) and 991. `Doc/Research/Weevil3/Feasibility.md`, the core and common instructions, and the monikers section of `Doc/Notes/Release/Help.md`.
- **Tests:**
  - Core unit tests: `Filter/*`, `Navigation/*`, `Analysis/**` (names, and samples of the bodies).
  - Core feature tests: `Filtering.feature`, `FilterOptions.feature`.
  - Core performance tests: `Filter/FilterManagerTests.cs`, `Analysis/AnalysisManagerTests.cs`, and the filter, Find and analyzer benchmarks.
  - `Tst/Disk.runsettings`, `.github/workflows/dotnet.yml`.
- **Measurements:** `Measurements/Ops-1gb-*.txt`, `Ops-4gb-*.txt` and `AllocDiag-1gb.txt`, used as they are, with nothing measured again.
- **History:** the analyzer commits since `baa70a9d`.
- **SonarCloud:** the leads for this unit in [Progress.md](../Progress.md#external-evidence-sonarcloud).

## Strengths

- **The filter's semantics are preserved, and the measurements confirm it.**
  - **The rule is unchanged.** `CanKeep` still tests, in order: no criteria, then pins and bookmarks, then include, then exclude (`Src/BlueDotBrigade.Weevil.Core/Filter/FilterStrategy.cs:129-173`). Only the source of a pin moved, from the record to the session's store, read by line number (`:141`). Bookmarks are still found by line number (`:142`).
  - **The counts match.** Each of the eight filters timed on 7,547,742 records returns the same count in 2.12.1, in RAM and on disk (`Measurements/Ops-1gb-*.txt`). On the 30-million-record log, RAM and disk agree too (`Ops-4gb-*.txt`).
  - **Each deliberate change has an issue and a test:**
    - `@Severity=warning` ignores case (#1227, `Expressions/Monikers/SeverityTypeExpression.cs:26-27`). In 2.x it matched nothing (`:20`).
    - F5 and Reload compare the elapsed times the filter compared before, while Apply measures them again over the records shown (#1224, #1231, `FilterManager.cs:294-317`).
    - An aborted filter no longer erases the elapsed times of the results still shown (#1016, regression test `Tst/BlueDotBrigade.Weevil.Core-UnitTests/Filter/FilterManagerTests.cs:15-40`).
  - **The rewritten length moniker is equivalent.** `ContentLengthExpression` rewrites 2.x's `HasContent && Length >= n` as `!IsWhiteSpace() && Length >= n` over a span: the same result (2.x `Common/Data/Record.cs:51`).
- **Filters are much faster, as measured.** Linux, 4 vCPUs. Each cell is the median of 5, given as the range over two sessions:

  | 1 GB log, 7,547,742 records | 2.12.1 | `main`, RAM | `main`, disk |
  |---|---|---|---|
  | Plain text `timeout` | 2.78 – 4.05 s | 0.34 s | 0.44 – 0.45 s |
  | Regular expression `retry.*connection` | 2.89 – 3.53 s | 0.39 s | 0.46 – 0.56 s |
  | Regular expression with groups | 2.95 – 3.42 s | 0.75 – 0.79 s | 0.86 – 0.88 s |
  | Moniker `@Severity=Warning` | 2.38 – 3.29 s | 0.10 – 0.11 s | 0.11 – 0.12 s |
  | Clear the filter | 2.00 – 2.19 s | 0.29 – 0.36 s | 0.32 – 0.37 s |

  The 2.x filter did several things that 3.0 does not:
  - It forced a full collection after every filter (2.x `FilterManager.cs:327`, removed by #969).
  - It wrote an elapsed time to every hidden record, then measured them all again (`:150`, `:322`).
  - Its threads shared one `Regex`: a regex filter allocated 2.5–4.7 GB in 2.12.1, against 0.37 GB on `main` in RAM.

  Which of these contributes most has not been measured.
- **RAM and disk sit behind one type.**
  - Every expression and strategy reads text with `content.Read(record)`, which `IExpression` and `IFilterStrategy` make part of their contract (`Src/BlueDotBrigade.Weevil.Common/Filter/Expressions/IExpression.cs:7-9`; `Common/Filter/IFilterStrategy.cs:11-13`).
  - No navigator or analyzer tests the storage mode. The unit's only mode test is `FilterManager`'s null source (U1-2).
- **Results are positions.**
  - A run marks the positions it keeps in a `bool[]`, then hands an `int[]` to the table's list (`FilterManager.cs:143-146`, `:225-240`). A run that keeps every record returns the list itself (`:219-223`).
  - 2.x allocated an `IRecord[]` the length of the log, then compacted it.
- **Inside one run, the threading is sound and visible.**
  - **Shared inputs are immutable or lock-free:**
    - the strategy's fields are read-only;
    - flag reads never wait (`Common/Data/FlagSet.cs:10-11`), and bookmarks are published as new arrays (`Core/BookmarkManager.cs:9`);
    - elapsed times "never change once created" (`Common/Data/ElapsedTimes.cs:15`) and are calculated before the loop starts (`Expressions/Monikers/ElapsedGreaterThanExpression.cs:27-29`);
    - each thread has its own `Regex` (`Common/Filter/Expressions/Regular/RegularExpression.cs:45-47`).
  - **Workers share no mutable state.** They write disjoint slots of `isKept` and merge their counts with `Interlocked` (`FilterManager.cs:180`, `:195`).
- **Read errors surface as they were thrown.** A failed read on disk is thrown as the read threw it, not wrapped in an `AggregateException` (`FilterManager.cs:199-203`), as `Doc/Design/DiskMode.md` (step 3 of "A Log That Changed") says.
- **Navigation creates only the record it finds.**
  - The comment, flag and pin searches read the stores by line number (`Navigation/CommentNavigator.cs:20-24`, `FlagNavigator.cs:18-22`, `PinNavigator.cs:19-21`).
  - The elapsed-time search measures by position (`ElapsedTimeNavigator.cs:39-57`).
  - Find reads text by position into one buffer (`ContentNavigator.cs:19-49`), and allocated 0.0 MB on the 1 GB log in RAM. A test proves it creates no record (`Tst/BlueDotBrigade.Weevil.Core-UnitTests/Navigation/ContentNavigatorTests.cs:147`).
  - `RecordSearch` no longer builds a fake `Record` to search with (2.x `RecordSearch.cs:32-49`).
- **Find and the analyzers can be cancelled.** They take a token and stop at the next record (#1027). A test aborts a search part-way (`ContentNavigatorTests.cs:13`).
- **Insights keep line numbers, not records** (`Common/Analysis/CriticalErrorsInsight.cs:16-37`), as `Doc/Design/InsightReport.md` says.
- **The record-free analyzers are simpler than their 2.x versions.** `TimeGapAnalyzer` is now one loop over timestamps, with no helper to find the next one (`Common/Analysis/TimeGapAnalyzer.cs:86-100`, against 2.x `:67-125`).
- **Remarks give the reason and the issue.** Examples:
  - why ranges follow chunks (`FilterManager.cs:152-153`, `:159`);
  - why elapsed times are calculated in the constructor (`ElapsedGreaterThanExpression.cs:27-29`);
  - why each thread has its own `Regex`, and why it is created in the original culture (`RegularExpression.cs:45-47`).
- **Room for #991.** The split matches the #991 plan (`Doc/Plans/991-MultipleViews.md:36-37`), with one exception (see Minor):
  - each engine owns its filter, its scope, its flags and its elapsed times;
  - the filter history and the annotations live in the session (`FilterManager.cs:51-53`; `Common/Data/IRecordState.cs:9-11`).
- **Specifications for the override.**
  - `Filtering.feature` states the include, exclude, pin and bookmark matrix as Gherkin, with requirement tags (`Tst/BlueDotBrigade.Weevil.Core-FeatureTests/Filter/Filtering.feature:3-258`).
  - CI runs it in RAM and on disk (`.github/workflows/dotnet.yml:60-66`).

## Findings

### U5b-1. The analyzers are near-copies, so 3.0 changed each copy by hand, and the record-free style reached 3 of 13

- **Priority:** Medium
- **Kind:** Maintainability
- **New in 3.0?** Existing weakness: the copies are 2.x's. The two styles of reading records are new in 3.0.
- **Evidence:**
  - **Pairs that differ by one line.**
    - `DetectRisingEdgeAnalyzer` and `DetectFallingEdgeAnalyzer` (155 and 154 lines) differ only in names, comments and the comparison (`Src/BlueDotBrigade.Weevil.Core/Analysis/Timeline/DetectRisingEdgeAnalyzer.cs:120`, `DetectFallingEdgeAnalyzer.cs:119`).
    - `FirstOccurrenceAnalyzer` and `LastOccurrenceAnalyzer` differ only in the direction of the loop (`FirstOccurrenceAnalyzer.cs:82`, `LastOccurrenceAnalyzer.cs:82-86`).
    - `TimeGapUiAnalyzer` copies `TimeGapAnalyzer` and adds one condition (`Src/BlueDotBrigade.Weevil.Common/Analysis/TimeGapUiAnalyzer.cs:96`) and the line of the longest gap (#1226). Yet `TimeGapAnalyzer` has virtual members meant for a subclass (`TimeGapAnalyzer.cs:38`, `:40`, `:112`, `:142`).
  - **One prologue, ten times.**
    - Ten regex analyzers repeat about 30 lines: the default expression from the filter, the dialog, parsing with aliases, and three early returns. Examples: `FirstOccurrenceAnalyzer.cs:45-75`, `DetectDataAnalyzer.cs:49-79`, `DetectRisingEdgeAnalyzer.cs:57-87`.
    - Each also has two `Analyze` overloads, one forwarding to the other.
  - **The cost was paid during 3.0.**
    - Eight commits since the merge base each touched 10 to 14 analyzer files. Six were epic work: #1095 (`b0a5f66a`), #1100 (`80da994d`), #1136 (`671bc383`), #1027 (`e277be44`, `e98d0c7e`) and #1141 (`a57d26d0`).
    - Fixes came in pairs, and so did their tests: #935 (`3bf2acc1`, both time-gap analyzers), #938 (`e55a00a4`, both edge analyzers) and #1140 (`9515b1d0`).
  - **Two generations of the record API.**
    - **The three Common analyzers** clear their flags with one call, and create a record only for a row they flag (`TimeGapAnalyzer.cs:80-84`, `:132`; `OutOfOrderTimestampsAnalyzer.cs:27-31`, `:44`).
    - **The ten Core analyzers** create every record, and clear its flag one line at a time (`FirstOccurrenceAnalyzer.cs:82-89`; `Timeline/AnalysisHelper.cs:109-115`). The two edge analyzers clear flags in a separate pass, so they create every record twice (`DetectRisingEdgeAnalyzer.cs:95-100`, then `:107`).
    - **The Common rule reads as if all of them used the first style:** an analyzer "flags or unflags many rows with one call to `SetFlagged(RecordList, bool)`" (`.github/instructions/common.instructions.md:34`). #1150 limited that change to the time-gap analyzers and the insights on purpose (`Doc/Plans/1150-RecordFreeReads.md:19-22`). So the difference is a scope limit, not an oversight.
  - **SonarCloud's complexity of 18–46 in the timeline analyzers is inherited.** Against `Releases/2.x`, `StableValueRunsAnalyzer` and `ThresholdCrossingsAnalyzer` gained only the token check, the buffer and the state argument.
- **Impact:**
  - **Every change is made ten times.** Anyone who changes how an analyzer reads records or writes state edits ten files, and must find the twin of every fix.
  - **No model to copy.** A plugin author who copies a built-in analyzer cannot tell which of the two styles is current.
  - **#991 multiplies it again.** Each further change to the record model, such as views, repeats the cost.
- **Recommendation:**
  - **Merge the pairs.** One edge analyzer with a direction, and one occurrence analyzer for first or last, each keeping its two `AnalysisType` keys. Make `TimeGapUiAnalyzer` a subclass of `TimeGapAnalyzer` that overrides the row test.
  - **Write the regex pass once.** Move the prologue and the pass into one internal helper that the ten analyzers call. It asks for and parses the expressions, clears flags with one `SetFlagged(records, false)`, and owns the buffer and the token check, so each analyzer keeps only what it does per record. A helper is enough; no base class is needed.
  - **Or reword the rule.** If the refactor waits, make the Common rule name only the record-free analyzers.
  - **The tests are ready.** The per-analyzer tests in `Tst/BlueDotBrigade.Weevil.Core-UnitTests/Analysis/Timeline/` guard the change.

  **Complexity:** M.
- **Sceptic:** Not challenged (Medium)

### U5b-2. A filter is stopped by a 2.x flag that an overlapping filter can reset, and no contract says which thread owns the engine

- **Priority:** Medium
- **Kind:** Architecture
- **New in 3.0?** Existing weakness, widened by 3.0. Since #1015, a filter's results also move the selection and the active record. Since #1028, Find writes the active record from a background thread.
- **Evidence:**
  - **Two ways to stop work.**
    - #1027 gave Find, the analyzers, export and Split a `CancellationToken` (`Src/BlueDotBrigade.Weevil.Common/Analysis/IRecordAnalyzer.cs:46`; `Src/BlueDotBrigade.Weevil.Core/Navigation/NavigationManager.cs:181`, `:200`).
    - The filter kept 2.x's `Abort()`, which sets a plain `bool` field (`Core/Filter/FilterManager.cs:55`, `:416-419`).
    - The GUI's Abort uses both (`Src/BlueDotBrigade.Weevil.Gui/Filter/FilterViewModel.cs:1731-1735`). The #1027 plan explains why: "Before this plan, Abort stopped only filters" (`Doc/Plans/1027-StreamingReads.md:61`).
  - **The flag belongs to the manager, not to one run.** Each run resets it to false when its loop starts (`FilterManager.cs:136`). That is after the run has built its strategy, which compiles its regular expressions (`:343-344`; `Common/Filter/Expressions/Regular/RegularExpression.cs:43`). So:
    - an Abort that arrives while a filter builds its strategy is lost, and the filter runs to the end;
    - a new filter that starts while an aborted one is still running clears the old one's abort.
  - **Overlapping filters are a designed path.**
    - When a filter is in progress, the GUI aborts it and starts the next one at once, without waiting (`FilterViewModel.cs:2520-2529`, `:2555`). It counts the filters running together (`:2532`, `:2589`), and logs the count as "QueuedFilters" (`:2511`), though nothing queues them.
    - Two runs then share several fields of `FilterManager`:
      - the results and the strategy;
      - the metric collectors, which the second run resets while the first may still be counting (`FilterManager.cs:138-141`, `:183-186`);
      - the view's elapsed times (`:359`).
    - Which run publishes last depends on timing. **This is plausible, not verified.** Building the strategy gives the old run's workers time to see the abort, so it should be rare.
  - **Since 3.0, more state depends on a filter's results.** `ResultsChanged` is raised on the filter's thread (`FilterManager.cs:396`), and its handler now keeps the selection and moves the active record (`CoreEngine.cs:423-428`). That handler must run before the GUI's (`:425`). This holds only because `CoreEngine` subscribes first (`:168`; `Engine.cs:279`).
  - **Find runs while clicks still arrive.**
    - Since #1028, Find runs on a background thread and writes the active record (`Navigation/ContentNavigator.cs:30-32`, `:46-48`).
    - For Find's first 250 ms, the grid still accepts clicks (`FilterViewModel.cs:53`, `:1885`, `:2277`). A click moves the same active record on the UI thread (`SelectionManager.cs:520-526`; `NavigationManager.cs:63-67`).
    - `ActiveRecord` holds an index and a record in two fields, without a lock (`Navigation/ActiveRecord.cs:14-15`, `:58-59`).
    - **Plausible, not verified.** See also U5a-4.
  - **Inside a run, the remarks name the wrong unit.** They speak of "each worker" and "one per thread" (`FilterManager.cs:152`, `:159`; `Common/Data/ContentBuffer.cs:10`). The unit that owns a buffer is the loop's local state, which .NET may create several times per thread (U3-2).
  - **The rules are written nowhere.**
    - `IFilterBehaviors` (`Common/Filter/IFilterBehaviors.cs:6-27`), `INavigate` and `IEngine` say nothing about threads, and no design note does either.
    - What keeps an engine to one operation at a time is the GUI disabling its menus (`FilterViewModel.cs:2289-2293`).
    - 2.x had the same flag and the same overlap (2.x `FilterManager.cs:40`, `:108`; 2.x `FilterViewModel.cs:1799-1810`).
- **Impact:**
  - **A maintainer cannot read the rules from Core.** Nothing says which calls may overlap, which thread raises events, or what a lock protects. The filter, navigation and the selection each answer differently (U5a-4).
  - **Overlapping filters can, rarely, go wrong.** They can show one filter's results under another filter's criteria, or wrong severity counts.
  - **#991 makes overlap normal.** It adds views whose filters run at the same time.
- **Recommendation:**
  - **Give the filter a token.** `IFilter.Apply` takes a `CancellationToken`, as Find and the analyzers do, checked where the loop now checks the flag. Each run owns its token, so a new filter cannot undo an old one's abort, and `Abort()` cancels the running filter's token.
  - **Run one filter at a time.** Inside `FilterManager`, a new `Apply` cancels the running one, and waits for it before starting. Callers then cannot overlap runs, and the GUI's counter can go.
  - **Write the contract down once,** on `IEngine` and in the design note U5b-5 proposes:
    - one operation at a time per engine;
    - events are raised on the thread that made the change, before the call returns;
    - Abort and the tokens are the only members that are safe to call from another thread.

  **Complexity:** S–M.
- **Sceptic:** Not challenged (Medium)

### U5b-3. Find stays on one thread by decision, and on a large log it is slower than a whole filter

- **Priority:** Low
- **Kind:** CPU
- **New in 3.0?** New design, decided. Searching on one thread is inherited from 2.x; decoding each record and reading from disk are new.
- **Evidence:**
  - **Measured.** Linux, 4 vCPUs. Each cell is the median of 5 (`Measurements/Ops-*.txt`). The Find searches for the last record's text from the top, so it reads every record:

    | Log | Version | Plain filter `timeout` | Find through every record |
    |---|---|---|---|
    | 1 GB | 2.12.1 | 2.78 – 4.05 s | 0.45 – 0.54 s |
    | 1 GB | `main`, RAM | 0.34 s | 0.53 s |
    | 1 GB | `main`, disk | 0.44 – 0.45 s | 0.73 – 0.97 s |
    | 4 GB | `main`, RAM | 1.49 s | 2.04 s |
    | 4 GB | `main`, disk | 1.69 s | 2.94 s |

  - **Why.** A filter runs in ranges on every core (`Src/BlueDotBrigade.Weevil.Core/Filter/FilterManager.cs:152-163`). Find reads one row after another, on one thread (`Navigation/ContentNavigator.cs:19-49`).
  - **Decided, and known.**
    - The feasibility study decided that "analyzers, export and Find read it once, in order" (`Doc/Research/Weevil3/Feasibility.md:60`).
    - The #988 plan recorded Find 40% slower on UTF-16 "because Find runs on one thread and now decodes each record" (`Doc/Plans/988-IndexingService.md:44`).
    - Since #1028, Find runs in the background and Escape stops it, so the window does not freeze.
- **Impact:**
  - **The most repeated full scan did not speed up.** Find (F3) kept 2.12.1's speed while filters became 4 to 30 times faster. On disk it takes 1.4 to 1.8 times its time in RAM.
  - **The 20 GB target is unknown.** On the 20 GB disk-mode log (#1024), a Find that matches nothing would take about 15 s if time grows linearly from 4 GB. That is not measured.
- **Recommendation:**
  - **Measure first.** Time Find on 20 GB (see To Measure).
  - **If it matters, search ahead in parallel.** Split the rows past the cursor into chunk-aligned ranges (`Data/RecordRanges.cs`), search them nearest first, and stop at the first range with a match.
  - **Then update the decision** in the feasibility study.

  **Complexity:** M.
- **Sceptic:** Not challenged (Low)

### U5b-4. Filtering across chunks on disk is checked by the bytes read, not by its results

- **Priority:** Low
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **CI's disk run never filters across a chunk.**
    - Every log in the Core unit, feature and functional tests, and in the CLI's functional tests, is under 2 MB, below one 16 MB chunk (`Src/BlueDotBrigade.Weevil.Core/IO/LogLineReader.cs:28`).
    - The engine builder offers no smaller chunk size.
    - So the disk run in CI (`.github/workflows/dotnet.yml:60-66`) never filters across a chunk boundary.
  - **The large-log tests split by mode.**
    - The droid-log test checks the count only in RAM (`Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/Filter/FilterManagerTests.cs:19-37`).
    - Its disk twin checks only the bytes read (`:39-71`).
    - What does check the results: `RecordRangesTests` checks the split, and the audit's harness found that its eight filters return the same counts in RAM and on disk, on 7.5 and 30 million records (`Measurements/Ops-{1gb,4gb}-main-{ram,disk}-run2.txt`).
  - **The override matrix is tested three times at unit level.**
    - `FilterStrategyTests` (34 methods, 816 lines), `FilterStrategyDataDrivenTests` (40 data rows) and `FilterStrategySimplifiedTests` (39 data rows) cover the same include, exclude, pin and bookmark matrix, and `Filtering.feature:94-258` states it once more.
    - All three files predate 3.0 (January 2026), but #990 had to change all three.
    - The review rules flag tests that repeat a behaviour already tested at another level (`.github/instructions/review.instructions.md`, "Regression Tests").
- **Impact:**
  - **A chunk-boundary defect would pass CI.** Examples: a range that starts inside a chunk, or a buffer that keeps the wrong chunk.
  - **The next change costs three times.** The triplicate unit tests triple the cost of the next change to `CanKeep`'s signature.
- **Recommendation:**
  - **Check the results on disk.** Assert the count in the disk test as well, or add the storage mode to the data rows of the droid-log test.
  - **Keep one statement of the matrix** besides the Gherkin scenarios: one unit-test file.

  **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U5b-5. Filtering has no design note, and four comments describe 2.x behaviour or the wrong rule

- **Priority:** Low
- **Kind:** Intent
- **New in 3.0?** New design for the missing note, since 3.0 changed the rules. The drift is mostly inherited.
- **Evidence:**
  - **The rules 3.0 changed are scattered.**
    - `Doc/Design/` has notes on disk mode, the record table and the insight report, but none on filtering.
    - What `@Elapsed` compares, and when (#1016, #1224, #1231), is in code remarks (`Src/BlueDotBrigade.Weevil.Core/Filter/FilterManager.cs:45-49`, `:294-323`; `Expressions/Monikers/ElapsedGreaterThanExpression.cs:7-13`) and in the dated #990 plan (`Doc/Plans/990-ImmutableCore.md:26`, `:81`).
    - That pins and bookmarks bypass both filters is in the instructions (`.github/instructions/core.instructions.md:38`).
    - How a run is split and reads a log on disk is in `DiskMode.md` and the remarks.
  - **Drift:**
    1. **`CanKeep`'s summary.** It says pins and bookmarks "override EXCLUDE filter" (`Core/Filter/FilterStrategy.cs:125-127`). The code keeps them before it tests either filter (`:141-148`), as `core.instructions.md:38` and `Filtering.feature:60` say. Inherited.
    2. **`RecordSearch.IndexOfLineNumber`.** Its documentation promises the bitwise complement of the insertion point when the line is missing (`Src/BlueDotBrigade.Weevil.Common/Navigation/RecordSearch.cs:17-30`). It throws `RecordNotFoundException` instead (`:54-57`, `:119-122`). Inherited; see U5a-5.
    3. **`ReApply`.** Its summary says it "re-applies the most recent filter to the in-memory records" (`FilterManager.cs:295`; `Common/Filter/IFilterBehaviors.cs:13`). On disk, the text is read from the log. The summary also leaves out that F5 keeps the elapsed times it compared (`FilterManager.cs:304-306`).
    4. **Help on `@Elapsed`.** It says `@Elapsed>5000` "shows records where the preceding gap exceeds 5 seconds" (`Doc/Notes/Release/Help.md:155-157`). The gap is measured between the records the previous filter showed, and a record it hid never matches (`ElapsedGreaterThanExpression.cs:10-13`).
- **Impact:** The instructions forbid changing the filter's semantics without approval, yet no single current document states them. The comment nearest the code states the old rule.
- **Recommendation:** Write a short `Doc/Design/Filtering.md`, and fix the four comments. It would cover:
  - the order of the tests: no criteria, pins and bookmarks, include, exclude;
  - what each moniker reads: the session's state, the previous elapsed times, or the text;
  - Apply against F5 and Reload;
  - how a run is split across threads and reads a log on disk;
  - the threading contract from U5b-2.

  **Complexity:** S.
- **Sceptic:** Not challenged (Low)

## Minor

- **The strategy changes before the results.**
  - `FilterStrategy` is replaced before a run starts, and is kept after an abort (`Core/Filter/FilterManager.cs:343-344`), while `Criteria` and `Results` keep the old filter.
  - The analyzers take their default expression from the strategy (`Analysis/AnalysisManager.cs:83`; `Timeline/AnalysisHelper.cs:122-129`).
  - Inherited (2.x `FilterManager.cs:309`).
- **`IMetricCollector` has no threading contract.**
  - The filter calls `Count` from every worker, in no order (`FilterManager.cs:183-186`). A plugin supplies collectors through `ICoreExtension.GetRecordCounters` (`Common/ICoreExtension.cs:49`), and nothing warns it.
  - The first fatal error that `SeverityMetrics` records is whichever worker counted first, and `Reset` does not clear it (`Common/Analysis/SeverityMetrics.cs:74-80`, `:85-91`). Only the insights read it, and they count in order.
  - Inherited.
- **#991: the metric collectors belong to the plugin's extension, not to the view.**
  - `DefaultCoreExtension` and `TsvCoreExtension` return the same list to every caller (`Core/DefaultCoreExtension.cs:38-41`, `:73-76`), and `CoreEngine` uses it as it is (`CoreEngine.cs:344-349`).
  - Views created from one engine would therefore share one `SeverityMetrics`, and reset each other's counts. The #991 plan gives each view its own metric counts (`Doc/Plans/991-MultipleViews.md:36`).
- **`RegularExpression` holds two kinds of `Regex`.**
  - A shared compiled `Regex` serves the analyzers' `GetKeyValuePairs`. A second, compiled once per filter thread, serves `IsMatch` (#976).
  - The per-thread copies live in a `ThreadLocal` that is never disposed (`Common/Filter/Expressions/Regular/RegularExpression.cs:15-16`, `:43-61`, `:79`, `:109`).
  - #976 is outside the epics; see To Measure.
- **`ActiveRecord` keeps both an index and a created record** (`Navigation/ActiveRecord.cs:14-15`). Only `TimestampNavigator` reads the record (`TimestampNavigator.cs:29-30`). Its `IList<IRecord>` constructor creates every record (U2, Minor).
- **Regular expressions have no timeout** (SonarCloud S6444) in Find and the comment search (`ContentNavigator.cs:55`; `CommentNavigator.cs:54`, `:90`), and in the filter's expressions too. Inherited from 2.x. Escape stops Find or an analysis between records, not inside one match (U3, Minor).
- **SonarCloud complexity.**
  - `FilterStrategy.CanKeep` (20, `FilterStrategy.cs:129`) and `RecordSearch.IndexOf` (22, `RecordSearch.cs:52`) are 2.x code, with only 3.0's renames. `CanKeep` would fit in four statements.
  - In `FilterAndCalculateMetrics` (20, `FilterManager.cs:134`), about half the branches are 3.0's: the ranges, the read-error unwrap, the keep-all shortcut and the pass over positions. It still reads top to bottom.
- **Running one analysis builds every analyzer.** That includes each plugin analyzer. An unknown key throws `InvalidOperationException` from `First` (`Analysis/AnalysisManager.cs:178`). Inherited.
- **Allocations per record in the regex analyzers.**
  - Each record gets a `string` from the buffer, a `Dictionary` per expression and, in Detect Data, a `HashSet` (`DetectDataAnalyzer.cs:94`, `:97`; `RegularExpression.cs:107`).
  - The string is new in 3.0: #1133 decodes each record's text once into it. The rest is inherited.
- **Test names.**
  - `ElapsedTimeNavigatorTest`, `PinNavigatorTest`, `TimeGapAnalyzerTest`, `ElapsedGreaterThanExpressionTest` and `FilterCriteriaTest` break the `{ClassUnderTest}Tests` rule.
  - `FilterStrategyTests`, and six of the nine tests in `ContentNavigatorTests`, keep `Method_Condition_Result` names.
  - The test helper `Tst/BlueDotBrigade.Weevil.Core-UnitTests/Analysis/Timeline/AnalysisHelper.cs` has the same name as the production class.
  - Inherited.

## To Measure in Stage 2

- **Overlapping filters (U5b-2).**
  - **Setup.** Open the 1 GB harness log. Start a slow regular-expression filter. After a random 1–100 ms, call `Abort()` and apply a second filter on another thread without waiting. Repeat 1,000 times.
  - **Measure.** Check that `Results`, `Criteria` and `GetMetrics()` match the second filter every time. Separately, count the aborts that are lost when `Abort()` is called while a filter builds its strategy.
- **Find at 20 GB (U5b-3).** The owner, on Windows, with a 20 GB log on disk.
  - **Measure.** A Find that matches nothing, against a plain filter, in RAM where it fits and on disk.
  - **If slow.** Time a prototype that searches chunk-aligned ranges in parallel.
- **The cost of creating a record per row.**
  - **The evidence so far.** The moniker filter takes 0.10–0.11 s on 7.5 million records, and allocates 372 MB, nearly all of it `Record` objects (`Measurements/AllocDiag-1gb.txt`).
  - **Measure.** Time it against a prototype `CanKeep` over positions, which reads facts and text from the list. That gives what `IFilterStrategy.CanKeep(IRecord, …)` costs. `core.instructions.md:58` estimates 0.15–0.2 s per 10 million records on one thread.
- **Regular expressions compiled per thread (#976).**
  - **Setup.** On a 16- or 32-core machine, filter a 1,000-record log with five regular expressions joined by `||`.
  - **Measure.**
    - The time and the allocation with a copy per thread, against one shared `Regex`.
    - After 100 filters, the number of `ThreadLocal` instances waiting for finalization.
- **Searches on the UI thread.**
  - **Setup.** On the 4 GB log's 30 million rows, run next comment, next flag and next elapsed time with no match. They stay on the UI thread (`Gui/Filter/FilterViewModel.cs:1845`).
  - **Measure.** Time each. Any above about 100 ms belongs in the background, as Find is.
- **The edge analyzers' extra pass (U5b-1).**
  - **Setup.** Run Detect Rising Edges on the 1 GB log, in RAM and on disk.
  - **Measure.** The time of the pass that clears flags and creates every record (`DetectRisingEdgeAnalyzer.cs:95-100`), against the whole analysis.

## Open Questions

- **Overlapping filters.** When a filter is running, should the GUI's "new filter takes precedence" wait for the aborted filter (`FilterViewModel.cs:2520-2529`), or should Core run one filter at a time? The name "QueuedFilters" (`:2511`) suggests queuing was intended.
- **The ten regex analyzers.** Should they adopt the bulk flag clear and the record-free reads, or should `common.instructions.md:34` name only the time-gap analyzers?
- **Find at 20 GB.** Is a Find on one thread acceptable for the 20 GB disk target (#1024)? The decision (`Feasibility.md:60`) predates the measurements.
