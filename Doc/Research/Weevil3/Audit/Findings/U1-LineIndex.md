# U1: Line Index and Parsing (#988)

Part of the [Weevil 3.0 audit](../Progress.md). This code reads a log once, records the byte offset of every line, parses records into a record table, and saves the index and table beside the log (`<log>.idx`) so a later open can skip the parse.

> **Status:** Final. Reviewed and challenged on 2026-11-17 at `main` e3dee822.

## Summary

Each class in this unit is careful and readable on its own. Each says what it is for, cites its issue, validates everything it reads from disk, and treats the `.idx` as a cache that is always safe to delete. Ownership is simple: no type here keeps a file handle beyond one method. Both line scanners are tested against `StreamReader` as an oracle.

Against 2.x (a `StreamReader`, a 190-line parser and a list of records, about 290 lines) this is roughly nine times the code. Most of that buys real capability: byte offsets per line, raw-byte content chunks, disk mode, and a saved table that makes a reopen 3 to 8 times faster than a parse.

**The weakness is between the classes, not in them.** What an open reuses and writes is decided in `LineIndexFile`. `CoreEngineBuilder` calls those rules in order around the parse, and the two share settable flags on `LineIndex`, where `HasTable` has three meanings. The design notes say what each open reads, but what it writes is only in code remarks and the dated plan. This is Medium, not High (U1-1). Every rule the review history fixed is now pinned by a scenario, and #1212 and #1213 (sections and regions) went in without touching the policy. The cost falls on whoever next changes what an open reuses or saves, which no planned 3.0 work does. The unit has no High finding.

Second, **"no source means RAM" is a protocol, not a type**, and `FilterManager` reads the storage mode back from a null. Third, **users are never told about the `.idx`** that now appears beside every log they open. It's about a fifth of the log's size, and nothing can turn it off.

## What Was Examined

- `Core/IO`: `LineIndex.cs`, `IndexSection.cs`, `LineIndexFile.cs`, `LogLineReader.cs`, `SavedParse.cs`, `SavedTableReader.cs`, `GrownLogParser.cs`.
- `Core/Data`: `MultiLineRecordParser.cs`, `SerializedRecordRepository.cs`, `RecordTable.cs` (saved-table members).
- Callers: `CoreEngineBuilder.cs`, `CoreEngine.cs` (save), `Engine.cs` (reload), `FilterManager.cs` (use of the source).
- 2.x: `Core/Data/MultiLineRecordParser.cs`, `SerializedRecordRepository.cs`, the `Core/IO` file list.
- Documents: `Doc/Design/RecordTable.md`, `DiskMode.md`; `Doc/Plans/988-IndexingService.md`, `1023-ReopenWithoutParsing.md`; `Doc/Notes/Release/Help.md`; the memory harness results.
- Tests: `LineIndexTests`, `LineIndexFileTests`, `LogLineReaderTests`, `IndexSectionTests`, `SavedTableReaderTests`, `EngineTests` (reopen), `Reopening.feature`, the reopen and open benchmarks.

## Strengths

- **The `.idx` is a self-validating cache.** It has a signature and version, a checksum per section and an end marker, and it's written to a temporary file and then moved, so a damaged or partial file is simply rebuilt (`Src/BlueDotBrigade.Weevil.Core/IO/LineIndex.cs:247-301`, `IO/LineIndexFile.cs:153-163`). A test flips every byte in turn (`Tst/BlueDotBrigade.Weevil.Core-UnitTests/IO/LineIndexTests.cs:297-316`).
- **Defensive reads.** Counts read from the file are checked against the section's length before anything is allocated. The code states the reason: "the checksum detects damage, not a file made to claim billions of lines" (`LineIndex.cs:418-422`, `IO/SavedTableReader.cs:109-113`, `:153-157`).
- **Explicit trust boundaries.** A grown log resumes from the checksummed table, never from the line index. A guard enforces this, and the remark cites the review defect that led to it (`IO/GrownLogParser.cs:15-21`, `:53-61`).
- **One contract, tested against an oracle.** "The same lines as `StreamReader.ReadLine`" is written down and tested over every byte order mark, LF, CR and CRLF, with buffers small enough to split a line break (`LineIndexTests.cs:12-110`, `LogLineReaderTests.cs:38-81`).
- **A parse is identified by build.** The MVIDs of the plugin, Core and Common identify a parse, so any change to the table's layout is safe without a version bump. The cost (every new build parses once) is stated honestly (`IO/SavedParse.cs:12-20`, `:211-214`).
- **Simpler than 2.x where it replaced 2.x.** `MultilineRecordParser` no longer mutates the current and next `IRecord`, and it builds a `StringBuilder` only for a multi-line record that keeps a string (`Data/MultiLineRecordParser.cs:157-158`, `:179-184`).
- **Specifications that read as requirements.** `Reopening.feature` is tagged `@Requirement:1085` and checks every case against a fresh parse of a copy (`Tst/BlueDotBrigade.Weevil.Core-FeatureTests/IO/Reopening.feature:6-24`).
- **Plans that record their own corrections.** The plans keep measurements beside decisions and note where the code departed from them (`Doc/Plans/1023-ReopenWithoutParsing.md:115-124`, `:158-162`).
- **Room for #991.** The table is immutable, the index is not changed after the open, and a `.idx` replaced by another Weevil instance is re-read rather than trusted (`LineIndexFile.cs:237-247`).

