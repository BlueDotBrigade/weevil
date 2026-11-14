# 1023: Reopen a Log Without Parsing It Again

> **Status:** In progress
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Step 5 of the [#988 plan](988-IndexingService.md#steps), and the last. Task #1023, under user story #978. It delivers requirement #1085: "When a log whose saved index matches the log is opened, the software shall open it without parsing the whole log again."

## Goal

- The `.idx` beside a log saves its record table, as well as the line index it holds today.
- When a log is opened again and its saved index matches it, Weevil builds the table from the index instead of parsing the log.
- A log that has grown since is parsed only in its new part.
- The index is rejected, and the log parsed as today, when the log has changed, or when the plugin or Weevil itself is a new build. That includes a change in the middle of the log that keeps its length, which the index cannot see today (the issue's comment of 2026-11-10).
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

- **the plugin's name**, and the **MVID** (module version ID) of its assembly. Builds are deterministic, so an MVID is a hash of the assembly's compiled output: a build that changed the plugin, even with the same version number, never reuses an old table, and a rebuild without a change keeps it. After installing a new Weevil or plugin, the first open of each log parses it again, and later opens are fast again;
- **the MVIDs of Weevil's Core and Common**: the same build of Weevil. Core's parser joins continuation lines, and Common holds the records and chunks the table is made of;
- **the storage mode**, because UTF-16 in the other byte order and UTF-32 keep strings in RAM and bytes on disk.

The Open As context is not part of it (decided 2026-11-13): the sidecar keeps it, and it never reaches the parser, since `GetRecordParser()` takes no argument.

The MVID needs no new member on `IPlugin`.

### Checking That the Log Has Not Changed

- **Block checksums are computed in RAM mode too,** not only on disk, at about 0.14 s per GB, so one `.idx` serves both modes.
- **Reopening checks every block against the saved checksums**, as the log is read. A changed block rejects the table, and the log is parsed instead. This closes the stale-index case the issue's comment describes, wherever the index came from.
  - **In RAM**, the log is read anyway to fill the chunks, so the check costs only the hashing.
  - **On disk**, it costs a full read of the log, about 6 s for 20 GB on NVMe, instead of a parse of about 2 minutes. The chunks are then built on file, from the saved checksums.
- **Common gains a small public API on `LogFileSource`:** read its checksums, and build a completed source from saved ones. Core uses it, and no `InternalsVisibleTo` is added.

### A Log That Has Grown

Logs usually grow, with records appended at the end, so a grown log is parsed only in its new part (decided 2026-11-13):

1. The saved table is loaded, and the log's old part is checked against the saved block checksums. A changed block means the log was not only appended to, so it is parsed in full.
2. The last record is dropped, because the lines added may continue it, and so is the last chunk, which may now extend further.
3. The log is parsed from that record's start to its end, with the last record's line number as the first line number.
4. The rows are appended to the table, and the extended table and checksums are saved.

The line index is still updated only in its new part, as today. A reopen of a grown log then costs the check of its old part plus a parse of its new lines.

### Memory and Time

- **The loaded table takes the same memory as a parsed one,** so #1086 and #1087 do not move.
- **A table on disk takes about 22 bytes per record** plus the strings, or about 0.17 GB per GB of the reference log. Writing it adds a few seconds to a first open of a 20 GB log.
- **#1085 sets no time target.** The plan measures a reopen against a first open.

## Decisions

Decided by the owner on 2026-11-13:

1. **Reopening checks every block** against the saved checksums, in both modes. It costs a full read on disk, about 6 s for 20 GB on NVMe, but rejects a stale index wherever it came from.
2. **RAM mode computes the block checksums too,** so one index serves both modes, at about 0.14 s per GB on a first open in RAM.
3. **A plugin is identified by its assembly's MVID,** not a version member on `IPlugin`. An unneeded parse after a build that did not change parsing is safer than stale records after a forgotten version bump.
4. **A grown log is parsed only in its new part,** in its own step.
5. **The index saves the length column,** as the table holds it. The last 4 bytes per record of #1086 (open question 1 of [RecordsOnDemand](../Research/Weevil3/RecordsOnDemand/README.md)) stay a separate decision.
6. **A saved table is reused only when the same build of Weevil, Core and Common, and the same build of the plugin made it** (decided after step 2). The Open As context is left out: the sidecar keeps it, and it never reaches the parser.

## Steps

One pull request per step, in order.

1. **Block checksums in both modes, and readable.**
   - `LogFileSource` gains its public API (Common).
   - RAM mode computes the checksums too. The cost is measured on the 10-million-record log.
   - Tests: the checksums round-trip; a source built from saved checksums detects a changed block.
   - Done on 2026-11-13: opening the 1 GB, 10-million-record log in RAM took 7.73 s against 7.60 s on `main` (median of three rounds, alternated, Release): about 0.13 s, within the machine's noise and as estimated. In RAM, the engine still gets no source, because `FilterManager` takes a source to mean disk mode.
2. **The `.idx` version 2 holds today's line index, in sections.**
   - It streams, and a file can exceed 2 GB.
   - It adds the block checksums and the parse's identity. There is no table yet.
   - `MemoryEstimator` reads the new header.
   - Tests: today's `LineIndexTests`, `LineIndexFileTests` and `MemoryEstimatorTests` move to version 2. A section whose checksum fails is rejected. A file larger than 2 GB is written and read back, in a test that creates a sparse file where the system allows.
   - Done on 2026-11-13. Three changes from the plan:
     - The line index is still saved before the parse, as before, and a full parse saves the index again with its parse. So nothing else about loading changed, and an open from a start line, or of a range, saves no parse.
     - The test of a file larger than 2 GB moves to step 3. Until the table is saved, an index holds a few MB at most, and the sections already use 64-bit lengths and stream.
     - What parsed the log and the block checksums share one optional Parse section, flagged in the header, rather than the header and a section of their own.
   - For step 3, from the review of step 2:
     - **The Open As context is lost on Reload.** Decided: the context leaves the identity (decision 6).
     - **An MVID changes only with its own assembly.** Decided: Common's MVID joins Core's (decision 6).
     - **A log written to while it is parsed** can have a parse that ends past the line index, possibly within a record. Reusing the table of a grown log must allow for it.
3. **The table is saved, and reopening uses it.**
   - A full parse saves the table. Every open with a matching index builds its table from it, and an open with a start line or range takes the rows it needs.
   - Tests:
     - **#1085:** a reopen calls no record parser. The test uses a parser that counts its calls.
     - **Equivalence:** a reopened table equals a parsed one. Each case runs in RAM and on disk, over the encodings and line breaks of `OpeningOnDisk.feature`, with strings a plugin changed.
     - **Rejection:**
       - a changed plugin, a changed Core, the other storage mode, or a changed Open As context;
       - a change in the middle that keeps the length;
       - a truncated or corrupt log or index. Until step 4, a grown log is parsed in full.
     - The equivalence harness compares reopened and parsed tables over the 120 test logs.
   - Done on 2026-11-13. Changes from the plan:
     - **An open from a start line, of a range or up to a limit still parses.** Taking its rows from the saved full table would not always give the same records: a parse from a start line searches only 256 lines for its first record, and a range search reaches further than a full parse's. Only plugins with their own Open As window open part of a log, and no built-in plugin has one.
     - **A damaged table costs only the table:** reading the index skips the table's sections, and they are checked when the table is used, so the line index is kept.
     - **The equivalence harness is replaced by tests** that compare a reopen with a parse: 9 encodings and line endings, in RAM and on disk, a log of several 16 MB chunks, and rows whose text a plugin changed.
     - **The #1085 test checks the log entry a reopen writes**, "The records were read from the saved index", rather than counting a parser's calls: a reopen that uses the table skips the parse entirely.
   - From the review of step 3:
     - **A parser must depend only on the log's lines,** which `IRecordParser` and `IPlugin.GetExtension` now say: a reopen no longer calls it. The identity cannot tell two parsers of one plugin assembly apart, which is safe while the choice depends only on the log's content, as it does for the built-in plugins.
     - **Switching between RAM and disk parses again,** and saves the table again (about 221 MB for 10 million records), because the storage mode is part of the identity (decision 6).
     - **The first open writes the table before it returns,** beside the log. For a log on a network share or a slow USB drive, that is about 22 bytes per record written to it. Writing the table after the engine is returned, or not for a remote path, is left for step 5's measurements.
     - **For step 4: a stale line index.** A change in the middle of a log that keeps its length, and adds or removes a line break, passes the line index's checks. The table is then rejected, and the log parsed, but the stale line index is saved with the new parse. When the block check fails, the line index should be rebuilt too.
     - **Notes, not changed:** a string a plugin cut in the middle of a surrogate pair reads back with U+FFFD, since strings are saved in UTF-8; a UTF-16 BE or UTF-32 log in RAM, whose rows all keep strings, takes about 28 bytes per record more while its table loads.
   - Measured on the 1 GB, 10-million-record UTF-8 log, in Release, alternating with `main` in one session: a first open in RAM takes 9.2 – 9.5 s against 8.4 s, to write the table, and a reopen takes 1.9 – 2.0 s against 7.4 – 7.6 s. On disk, a reopen takes 0.9 s. The `.idx` holds 221 MB, 22 bytes per record.
4. **A grown log is parsed only in its new part.**
   - Tests: records appended at the end; a last record that gains continuation lines; growth that starts a new chunk; a log that grew and also changed in its old part, which is parsed in full; the extended table equals a full parse, in RAM and on disk.
   - **Done.** `GrownLogParser` extends the saved table; `LineIndexFile.Load` no longer saves the index of a grown log that holds a table, which dropped the table.
     - **The rows from the last chunk's first record on are dropped,** which includes the last record, and the log is parsed from there. A log under 16 MB has one chunk, so it is parsed again in full, which costs at most 16 MB.
     - **Where parsing resumes comes from the table,** which the checksums vouch for, never from the line index, which can be stale. A table with no chunk, whose rows all keep a string, as UTF-16 BE and UTF-32 in RAM, cannot say, so its log is parsed in full (found in review: resuming from a stale line index lost a record, and saved the table).
     - **The checksums of the new part follow those saved for the old part:** the parse hashes from the start of the block that holds its first byte.
     - **The whole table is saved again,** about 0.8 s for 10 million records. Appending to the `.idx` in place is left out.
     - **After a partial open of a grown log, the line index's new part is not saved,** so the next open scans it again. A full open saves both. Likewise, a log written to while it was parsed has its line index's tail scanned on each reopen until it grows.
     - **The stale line index is fixed where a full parse came before:** when the bytes a full parse read differ from those of the index's earlier parse, in any storage mode or build, the log changed in the middle, and the line index is rebuilt before it is saved. A damaged table costs only the table, as in step 3. An index saved only by a partial open has no parse to compare with, so a change in the middle stays unseen, as on `main`.
     - **A grown reopen holds the saved table's rows and the longer table's at once,** about 22 bytes per record more while it copies them. Step 5's memory case of a reopen after the log grew measures it.
     - Tests: appended records in 5 encodings and line endings, in RAM and on disk; a UTF-16 BE log in RAM, parsed in full; a last record that gains lines; a log that grew twice; a log that grew and changed in its old part; a line break put in the middle, then a full open in each mode and an open from a start line; a 40 MB log that grows by 25 MB into a new chunk, reopened twice, in RAM and on disk, which keeps the first chunks' rows; the comparison of two parses' bytes.
   - Measured on the 1 GB, 10-million-record UTF-8 log, in Release: its first 99% opened, then grown back by 1% (about 10 MB). A reopen takes 3.0 s in RAM and 2.1 s on disk, against 8.4 s and 7.6 s to open it in full.
5. **Measured and documented.**
   - A reopen benchmark, with logs outside the temporary folder.
   - A reopen case in the memory harness, on the 1 GB and 4 GB logs, and a reopen after the log grew.
   - [RecordTable.md](../Design/RecordTable.md) and [DiskMode.md](../Design/DiskMode.md) describe the saved table.
   - Step 5 of the #988 plan is marked done.

## Not in This Plan

- Removing the 4-byte length column (#1086's last 4 bytes).
- Memory-mapping the `.idx`. Loading reads it into the table's arrays. Mapping stays possible if a table is ever too large to hold.
- Reading the log once on a first open, instead of twice: a future consideration of the #988 plan.
- Saving a table from a parse that started at a line or range.
