# U2: Record Representation (#988)

Part of the [Weevil 3.0 audit](../Progress.md). This code holds a log's records: a record table of 21-byte rows in Core, the log's bytes in shared content chunks that live in RAM or are read back from the file, and read-only record lists that create each record when it is read.

> **Status:** Final. Reviewed on 2026-11-17 at `main` e3dee822, by one reviewer covering every lens.

## Summary

This is careful, well-explained code. Each type has one job, says why it exists and cites its issue. `Record` is now truly immutable (2.x's settable `Metadata` is gone), and the table, the chunks and the lists never change, so filter threads read them without a lock. `ContentChunk` keeps RAM and disk behind one type for the *text*: an oracle test and a feature outline show both modes give the same text in every encoding. The unit is about 2.5 times 2.x's code (about 2,500 lines against 1,000), and that buys the drop from 3.25 to 1.22 GB per GB of log, and disk mode. Core's own scans all use the readers that create no record, so the efficient path is not hard to follow.

**The weakness is the default path.** `IRecord.Content` looks like a field, but on disk each read opens the log, reads and checksums at least one 64 KB block, and can throw. The interface says none of this, and Copy, unchanged since 2.x, now reads the log once per selected record on the UI thread. Second, a record keeps its whole 16 MB chunk alive, which no design note says, and in RAM the graph window keeps an old log's content after another log opens. Third, the table's fast paths are chosen by testing a list's concrete type, and most unit tests run a different list from production. None of these is High.

## What Was Examined

- `Common/Data`: `IRecord.cs`, `Record.cs`, `RecordFacts.cs`, `RecordList.cs`, `ContentChunk.cs`, `ContentBuffer.cs`, `RecordCollectionExtensions.cs`; `Common/IO/LogFileSource.cs` (`Read`).
- `Core/Data`: `RecordTable.cs`, `TableRecordList.cs`, `PositionRecordList.cs`, `InMemoryRecordRepository.cs`. Their consumers: `SelectedRecordList`, `SelectedRecords`, `RecordRanges`, `AnalysisHelper`, `FilterManager` (results), `CoreEngine.Narrow`.
- Readers across layers: every `.Content` read and `new ContentBuffer` in `Src/`; the GUI's Copy (`ClipboardHelper`), rows (`RowViewModel`, converters) and graph (`FilterViewModel`, `DialogBoxService`); `Get-Records`.
- `TestTools/Data`: `R.cs`, `ColumnsOnlyRecordList.cs`.
- 2.x: `Record.cs`, `Metadata.cs`, `RecordCollectionExtensions.cs`, `InMemoryRecordRepository.cs`, `ClipboardHelper.cs`.
- Documents: `Doc/Design/RecordTable.md`, `DiskMode.md`; `Doc/Plans/988-IndexingService.md`, `991-MultipleViews.md`; `Doc/Research/Weevil3/RecordsOnDemand/`, `RawContent/`; the common, core and review instructions; `Doc/LessonsLearned/FluentAssertionsComparesRecordsByLineNumber.md`.
- Tests: `RecordTests`, `RecordListTests`, `ContentChunkTests`, `ContentBufferTests`, `RecordCollectionExtensionsTests`, `RecordTableTests`, `TableRecordListTests`, `InMemoryRecordRepositoryTests`, `OpeningOnDisk.feature`; the CI workflow and `Tst/Disk.runsettings`.
- SonarCloud's three leads for U2.

## Strengths

- **Immutability is stronger than in 2.x.**
  - 2.x's `Record` carried a `Metadata` with setters for the comment, pin, flag and elapsed time (`Releases/2.x`: `Common/Data/Metadata.cs:51-141`). 3.0's `Record` has only read-only fields (`Src/BlueDotBrigade.Weevil.Common/Data/Record.cs:26-31`).
  - `IRecord`'s two new facts say "Set by the parser; never changes" (`Common/Data/IRecord.cs:34-42`).
  - A chunk copies the bytes and the encoding it is given (`Common/Data/ContentChunk.cs:24-35`), and a test changes the source bytes afterwards to prove it (`Tst/BlueDotBrigade.Weevil.Common-UnitTests/Data/RecordTests.cs:81`).
  - A chunk on file cannot return other text: a changed block throws instead (`Common/IO/LogFileSource.cs:176-178`). The review rule on `IRecord` and `Record` holds.
- **Equality has one home.** `Record.Equals` compares line numbers, and its remarks say why (`Record.cs:159-179`). No production code compares records by reference, except `ContentBuffer`'s cache, where a miss only decodes again (`ContentBuffer.cs:49`). The FluentAssertions trap that follows got a lessons-learned note.
- **RAM vs disk behind one type, for the text.**
  - Only `ContentChunk` and `ContentBuffer` test `IsOnFile` (`ContentChunk.cs:68`, `:85`, `:105`; `ContentBuffer.cs:94`). No reader in Core checks the storage mode.
  - An oracle test reads a chunk on file and in memory over five encodings, mixed line breaks and invalid bytes (`Tst/BlueDotBrigade.Weevil.Common-UnitTests/Data/ContentChunkTests.cs:28-60`).
  - `OpeningOnDisk.feature:7-22` checks the same records and filter results in both modes over eight encodings and line breaks.
- **A compact table that explains itself.** Blocks of 65,536 rows, strings kept sparsely, and each block's first chunk, so finding a record's chunk is a short search (`Src/BlueDotBrigade.Weevil.Core/Data/RecordTable.cs:7-17`, `:225-255`, `:413-472`). A grown log's table shares the old table's full blocks (`:150-173`, `:487-520`): 5.0 instead of 5.5 GB at the peak (#1201, `Doc/Design/RecordTable.md:58`).
- **The design's challenge was acted on.** It asked for `IList<IRecord>` (for LINQ's fast paths), sparse strings, a first chunk per block and a defined result for a default array (`Doc/Research/Weevil3/RecordsOnDemand/README.md:210-221`, `:265-286`). All four are in the code (`RecordList.cs:17`, `:72-78`; `RecordTable.cs:401`, `:413-418`).
- **Reading efficiently is not hard.**
  - Every list offers `GetLineNumber`, `GetCreatedAt`, `GetFacts` and `ReadContent`, which create no record (`RecordList.cs:32-64`).
  - Every scan in Core and the CLI uses them: 17 places create a `ContentBuffer`, among them the filter, Find, export, Split and every timeline analyzer. `core.instructions.md` ("Performance") states the cost of creating a record.
  - `ColumnsOnlyRecordList` throws when a record is created, so "creates no record" is a testable specification (`Src/BlueDotBrigade.Weevil.TestTools/Data/ColumnsOnlyRecordList.cs:6-9`). 15 test files use it.
- **`ContentBuffer`'s disk read policy is written as three rules, each with a test** (`ContentBuffer.cs:12-17`; `ContentBufferTests.cs:65`, `:86`, `:111`).
- **Simple ownership and threading.** No type here takes a lock or needs `IDisposable`: nothing changes after it is built, and a chunk on file holds no handle (each read opens its own). `ContentBuffer` is one per thread, as its summary says (`ContentBuffer.cs:10`).
- **Room for #991.** The table is immutable, so views can share it. A Clear gives the view a table of its own that shares the old one's chunk objects (`RecordTable.cs:114-135`). In RAM, the garbage collector then frees a chunk only when no view's table refers to it, which is the rule #991's step 2 sets out to compute (`Doc/Plans/991-MultipleViews.md:39-40`).

## Findings

### U2-1. `IRecord.Content` reads the log in disk mode, and its contract does not say so

- **Priority:** Medium
- **Kind:** Intent
- **New in 3.0?** New design
- **Evidence:**
  - **Same text, different cost.** In RAM, `Record.Content` decodes the chunk's bytes into a new string (`Common/Data/Record.cs:86`, `Data/ContentChunk.cs:85-88`). On disk, every read rents a buffer and calls `LogFileSource.Read` (`ContentChunk.cs:103-122`). That opens the file, reads each 64 KB block holding the bytes, hashes each with XxHash3, and closes the file (`Common/IO/LogFileSource.cs:154-183`). It can throw `LogFileChangedException`, another `IOException` or `UnauthorizedAccessException`.
  - **The contract is silent.** `IRecord.Content` says only "Represents a single log file entry" (`Common/Data/IRecord.cs:7-10`). The warnings exist, but in six other places:
    - `Record.Content`'s remark, on cost only (`Record.cs:83-85`);
    - `IRecordAnalyzer.Analyze`, the clearest (`Common/Analysis/IRecordAnalyzer.cs:30-33`);
    - `IEngine` (`Common/IEngine.cs:8-12`), `IFilterBehaviors.Apply` (`Common/Filter/IFilterBehaviors.cs:18-21`) and `IPlugin.ShowDashboard` (`Common/IPlugin.cs:20-24`);
    - `Doc/Design/DiskMode.md:21`.
  - **Inherited code fell into it.** Copy (Ctrl+C) is unchanged from 2.x. It formats `Selected.Values` one record at a time through `record.Content` (`Gui/ClipboardHelper.cs:22-49`, `Common/RawRecordFormatter.cs:15`). On disk, that is one file open and at least 64 KB read and hashed per selected record, for up to 100,000 records (`Gui/Filter/FilterViewModel.cs:69`), in a command handler on the UI thread (`FilterViewModel.cs:1143-1161`, `Gui/Input/UiBoundCommand.cs:112-119`). The graph copies the same records in the background through one `ContentBuffer` (`FilterViewModel.cs:1436-1455`, `:1549-1556`); Copy does not. The review rules flag I/O on the UI thread (`.github/instructions/review.instructions.md`, "UI Responsiveness"). U6 should confirm.
  - **Other readers of the same kind.**
    - The grid reads each visible row's text twice, through two converters, each a separate read on disk (`Gui/Converters/ContentConverter.cs:34`, `ContentToTruncationIconConverter.cs:30`, `Gui/Filter/RowViewModel.cs:107-121`).
    - `Get-Records -Storage Disk` gives scripts records whose `Content` reads the log on every access (`PowerShell/GetRecordsCmdlet.cs:41-43`, `:77`).
    - `HasContent` decodes, and on disk reads, the whole text to answer yes or no, and `ToString` calls it (`Record.cs:88`, `:188`).
- **Impact:** The text is the same in both modes, but the cost of the obvious loop is not. On disk it opens the file once per record, where a `ContentBuffer` reads about once per 16 MB chunk. A maintainer keeping 2.x code, or a plugin or script author who reads only `IRecord`, cannot see that. Copy is a live instance; its cost is unmeasured (see Stage 2).
- **Recommendation:** Say it once, where every caller looks: on `IRecord.Content`, that on disk a read opens the log and can throw an `IOException`, and that many records are read through a `ContentBuffer` or `RecordList.ReadContent`. Let the other remarks point there. Move `CopyWithText` from the GUI into Core, beside `RecordList`, since the CLI and PowerShell need it too (the layering rule in `.github/copilot-instructions.md`). Then use it for Copy, in the background, as the graph does. **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U2-2. A record keeps its whole content chunk alive, so in RAM the graph keeps an old log's content

- **Priority:** Medium
- **Kind:** RAM
- **New in 3.0?** New design; for the graph, a likely regression against 2.x (not measured)
- **Evidence:**
  - **What a record holds.** A record that holds bytes keeps a reference to its chunk (`Common/Data/Record.cs:28-29`, `:67`). In RAM a chunk is one array of about 16 MB, shared by all its records (`ContentChunk.cs:19`, `:32`; `Doc/Plans/988-IndexingService.md:41`). So one record kept anywhere keeps 16 MB alive, and N records spread over a log keep up to N chunks.
  - **Known to the design, not written down.** The design says "Retained records still pin their chunks" (`Doc/Research/Weevil3/RecordsOnDemand/README.md:160`). `Doc/Design/RecordTable.md:25` says only that a record "is not kept". `IEngine` says a kept record "still reads its own log", without the memory (`Common/IEngine.cs:10`).
  - **The graph in RAM.** The graph keeps `GetSelected().ToImmutableArray()`, up to 100,000 records, on the reasoning "In RAM a record keeps its text, so only a log on disk needs the copy" (`Gui/Filter/FilterViewModel.cs:1539-1547`). The window is modeless (`Gui/IO/DialogBoxService.cs:125-131`), and nothing closes it on Open, Reload or Clear. The design decided the graph is "kept as its own copy" (`RecordsOnDemand/README.md:9`, problem 3 at `:233-236`); that copy is made only on disk.
  - **Scale.** Filter results selected across the 4 GB harness log, about 250 chunks, can reach every chunk. Content is most of the 4.7 GB working set measured after opening that log in RAM ([Memory-Main-Run2.md](../Measurements/Memory-Main-Run2.md)). 2.x's graph kept only the selected records' own strings.
- **Impact:** In RAM, a user who graphs filter results and then opens another log, reloads or clears may keep most of the old log's content in memory until the graph closes. While a graph is open, it also undoes Clear's freeing of chunks (#1022). A maintainer cannot learn the rule from the types.
- **Recommendation:** Measure first (Stage 2). Then make the graph copy its records' text in both modes: `CopyWithText` exists, and U2-1 moves it to Core. Add one sentence to `RecordList` and to `RecordTable.md`: a record keeps its chunk (about 16 MB in RAM) alive, so to keep records past their log, keep line numbers or copy the text. **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U2-3. Record lists choose their fast path by concrete type, and unit tests mostly run a different list

- **Priority:** Medium
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **Five lists.** `ArrayRecordList` (`Common/Data/RecordList.cs:215-268`), `TableRecordList`, `PositionRecordList` (`Core/Data/`), `SelectedRecordList` (`Core/Collections/`) and TestTools's `ColumnsOnlyRecordList`.
  - **Type tests pick the path.** `TableRecordList.Select` and `Keep` use the table only when the list `is TableRecordList`. Any other list falls back to `CopyRecords`, which creates and holds every record, and so every chunk (`Core/Data/TableRecordList.cs:71-100`, `:118-131`, "as in tests"). `RecordRanges` aligns a filter's work to chunks only for a `TableRecordList { ListsWholeTable: true }` (`Core/Data/RecordRanges.cs:32`). See also the downcasts in U1-2.
  - **Safe today, by circumstance.** Production passes only a table's list (`Core/Filter/FilterManager.cs:240`, `Core/Data/InMemoryRecordRepository.cs:397-399`). A list over a list, which `PositionRecordList` and `SelectedRecordList` already are, would take the slow path without a sign. #991's views and partial opens are where such lists would appear next.
  - **Two types for one job.** `PositionRecordList` (#1137) and `TableRecordList.Select` (#1140) both mean "these positions of that list". The first has one production user, which sorts timeline positions (`Core/Analysis/Timeline/AnalysisHelper.cs:70-81`).
  - **Tests run the array list.** `R` builds only `ArrayRecordList`s of records that keep strings (`TestTools/Data/R.cs:29-31`, `:88-97`), and about 43 test files use it. There, a position read twice returns the same object, `ToImmutableArray` copies nothing, and `Select` and `Keep` copy records.
  - **The switch that would close the gap is never run.** `WEEVIL_TEST_RECORD_COPIES` makes the array list return a new record on every read (`RecordList.cs:217-222`). No CI job or runsettings sets it (`.github/workflows/dotnet.yml:55`, `:65`; `Tst/Disk.runsettings`), although the design asked for the whole suite to run that way (`RecordsOnDemand/README.md:245-248`).
  - **A table's no-record reader has no direct test.** `RecordTable.ReadContent` serves Find, the analyzers, export and Split. No test checks it against `CreateRecord(n).Content` for string rows, multi-line rows or chunk edges; `RecordTableTests` checks only created records (`Tst/BlueDotBrigade.Weevil.Core-UnitTests/Data/RecordTableTests.cs:73-124`).
- **Impact:** Code that passes a unit test can create a record per row in production, or depend on reading the same object twice. A new kind of list loses the table's paths without warning. The cost is small today, and grows with #991.
- **Recommendation:** Give Core one internal way to map any of its lists to a table and record numbers, for example a small internal interface implemented by the three Core lists. `Select`, `Keep` and `RecordRanges` then use it instead of type tests. `Select`'s fallback for any other list becomes a `PositionRecordList`, which creates nothing, instead of a copy of every record. Add one table-driven test that every no-record reader (`GetLineNumber`, `GetCreatedAt`, `GetFacts`, `ReadContent`) matches the created record, over the mixed table `RecordTableTests` already builds. Either run the Core unit tests once in CI with `WEEVIL_TEST_RECORD_COPIES=1`, or delete the switch. **Complexity:** S–M.
- **Sceptic:** Not challenged (Medium)

### U2-4. `RecordFacts` exposes the table's storage bit in a public type

- **Priority:** Low
- **Kind:** Intent
- **New in 3.0?** New design
- **Evidence:**
  - **A storage detail among the facts.** `RecordFacts` is "What a parser found about a record" (`Common/Data/RecordFacts.cs:5-11`), yet it carries `KeepsString`: how the table stores the content (`:16`, `:42`).
  - **Always false outside the table.** `Record` never sets it, and `RecordList.GetFacts` says it is always false (`Common/Data/RecordList.cs:61-63`). The table strips it on every read (`Core/Data/RecordTable.cs:56-63`) and overwrites whatever a caller passes (`:535-536`, `:548`).
  - **Against the design's advice.** The challenge said "Keep the keeps-string bit in the table only, never in `Record`'s facts" (`RecordsOnDemand/README.md:288-290`). The bit stayed out of `Record`, but not out of the type.
  - **The `.idx` byte became public API.** `Value` and `TryFromValue` are public, "as the saved index keeps them" (`RecordFacts.cs:44-65`), because Common's `InternalsVisibleTo` is Debug-only.
- **Impact:** A plugin author sees a fact that is never true for them. A maintainer must remember the three places that strip or set it. Changing the layout of the saved byte is a public API change.
- **Recommendation:** Keep `RecordFacts` to the three parser facts, and keep the storage bit and the saved byte layout in Core (`RecordTable` and `SavedTableReader`). **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U2-5. Clear's rules live in a class named and shaped as a repository

- **Priority:** Low
- **Kind:** Intent
- **New in 3.0?** Existing weakness, kept by 3.0
- **Evidence:**
  - **Not a repository.** `InMemoryRecordRepository` decides which records a Clear keeps; its summary says so (`Core/Data/InMemoryRecordRepository.cs:26-34`). It implements `IRecordRepository`, an interface for reading, but `GetNext` and `Get(Range, int)` throw `NotImplementedException` (`:295-298`, `:402-405`), and `_visibleRecords` is stored and never read (`:16`, `:43`).
  - **Single use, guarded anyway.** It is built and used once per Clear (`Core/CoreEngine.cs:378-384`), yet guards against a second concurrent call with `Interlocked` (`:305-308`, `:390-393`).
  - **Inherited shell, new contents.** The shell is as in 2.x (`Releases/2.x`, same file, lines 9-50 and 229-327). #1140 rewrote every rule to read line numbers by position without creating records (`:57-289`), which is good work, but kept the shell.
  - **SonarCloud.** The complexity lead (16 at `:303`) is this inherited switch, not new logic.
- **Impact:** Someone looking for Clear's rules does not look for a repository, and the interface promises reads the class does not offer. A small cost, paid by everyone who touches Clear, as #991 will: each view clears on its own.
- **Recommendation:** Name it for what it does, make it a static method that returns the kept positions, and drop the interface, the unused field and the guard. Behaviour does not change. See U1's open question on `IRecordRepository`. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U2-6. `GetSectionRecords` throws for the last section (SonarCloud S2259), and nothing calls it

- **Priority:** Low
- **Kind:** Correctness
- **New in 3.0?** Existing weakness
- **Evidence:**
  - **The defect.** For the last section, `nextSection` is null (`Common/Data/RecordCollectionExtensions.cs:423-431`), and the loop then reads `nextSection.LineNumber` (`:440`): a `NullReferenceException` on the first record. The SonarCloud lead is real.
  - **Inherited.** The same code is in 2.x (`Releases/2.x`, same file, lines 357-391). #1136 only retyped it.
  - **Unused, and duplicated correctly elsewhere.** No production code calls it. 3.0 added its own section range for `UsingSection`, which handles the last section (`Core/CoreEngineBuilder.cs:523-537`). The two tests both ask for the middle section (`Tst/BlueDotBrigade.Weevil.Common-UnitTests/Data/RecordCollectionExtensionsTests.cs:179-230`).
  - **`Compact<T>` is in the same state.** The design said it would be deleted (`RecordsOnDemand/README.md:105`), and only its tests call it (`RecordCollectionExtensions.cs:65-86`).
- **Impact:** Public API in Common that fails on an obvious input, kept alive only by tests, beside a second, correct rule for the same thing.
- **Recommendation:** Delete both methods and their tests. If `GetSectionRecords` is wanted, build it on `GetSectionRange`'s rule and test the last section. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

## Minor

- **`IndexOf` and `Contains` are linear.** No list overrides them (`Common/Data/RecordList.cs:83-104`), while `IndexOfLineNumber` binary-searches (`RecordCollectionExtensions.cs:111-114`). `RecordList.cs:15` and `common.instructions.md` say both "find a record by its line number", without the cost. No production caller was found.
- **Comments still in the future tense** since #1140 shipped: `RecordList.cs:12` ("Once records are created when they are read"), `:218`, `Record.cs:92`, `:163`, `RecordFacts.cs:9` ("and later the saved index").
- **The table is immutable by convention.** A block's columns are public, settable array fields (`Core/Data/RecordTable.cs:403-411`), and `Share` puts the same blocks in two tables (`:496-520`). Safe while `Block` stays private and the builder writes only its current block.
- **Chunks are identified by reference.** `Builder.Add` (`RecordTable.cs:529`), `ReplaceChunks` (`:176-182`) and `ContentBuffer` (`ContentBuffer.cs:99-104`) rely on one object per chunk. A producer that made a new object per record would add a directory entry per record and defeat the buffer. Stated only in comments.
- **`Select` and `Keep` overwrite the caller's array** with record numbers (`Core/Data/TableRecordList.cs:67`, `:86`, `:105-116`). Documented only in a parameter remark.
- **`ContentBuffer` is also the disk read policy.** Its summary says it "Holds the content of one record at a time" (`ContentBuffer.cs:7`), but most of its state is a 16 MB chunk cache and the set of chunks read whole (`:31-40`), tuned by two fixes in one day (#1027: 865dd3f8, 1bf89fbe). Well documented and tested; the name undersells it.
- **A trap overload.** `ActiveRecord(IList<IRecord>)` creates every record through `ToImmutableArray` (`Core/Navigation/ActiveRecord.cs:17`). Production uses only the `RecordList` overload (`Core/Navigation/NavigationManager.cs:41`). For U5.
- **Doc drift in `RecordTable.md`.** It names one list over a table (`Doc/Design/RecordTable.md:24`); Core has three (`TableRecordList`, `PositionRecordList`, `SelectedRecordList`). It says nothing about what a kept record keeps alive (U2-2).
- **SonarCloud leads.**
  - `RecordTable.cs:333` (cognitive complexity 19) is `ReadRows`: checks of untrusted bytes, so the complexity is inherent. Its writer sits in the same class, while the chunk and string readers sit in `SavedTableReader` (see U1-3).
  - `InMemoryRecordRepository.cs:303` (16) is inherited (U2-5).
  - `RecordCollectionExtensions.cs:440` (S2259) is real and inherited (U2-6).

## To Measure in Stage 2

- **Copy on disk (U2-1).** Open the 1 GB log on disk, select about 100,000 records spread over it (every 75th), and time the text Copy builds: `Selector.Selected.Values` through `RawRecordFormatter`. Compare with the same records read through one `ContentBuffer`. The owner should repeat it on a network share on Windows, where Copy runs on the UI thread.
- **A scan through `Content` against `ReadContent` (U2-1).** On one thread, read every record of the 1 GB log both ways, on disk and in RAM. This sizes the gap the guidance warns about.
- **Graph retention in RAM (U2-2).** Open the 4 GB log in RAM, filter on a term that matches about 50,000 records spread over it, and hold `GetSelected().ToImmutableArray()` as the graph does. Then open the 1 GB log, collect, and read the working set. Compare with the same records copied as text.
- **Clear keeping most records.** In RAM, on the 4 GB log, Clear Before Selected with the selection near the top, so 99% is kept: peak working set and time, against 2.12.1. While it runs, the old table, the kept positions (a `List<int>` and its array copy) and the new table are all alive (`InMemoryRecordRepository.cs:85-103`, `:399`; `RecordTable.cs:114-135`). The #1022 figures cover only keeping 1% (`Doc/Plans/988-IndexingService.md:83`).
- **The grid on disk.** Each visible row's text is read twice per render, and each read opens the log (U2-1). The owner should time a page of rows on a network share on Windows (with U6).

## Open Questions

- **Should a window that outlives its log keep text, not records, in RAM too?** The design's decision 2 says the graph keeps "its own copy"; the code copies only on disk.
- **Are `RecordFacts.KeepsString`, `Value` and `TryFromValue` meant for plugins,** or public only because Common's `InternalsVisibleTo` is Debug-only?
- **Is `WEEVIL_TEST_RECORD_COPIES` still wanted?** If so, which CI job should run it?
- **Should Copy on disk work like the graph** (in the background, where Escape stops it), since both read up to 100,000 records?