## Findings

### U1-1. The open-and-save policy is split between `LineIndexFile` and `CoreEngineBuilder`, which coordinate through flags on `LineIndex`

- **Priority:** Medium
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **The shared state.** `LineIndex` carries what the saved file holds as settable properties: `Parse` (public set, `Src/BlueDotBrigade.Weevil.Core/IO/LineIndex.cs:72`) and `HasTable` (internal set, `:80`). `Write` sets `HasTable` as a side effect (`:298`), and `Reset` clears both (`:183-184`). `TableOffset` (`:85`) is not part of this: its setter is private, and it is read only from an index just read from the file (`IO/LineIndexFile.cs:249`).
  - **`HasTable` has three meanings.** After `TryRead` it means "the file holds a table" (`LineIndex.cs:518`). In `Load` it means "do not save over the file, or the table is lost" (`IO/LineIndexFile.cs:60`). After a rejected table it is cleared "so the parse that follows saves a table again" (`:258`, `:269`), which `SaveParse` reads as "save" (`:192`). `SaveParse` also assigns `Parse` (`:205`) and may rebuild the line index (`:197-203`).
  - **Two classes decide, in order.** `LineIndexFile` holds the rules: load, the line-index save, table reuse, the save after a parse, and deletion. `CoreEngineBuilder` runs them around the parse: `isFullOpen` (`CoreEngineBuilder.cs:359`), `TryReopen` (`:253-275`) and the save after a parse (`:393-395`). The other checks are local. `GrownLogParser.cs:48` and `:58` are preconditions of its own algorithm, and `Engine.cs:233-238` deletes the index on Reload, as `Doc/Design/DiskMode.md:43-44` says. The line index is saved before the parse and again after it. Step 2 of the #1023 plan chose this so that loading did not change (`Doc/Plans/1023-ReopenWithoutParsing.md:141`).
  - **About nine open paths.** First open; reopen; grown reopen; table rejected; plugin, build or mode changed; partial open with a table; partial open without; temporary folder; Reload after a change. The design notes describe what each open reads: which opens reuse, parse, extend or rebuild (`Doc/Design/RecordTable.md:43-46`, `DiskMode.md:29`, `:43-44`). What each open writes is only in code remarks (`LineIndexFile.cs:35-38`) and the dated plan (`1023-ReopenWithoutParsing.md:177-178`). That covers when `Load` saves the line index, that a partial open of a grown log saves nothing, and that a damaged table is saved again.
  - **History.** Review found two defects of this kind: a stale line index saved with a new parse (`1023-ReopenWithoutParsing.md:167`), and resuming from a stale line index, which lost a record (`:174`). Step 4 also had to stop `Load` saving a grown index, which dropped the table (`:172`). A review added the `HasTable` reset (commit c78a9793). All were caught before merge, and a `Reopening.feature` scenario now pins each rule (`Tst/BlueDotBrigade.Weevil.Core-FeatureTests/IO/Reopening.feature:49`, `:74`, `:91`, `:110`).
  - **Memory can disagree with the file.** When the move after a save fails, `HasTable` and `Parse` stay set although the file was not replaced (`LineIndex.cs:298` runs before the move at `LineIndexFile.cs:163`). This is harmless today. The engine then reads only `LineCount` and `GetByteOffset`, and `TryLoadTable` reads the file again (`:241`).
