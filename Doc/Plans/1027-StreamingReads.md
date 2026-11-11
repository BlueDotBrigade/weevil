# 1027: Read the Log Once per Pass in Disk Mode

> **Status:** In progress
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Step 4 of the [#986 plan](986-DiskMode.md#steps): in disk mode, filters, analyzers and export read the log as few times as possible. Task #1027, under user story #421.

## Goal

- A filter on a log opened on disk reads each byte of the log once, in parallel ranges split at record boundaries.
- Analyzers, export, the graph and the CLI's output read the log once, in order.
- Filters on monikers that never read content, such as `@Pinned` or `@Flagged`, do not read the file.
- Abort stops them (see decision 4).
- RAM mode finds the same records, at least as fast as today.

## Where Disk Mode Stands

Measured on 2026-11-10, in a Debug build on Linux with 4 cores, on the 1 GB UTF-8 and 2 GB UTF-16 droid logs (`Core-PerformanceTests`), with the log in the operating system's cache.

- **Filters read the log 1.3 to 2.1 times.** A filter took 0.8 – 1.2 s on disk against 0.6 – 0.9 s in RAM. With the log already cached, the time hides the extra reads; on a drive it will not.
  - The cause: `Parallel.ForEach` over a `RecordList` hands each worker small, growing runs of positions, so the workers interleave. A `ContentBuffer` reads a whole 16 MB chunk when a record in it is read, so several workers read the same chunk.
  - The share read twice changes from run to run, and grows with the number of cores.
- **A read through `record.Content` costs about 7 µs on disk**, against 0.2 µs through a `ContentBuffer`: it opens the log, reads and checks the 64 KB block that holds the record, and closes the log.
  - 10 million records take about 72 s that way, even from the cache, and read 64 KB for each record from a drive. Through a `ContentBuffer`, the whole log takes 1.5 s.
  - Analyzers, export, the log splitter, the CLI's filter output and the graph still read `record.Content`.
- **Already done:** Find (`ContentNavigator`), the matching-record-runs analyzer and filters read through a `ContentBuffer`. Insights read facts only. Rebuilding the table of contents reads the log once with its own reader.
- **Monikers:** `@Pinned`, `@Flagged` and the other monikers that test a record's state never call `content.Read`, and a `ContentBuffer` reads only when asked, so they already read nothing from the file. No test says so yet.

## Design

### Filters read each chunk once

- `FilterManager` splits the records into ranges whose records all read the same content chunk, and gives whole ranges to the workers (`Partitioner` over ranges, load balanced). Each worker reads a range in order, with its own `ContentBuffer`.
- The ranges come from the record table's chunk directory, which already knows the record each chunk starts at. A record list that is not a table, such as filter results, is split into fixed ranges of 64 K positions.
  - Both are internal to Core: `RecordList` gains no public member.
- The results, and the order they are kept in, do not change. Abort is still checked at every record.
- RAM mode uses the same ranges (decision 1).
- About 60 chunks per GB, so a 20 GB log gives about 1,200 ranges: enough for load balancing on any number of cores.
- Memory: each worker holds one 16 MB chunk while it reads, as today: 256 MB on 16 cores.
- The number of workers stays the default, one per core, until #1024 measures the drives (decision 2).

### Sequential readers read through a `ContentBuffer`

Each reads through one `ContentBuffer` for the whole pass, with `buffer.Read(record)`, instead of `record.Content`, so it reads each chunk once:

- the analyzers: statistical, detect data, first and last occurrence, threshold crossings, and `AnalyzerExpressionHelper`;
- export: `DiskWriter`, raw and TSV;
- `LogFileSplitter`;
- the CLI's filter output;
- the graph (`GraphViewModel`).

Where a reader needs a string, for example `RegularExpression.GetKeyValuePairs(string)`, it makes one from the span: the same allocation as `record.Content` today. The public `GetKeyValuePairs(IRecord)` stays, for plugins.

A plugin that reads `record.Content` still works, at 7 µs per record on disk. [DiskMode.md](../Design/DiskMode.md) and the remarks on `IRecordAnalyzer.Analyze` say to read through `RecordList.ReadContent`.

### Abort

Today Abort stops only filters. Analyzers, export and Find run to the end. `IRecordAnalyzer.Analyze` takes no cancellation, and it is a public plugin interface (decision 4).

## Decisions for the Owner

Decisions 1 to 3 were taken as recommended on 2026-11-10, when the owner merged this plan and asked to proceed. Decision 4 is still open.

1. **RAM mode uses the same filter ranges.** Recommended: one code path, kept only if the RAM filter times do not get slower. Otherwise disk mode alone uses them.
2. **Reader threads:** one per core until the #1024 runs on NVMe and SATA decide the count, by the rule in the [storage benchmark](../Research/LargeLogFiles/StorageBenchmarks/README.md).
3. **"Filtering complete." logs the bytes read from the log**, in disk mode. Recommended: the owner gets a figure from real use, and the performance test checks that a filter reads the log once.
4. **Abort for analyzers, export and Find.** Options:
   - **A (recommended):** a second `IRecordAnalyzer.Analyze` overload taking a `CancellationToken`, as a default interface method that calls the current one, so existing plugins still build and run. The built-in analyzers, `DiskWriter.Write` and Find take the token, and the GUI's Abort button cancels them. A separate pull request, step 3.
   - **B:** a new user story after #1028, since Abort never stopped them in RAM mode either.
5. **Pull requests:** one per step below, as #1025 did.

## Steps

1. **Filters read each chunk once.** Chunk-aligned ranges, and the log of bytes read (decision 3).
   - Tests: the droid filter performance test also checks, in disk mode, that a filter reads the log's bytes once. A unit test checks that the ranges cover every position once, in order, at chunk boundaries. A feature test checks that a moniker filter on a log opened on disk finds its records after the log is deleted, so it read nothing.
   - Done second (2026-11-10), with decisions 1 to 3 as recommended. On the droid logs on disk, a filter reads 1.008 times the log, against 1.3 to 2.1 before: each chunk's first record reads its own blocks first. On disk, the 1 GB log filtered in about 770 ms against 1,100 (medians of 7, with the log in the operating system's cache).
   - Ranges follow chunks only on disk. In RAM, where a chunk costs nothing to read, one range per chunk left a log under 16 MB on one thread, about 3 times slower (found in review), so RAM splits into 4 ranges per core. A log on disk with at most half as many chunks as cores splits each chunk's range, so more cores are busy, and each part's worker reads its chunk once. RAM filter times then matched main's, within noise: 16 – 23 ms against 15 – 18 on a 13 MB log, and 600 – 700 ms against 620 – 655 on the 1 GB log.
2. **Sequential readers read through a `ContentBuffer`.**
   - Tests: a performance test exports the 1 GB droid log on disk, and runs one analyzer over it, each within a generous bound that reads through `record.Content` would miss by a wide margin. The existing analyzer, export and CLI tests run on disk in CI's disk run, which now includes the CLI's functional tests.
   - A `ContentBuffer` reads a whole chunk only for a second record in a row from it, and each chunk whole once at most, so a sparse list reads only each record's blocks, and records sorted by time never make a pass read more than the log once (found in review).
   - Done first, since it needs no decision: on the 1 GB droid log on disk, export takes 3.0 s instead of 80 s, and the detect-data analyzer 4.4 s instead of 83 s (2026-11-10, with the log in the operating system's cache).
3. **Abort** (if decision 4 picks A).

## Not in This Plan

- A cache for screen rows: only if a measurement shows it is needed (decision 2 of the [#1025 plan](1025-ContentFromFile.md#decisions)).
- Copying selected records to the clipboard reads `record.Content` through the formatters. A selection is usually small; selecting every record of a large log on disk is slow, as it is in RAM.
- Saving the record table and checksums in the `.idx` (#1023).
