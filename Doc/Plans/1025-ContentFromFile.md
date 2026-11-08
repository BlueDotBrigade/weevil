# 1025: Read Content Back from the File

> **Status:** Draft
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Step 2 of the [#986 plan](986-DiskMode.md#steps): disk mode keeps no content in memory and reads it from the log when it is needed. Task #1025, under user story #421.

## Goal

- In disk mode, a record's content is read from the log when it is needed. The record table stays in memory: about 22 bytes per record.
- RAM mode does not change.
- Disk mode can be chosen headless, through the engine builder, so it can be tested before the Open As window (#1028).
- Weevil never keeps the log open between reads, never blocks the application writing it, and never modifies it.

## Design

Drafted on 2026-11-08 from two independent designs and a judge's synthesis.

- **`StorageMode { Ram, Disk }`**: a public enum in Common, `BlueDotBrigade.Weevil.IO`. Until #1028, only internal builders take it.
- **`LogFileSource`**: one per opened log, in Common. It holds the path, the length that was parsed, and a checksum of every 64 KB block of the log.
  - Opening computes the checksums as the bytes go past: a fast 64-bit hash (XxHash3, from Microsoft's `System.IO.Hashing` package), about 8 bytes per 64 KB, or 2.4 MB for a 20 GB log. Its cost, estimated at about a second of CPU per 20 GB, is measured in step 2.
  - Each read opens its own handle, sharing reading, writing and deletion, reads whole 64 KB blocks around what it needs, checks their checksums, and closes the handle. It never memory-maps the log, never retries, and keeps no cache.
- **A changed log** (decided 2026-11-08):
  - **Grown:** the log is longer than when it was opened, and the blocks read still match. This is normal: Weevil logs a warning once and shows nothing.
  - **Truncated, replaced or corrupted:** the log is shorter, or a block read has a different checksum. The read that found it stops and returns no text. Weevil shows and logs a warning, with the size expected and the size found, or the offset of the changed block, then reloads the log, as Ctrl+F5 does. Reload parses the log again and rebuilds its indexes, and keeps comments, pins, bookmarks, the filter and the mode.
  - Disk mode can only show or filter bytes it reads back, and it checks every block it reads, so a change anywhere is caught before it can show other text. RAM mode needs no check: its content is in memory until Reload.
  - How Core signals the change to the engine and the GUI is settled in step 3.
- **`ContentChunk`**: a second constructor makes a chunk on file, which holds its file range but no bytes. A chunk never changes, so records on any thread can still read it without locks.
  - A single record read from a chunk on file reads only the 64 KB blocks that hold it.
  - `ContentBuffer` keeps one chunk on file at a time. It reads the whole chunk when a scan reaches it, and reuses its array.
- **Parsing**: in disk mode, `LogLineReader` makes chunks on file instead of copying their 16 MB of bytes.
- **The engine**: `CoreEngineBuilder` and `EngineBuilder` take the mode. `CoreEngine` keeps it, and Clear, Reload and a retry after a failed Reload carry it.
- **Caching rules**:
  - RAM mode: unchanged.
  - Disk mode: no chunk keeps its bytes at rest. Each reader holds one chunk while it reads it, and single-record reads cache nothing.
- **Clear and RAM read-back**: Clear's new table keeps only the chunks its kept rows use (`RecordTable.Keep`), so RAM mode never needs to read a chunk back in this step. #1023, #1026 and #991 will read bytes up front through the same `LogFileSource`.
- **Records in other byte orders and encodings**:
  - In RAM mode, as today, records of UTF-16 logs in the other byte order and of UTF-32 logs keep a string: .NET decodes those 10 to 50 times more slowly (#1133).
  - In disk mode, every record is read from the drive, whatever the encoding. Those two encodings only decode more slowly.
  - Text a plugin's parser changed is not in the file, so it stays in memory in both modes.
- **Zipped logs**: the GUI keeps the extracted copy while its log is open. It deletes the copy after another log opens, after a zip open that fails or is cancelled, and when Weevil exits; Reload keeps it. This also fixes Reload of a zipped log (#1005).

## Decisions

Proposed on 2026-11-08, to confirm:

1. **No handle is held between reads.** Each read opens its own. "Disk mode keeps the file open" in plan 986 changes to match, and `IEngine` stays non-disposable, as #1030 decided.
2. **Disk caching:** one chunk per reader, and single-record reads are not cached. A cache for screen rows comes only if a measurement shows it is needed (step 4).
3. **No RAM read-back in this step.** Later steps read bytes up front through the same source.
4. **A changed log** (decided 2026-11-08): detected by a checksum of every 64 KB block, verified on every read back. A grown log logs a warning and nothing else; a truncated, replaced or corrupted log shows and logs a warning, then reloads.
5. **API:** the enum is public now; `IEngineBuilder.UsingStorage` and the CLI and PowerShell parameter come with #1028.
6. **Disk mode reads every encoding from the drive** (changed 2026-11-08, after the owner's question). Only text a plugin changed stays in memory, because it is not in the file.
7. **The zip copy:** deleted as described in the design. Extracting only the chosen file, and removing leftovers at startup, are later issues.
8. **Sharing deletion everywhere:** `FileHelper.Open` shares deletion for every open of the log, in its own pull request.
9. **#1087:** its 137 MB per GB becomes the measured disk-mode figure, about 22 bytes per record plus content being read. Dropping table columns would change the `.idx` format, which this phase excludes.
10. **Several pull requests for one step**, as #988 did.
11. **CI** runs the Core tests a second time in disk mode, through a Debug-only `WEEVIL_TEST_STORAGE=Disk`.
12. **Order:** #1025 does not need #1022 or #1023. #1022 has one concrete fix left (Clear's forced garbage collection runs before the kept table is built) and stays under #988.

## Steps

One pull request each, in order:

1. **Keep a zipped log's copy while it is open (GUI).** Tests: Reload of a zipped log brings its records back (`[WorkItem(1005)]`); the copy is deleted when another log opens and when Weevil exits; it is kept when another log fails to open; an export saved beside it is kept.
2. **The read path in Common, with no caller yet:** `LogFileSource`, chunks on file, `ContentBuffer` and an internal `Record` accessor. Tests: a chunk on file decodes the same text as the same chunk in memory, for single and multi-line records, lone `\r`, invalid bytes, UTF-8, both UTF-16 byte orders and UTF-32; a buffer that moves to another chunk; a truncated, replaced or corrupted log, including one byte changed in the middle, is reported as changed; a log that only grew is reported as grown, not changed; a writer is never blocked; a missing log fails at once; the time the checksums add to opening a large log.
3. **Disk mode in Core, headless.** Tests: a Reqnroll feature, "A log opened on disk shows the same records and filter results as in RAM", over several encodings and line endings; a log that changed is reloaded after a warning; a log that grew only logs a warning; Reload, a failed Reload and Clear keep disk mode; a builder with no mode opens in RAM; a memory budget for disk mode. CI gains the disk-mode run.
4. **Only if needed: a cache for screen rows.** A grid row reads its content up to five times through the converters, and each read opens the log. Measure it with a screen case that opens a handle per read, in the #1024 harness, before deciding.

Outside this plan: Clear's garbage collection fix (#1022), and `FileHelper.Open` sharing deletion (decision 8).

## Risks

- **RAM mode regresses:** steps 2 and 3 leave its path untouched; the existing Clear, Reload and memory-budget tests guard it.
- **File reads on the UI thread:** nobody can choose disk mode until #1028; #1027 moves scans off the UI thread; step 4 covers screen rows.
- **The log changes or disappears:** a changed log is reloaded rather than shown with other text. A log that disappears cannot be reloaded, so Reload's existing message says so.
- **A new package:** `System.IO.Hashing`, from Microsoft, for the checksums.
- **UTF-16 in the other byte order and UTF-32 decode slowly in disk mode:** filtering such a log is slower than a UTF-8 one, but it fits in memory.
- **A handle per read is slow** on network drives or with antivirus: measured before step 4 is decided.
- **Parallel filter workers may read the same chunk twice** where their ranges meet: correct but slower; #1027 replaces the split.

## Size

About 600 lines of source and 700 of tests over steps 1 to 3, and about 200 more if step 4 is needed.