- **Impact:** Anyone who next changes what an open reuses or saves must read `LineIndexFile`, two blocks of `CoreEngineBuilder` and the dated plan to learn the save rules, then get the call order and the flags right. The ideas the plans list are that kind of change: partial opens that reuse the table, appending to the `.idx`, and one pass on a first open (`1023-ReopenWithoutParsing.md:159`, `:176`; `988-IndexingService.md:107`). None is planned for 3.0. Changes that only narrow an open leave the policy alone. #1212 and #1213 (sections and regions, the Core part of #1157) became ranges, and `isFullOpen` kept them from reusing or saving a table. Users are mostly protected: every block is checked before a table is reused. A save in the wrong state can still keep a stale line index, which is checked only at its start and end.
- **Recommendation:** Now, extend `Doc/Design/RecordTable.md#saved-with-the-index` with what each open writes, and a table of the open paths: what is read, checked and written, and which class decides. That takes about an hour. Later, as the first step of any change to what an open reuses or saves, make `LineIndexFile` the one home of the policy:
  - move `TryReopen` and the save after a parse into it, so that one method opens the saved index and one saves it;
  - consider renaming it, for example `SavedIndex`, since half of it is about the table;
  - replace the `HasTable` writes with return values: `TryLoadTable` returns that the table was rejected, and the save takes that result;
  - keep the reopen out of the opening method's frame (`CoreEngineBuilder.cs:243-248`).

  Do not do this before 3.0: it would rework fresh, reviewed code with no change in behaviour or file format. **Complexity:** S for the document, M for the refactor.
- **Sceptic:** Revised: High to Medium. The policy lives in two classes, not four. `GrownLogParser`'s checks are its own, and `Engine`'s delete is documented. `TableOffset` is not settable. `RecordTable.md:43-46` already lists what each open reads, so only what it writes is undocumented. Every past defect is now pinned by a scenario. The #1157 sections and regions (#1212, #1213) went in without touching the policy, and no planned 3.0 work changes it.

### U1-2. "No source means RAM" is a protocol, not a type

- **Priority:** Medium
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **Untyped hand-off.** A parse yields a table, the source records read back through, and the checksummed source for the `.idx`. They travel as two properties, one of them null in RAM (`Data/SerializedRecordRepository.cs:65`, `:74`, `:125-126`), and as out-parameter pairs (`IO/LineIndexFile.cs:219`; `IO/SavedTableReader.cs:27-35`; `IO/GrownLogParser.cs:34-43`, seven inputs and two outputs).
  - **The mode is read back from a null.** `CoreEngineBuilder.cs:274` and `SerializedRecordRepository.cs:126` set the source to null in RAM. `Filter/FilterManager.cs:154` reads it back as the mode: `alignToChunks: _source != null`. The plan states the rule in prose: "FilterManager takes a source to mean disk mode" (`Doc/Plans/1023-ReopenWithoutParsing.md:134`).
  - **Downcasts.** Both production callers pattern-match the table back out of a `RecordList` (`CoreEngineBuilder.cs:393`, `GrownLogParser.cs:73`: `is TableRecordList { WholeTable: RecordTable ... }`).
  - **Chunks are created by mode in three places:** `IO/LogLineReader.cs:211-222`, `SavedTableReader.cs:47-72` and `GrownLogParser.cs:92-94`.
- **Impact:** Reading content is well hidden behind `ContentChunk`, so the mode does not spread through readers. Producing a table is a different story. Each new producer, such as #991's views or a partial open that reuses the table, must repeat the null rule and the downcast. A mistake, such as passing the parsed source in RAM, silently changes how filters split their work, and no test would notice.
- **Recommendation:** Return one small internal value from parse, reopen and extend, holding the `RecordTable`, the checksummed `LogFileSource` and the `StorageMode`. Derive "the source records read back through" from the mode in one place. Let `FilterManager` ask the mode instead of testing for null. This removes both downcasts and the null protocol. **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U1-3. `LineIndex` is three things: line offsets, encoding detection and the `.idx` file format

