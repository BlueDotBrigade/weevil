# 988: Keep a Log in a Third of the Memory

> **Status:** Draft
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Phase 3 of the [Weevil 3.0 feasibility study](../Research/Weevil3/Feasibility.md). Epic #988; the remaining work is user story #978. It needs #990: records can be created on demand only once they hold no mutable state.

This plan replaces an earlier draft, `978-CompactRecordStorage.md` on branch `Feature/978-CompactRecords`, which was never merged. Its steps 2 and 3 moved into #990.

## Goal

From #978's acceptance criteria:

- Opening a log produces the same records, line numbers and content as before.
- An open log uses about a third of today's memory: about 1.1 GB per GB of log.
- Filtering is no slower.
- Comments, pins, flags, bookmarks, regions, Clear and the sidecar work as before.
- Reopening a log whose saved index matches does not parse the whole log again.

## Steps

One sub-issue of #978 and one pull request per step, in order. Each step is measured with the phase 1 harness before and after.

1. **Compact content.**
   - Content is stored as UTF-8 in shared, read-only 16 MB chunks, so no array reaches .NET's 2 GB limit. A record keeps an offset and a length instead of a string.
   - `Content` decodes on each access.
   - Regular expression and plain-text filters match a `ReadOnlySpan<char>` decoded into a reused buffer per thread, keeping today's culture and case rules exactly.
   - Content is stored exactly as `Content` returns it today, including multi-line records rejoined with `\r\n`.
   - A record that cannot round-trip through UTF-8 (an unpaired surrogate in a UTF-16 log) keeps its string.
   - Estimate: −38% memory on the test log.
2. **Proof of concept: timestamps.**
   - The question: parse timestamps from content when needed, or keep an 8-byte column?
   - Measure on a 1 GB log: an `@Elapsed` filter, the elapsed-time analyzer, and a go-to-timestamp search, each both ways.
   - This decides the record table in step 3. Record the result in `Research/Weevil3/`.
3. **Records on demand.**
   - Records live in a record table: line number, content offset, the facts byte, and a timestamp if step 2 says so.
   - `IRecord` objects are created for shown rows, a selection or an export, and compare equal by line number.
   - A read-only record list replaces `ImmutableArray<IRecord>`, with `IndexOf` by line number. Filter results and the Clear record set become lists of record numbers.
   - This is the largest step: 147 references in 60 source files and 83 in 21 test files. When writing its sub-issue, split it, for example Core behind an adapter first, then the GUI, then the tests.
4. **Reopen without parsing.**
   - The `.idx` file gains the record table: which lines start records, the facts byte and timestamps if kept.
   - When the saved index matches the log, Weevil builds the table from it instead of parsing. The index format version increases, and older index files are rebuilt.

## Compatibility

- The sidecar format does not change.
- Line numbers, content and filter results stay identical. Each step is checked against the current parser on the test logs and on the 52 real logs used for #977.

## Risks

- After step 1, `Content` allocates a string on every access. Hot paths that read it repeatedly must move to spans.
- Step 3 relies on the row design proven in #990 step 4.
