# Weevil 3.0: Is It Feasible?

Question: can the remaining Weevil 3.0 epics be delivered together? They are an immutable core (#990), less memory (#988, through #978), disk mode (#986) and multiple views (#991). #987 is done.

> Notes from 2026-10-18. This builds on [LargeLogFileStrategy.md](../LargeLogFiles/LargeLogFileStrategy.md), which covers the constraints, storage options and index format, and on [MutableStateInCore.md](MutableStateInCore.md), which lists what changes in Core today.

## Answer

Yes, with two conditions:

1. **Disk-mode filtering meets its target only on an NVMe drive.** The target is a 20 GB log filtered in under 10 s. An NVMe drive reads 20 GB in about 6 s, a SATA SSD in about 36 s.
2. **The record API is replaced, not adapted.** `ImmutableArray<IRecord>` appears 147 times in 60 source files and 83 times in 21 test files. External plugins need not stay compatible, so this is a large change but not a blocker.

## Target Design

The data is split by who owns it:

| Part | One per | Contains | Changes? |
|---|---|---|---|
| Log source | Log, shared by every view | Line index; a record table (first line, plus a facts byte holding severity, multi-line and UI thread); content, held in RAM as chunks or read from the file | Never. A grown log gets a new source that shares the old part. |
| Session | Open log | An annotation byte per record (pinned, has comment); a comment table; bookmarks, regions, remarks, table of contents, filter history and context. It loads and saves the sidecar and raises change events. | Yes. It is the only mutable part, and one lock guards its writes. |
| View | Window | Filter, results as record numbers, selection, active record and metric counts. Elapsed time is calculated for the rows shown. | Yes, by the UI thread. |

Records become small immutable objects, created only when needed (visible rows, a selection, an export) and equal by line number.

Timestamps are not stored. They are parsed when needed: two per shown row for elapsed time, about 30 for a binary search by timestamp. A proof of concept decides whether RAM mode needs an 8-byte column for whole-log work, such as an `@Elapsed` filter or an analyzer.

## Memory per Record

For the test log used in the #978 plan, which averages 131 bytes per record. Shorter records cost more per byte of log.

| | Today | RAM mode | Disk mode |
|---|---|---|---|
| Content | 288 (UTF-16 string) | ~131 (UTF-8) | 0: read from the file |
| `Record`, `Metadata` and array slot | 104 | 0 | 0 |
| Record table: line number (4), content offset (8), facts (1) | | 13 | 5: offsets come from the line index |
| Annotation byte | | 1 | 1 |
| **Bytes per record** | **392** | **~145** | **~6** |
| 1 GB log | 3.0 GB | ~1.1 GB | ~46 MB |
| 64 GB log | Does not fit | Does not fit in 32 GB | ~2.9 GB, or ~6.8 GB with a timestamp column |

## Challenges and Solutions

| Challenge | Solution | Epic |
|---|---|---|
| Records carry mutable state, so they cannot be created on demand: a re-created record would lose its comment | Move every changing value off the record: annotations to the session, elapsed time to the view, parsing facts into the immutable facts byte | #990 |
| The GUI subscribes to events again after Open, Clear and Reload ([lesson](../../LessonsLearned/ImmutableCoreLibrary.md)) | The GUI subscribes once, to `Engine`. `Engine` already swaps the engine behind it on Clear and Reload; it also forwards the events. | #990 |
| The grid edits `Metadata` through two-way bindings | One view-model per realized row. It reads the record and the session, writes to the session, and raises `PropertyChanged` when the session reports a change to its line. | #990 |
| Writes from the grid, filters and analyzers run on different threads, with no locking | Session writes take one lock. A filter reads the annotation byte without a lock, so a pin changed mid-filter counts either before or after, as today. | #990 |
| Analyzers set flags on the records themselves | Flags live in the view, one bit per record, so different analyzers can run in different views. | #990 |
| The `@Elapsed` filter reads values left by the previous filter | Elapsed time is the time between two consecutive visible records (decided). The view calculates it from its results, so nothing is stored per record and an `@Elapsed` filter behaves as today. | #990 |
| Clear rebuilds the engine | Clear narrows the view's record scope, a short list of record-number ranges; annotations stay in the session. In RAM mode, content chunks that no scope includes are released, and read back from the file when needed. See [EngineApi.md](EngineApi.md). | #990, #988, #986 |
| Content is 73% of memory | UTF-8 chunks; filters match on spans (#978 step 1) | #988 |
| Record arrays throughout the API | A read-only record list that creates records on demand, with `IndexOf` by line number (a binary search), so WPF selection never scans | #988 |
| A WPF list of hundreds of millions of rows | The list is already virtualized with recycling. A proof of concept checks scrolling, Shift/Ctrl selection (#806) and editing at 100 M rows. | #990, #988 |
| Logs larger than RAM | Disk mode: filters stream the file in parallel ranges, and shown rows are read through the line index. Never memory-map the log; Weevil's own `.idx` may be mapped. | #986 |
| Disk speed | Warn when a disk-mode log is on a network drive. A proof of concept measures NVMe and SATA. | #986 |
| Several views of one log | Views share the log source and session. Each view has its own record scope, so two views can clear different records. Each view also has its own flags. | #991 |
| Sidecar compatibility | Format unchanged. It is saved from the session; byte offsets come from the line index (#977). | All |

## Phases: One per Epic

| Phase | Epic | Delivers | Needs |
|---|---|---|---|
| 1 | #987 Faster loading and filtering | Done | |
| 2 | #990 Immutable core | Records without mutable state; session and view; the GUI subscribes once; Clear without rebuilding; no `Metadata` objects (−12% memory) | |
| 3 | #988 Indexing service (#978) | Compact content, records on demand, record table in `.idx`: about a third of today's memory, and reopening without parsing | #990 |
| 4 | #986 RAM or SSD | Disk mode and an Open As dialog | #988 |
| 5 | #991 Multiple views | Several views of one log. Implemented last, so the release does not depend on it. | #990, #988, #986 |

#990 comes first because records can be created on demand only once they hold no mutable state.

## Proofs of Concept

Only where a decision depends on a measurement or on behaviour nobody has checked:

| Proof of concept | Question | Decides | Plan |
|---|---|---|---|
| WPF virtual list | Do scrolling, selection ranges and editing work with a virtual list of row view-models at 100 M rows? | The GUI design | #990 |
| Timestamps | Is parsing timestamps during an `@Elapsed` filter or an analyzer fast enough in RAM mode, or is an 8-byte column needed? | The record table | #988 |
| Disk filtering | Does a parallel streaming filter read 20 GB in under 10 s on NVMe, and how slow is SATA? | The disk-mode design | #986 |

Not needed: annotation byte versus bit arrays (the arithmetic above decides), and the cost of decoding UTF-8 while filtering (already measured in the [storage benchmark](../LargeLogFiles/StorageBenchmarks/README.md)).

## Decisions

Decided 2026-10-19:

- Elapsed time is the time between two consecutive visible records.
- Clear and flags belong to each view.
- RAM or disk is chosen in Weevil's Open As dialog, shown on every open after the Windows file dialog.

## Status

Draft, 2026-10-18. Engine API and Clear: [EngineApi.md](EngineApi.md). Plans: [#990](../../Plans/990-ImmutableCore.md), [#988](../../Plans/988-IndexingService.md), [#986](../../Plans/986-DiskMode.md), [#991](../../Plans/991-MultipleViews.md).