- **Priority:** Medium
- **Kind:** Intent
- **New in 3.0?** New design
- **Evidence:**
  - **The class does more than its summary says.** The summary reads "The byte offset of every line in a log file" (`IO/LineIndex.cs:12-14`), yet about 280 of its 721 lines are the container format. `Write` (`:247-301`) writes the whole file, including the record table's sections. `TryRead` (`:349-529`, 181 lines, SonarCloud cognitive complexity 28) reads the header, checkpoints and parse, skips the table, checks the end marker and fingerprints the log.
  - **Encoding detection is borrowed.** `LogLineReader` calls `LineIndex.DetectEncoding` and `CreateEncoding` (`IO/LogLineReader.cs:69`, `:71`).
  - **The header layout is written out three times by position:** `LineIndex.cs:260-275` (write), `:321-330` (`TryReadLineCount`) and `:373-391` (`TryRead`).
  - **Each section's reader is apart from its writer.** The chunk directory is written in `Data/RecordTable.cs:260-270` and read in `IO/SavedTableReader.cs:96-138`, with a literal `16L` at `:110`. Strings are written at `RecordTable.cs:275-300` and read at `SavedTableReader.cs:140-173`, with a literal `5L` at `:154`. `FormatVersion` lives in `LineIndex.cs:29`, and nothing in `RecordTable` points to it.
  - **Names.** The documents call the file "the saved index". The code calls the container `LineIndex`, and its policy class `LineIndexFile`, half of which is about the record table (`LineIndexFile.cs:186-290`).
- **Impact:** A reader looking for "where is the `.idx` format" or "where is the encoding detected" does not expect `LineIndex`. A format change touches four classes, with no compiler help that reader and writer agree. The MVID check limits the damage to the header, checkpoints and parse sections, since a new build never reads an old table (`IO/SavedParse.cs:94-98`).
- **Recommendation:** Keep `LineIndex` to scanning, checkpoints and lookup. Move the container (signature, version, section order, end marker) into the class proposed in U1-1. Give the header one small type with `Read` and `Write`, put each section's reader beside its writer, and split `TryRead` into the steps its comments already name. The format does not change. **Complexity:** M.
- **Sceptic:** Not challenged (Medium)

### U1-4. Two hand-written line-break scanners and an unnamed encoding tuple

- **Priority:** Medium
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **Two scanners.** `LineIndex` scans with a generic `ScanUnits<T>` and a `ScanState` carried across buffers (`IO/LineIndex.cs:612-708`). `LogLineReader` solves the same problem another way: a switch per code-unit size (`IO/LogLineReader.cs:289-317`, SonarCloud complexity 16), `IsLineFeed` (`:319-327`), and `\r\n` handled by reading more (`:145-158`). Both must count lines exactly as `StreamReader.ReadLine` does, or a start-line open and the sidecar's byte offsets point at the wrong line.
  - **An unnamed tuple.** The encoding is `(int UnitSize, bool IsBigEndian, int PreambleLength)` at `LineIndex.cs:122`, `:179`, `:556` and `LogLineReader.cs:69`. The same-byte-order test is repeated at `LineIndex.cs:645` and `LogLineReader.cs:113`, `:291`.
  - **A third detection** goes through `StreamReader` at `CoreEngineBuilder.cs:334`, inherited from 2.x.
  - **How they agree today.** Each scanner is tested against `StreamReader`, so they agree indirectly. No test compares the two with each other.
- **Impact:** A fix to line-break or byte-order-mark handling must be made twice, in two styles. The two scanners can drift on an edge case the shared oracle does not cover. It also keeps "one pass on a first open" (`Doc/Plans/988-IndexingService.md:107`) further away than it needs to be.
- **Recommendation:** Introduce one value type, for example `LogEncoding` (unit size, byte order, preamble length; `Detect`, `ToEncoding`, `IndexOfLineBreak`). Use it in both classes and in the header. Add one test asserting that `LineIndex` offsets equal `LogLineReader` offsets over the encodings and line breaks already in the tests. Leave `TryReadLine`'s buffer-edge state machine as it is; its complexity is inherent and well tested. **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U1-5. Users are not told about the `.idx`, and nobody can turn it off

