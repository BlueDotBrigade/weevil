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

1. **Compact content (#1020). Done 2026-10-29.** Split into two pull requests (2026-10-29): filters and Find read each record's content once, through a buffer they reuse (#1131), then content is held as raw bytes and the buffer decodes them.
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
     - **Chunks:** a chunk ends at the first record start after it reaches 16 MB, so it holds about 16 MB plus at most one record, and a record larger than 16 MB shares its chunk with the records before it rather than getting a chunk of its own. A chunk is the exact range of the log's bytes, so #986 can read a released chunk back with a plain file read.
     - **Encodings:** records hold bytes for UTF-8 logs, and UTF-16 logs in the computer's byte order. Records of UTF-16 logs in the other byte order, and of UTF-32 logs, keep a string as before: .NET decodes those one character at a time, 10 to 50 times more slowly, and UTF-32's bytes take more memory than a string.
     - **Same records:** every record of 120 test and generated logs, opened from the top and from three start lines, was identical to `main`. The logs covered UTF-8, UTF-16 and UTF-32 in both byte orders, LF, CR and CRLF line breaks, invalid bytes, and logs larger than a chunk.
     - **Speed:** on the 10-million-record log, every filter kept the same records. On UTF-8, opening was 48% faster and filters 22–69% faster; on the 4 GB harness log, opening took 75 s instead of 114 s. On UTF-16, filters were within noise, but a Find through every record took about 0.2 s longer (+40%), because Find runs on one thread and now decodes each record.
2. **Proof of concept: timestamps (#1008). Done 2026-10-20: keep an 8-byte timestamp column.**
   - The question was whether to keep each record's timestamp in a column, as today, or parse it from the content each time it is needed.
   - Even the fastest re-read added 5.2 s to opening a 20 GB log and up to 3.8 s to each filter. The column costs 60 MB per GB of log. See the [results](../Research/Weevil3/TimestampPoC/README.md#results).
3. **Records on demand (#1021).**
   - Records live in a record table, in columns: the line number, the content's offset in its chunk and its length, the facts byte and the timestamp, 21 bytes per record. Storing the length, rather than deriving it from the next record, avoids a table of exceptions.
   - `IRecord` objects are created for shown rows, a selection or an export, and compare equal by line number.
   - A read-only record list replaces `ImmutableArray<IRecord>`, with `IndexOf` by line number. Filter results and the Clear record set become lists of record numbers.
   - This is the largest step. On 2026-10-30 there were 169 references to `ImmutableArray<IRecord>` in 61 source files and 128 in 28 test files, and `ISelect` also exposes records as a dictionary and a list.
   - **Split into seven pull requests (decided 2026-10-30)**, in this order. The design and the challenge it survived are in [RecordsOnDemand](../Research/Weevil3/RecordsOnDemand/README.md).
     1. Records are equal when their line numbers are equal (#1135). **Done 2026-10-30.**
     2. Callers read records from a read-only record list, an `IList<IRecord>` (#1136). **Done 2026-10-30.**
     3. Code that needs only line numbers or timestamps stops creating records (#1137). **Done 2026-10-31.**
     4. The parser fills a record table, and Core builds its records from it (#1138). **Done 2026-11-02.**
     5. Insights and the graph stop keeping records of a log that is no longer open (#1139). **Done 2026-11-02.**
     6. Filter results, the selection and Clear hold record numbers, and records are created only when read (#1140). **Done 2026-11-07.**
     7. Tests use record lists, the temporary forwarders go, and records on demand are measured (#1141). **Done 2026-11-13.**
   - **Done 2026-11-13.** On the 4 GB log of the memory harness, Weevil needs 1.22 GB of working set per GB of log, at the peak and after opening, against 1.62 after step 1 and 3.25 at the peak for 2.12.1 ([results](../Research/Weevil3/MemoryHarness/README.md#results)). #1086 asks for 1.2. [RecordTable.md](../Design/RecordTable.md) describes the result.
   - **Decisions (2026-10-30):**
     - About 161 bytes per record is accepted for now. The last 4 bytes to reach #1086's 157 are revisited with #1023.
     - The graph graphs at most 100,000 selected records, like Copy and Paste.
     - `IInsight.RelatedRecords` becomes line numbers.
     - Records are equal when their line numbers are equal, even across logs.
   - **Decision (2026-11-02):** records created when they are read (#1140) make filters that never read text, and a Find through every record, about 0.4 s slower on 10 million records. Opening takes about half the time and filters that read text are as fast or faster. The user accepted this; #1150 follows up.
   - **Correction (2026-11-06):** most of that 0.4 s was not the cost of creating records.
     - Creating a record costs about 14 ns more than reading one held in an array, on one thread: a Find through 10 million records takes 0.15 – 0.2 s longer, and the elapsed-time analyzer, which created two records per row, about 0.25 s longer. On four threads, the filter loop takes 0.06 – 0.08 s longer.
     - The rest came from `ElapsedTimes.Calculate`. After every filter it allocates an array as long as the highest line number, about 65 MB even for 16 results. The young collections that creating records triggers left that array on fresh memory each time, and on the 4-vCPU test VM its 15,800 page faults took 0.27 – 0.30 s. `main` reused the memory, in 0.015 s.
     - The design's benchmark (80 ms against 106 ms) compared two ways of storing results, not the cost of a record. Its own figures showed creating a record nearly doubled the loop. It never timed single-thread scans, the real path through the table, or a session after many filters.
     - Prototypes that read the table by position, creating no record per row, measured: Find 0.35 – 0.39 s (this branch 0.53 – 0.55, `main` 0.36 – 0.40, 2.12.1 0.44 – 0.46), and the elapsed-time analyzer 0.07 – 0.08 s (this branch 0.59 – 0.64, `main` 0.30 – 0.32, 2.12.1 0.41). #1150 follows up with those and with `ElapsedTimes`.
   - **Decision (2026-11-07):** elapsed times are mostly displayed, and only sometimes filtered on or searched, and the analyzers and insights may take slightly longer if not excessively (the user). So #1150 calculates elapsed times when they are read, and Find and the elapsed-time search read each row by position; the analyzers still create a record per row.
     - Measured on 10 million records, after 28 filters, against `main` before #1140: moniker 0.11 – 0.12 s (0.12), no filter 0.12 s (0.32 – 0.33), plain text 0.25 s (0.31 – 0.32), Find 0.35 – 0.36 s (0.36 – 0.39). The elapsed-time analyzer takes 0.57 s (0.25 – 0.30).
     - Memory: 153 bytes per record (161 after #1140), and 1.22 GB of working set per GB of the 4 GB harness log (1.28), against #1086's 1.2.
     - Later that day the user asked for the analyzers and insights too. They now read columns and clear flags in one call: the elapsed-time analyzer takes 0.07 s (0.26 – 0.30 before #1140), the out-of-order analyzer 0.09 s (0.23), and the insights after an open 0.18 s (0.20 – 0.21).
   - **Decision (2026-11-06):** Clear keeps a new record table of only the records it keeps (#1140), rather than numbers of records in the log's table, as the [design](../Research/Weevil3/RecordsOnDemand/README.md) planned. Otherwise the kept list would hold the whole table, and Clear would no longer free the memory of the records it clears, as it always has. So record numbers do not survive Clear: #991, whose views would share one table, starts from this. The chunks that only cleared records were read from are already freed, so #1022 starts by checking what is left to do.
4. **Clear frees memory (#1022).**
   - Clear narrows the view's record scope (#990 step 8). Content chunks that the scope no longer includes are released, followed by one compacting garbage collection, as Clear does today. See [EngineApi.md](../Research/Weevil3/EngineApi.md#freeing-ram).
   - Only whole chunks are released: up to about 16 MB stays at each edge of a kept range, and `Clear Selected` frees nothing.
   - Released chunks are not read back until #986, so cleared records return after Reload, as today.
   - This needs only one view, so it does not depend on #991.
   - **Done 2026-11-13.** Since #1140 the records Clear keeps get a table of their own, so the chunks only the cleared records were read from can be freed. But Clear forced its collection before the view, the filter and the selection had let go of the old table, so the memory was freed only by a later collection. The collection now runs once they hold only the records kept, and only when Clear cleared records; before, it ran on every Clear. Managed memory, on the 200,000-record reference log of the performance tests, keeping 1%: 30.6 MB when opened, and 17.1 MB as soon as Clear returns, against 32.3 MB before. The 17.1 MB is mostly the 16 MB chunk the kept records were read from. The collection is now `Aggressive`, as opening's is, rather than `Forced` (decided by the user): `Forced` kept most of the freed memory committed. Keeping 1% of 400,000 records, the working set fell from 207 MB to 99 MB, against 187 MB with `Forced`, and Clear took about 8 ms longer.
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
