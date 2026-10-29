# 988: Keep a Log in Much Less Memory

> **Status:** In progress
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Phase 3 of the [Weevil 3.0 feasibility study](../Research/Weevil3/Feasibility.md). Epic #988; the remaining work is user story #978. It needs #990: records can be created on demand only once they hold no mutable state.

This plan replaces an earlier draft, `978-CompactRecordStorage.md`, which was never merged. Its steps 2 and 3 moved into #990.

## Goal

From #978's acceptance criteria:

- Opening a log produces the same records, line numbers and content as before.
- An open log uses less memory than with Weevil 2.12.1. About 40% of today's is expected: about 1.2 GB per GB of log, including 60 MB per GB for timestamps (step 2). #978 records memory before and after.
- Filtering is no slower.
- Comments, pins, flags, bookmarks, regions, Clear and the sidecar work as before. Clear still frees memory.
- Reopening a log whose saved index matches does not parse the whole log again.

A first open may take as long as today: about 2 minutes for a 20 GB log on an NVMe drive (accepted 2026-10-20). See [Future Considerations](#future-considerations).

## Steps

One sub-issue of #978 and one pull request per step, in order. The step issues were created on 2026-10-20, so a change to the plan can be traced through them.

Before step 1, #1019 measures Weevil 2.12.1's memory as the baseline: 1, 4 and 8 GB logs measured, noting any too big for the RAM, and 12, 16, 20 and 24 GB estimated. Its harness measures memory again after each step. **Done 2026-10-20:** 2.12.1 needs about 3.25 GB of RAM per GB of log at the peak while opening: 13.0 GB for the 4 GB log, and about 65 GB for a 20 GB log. The 8 GB log is too big for the 14.2 GB test machine, so it is estimated with the larger sizes. See the [results](../Research/Weevil3/MemoryHarness/README.md#results).

1. **Compact content (#1020).** Split into two pull requests (2026-10-29): filters and Find read each record's content once, through a buffer they reuse (#1131), then content is held as raw bytes and the buffer decodes them.
   - Content is held as the file's raw bytes, as the [storage benchmark](../Research/LargeLogFiles/StorageBenchmarks/README.md) and [LargeLogFileStrategy.md](../Research/LargeLogFiles/LargeLogFileStrategy.md) recommend. It is kept in shared, read-only 16 MB chunks split at record boundaries, so no array reaches .NET's 2 GB limit. A record larger than a chunk gets a chunk of its own.
   - A record keeps an offset and a length instead of a string.
   - `Content` decodes on each access with the log's encoding, rejoining multi-line records with `\r\n`, so it returns exactly what it returns today. Invalid bytes become U+FFFD, as today. Reading a released chunk back (#986) is then a plain file read.
   - A filter decodes each record once, into a buffer reused per thread, and passes that text to every expression; `IExpression.IsMatch` takes the text (decided 2026-10-20). Changed on 2026-10-29 (#1131): `IsMatch` takes the record and a `ContentBuffer`, which decodes the record's content the first time an expression reads it, so a filter of monikers other than `@ContentLength` decodes nothing. The rules for each filter type stay as they are:
     - Regular expressions and plain text keep today's culture and case rules exactly.
     - Aliases (`#`) expand to text or regular expressions first.
     - Monikers (`@`) read the facts byte, annotations, flags or elapsed times, and never decode content. The exception is `@ContentLength`, which reads the content's length.
   - Check that every filter type keeps exactly the same records. The storage benchmark measured regular expressions on UTF-8 only, so also time plain text and a UTF-16 log.
   - Estimate: −38% memory on the test log.
   - **Done 2026-10-29 (#1131, #1133):**
     - **Memory:** the memory kept per record fell from 348 to 205 bytes (−41%), and the budget test (#1086) is now 250 bytes. On the 4 GB harness log, the working set after opening fell from 10.9 to 6.5 GB, or 1.62 GB per GB of log, and the peak while opening is the same. See the [results](../Research/Weevil3/MemoryHarness/README.md#after-step-1-of-988-content-held-as-the-logs-bytes-2026-10-29).
     - **Same records:** every record of 120 test and generated logs, opened from the top and from three start lines, was identical to `main`. The logs covered UTF-8, UTF-16 and UTF-32 in both byte orders, LF, CR and CRLF line breaks, invalid bytes, and logs larger than a chunk.
     - **Speed:** on the 10-million-record log, every filter kept the same records. On UTF-8, opening was 48% faster and filters 22–69% faster; on the 4 GB harness log, opening took 75 s instead of 114 s. On UTF-16, filters were within noise, but a Find through every record took about 0.2 s longer (+40%), because Find runs on one thread and now decodes each record.
2. **Proof of concept: timestamps (#1008). Done 2026-10-20: keep an 8-byte timestamp column.**
   - The question was whether to keep each record's timestamp in a column, as today, or parse it from the content each time it is needed.
   - Even the fastest re-read added 5.2 s to opening a 20 GB log and up to 3.8 s to each filter. The column costs 60 MB per GB of log. See the [results](../Research/Weevil3/TimestampPoC/README.md#results).
3. **Records on demand (#1021).**
   - Records live in a record table: line number, content offset, the facts byte and the timestamp.
   - `IRecord` objects are created for shown rows, a selection or an export, and compare equal by line number.
   - A read-only record list replaces `ImmutableArray<IRecord>`, with `IndexOf` by line number. Filter results and the Clear record set become lists of record numbers.
   - This is the largest step. In October 2026 there were 156 references in 60 source files and 81 in 20 test files, and `ISelect` also exposes records as a dictionary and a list. When writing its sub-issue, split it: for example Core behind an adapter first, then the GUI, then the tests.
4. **Clear frees memory (#1022).**
   - Clear narrows the view's record scope (#990 step 8). Content chunks that the scope no longer includes are released, followed by one compacting garbage collection, as Clear does today. See [EngineApi.md](../Research/Weevil3/EngineApi.md#freeing-ram).
   - Only whole chunks are released: up to about 16 MB stays at each edge of a kept range, and `Clear Selected` frees nothing.
   - Released chunks are not read back until #986, so cleared records return after Reload, as today.
   - This needs only one view, so it does not depend on #991.
5. **Reopen without parsing (#1023).**
   - The `.idx` file gains the record table: which lines start records, the facts byte and the timestamps.
   - The header already holds a format version, the encoding, the line count, the length indexed, and hashes of the first and last 4 KB. Those are how it detects a grown log and indexes only the new part.
   - The header also gains the plugin's name and version and its Open As settings, so a change of plugin rebuilds the record table.
   - This needs a new layout, not just a new version number. Today the whole index is written through one memory buffer, and files over 2 GB are rejected; a 64 GB log's record table takes several GB.
   - When the saved index matches the log, Weevil builds the table from it instead of parsing. Older index files are rebuilt.

## Compatibility

- The sidecar format does not change.
- Line numbers, content and filter results stay identical. Each step is checked against the current parser on the test logs and on the 52 real logs used for #977.

## Risks

- After step 1, `Content` allocates a string on every access. Hot paths that read it repeatedly must move to spans.
- Step 3 relies on the row design proven in #990 step 4.

## Future Considerations

- **Parallel parser.**
  - Parsing runs on one thread. Splitting the log at line-index checkpoints and parsing the parts in parallel could bring a first open of 20 GB well under a minute.
  - Plugin parsers would need to read spans instead of strings, allocate nothing per line, and be thread-safe.
- **One pass on a first open.** The line index is built in a pass of its own before parsing, so a first open reads the file twice. Building both in one pass saves one read: about 6 s per 20 GB on NVMe, or 36 s on SATA.