- **Priority:** Medium
- **Kind:** Intent
- **New in 3.0?** New design
- **Evidence:**
  - **Every full open writes it.** Any full open outside the temporary folder writes `<log>.idx` beside the log before `Open` returns (`IO/LineIndexFile.cs:143-174`, reached from `CoreEngineBuilder.cs:275`, `:393-396`).
  - **It's large.** Rows are 22 bytes each (`Data/RecordTable.cs:303-324`): 221 MB for the 1 GB, 10-million-record log (`Doc/Design/RecordTable.md:51`) and 0.66 GB for the 4 GB harness log.
  - **It's rewritten often.** Switching storage mode, a new build of Weevil or a plugin, and every reopen of a grown log rewrite the whole file (`Doc/Plans/1023-ReopenWithoutParsing.md:165`, `:176`, `:190`).
  - **No way to opt out.** `IEngineBuilder` has no option for it (`Src/BlueDotBrigade.Weevil.Common/IEngineBuilder.cs:11-63`), and there is no setting.
  - **Not documented.** `Doc/Notes/Release/Help.md` explains RAM and Disk but never mentions the file, and the 3.0 change log is still TBD. `.github/copilot-instructions.md:10` says persisted state lives in XML sidecars.
  - **Tests are affected too.** Test logs opened through DatenLokator sit outside the temporary folder, so whether an engine test exercises the parser or the saved-table reader depends on what opened the same log first (`.gitignore:380-382`; `Tst/BlueDotBrigade.Weevil.Core-UnitTests/IO/LineIndexFileTests.cs:17-18`). The benchmarks avoid this on purpose by keeping logs in the temporary folder (`Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/Benchmarks/OpenBenchmarks.cs:8-10`). The unit, feature and functional tests don't.
  - **Network shares.** The plan left a slow network share unmeasured: "an issue can follow if it proves costly" (`Doc/Plans/1023-ReopenWithoutParsing.md:189`).
- **Impact:** In 2.x the only extra file was the `.xml` sidecar, written when the user saved. In 3.0, opening a log creates a second file about a fifth of the log's size in the log's folder. That affects shared evidence folders, synced or backed-up folders and nearly full disks, and a user who notices it has nowhere to read what it is.
- **Recommendation:** Before 3.0 ships, say in `Help.md` and the change log what the `.idx` is, how large it gets, and that it is safe to delete. Consider a builder option and CLI switch to open without reading or saving it. Tests and benchmarks could use the same option, and each test would then state which path it specifies. Documentation: S. Option: M.
- **Sceptic:** Not challenged (Medium)

### U1-6. Reopen tests leave disk-mode rejection and the table's structural checks unspecified

- **Priority:** Medium
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **Two paths.** `SavedTableReader` checks the log differently in each mode: on disk it builds chunks from the saved checksums (`IO/SavedTableReader.cs:47-60`); in RAM a `ChunkFiller` copies the bytes (`:62-72`).
  - **Rejection is specified only in RAM.** The scenarios "A log changed in the middle", "saved record table is damaged" and "grew and also changed in its old part" all open in RAM (`Tst/BlueDotBrigade.Weevil.Core-FeatureTests/IO/Reopening.feature:26-32`, `:41-56`, `:101-108`). The Disk row of the line-break outline (`:110-121`) first opens in RAM, so its Disk open is rejected by the storage mode (`IO/SavedParse.cs:98`), not by a block check. No test rejects a Disk reopen because a block changed.
  - **CI's Disk run adds nothing here.** Each scenario names its mode, so the second run with `Tst/Disk.runsettings` doesn't change these tests (a mode given to the builder wins, `Doc/Design/DiskMode.md:50`).
  - **The structural checks are untested.** `SavedTableReaderTests` holds one round-trip test. Nothing tests the checks that guard a file whose checksums are valid but whose contents are wrong: overlapping or overrunning chunks, wrong counts, duplicate strings (`SavedTableReader.cs:110`, `:126`, `:154`, `:165`; `LineIndex.cs:399-407`). The same goes for the "index changed after it was loaded" branch (`LineIndexFile.cs:241-247`).
- **Impact:** The check that makes a saved table trustworthy on disk is the one path with no specification. On disk a wrong accept would surface later as a `LogFileChangedException`, not as wrong text, so the risk is bounded. A regression would still cost a confusing reload instead of a failing test.
- **Recommendation:** Turn the three RAM-only rejection scenarios into outlines over RAM and Disk, with a few Examples rows each. Add table-driven unit tests to `SavedTableReaderTests` for a directory that overlaps, runs past the parse or disagrees with its count, and for a duplicate string. **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U1-7. Changing storage mode rejects a saved table that would be identical

