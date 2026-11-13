# 1023: Reopen a Log Without Parsing It Again

> **Status:** Draft
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Step 5 of the [#988 plan](988-IndexingService.md#steps), and the last. Task #1023, under user story #978. It delivers requirement #1085: "When a log whose saved index matches the log is opened, the software shall open it without parsing the whole log again."

## Goal

- The `.idx` beside a log saves its record table, as well as the line index it holds today.
- When a log is opened again and its saved index matches it, Weevil builds the table from the index instead of parsing the log.
- The index is rejected, and the log parsed as today, when the log, the plugin or Weevil itself has changed. That includes a change in the middle of the log that keeps its length, which the index cannot see today (the issue's comment of 2026-11-10).
- An index file can be larger than 2 GB.
- Nothing else changes: the same records, in RAM and on disk, and the sidecar keeps its format.

## Where Things Stand

Read from the code on 2026-11-13.

- **The `.idx` holds only a line index** (`LineIndex`, version 1). Its header has:
  - the encoding;
  - the line count and the length indexed;
  - SHA-256 hashes of the log's first and last 4 KB.

  Then come a byte offset for every 64th line and a SHA-256 of the whole file.
  - It is written through one `MemoryStream` and read into one array, so a file over 2 GB is rejected.
  - A log that has grown is indexed only in its new part (#977).
- **A first open reads the log twice:** once for the line index, then once to parse. A 20 GB log takes about 2 minutes, which was accepted.
- **The record table** (`RecordTable`, [RecordTable.md](../Design/RecordTable.md)) holds 21 bytes per record:
  - line number, offset within the chunk, length, the facts byte and the timestamp;
  - in blocks of 65,536 rows, with a chunk directory;
  - strings for rows that keep text a plugin changed. In RAM mode, rows of UTF-16 in the other byte order and UTF-32 also keep strings.
- **Block checksums (#1025)** are an XxHash3 of each 64 KB block. They are computed only in disk mode, and held privately in `LogFileSource` (Common). Core can neither read them nor build a `LogFileSource` from saved ones.
- **The table depends on how the log was opened:**
  - the plugin and its parser;
  - the storage mode;
  - the start line, `UsingRange` and `UsingLimit`.

  Only the GUI passes a start line or a range, when a plugin's Open As window returns one. Reload always parses the whole log from line 1.
- **Plugins:**
  - `IPlugin` has a name but no version. The built-in plugins' names carry `Version=3.0.0.0` for every build of 3.0.
  - A plugin's Open As context is used only after parsing: `GetRecordParser()` takes no argument.
- **Nothing measures a reopen.** The open benchmark, the memory harness and the performance tests all open logs with no `.idx`.

## Design

### The new `.idx`

Version 2, which replaces version 1. The format is new in Weevil 3.0, so an old file is simply rebuilt (decided 2026-11-08).

- **Sections.** A header, then sections, each with its kind, length and XxHash3 checksum:
  - the line index (today's checkpoints);
  - the block checksums;
  - the chunk directory;
  - the record table's columns;
  - the strings.

  A section is written and checked as it streams, so no buffer holds the whole file, and a file can exceed 2 GB.
- **The header:**
  - today's fields, the same encoding, line count and length indexed;
  - who parsed: see [What Identifies a Parse](#what-identifies-a-parse);
  - whether the file holds a record table, and its row and chunk counts.

  `MemoryEstimator` reads the line count from the new header.
- **The columns** are the table's own: line number, chunk-relative offset, length, facts and timestamp. Strings are saved per row, in UTF-8. Loading rebuilds the blocks, the chunk directory and the string lists exactly as the parser does.
- **The head and tail SHA-256 hashes** are kept for a log that grew, to check that its old part is the same. A full check uses the block checksums.

### Which opens save the table, and which use it

- **Only a full parse saves the table:** from line 1, with no range and no limit, outside the temporary folder, as the line index is today. It is saved once the parse has finished, written to `.idx.tmp` and then moved over the `.idx`.
- **Every open uses a matching table.**
  - A full open builds its table from it.
  - An open with a start line or a range takes the rows it needs from it, as Clear does, so it does not parse either.
- **A table that does not match is ignored,** and the log is parsed as today. A full parse then saves a new one.

### What Identifies a Parse

The table is saved with what produced it. Any difference means a parse:

- **the plugin's name**, and the **MVID** (module version ID) of its assembly. It changes with every build, so a rebuilt plugin, even within one version number, never reuses an old table;
- **the MVID of Weevil's Core**, whose parser joins continuation lines;
- **the storage mode**, because UTF-16 in the other byte order and UTF-32 keep strings in RAM and bytes on disk;
- **for a plugin with its own Open As window**, a hash of the context it returned, in case its parser depends on it.

The MVID needs no new member on `IPlugin`.

### Checking That the Log Has Not Changed

- **Block checksums are computed in RAM mode too,** not only on disk, at about 0.14 s per GB, so one `.idx` serves both modes.
- **Reopening checks every block against the saved checksums**, as the log is read. A changed block rejects the table, and the log is parsed instead. This closes the stale-index case the issue's comment describes, wherever the index came from.
  - **In RAM**, the log is read anyway to fill the chunks, so the check costs only the hashing.
  - **On disk**, it costs a full read of the log, about 6 s for 20 GB on NVMe, instead of a parse of about 2 minutes. The chunks are then built on file, from the saved checksums.
- **Common gains a small public API on `LogFileSource`:** read its checksums, and build a completed source from saved ones. Core uses it, and no `InternalsVisibleTo` is added.

### A Log That Has Grown

- The line index is still updated only in its new part, as today.
- **The table is not reused in this plan: a grown log is parsed in full,** and its table saved again. Parsing only the new part must reopen the last record, which may have gained continuation lines, and the last chunk. That is left to a follow-up if reopening growing logs proves slow.

### Memory and Time

- **The loaded table takes the same memory as a parsed one,** so #1086 and #1087 do not move.
- **A table on disk takes about 22 bytes per record** plus the strings, or about 0.17 GB per GB of the reference log. Writing it adds a few seconds to a first open of a 20 GB log.
- **#1085 sets no time target.** The plan measures a reopen against a first open.

## Decisions for the Owner

1. **Check every block when reopening?** Recommended: yes, in both modes. It costs a full read on disk, but rejects a stale index wherever it came from. The alternative is to trust the head and tail hashes, and let disk mode's checks on each read find a change later. That is faster on disk, but the stale-index case in the issue's comment stays open.
2. **Compute block checksums in RAM mode too?** Recommended: yes, so one index serves both modes, at about 0.14 s per GB on every first open in RAM.
3. **Identify a plugin by its assembly's MVID?** Recommended: yes. A plugin then needs no version member, and any rebuild parses again. The alternative is a version member on `IPlugin`, a public contract change that each plugin would have to keep up to date.
4. **A grown log is parsed in full** in this plan. Recommended: yes. Parsing only the new part is a follow-up if needed.
5. **The 4 bytes per record of #1086** (open question 1 of [RecordsOnDemand](../Research/Weevil3/RecordsOnDemand/README.md)): the index saves the length column, as the table holds it. Recommended: keep the length, and leave the 4 bytes to a separate decision. Deriving the length would need a table of exceptions in memory and in the file.

## Steps

One pull request per step, in order.

1. **Block checksums in both modes, and readable.**
   - `LogFileSource` gains its public API (Common).
   - RAM mode computes the checksums too. The cost is measured on the 10-million-record log.
   - Tests: the checksums round-trip; a source built from saved checksums detects a changed block.
2. **The `.idx` version 2 holds today's line index, in sections.**
   - It streams, and a file can exceed 2 GB.
   - It adds the block checksums and the parse's identity. There is no table yet.
   - `MemoryEstimator` reads the new header.
   - Tests: today's `LineIndexTests`, `LineIndexFileTests` and `MemoryEstimatorTests` move to version 2. A section whose checksum fails is rejected. A file larger than 2 GB is written and read back, in a test that creates a sparse file where the system allows.
3. **The table is saved, and reopening uses it.**
   - A full parse saves the table. Every open with a matching index builds its table from it, and an open with a start line or range takes the rows it needs.
   - Tests:
     - **#1085:** a reopen calls no record parser. The test uses a parser that counts its calls.
     - **Equivalence:** a reopened table equals a parsed one. Each case runs in RAM and on disk, over the encodings and line breaks of `OpeningOnDisk.feature`, with strings a plugin changed.
     - **Rejection:**
       - a changed plugin, a changed Core, the other storage mode, or a changed Open As context;
       - a change in the middle that keeps the length;
       - a truncated, grown or corrupt log or index.
     - The equivalence harness compares reopened and parsed tables over the 120 test logs.
4. **Measured and documented.**
   - A reopen benchmark, with logs outside the temporary folder.
   - A reopen case in the memory harness, on the 1 GB and 4 GB logs.
   - [RecordTable.md](../Design/RecordTable.md) and [DiskMode.md](../Design/DiskMode.md) describe the saved table.
   - Step 5 of the #988 plan is marked done.

## Not in This Plan

- Parsing only the new part of a grown log.
- Removing the 4-byte length column (#1086's last 4 bytes).
- Memory-mapping the `.idx`. Loading reads it into the table's arrays. Mapping stays possible if a table is ever too large to hold.
- Reading the log once on a first open, instead of twice: a future consideration of the #988 plan.
- Saving a table from a parse that started at a line or range.
