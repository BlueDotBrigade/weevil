# Weevil 3.0: Is It Feasible?

Question: can the remaining Weevil 3.0 epics be delivered together? They are an immutable core (#990), less memory (#988, through #978), disk mode (#986) and multiple views (#991). #987 is done.

> Notes from 2026-10-18, updated 2026-10-20 after a review of the plans against the code. This builds on [LargeLogFileStrategy.md](../LargeLogFiles/LargeLogFileStrategy.md), which covers the constraints, storage options and index format, and on [MutableStateInCore.md](MutableStateInCore.md), which lists what changes in Core today.

## Answer

Yes, with two conditions:

1. **Disk-mode filtering meets its target only on an NVMe drive.** The target is a 20 GB log filtered in under 10 s. An NVMe drive reads 20 GB in about 6 s, a SATA SSD in about 36 s.
2. **The record API is replaced, not adapted.** In October 2026, `ImmutableArray<IRecord>` appeared 156 times in 60 source files and 81 times in 20 test files. External plugins need not stay compatible, so this is a large change but not a blocker.

## Target Design

The data is split by who owns it:

| Part | One per | Contains | Changes? |
|---|---|---|---|
| Log source | Log, shared by every view | Line index; a record table (first line, timestamp, and a facts byte holding severity, multi-line and UI thread); content, held in RAM as chunks of the file's raw bytes, or read from the file | Never. A grown log gets a new source that shares the old part. |
| Session | Open log | An annotation byte per line (pinned, has comment); a comment table; bookmarks, regions, remarks, table of contents, filter history and context. It loads and saves the sidecar and raises change events. | Yes. It is the only mutable part, and one lock guards its writes. |
| View | Window | Filter, results as record numbers, elapsed times for every result, flags (one bit per line), selection as ranges, active record and metric counts | Yes, by the UI thread. |

Records become small immutable objects, created only when needed (visible rows, a selection, an export) and equal by line number.

Timestamps are kept: an 8-byte column, filled when the log is indexed (decided 2026-10-20 after the [timestamp proof of concept](TimestampPoC/README.md#results), #1008). Re-reading them would slow every open and every filter: insights read every timestamp on every open, and every filter calculates the Elapsed column for the records it shows.

## Memory per Record

For the test log used in the #978 plan, which averages 131 bytes per record. Shorter records cost more per byte of log.

| | Today | RAM mode | Disk mode |
|---|---|---|---|
| Content | 288 (UTF-16 string) | ~131 (raw bytes) | 0: read from the file |
| `Record`, `Metadata` and array slot | 104 | 0 | 0 |
| Record table: line number (4), content offset (8), facts (1), timestamp (8) | | 21 | 13: offsets come from the line index |
| Annotation byte | | 1 | 1 |
| Filter results, with no filter | 8 | 4 | 4 |
| **Bytes per record** | **400** | **~157** | **~18** |
| 1 GB log | 3.1 GB | ~1.2 GB | ~137 MB |
| 64 GB log | Does not fit | Does not fit in 32 GB | ~8.8 GB |

While a filter runs, it also holds a temporary array of 4 bytes per record.

## Challenges and Solutions

| Challenge | Solution | Epic |
|---|---|---|
| Records carry mutable state, so they cannot be created on demand: a re-created record would lose its comment | Move every changing value off the record: annotations to the session, elapsed time to the view, parsing facts into the immutable facts byte | #990 |
| The GUI subscribes to events again after Open, Clear and Reload ([lesson](../../LessonsLearned/ImmutableCoreLibrary.md)) | The GUI subscribes to `Engine` once per open log. `Engine` already swaps the engine behind it on Clear and Reload; it also forwards the events. | #990 |
| The grid edits `Metadata` through two-way bindings | One view-model per realized row. It reads the record and the session, writes to the session, and refreshes when the session reports a change to its line. | #990 |
| Writes from the grid, filters and analyzers run on different threads, with no locking | Session writes take one lock. A filter reads the annotation byte without a lock, so a pin changed mid-filter counts either before or after, as today. Grid updates never block: a row learns of a change asynchronously, and a bulk operation raises one event. | #990 |
| Analyzers set flags on the records themselves | Flags live in the view, one bit per line, so different analyzers can run in different views. | #990 |
| The `@Elapsed` filter reads values left by the previous filter | Elapsed time is the time between two consecutive visible records (decided). After each filter, the view calculates elapsed times for all its results and keeps them, so nothing is stored per record, and the `@Elapsed` filter and iterating by elapsed time behave as today. | #990 |
| Clear rebuilds the engine | Clear narrows the view's record scope, a list of record-number ranges (Clear Selected can make millions); annotations stay in the session. In RAM mode, content chunks that no scope includes are released, and read back from the file when needed. See [EngineApi.md](EngineApi.md). | #990, #988, #986 |
| Content is 73% of memory | Chunks of the file's raw bytes. A filter decodes each record once and passes the text to every expression (#978 step 1). | #988 |
| Record arrays throughout the API | A read-only record list that creates records on demand, with `IndexOf` by line number (a binary search), so WPF selection never scans. Selection is stored as ranges, and Copy is limited to 100,000 records. | #990, #988 |
| A WPF list of hundreds of millions of rows | The list is already virtualized with recycling. A proof of concept checks scrolling, Shift/Ctrl selection (#806), Ctrl+A and editing at 100 M rows. | #990, #988 |
| Insights read every record's timestamp on every open, and every filter calculates elapsed times | Keep an 8-byte timestamp column, chosen after a proof of concept (#1008) | #988 |
| Logs larger than RAM | Disk mode: filters read the file in parallel ranges; analyzers, export and Find read it once, in order; shown rows are read through the line index. Never memory-map the log; Weevil's own `.idx` may be mapped. | #986 |
| Disk speed | Warn when a disk-mode log is on a network drive. A proof of concept measures NVMe and SATA. | #986 |
| A log opened from a zip file is deleted right after opening | Out of scope for this release (#1005). Disk mode and restoring cleared records need the file. | #986 |
| Several views of one log | Views share the log source and session. Each view has its own record scope, so two views can clear different records. Each view also has its own flags. | #991 |
| Sidecar compatibility | Format unchanged. It is saved from the session, which keeps every entry, including cleared lines (bug #1004). Byte offsets come from the line index (#977). | All |

## Phases: One per Epic

| Phase | Epic | Delivers | Needs |
|---|---|---|---|
| 1 | #987 Faster loading and filtering | Done | |
| 2 | #990 Immutable core | Records without mutable state; session and view; the GUI subscribes once; Clear without rebuilding; no `Metadata` objects (−12% memory) | |
| 3 | #988 Indexing service (#978) | Compact content, records on demand, record table in `.idx`: about 40% of today's memory, and reopening without parsing | #990 |
| 4 | #986 RAM or SSD | Disk mode and an Open As dialog | #988 |
| 5 | #991 Multiple views | Core support for several views of one log; the GUI follows in the next milestone. Implemented last, so the release does not depend on it. | #990, #988, #986 |

#990 comes first because records can be created on demand only once they hold no mutable state.

Before phase 2 (decided 2026-10-20): the bugs #1001, #1002 and #996, then the upgrade to .NET 10 (#1003), so #990 is built and tested on .NET 10. Every step of phases 2 to 4 has an issue: #1011–#1018 under #990, #1019–#1023 and #1008 under #978, and #1024–#1028 under #421.

## Proofs of Concept

Only where a decision depends on a measurement or on behaviour nobody has checked:

| Proof of concept | Question | Decides | Plan |
|---|---|---|---|
| WPF virtual list | Do scrolling, selection ranges, Ctrl+A and editing work with a virtual list of row view-models at 100 M rows? | The GUI design | #990 |
| Timestamps | Is parsing timestamps when needed fast enough for insights at open and an `@Elapsed` filter on a 20 GB log, or is an 8-byte column needed? | The record table. Done: keep the column ([results](TimestampPoC/README.md#results)) | #988 |
| Disk filtering | Does a parallel streaming filter read 20 GB in under 10 s on NVMe, and how slow is SATA? | The disk-mode design | #986 |

Not needed:
- **Annotation byte versus bit arrays:** the arithmetic above decides.
- **The cost of decoding while filtering:** already measured for regular expressions on UTF-8 in the [storage benchmark](../LargeLogFiles/StorageBenchmarks/README.md). #978 step 1 checks plain text and UTF-16.

## Decisions

Decided 2026-10-19:

- Elapsed time is the time between two consecutive visible records.
- Clear and flags belong to each view.
- RAM or disk is chosen in Weevil's Open As dialog, shown on every open after the Windows file dialog.

Decided 2026-10-20:

- **First open:** a 20 GB log may take about 2 minutes, as v2.12.1 does on an i9-9900K with an NVMe drive. A parallel parser is a [future consideration](../../Plans/988-IndexingService.md#future-considerations).
- **Annotations and flags:** kept by line number.
- **Elapsed times:** calculated after each filter for all results, and kept by the view.
- **The grid:** updates never block the UI thread.
- **Selection:** stored as ranges. Copy is limited to 100,000 records.
- **Content and filtering:** content is held as the file's raw bytes, and a filter decodes each record once for all its expressions.
- **Disk mode:** filters read the file in parallel; analyzers, export and Find read it once, in order.
- **Zipped logs:** out of scope for this release (#1005).
- **Timestamps:** kept in an 8-byte column, in RAM and disk mode (#1008). Re-reading them, even with the fastest parse, added 5.2 s to opening a 20 GB log and up to 3.8 s to each filter.
- **Restoring cleared records:** Reload (Ctrl+F5) already brings them back, so there is no separate Restore command.
- **Multiple views (#991):** this milestone builds the Core only. The GUI, with the latest MaterialDesignInXamlToolkit, comes in the next milestone, which also decides how Reload works with several views.
- **`IDisposable`:** whether `IEngine` needs it is #1030.

## Status

Draft, 2026-10-18; reviewed against the code 2026-10-20. Engine API and Clear: [EngineApi.md](EngineApi.md). Plans: [#990](../../Plans/990-ImmutableCore.md), [#988](../../Plans/988-IndexingService.md), [#986](../../Plans/986-DiskMode.md), [#991](../../Plans/991-MultipleViews.md).