- **Priority:** Medium
- **Kind:** Architecture
- **New in 3.0?** New design
- **Evidence:**
  - **The check.** `SavedParse.IsSameParser` compares the storage mode (`IO/SavedParse.cs:98`), because "some encodings keep strings in RAM and bytes on disk" (`:17`).
  - **The reason covers only some encodings.** That reason applies only to logs that are not quick to decode: UTF-16 in the other byte order and UTF-32 (`IO/LogLineReader.cs:113-119`). For UTF-8 and native UTF-16, rows keep bytes in both modes (`Data/MultiLineRecordParser.cs:311`), and chunk boundaries do not depend on the mode (`LogLineReader.cs:199-202`). `SavedTableReader` already builds the chunks for the mode asked for now, not the mode saved (`IO/SavedTableReader.cs:47-72`).
  - **It is deliberate.** Decision 6 records it ("about 221 MB for 10 million records", `Doc/Plans/1023-ReopenWithoutParsing.md:165`), and a test pins it (`Reopening.feature:34`).
- **Impact:** A user opens a large UTF-8 log in RAM, then follows the Open As advice and reopens it on disk, which is the scenario #986 exists for. That user pays a full parse and a full rewrite of the `.idx` instead of a block check: 8.4 s instead of about 1 s for the 1 GB log, by the repo's own figures.
- **Recommendation:** Record in the parse whether the mode shaped the table (the log is not quick to decode), and compare the mode only then. One `.idx` then serves both modes for almost every log, which is what decision 2 of the #1023 plan set out to do. This needs the owner's agreement, since decision 6 made the mode part of the identity. **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U1-8. A range, region or section opened by name parses from line 1

- **Priority:** Low
- **Kind:** CPU
- **New in 3.0?** Existing weakness, made more visible by 3.0
- **Evidence:**
  - **Only a start line seeks.** `CoreEngineBuilder.cs:373-381` uses `lineIndex.GetByteOffset` when a start line is above 1. A range reads from byte 0 (`:382-386`, `:576-585`: "Reading starts at the top of the file"). `Data/SerializedRecordRepository.cs:109-121` parses and discards every record before the range.
  - **What 3.0 changed.** 2.x read the whole file for a range. 3.0 added `UsingRegion` and `UsingSection` (`CoreEngineBuilder.cs:204`, `:226`; #1212, #1213), which the CLI and PowerShell offer, and a line index whose purpose is exactly this seek.
  - **The GUI is not affected.** It passes the range's start as the start line (`Src/BlueDotBrigade.Weevil.Gui/Filter/FilterViewModel.cs:825-828`).
- **Impact:** From the CLI or PowerShell, opening the last region of a large log costs a parse of everything before it. Not measured.
- **Recommendation:** For a region or section, whose first line is a record's first line, seek through the line index as the start-line path does, with the #1237 allowance. Measure first. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U1-9. How a log was opened is visible only as log text

- **Priority:** Low
- **Kind:** Intent
- **New in 3.0?** New design
- **Evidence:**
  - **Not a fact the engine exposes.** Whether records were parsed, read from the saved table or extended is not on the engine: `LogFileMetrics` holds file size, record count and two durations (`Src/BlueDotBrigade.Weevil.Common/LogFileMetrics.cs:16-19`).
  - **So the tests match log text.** Production code carries message constants for tests to match (`IO/LineIndexFile.cs:24-30`, `IO/GrownLogParser.cs:24`). Requirement #1085 is verified by swapping the process-wide logger (`Tst/BlueDotBrigade.Weevil.Core-FeatureTests/IO/ReopeningSteps.cs:124-156`), which is why those tests need `[DoNotParallelize]`.
  - **And one copy can drift.** The benchmark repeats the string because a Release build cannot see the constant (`Benchmarks/ReopenBenchmarks.cs:17-18`).
- **Impact:** The tests assert on a diagnostic side channel, and the GUI, CLI and telemetry cannot say that an open was a reopen, which is the user-visible benefit of #1023.
- **Recommendation:** Record how the records were obtained (parsed, reopened, reopened and extended) on the engine's metrics, and assert on that. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U1-10. `MultilineRecordParser` encodes a two-case record as sentinels

- **Priority:** Low
- **Kind:** Intent
- **New in 3.0?** New design
- **Evidence:**
  - **Two cases, told apart by sentinels.** A read record is either "bytes at an offset" or "a record that keeps its string". The case is told by `Offset == -1` and a null `Record`, stated only in comments (`Data/MultiLineRecordParser.cs:326-365`).
  - **Two different tests for the same thing.** `CompleteChunk` tests `read.Offset >= 0` (`:233`), while `ParsedRecord` tests `_chunk == null` (`:266`, `:270`, `:285`).
  - **Near-synonym names.** Three nested types are called `RecordStart`, `ReadRecord` and `ParsedRecord`.
  - **No summary.** The class has none, which is inherited from 2.x.
- **Impact:** The central 3.0 idea of the class, bytes or string and why, has to be reconstructed from three structs and two null tests.
- **Recommendation:** Name the case (a `KeepsString` property used by both checks), and add a class summary stating the bytes-or-string rule. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U1-11. A grown reopen rewrites the whole table

- **Priority:** Low
- **Kind:** CPU
- **New in 3.0?** New design (deliberate)
- **Evidence:**
  - **The whole file is written again.** A grown reopen hashes the old part, reparses from the last chunk, then rewrites the entire file (`IO/LineIndexFile.cs:205-206`, `IO/LineIndex.cs:290-300`).
  - **This is a recorded choice.** "The whole table is saved again, about 0.8 s for 10 million records. Appending to the `.idx` in place is left out" (`Doc/Plans/1023-ReopenWithoutParsing.md:176`).
  - **The measured cost.** On the 4 GB log, a reopen after 1% growth took 11.4 s, against 8.0 s for a plain reopen (this audit, [Memory-Main.md](../Measurements/Memory-Main.md)).
- **Impact:** For a user following a live log, each Reload costs time in proportion to the whole log, not to what was added.
- **Recommendation:** None before measuring how the extra time divides between rewriting and reparsing. If the rewrite dominates, skip the save when growth is small. **Complexity:** M.
- **Sceptic:** Not challenged (Low)

### U1-12. "Block", "section" and "source" each mean two things

- **Priority:** Low
- **Kind:** Intent
- **New in 3.0?** New design
- **Evidence:**
  - **Block:** a 64 KB checksum block of the log (`IO/SavedTableReader.cs:176-209`) and a 65,536-row block of the table (`Data/RecordTable.cs:154-155`).
  - **Section:** a part of the `.idx` (`IO/IndexSection.cs:11`) and a user-facing section of the log (`CoreEngineBuilder.cs:226`).
  - **Source:** a `Stream` and a `LogFileSource` in one class (`Data/SerializedRecordRepository.cs:19` against `:65`).
- **Impact:** The same word means different things in neighbouring files, so the reader has to check which meaning applies each time. A small cost, paid often.
- **Recommendation:** Use "checksum block" and "row block", "index section" and "log section", and keep "source" for `LogFileSource`. Do it when the files are next touched. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

## Minor

- **Concurrent saves.** `LineIndexFile.TrySave` uses one fixed temporary name and deletes it in its catch, so a second Weevil instance can delete the first's file (`IO/LineIndexFile.cs:154-172`). Harmless: that open saves no index.
- **Inconsistent sharing modes.** The `.idx` is opened with three different sharing modes (`LineIndexFile.cs:100`, `:121`, `:238`). The one without `Delete` can make another instance's `File.Move` fail for a moment.
- **Header read twice.** A reopen parses the `.idx` header and every checkpoint twice, in `Load` and again in `TryLoadTable` (`LineIndexFile.cs:46`, `:241-249`).
- **Misleading error.** `TryLoadTable` reports any `IOException` as "The saved record table could not be read", even when the log was the file that failed (`LineIndexFile.cs:251`, `:263-273`).
- **Two writes on a first open.** A first full open writes the `.idx` twice, once with the line index and once with the table (`LineIndexFile.cs:46-62`, `:186-207`; recorded in the #1023 plan).
- **No cancellation.** No loop in this unit accepts a `CancellationToken` (`LineIndex.cs:612-640`, `SavedTableReader.cs:179-209`). The GUI opens on a worker thread, so the window stays responsive, but an open can't be stopped. Inherited from 2.x, and more noticeable now that a first open of a 20 GB log is expected to take minutes.
- **Disk-mode buffer.** In disk mode, `LogLineReader`'s buffer still keeps every byte from the chunk's start, though it only needs the current line (`IO/LogLineReader.cs:252-270`). For one record over 1 GB, the doubling overflows; 2.x failed there too.
- **Behaviour depends on the stream's type.** Checksums, and so a saved table, exist only when the source is a `FileStream` (`LogLineReader.cs:79-98`, documented at `:51-53`).
- **Dead or one-shot code.** `SerializedRecordRepository.GetNext`, `GetAll` and `Get(int)` have no production caller (`Data/SerializedRecordRepository.cs:76-89`), and `Get` can be called once by remark only (`:8-11`).
- **Line-count overflow.** A log of more than 2,147,483,647 lines throws `OverflowException` out of `LineIndexFile.Load` (`IO/LineIndex.cs:134`).
- **Block arithmetic repeated.** The block-boundary arithmetic is re-derived in three places instead of being asked of `LogFileSource` (`SavedParse.cs:134-136`, `SavedTableReader.cs:193`, `GrownLogParser.cs:64-65`).
- **Doc drift in `RecordTable.md`.**
  - `Doc/Design/RecordTable.md:41` omits the end marker.
  - "A damaged table section costs only the table" holds for the section's bytes but not for its 20-byte header, which rejects the whole index (`LineIndex.cs:464-469`).
  - Its timing table names no machine or build (`:48-54`).
- **Doc drift in the instructions.** `.github/instructions/core.instructions.md:56` still sizes logs at "hundreds of thousands of records".
- **Doc drift in a test comment.** `Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/IO/LogFileSourceBuilderTests.cs:11-12` says the parser reads 64 KB pieces. `LogLineReader` reads into a buffer that grows past 16 MB.
- **Inherited names.** `MultilineRecordParser` lives in `MultiLineRecordParser.cs`, and its tests use pre-convention names (`Tst/BlueDotBrigade.Weevil.Core-UnitTests/Data/MultiLineRecordParserTest.cs`). Both inherited from 2.x.

## To Measure in Stage 2

- **The line-index pass alone.** The plan estimates about 6 s per 20 GB on NVMe (`Doc/Plans/988-IndexingService.md:107`) and records no measurement. Compare the "The line index is ready." duration with the whole first open, on the 1 GB and 4 GB logs.
- **First open including the `.idx` save, against one in the temporary folder.** The repo claims 9.2–9.5 s against 8.4 s for the 1 GB log (`Doc/Design/RecordTable.md:50-51`). Delete `<log>.idx` before each run.
- **Reopen in the other storage mode (U1-7).** Open in RAM, reopen on disk, and compare with a same-mode reopen.
- **Grown reopen (U1-11).** Split the extra 3.4 s between rewriting the table and reparsing the last chunk.
- **The last region by name against by start line (U1-8),** on the 10-million-record log.
- **A rejected reopen in RAM.** Change one byte near the end of the 4 GB log, then measure three passes against a first open, with the peak working set.
- **Cold cache.** Every recorded reopen figure reads the log from the page cache. Repeat one reopen after dropping the cache.
- **Sidecar save with many annotations.** Each annotated record costs a `LineIndex.GetByteOffset` with a file read (`CoreEngine.cs:453`, `SidecarLoader.cs:164-193`). Time a save with 1,000 and 100,000 pinned records.

## Open Questions

- **Is the `.idx` meant to be invisible to users?** Is there an issue for logs on network shares and USB drives, as the #1023 plan anticipated?
- **Are the sidecar's byte offsets still needed?** Nothing in 3.0 reads them back except a round trip (`SidecarLoader.cs:102`, `:281`). If they are written only so that 2.x can read a 3.0 sidecar, is that still wanted, given that a save reopens the log to compute them (`CoreEngine.cs:441-456`)?
- **Is a `.idx` that arrives with a log untrusted input?** It is identified by content, so it stays valid when copied with a log. Once the block checksums match, its rows are trusted, and XxHash3 is not a cryptographic hash. The MVID check limits this to the same build.
- **Is `IRecordRepository` still meant as a public abstraction?** Both production callers construct `SerializedRecordRepository` directly and downcast its result.
