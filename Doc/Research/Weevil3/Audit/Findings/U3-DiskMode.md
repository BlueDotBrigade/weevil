# U3: Disk Mode and the Log File Source (#986)

Part of the [Weevil 3.0 audit](../Progress.md). This code lets a log be opened in RAM or on disk: on disk, only the record table stays in memory, and each record's text is read back from the log and checked against the bytes that were parsed.

> **Status:** Final. Reviewed on 2026-11-17 at `main` e3dee822, by one reviewer covering every lens.

## Summary

Disk mode is well shaped. One small class, `LogFileSource`, owns reading back from the log. It is thread-safe once complete, holds no handle between reads, and checks every 64 KB block it reads, so a changed log is detected rather than misread. Reading sits behind one switch (`ContentChunk.IsOnFile`), so filters, analyzers, export, Find, the CLI and PowerShell never ask the mode. Measured on Linux, a log on disk keeps 22 bytes per record once open, and the 4 GB log filters from the page cache in 1.1 to 1.4 times RAM's time. 2.x had nothing comparable, and the roughly 1,200 new lines buy a log that no longer has to fit in memory.

**The main cost is that a read of a record's text can now fail, but only on disk** (U3-1). Each GUI reader catches and routes the failure by hand. The public `IRecord.Content` doesn't say it can throw, and the rule lives in one paragraph of `DiskMode.md`. Second, **"only the record table stays in memory" holds at rest, not while filtering** (U3-2). Each filter on disk allocated 0.3 to 3.2 GB more than the same filter in RAM, and the working set grew to about the size of the log. There are two smaller problems where two pieces of work meet. The CLI's and PowerShell's retry after a change reloads the whole log, not the region or section that was asked for (U3-3). And the opens that parse the log still don't let another application delete or rename it: the #1025 plan decided to fix this, then left it out with no issue (U3-4). The unit has no High finding.

## What Was Examined

- Common: `IO/LogFileSource.cs`, `LogFileSourceBuilder.cs`, `LogFileChangedException.cs`, `StorageMode.cs`, `OpenAsResult.cs`, `FileHelper.cs`; `Data/ContentChunk.cs` and `ContentBuffer.cs` (the mode switch and the disk buffer); `IEngine.cs`, `IEngineBuilder.cs`, `Data/IRecord.cs`, `Data/Record.cs`.
- Core: `IO/MemoryEstimator.cs`, `IO/DiskWriter.cs`, `Data/RecordRanges.cs`, `Navigation/ContentNavigator.cs`, `EngineExtensions.cs`; how the mode flows through `CoreEngineBuilder.cs`, `CoreEngine.cs`, `Engine.cs` (Reload), `Filter/FilterManager.cs` and `FilterMapReduceState.cs`.
- GUI: `IO/ExtractedLog.cs`, `OpenAsAdvice.cs`, `OpenAsDialog.xaml(.cs)`, `DialogBoxService.cs`, `SourceFileOpenedBulletin.cs`, `Diagnostics/ComputerSnapshot.cs`; in `Filter/FilterViewModel.cs`, the opening, zip, Open As, failed-read and graph code; `Filter/RowViewModel.cs`.
- CLI and PowerShell: `Filter/FilterCommands.cs`, `Analysis/InsightCommands.cs`, `GetRecordsCmdlet.cs`, `GetInsightCmdlet.cs`, the PowerShell `ReadMe.md`.
- The second dialog: `Windows/Dialogs/OpenAsDialog.xaml.cs`, and `Storage/ContainerReader.Extract`.
- 2.x: `FilterViewModel.OpenCompressedAsync`, `FileHelper.cs`, `DiskWriter.cs`, `ContentNavigator.cs`, the CLI's `FilterCommands.cs`.
- Documents: `Doc/Design/DiskMode.md`; `Doc/Plans/986-DiskMode.md`, `1025-ContentFromFile.md`, `1028-OpenAs.md`, `1157-OpenAsDialog.md`, `991-MultipleViews.md`; `Doc/Notes/Release/Help.md`; `.github/instructions/*.md`; `.github/workflows/dotnet.yml`.
- Tests: `LogFileSourceTests`, `EngineExtensionsTests`, `MemoryEstimatorTests`, `RecordRangesTests`, the disk tests in Core `EngineTests`, `OpeningOnDisk.feature`, `OpenAsAdviceTests`, the disk and zip tests in `FilterViewModelTests`, the CLI and PowerShell disk tests, the disk budgets in `Core-PerformanceTests`, `Tst/Disk.runsettings`.
- Measurements: [Memory-Main.md](../Measurements/Memory-Main.md), [Memory-Main-Run2.md](../Measurements/Memory-Main-Run2.md), and the OpsHarness runs of 2026-11-17 on the 1 GB and 4 GB logs in both modes (quoted in U3-2; the files are not yet in `Measurements/`).
- SonarCloud issues on every file in the unit.

## Strengths

- **One class reads the log back, and says how.** `LogFileSource`'s remarks state its contract: thread-safe once complete; one handle per read, shared for reading, writing and deletion; no cache, no retry, no memory map (`Src/BlueDotBrigade.Weevil.Common/IO/LogFileSource.cs:11-25`). The code matches it (`:135-211`), and so do decisions 1 and 4 of the #1025 plan (`Doc/Plans/1025-ContentFromFile.md:48`, `:51`).
- **Rules are enforced in code, not just written down.** A source can't be read before it is complete (`LogFileSource.cs:137`), and its checksums can't be read early either (`:122`). Saved checksums must cover the range exactly (`:101-106`). A log that grew is only counted as grown after the blocks it read pass their checks, so a longer log that replaced the original is not mistaken for growth (`:204-210`). The change warning is written once, however many workers find it (`:189-195`).
- **The mode is hidden from code that reads.** `ContentChunk.IsOnFile` is the only switch on the read path (`Common/Data/ContentChunk.cs:68`, `:85`, `:105`; `ContentBuffer.cs:94`). Filters, analyzers, export, Find, the CLI and PowerShell read through `ContentBuffer` and never test the mode.
- **Measured, it does what it set out to do** (Linux, 4 vCPUs, page cache warm; OpsHarness, median of 5):
  - **Memory once open:** after opening, the managed heap is 0.17 GB for 7.5 million records and 0.67 GB for 30 million: 22 bytes per record, as `DiskMode.md:10` says. RAM uses 1.17 and 4.67 GB.
  - **Filter speed against RAM:** filters that read text take 1.1 to 1.4 times as long as in RAM on the 4 GB log, and Find 1.4 times as long.
  - **Filter speed against 2.12.1:** on the 1 GB log, the same filters run 3.9 to 11 times faster than 2.12.1 did in RAM.
  - **Each chunk is read once:** the performance test pins that a filter reads each chunk once (`Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/Filter/FilterManagerTests.cs:43-71`).
- **No handle outlives a read,** so `IEngine` stays non-disposable (#1025 decision 1), and a writer is never blocked by a read (`Tst/BlueDotBrigade.Weevil.Common-UnitTests/IO/LogFileSourceTests.cs:205`).
- **Core never reloads by itself.** Callers decide what to do (`Doc/Design/DiskMode.md:42`). The CLI and PowerShell share one helper, written and tested once (`Src/BlueDotBrigade.Weevil.Core/EngineExtensions.cs:21-40`). The GUI has one failed-read handler. It works per generation of the log, always defers, and states which thread owns what (`Src/BlueDotBrigade.Weevil.Gui/Filter/FilterViewModel.cs:128-134`, `:1460-1518`).
- **A zipped log's extracted copy has one clear owner.** `ExtractedLog` deletes only what Weevil extracted, and keeps a file written since then (`Gui/IO/ExtractedLog.cs:9-14`, `:47-87`, `:100-103`). Eleven GUI tests pin the rules (`Tst/BlueDotBrigade.Weevil.Gui-FeatureTests/Filter/FilterViewModelTests.cs:593-795`). 2.x deleted only the chosen file and left the rest of the zip in the temporary folder on every open.
- **The advice is sound and easy to test.** `OpenAsAdvice` is a plain class, tested without a window. Its estimate matches the measured peak while opening, within 10% on both logs: 1.25 GB against 1.41 GB for the 1 GB log, 5.01 GB against 4.91 GB for the 4 GB log ([Memory-Main.md](../Measurements/Memory-Main.md)). The 1 GB reserve covers the difference.
- **Tests read as specifications.** `LogFileSourceTests` covers each case in a Given/When/Then name: truncation, replacement, one byte changed in the middle, growth, a log opened only in part, a writer, a log that is gone. A test fails if the disk run silently opens in RAM (`Tst/BlueDotBrigade.Weevil.Core-UnitTests/EngineTests.cs:389-396`). CI runs the Core and CLI tests a second time on disk (`.github/workflows/dotnet.yml:57-66`).
- **Little for SonarCloud to report.** Its own rules raise nothing on `LogFileSource`, `LogFileSourceBuilder`, `LogFileChangedException`, `MemoryEstimator`, `ExtractedLog`, `OpenAsAdvice` or `EngineExtensions`. Three of its five issues in the unit were inherited from 2.x (see Minor).
- **Room for #991.** A complete `LogFileSource` never changes, and many threads can read through it, so several views of one log can share it. `HasChanged` would then tell every view at once.

## Findings

### U3-1. A read of a record's text can fail only on disk, and nothing but one paragraph of the design note says so

- **Priority:** Medium
- **Kind:** Architecture
- **New in 3.0?** New design
- **Evidence:**
  - **The public contract is silent.** `IRecord.Content` and `Record.Content` list no exception (`Src/BlueDotBrigade.Weevil.Common/Data/IRecord.cs:7-10`, `Data/Record.cs:83-86`). Only `ContentBuffer` and `LogFileSource.Read` mention them (`Data/ContentBuffer.cs:19-20`, `IO/LogFileSource.cs:131-134`).
  - **Three exceptions, two kinds.** A changed log throws `LogFileChangedException`, which is an `IOException`. A log that can't be opened throws whatever `File.OpenHandle` throws: an `IOException` or an `UnauthorizedAccessException` (`LogFileSource.cs:133-134`, `:158`). From the type alone, a caller can't tell a failed read of the log from any other I/O error.
  - **Each GUI reader handles it by hand, and differently.** The cases:
    - rows: `Gui/Filter/RowViewModel.cs:113-118`;
    - the insights after an open: `FilterViewModel.cs:975-983`;
    - copy: `:1155-1158`;
    - F5: `:1717-1720`;
    - background work, which reloads only for a changed log: `:2338-2342`;
    - a filter: `:2575-2579`;
    - the dashboard: `Analysis/DashboardDialog.xaml.cs:112`.

    `DiskMode.md:46` explains why they differ: export, split, search and analysis show their own error, "because their other I/O errors, such as a save to a locked file, are not the log's".
  - **A forgotten handler fails badly, in one of two ways.** In a binding, it ends Weevil: "WPF does not catch a converter's exception, so Weevil would end" (`RowViewModel.cs:103`). Elsewhere it fails quietly. `NavigateToSourceCode` reads `selectedRecord.Content` (`FilterViewModel.cs:2016`) inside a `catch (Exception)` that logs "Unexpected error" and shows nothing (`:2032-2035`). On a changed log, the user gets no message and no reload. That command came from #800, before 3.0, and was not adapted.
  - **RAM-mode tests can't catch a forgotten handler,** because in RAM no read fails. The GUI tests are left out of CI's disk run (`.github/workflows/dotnet.yml:58`, `:64`). Five GUI tests open a log on disk on purpose (`FilterViewModelTests.cs:195-294`).
  - **The rule is written in one place.** It is one ten-sentence paragraph (`Doc/Design/DiskMode.md:46`). `gui.instructions.md`, `core.instructions.md` and `common.instructions.md` say nothing about disk mode or about reads that fail. Plugins read `record.Content` too. #1028 accepted that a plugin which swallows the exception loses only its own read (`Doc/Plans/1028-OpenAs.md:213-216`).
- **Impact:** Anyone who adds a GUI command or a plugin that reads text must already know this rule, and no compiler warning or RAM-mode test will remind them. Forgetting it ends Weevil from a binding, or does nothing visible from a command, and only on a user's log on disk. In 2.x a read could not fail.
- **Recommendation:**
  - **Now (S).** Document the exceptions on `IRecord.Content`. Add the rule to `gui.instructions.md` and `core.instructions.md`: Core lets a read failure propagate, and the GUI sends it to `ReportFailedRead` or a reporter from `CreateFailedReadReporter`. Turn `DiskMode.md:46` into a table of readers, saying what each does with a changed log and with a log it can't read.
  - **Consider (S/M).** Have `LogFileSource.Read` wrap a failure to open the log in one exception type, for example `LogFileUnreadableException : IOException`, beside `LogFileChangedException`, or under a shared base. Every reader could then catch "the log failed" without mistaking a locked export target for it, and the background-work wrapper could route both kinds the same way.
- **Sceptic:** Not challenged (Medium)

### U3-2. On disk, "only the record table stays in memory" holds at rest, but a filter allocates far more than in RAM

- **Priority:** Medium
- **Kind:** RAM
- **New in 3.0?** New design
- **Evidence:**
  - **Measured.** Linux, 4 vCPUs, the page cache warm. OpsHarness ran an open, then nine operations in a row (eight filters and a Find), with `DOTNET_GCHeapHardLimitPercent=0x63`. Each figure is the median of 5:

    | Log | Mode | After open: working set / heap | Allocated per filter that reads text | At the end: working set / heap after a full GC | Highest peak in one operation |
    |---|---|---|---|---|---|
    | 1 GB | RAM | 1.22 / 1.17 GB | 370–382 MB | 1.38 / 1.17 GB | 1.79 GB |
    | 1 GB | Disk | 0.22 / 0.17 GB | 655–1,078 MB | 1.21 / 0.17 GB | 1.75 GB |
    | 4 GB | RAM | 4.69 / 4.67 GB | 1,471–1,517 MB | 6.34 / 4.67 GB | 6.50 GB |
    | 4 GB | Disk | 0.69 / 0.67 GB | 3,011–4,718 MB | 4.79 / 0.67 GB | 5.10 GB |

  - **The extra comes from reading text.** A filter on a moniker reads no text, and it allocates the same in both modes: 372 MB on the 1 GB log, 1,480 MB on the 4 GB log.
  - **Suspected cause, not verified.** `Parallel.ForEach` creates a new `FilterMapReduceState` for each local state (`Src/BlueDotBrigade.Weevil.Core/Filter/FilterManager.cs:163`). Each holds a new `ContentBuffer` (`Filter/FilterMapReduceState.cs:19`), whose first whole-chunk read allocates a new 16 MB array (`Common/Data/ContentBuffer.cs:120-123`; `Core/IO/LogLineReader.cs:28`).
    - The extra allocation equals about 18 to 200 such arrays per filter: for example, +3,229 MB is about 200 arrays on the 4 GB log's roughly 250 chunks. Four workers would need four.
    - .NET's parallel loops yield every 100 ms or so and start again with a new local state. They yield between ranges, and each range is one chunk, which would explain why the bytes read stay at once per chunk while the allocations grow.
  - **The claims cover only the memory kept after opening.**
    - **Documents:** `Doc/Design/DiskMode.md:10` says "only the record table stays in memory". `Doc/Notes/Release/Help.md:79` and the Open As window (`Gui/IO/OpenAsAdvice.cs:63`) say "Keeps about 22 bytes per record in memory".
    - **Tests:** the budget tests measure memory kept after opening (`Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/EngineTests.cs:55-63`, `:78-90`). The disk filter test pins the bytes read, not the bytes allocated (`Filter/FilterManagerTests.cs:43-71`).
- **Impact:** Users choose Disk because memory is short. Most of the extra memory is garbage: after a full collection, the live heap is back to 22 bytes per record. So the working set measured here is mostly freed memory that a collector under pressure might return. Two costs remain:
  - **More full collections.** Arrays of 16 MB go to the large object heap, which only a full (gen 2) collection frees, so each filter on disk probably triggers extra full collections.
  - **An unknown footprint where it matters.** Nothing measures the footprint while filtering when memory is short, which is the situation disk mode exists for: a 64 GB log on a 32 GB machine (`Doc/Plans/986-DiskMode.md:12`).
- **Recommendation:**
  - **Measure first** (see To Measure).
  - **If confirmed,** keep one buffer per worker thread for the whole filter instead of one per local state, or rent `_fileBytes` from `ArrayPool`.
  - **Pin it** with an allocation budget in the disk filter test.
  - **Document it.** State in `DiskMode.md`, and in Help, that a filter on disk briefly needs memory for the chunks being read, beyond the table.

  **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U3-3. After a change, the CLI's and PowerShell's retry reloads the whole log, not the region or section asked for

- **Priority:** Medium
- **Kind:** Correctness
- **New in 3.0?** New design
- **Evidence:**
  - **What runs.** `weevil filter --region` and `--section` open only that part of the log (`Src/BlueDotBrigade.Weevil.Cli/Filter/FilterCommands.cs:47-50`). They then filter and save through `RunReloadingOnChange` (`:68-73`). `Get-Records` does the same (`Src/BlueDotBrigade.Weevil.PowerShell/GetRecordsCmdlet.cs:67-74`).
  - **What Reload rebuilds.** On a change, the helper calls `engine.Reload()` (`Src/BlueDotBrigade.Weevil.Core/EngineExtensions.cs:36`). Reload rebuilds from the path, the session state and the mode (`Engine.cs:254-257`). `UsingStateOf` copies the session, the record state and telemetry (`CoreEngineBuilder.cs:167-179`). It does not copy the range, region, section or start line. `IEngine.Reload`'s remarks list what stays, and the range is not on that list (`Src/BlueDotBrigade.Weevil.Common/IEngine.cs:63-71`).
  - **The order of the work.** #1028 added the retry on 2026-11-11 (6165ddc9). Regions and sections came on 2026-11-15, #1213 (f6d07679) and #1212 (fa0dfe28).
  - **No test covers it.** `EngineExtensionsTests` opens whole logs only (`Tst/BlueDotBrigade.Weevil.Core-UnitTests/EngineExtensionsTests.cs:28-43`). `OpeningRegion.feature` and `OpeningSection.feature` have no Reload scenario.
  - **The GUI's version is tracked; the CLI's is not.** Step 5 of #1157 covers "what the status bar and Reload show after a partial open" (`Doc/Plans/1157-OpenAsDialog.md:63`). Nothing covers the CLI and PowerShell.
- **Impact:** Suppose a log on disk is rotated or replaced while `weevil filter <log> --region Startup --storage Disk` runs. The command then writes the results of the whole new log to `.Results`, as if they were the region's, and reports no error. `Get-Records -Region` does the same. It is rare, since it needs disk mode and a change during the command, but the wrong result is silent.
- **Recommendation:** Pick one, together with the GUI's question in #1157 step 5:
  - keep the part that was opened across Reload: store the range, region or section on `CoreEngine`, and carry it in `Release`;
  - or have `RunReloadingOnChange` throw when the engine was opened in part.

  Either way, add a scenario. **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U3-4. The opens that parse the log still don't share deletion, a fix the #1025 plan decided and then left untracked

- **Priority:** Medium
- **Kind:** Correctness
- **New in 3.0?** Existing weakness, which logs of many gigabytes make more noticeable
- **Evidence:**
  - **The helper.** `FileHelper.Open` shares reading and writing only (`Src/BlueDotBrigade.Weevil.Common/IO/FileHelper.cs:38`), as in 2.x.
  - **Where it is used.** It holds the log for the whole line-index scan and parse (`Src/BlueDotBrigade.Weevil.Core/CoreEngineBuilder.cs:336`, until `:433`). It is also used for encoding detection (`:334`), the sidecar save's byte offsets (`CoreEngine.cs:466`), rebuilding the table of contents (`Navigation/NavigationManager.cs:100`), the TSV plugin (`TsvPlugin.cs:23`) and the CLI's encoding read (`Src/BlueDotBrigade.Weevil.Cli/Filter/FilterCommands.cs:62`).
  - **New 3.0 code does share deletion:** `LogFileSource.cs:158` and `IO/MemoryEstimator.cs:33`.
  - **The plans.** The goal is that "Weevil never blocks the application that writes the log" (`Doc/Plans/986-DiskMode.md:16`), and the #986 plan says to add deletion (`:46`). The #1025 plan decided "**Sharing deletion everywhere:** `FileHelper.Open` shares deletion for every open of the log, in its own pull request" (`Doc/Plans/1025-ContentFromFile.md:55`). It then listed this as "Outside this plan" with no issue number (`:71`), and is marked Done (`:3`). No commit since the merge base touches `FileHelper.cs`.
- **Impact:** On Windows, a file can't be renamed or deleted while any handle on it doesn't share deletion. A logger that rolls its file by renaming it can't roll while Weevil parses that file. A first open of a 20 GB log is expected to take about 2 minutes (`986-DiskMode.md:13`), and another full read follows when the table of contents is rebuilt. Once the log is open, reads on disk do share deletion, so the problem is confined to these whole-file passes. Linux is not affected.
- **Recommendation:** Add `FileShare.Delete` in `FileHelper.Open`, which is one line, as decision 8 asked. Test it as `LogFileSourceTests` tests a writer (`Tst/BlueDotBrigade.Weevil.Common-UnitTests/IO/LogFileSourceTests.cs:205`). Otherwise, open an issue so the decision is not lost. **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U3-5. The advice estimates RAM only: Disk's own memory need is never estimated or warned about

- **Priority:** Low
- **Kind:** Intent
- **New in 3.0?** New design
- **Evidence:**
  - **Warnings cover RAM and the network only.** `OpenAsAdvice.GetWarning` warns only when RAM won't fit, or when Disk would read over the network (`Src/BlueDotBrigade.Weevil.Gui/IO/OpenAsAdvice.cs:68-82`). The text describing Disk is a constant (`:63`).
  - **The data is already there.** The estimate counts lines (`Src/BlueDotBrigade.Weevil.Core/IO/MemoryEstimator.cs:42-48`), and lines are what disk mode's 22 bytes per record scale with.
  - **A worked example.** A 64 GB log of 100-byte lines has about 640 million records. Its table alone takes about 14 GB at rest, before the costs in U3-2. With 40-byte lines it would need about 35 GB, and come close to the limit of 2,147,483,647 lines (U1, Minor).
  - **The goal assumes long records.** "A 64 GB log opens on a machine with 32 GB of RAM" (`Doc/Plans/986-DiskMode.md:12`) holds for records of about 130 bytes, like the test logs, but not for short ones.
- **Impact:** For a log that fits in neither mode, the window suggests Disk and gives no warning. The analyst learns only when Weevil runs out of memory part-way through the open.
- **Recommendation:** Have `EstimateMemory` also return the line count, or an estimate for each mode. Warn when Disk's table would not fit either. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U3-6. Two Open As dialogs and two ways to extract a zip: the overlap is planned, but the extraction rules are not carried over

- **Priority:** Low
- **Kind:** Maintainability
- **New in 3.0?** New design (transitional)
- **Evidence:**
  - **Two dialogs.** `Gui/IO/OpenAsDialog` (#1028) is the one in use (`Src/BlueDotBrigade.Weevil.Gui/IO/DialogBoxService.cs:177`). `Windows/Dialogs/OpenAsDialog` (#1157) is not yet referenced outside its own folder.
  - **Duplicate names.** There are two classes named `OpenAsDialog` and two named `OpenAsResult`: the plugins' (`Src/BlueDotBrigade.Weevil.Common/IO/OpenAsResult.cs:5`) and the new dialog's (`Src/BlueDotBrigade.Weevil.Windows/Dialogs/OpenAsDialog.xaml.cs:13-36`). Both namespaces will meet in `FilterViewModel`.
  - **Two ways to extract.**
    - **The GUI today** extracts the whole zip on the UI thread, to a bare GUID folder under `%TEMP%` (`FilterViewModel.cs:566`, `:572`). It keeps that copy while any file from it is open (`:633-636`, #1162).
    - **The new dialog** extracts only the chosen file and its companions, off the UI thread, to `%TEMP%\BlueDotBrigade.OpenAs.Extracted\<guid>` (`Windows/Dialogs/OpenAsDialog.xaml.cs:144-147`; `Storage/ContainerReader.cs:207-232`). It leaves deletion to the host (`OpenAsDialog.xaml.cs:33`).
  - **What is tracked.**
    - The #1157 plan says its dialog replaces the #1028 window and the Select File list (`Doc/Plans/1157-OpenAsDialog.md:7`, `:63`).
    - No plan says how `ExtractedLog`'s rules, and the eleven tests that pin them, carry over to per-file copies. The rules: keep the copy while its log is open, so Reload and disk mode can read it; delete it on the next open or at exit; keep files written since extraction.
    - Those rules, and the fact that a log in the temporary folder never saves an `.idx` and so is parsed on every open, appear only in dated plans (`Doc/Plans/986-DiskMode.md:49`, `:76`; `1025-ContentFromFile.md:42`). `Doc/Design/DiskMode.md` doesn't mention zipped logs.
- **Impact:** Until step 5 of #1157 lands, a maintainer sees two dialogs and two results with the same names, and two layouts for temporary folders. When it lands, the #1162 rules must be carried over by whoever remembers them. Losing them would bring back #1005: Reload of a zipped log failing.
- **Recommendation:**
  - **Plan:** add to step 5 of #1157: "`ExtractedLog` takes `OpenAsResult.ExtractedFolder`; the #1162 rules and tests stay."
  - **Design note:** describe zipped logs in `DiskMode.md`.
  - **Names:** rename the new result type, for example `ChosenFile`.

  **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U3-7. Outside Core, the mode is checked as one of two values, and one check uses the mode where it means something else

- **Priority:** Low
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **Readers never ask.** See Strengths.
  - **Producers ask in several places.** In Core, the mode is decided where chunks are made, by the rule that a null source means RAM, and in `FilterManager`, all covered by U1-2. It is also saved in the `.idx` as a byte (`Src/BlueDotBrigade.Weevil.Core/IO/SavedParse.cs:160`, `:183`).
  - **The GUI assumes two values in four places:**
    - the dialog keeps two booleans (`Src/BlueDotBrigade.Weevil.Gui/IO/OpenAsDialog.xaml.cs:15-27`, `:80`);
    - `OpenAsAdvice.DefaultMode` returns one of the two (`OpenAsAdvice.cs:54`);
    - the status bar shows `== Disk ? "Disk" : "RAM"` (`IO/SourceFileOpenedBulletin.cs:33`);
    - the graph tests `engine.StorageMode != StorageMode.Disk` (`Filter/FilterViewModel.cs:1543`).
  - **The graph's check means something else.** It really asks whether the records never read the log. A third mode would fall into the RAM branch without anyone noticing.
- **Impact:** A third mode, such as one that caches chunks, would touch about ten places in eight files, and the graph would break without a compiler error. No third mode is planned, so the cost is small. The question was asked, and this is the answer.
- **Recommendation:** Nothing now. If a mode is ever added, give the engine a fact the graph can ask directly ("the records read their text from the log"), rather than a test of the mode. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

## Minor

- **Duplicate save.** The CLI repeats `DiskWriter`'s write-to-a-temporary-file-then-move save, down to an identical `DeleteTemporaryFile` (`Cli/Filter/FilterCommands.cs:88-127` against `Core/IO/DiskWriter.cs:73-130`).
- **A fourth encoding detection.** The CLI detects the log's encoding again (`FilterCommands.cs:61-65`), although the engine detected it while opening (`CoreEngineBuilder.cs:334`). U1-4 counts three detections.
- **`DiskWriter`, inherited from 2.x.**
  - Its summary says it "Produces a report that includes the amount of time that has elapsed between the selected log entries", but it exports records as raw text or TSV (`Core/IO/DiskWriter.cs:12-13`).
  - Next to "disk mode", its name suggests disk mode.
  - Sonar's S3928 (`:37`, `nameof` of a local) and S1854 (`:161`) are inherited too.
- **Sentinel values.** `LogFileChangedException` tells its two cases apart by fields set to -1 (`Common/IO/LogFileChangedException.cs:47-63`), like U1-10.
- **Extraction on the UI thread.** A zip is extracted on the UI thread before the first `await` (`FilterViewModel.cs:572`), as in 2.x, so a large zip freezes the window. The #1157 dialog extracts in the background.
- **Copies left by a crash can't be found.** Extracted folders get bare GUID names (`FilterViewModel.cs:566`), so a copy left by a crash can't be recognised or cleaned up at the next start. #1025 decision 7 left that to "later issues". It is still far better than 2.x.
- **A public method kept for tests.** `OpenAsync(string)` stays public, only tests call it, and it opens without asking Open As (`FilterViewModel.cs:763-766`). It was public in 2.x.
- **A stale comment.** The graph's comment says "In RAM a record keeps its text" (`FilterViewModel.cs:1542`). In RAM a UTF-8 record keeps bytes. What matters is that it never reads the log.
- **Regular expression without a timeout.** Find builds one (`Core/Navigation/ContentNavigator.cs:55`, Sonar S6444), as 2.x did. Since #1028, Find runs in the background, where Escape stops it between records but not inside one match.
- **Ranges after Clear.** `RecordRanges` lines ranges up with chunks only over a whole table (`Core/Data/RecordRanges.cs:32`). After Clear on disk, a filter splits by position, so a chunk on a range boundary is read twice. Not measured.
- **The network warning names filters only** (`OpenAsAdvice.cs:78`). Each row read also opens the log over the network, about 250 opens per screen (`Doc/Plans/1028-OpenAs.md:175`).
- **Drift in `DiskMode.md`:**
  - `:3` links only the #1025 plan, although the choice of mode and the window come from #986 and #1028.
  - `:9` says RAM keeps content "until Reload", but Clear also frees what it clears (#1140).
  - `:31`'s timings name no machine or build.
  - `:46` is one ten-sentence paragraph that covers the GUI, the CLI, rows, the graph and failures other than a change (see U3-1).
  - Zipped logs are not described (see U3-6).

## To Measure in Stage 2

- **Why a filter on disk allocates more (U3-2).**
  - On the 4 GB log on disk, count the `FilterMapReduceState` and `ContentBuffer` objects created per filter, and the 16 MB arrays allocated.
  - Run the same filters with `DOTNET_GCHeapHardLimit=0x60000000` (1.5 GB). Do they finish, and how much slower?
  - Record full (gen 2) collections per filter, in both modes.
  - Copy the OpsHarness results quoted in U3-2 into `Measurements/`.
- **Reopening on disk.** `DiskMode.md:31` claims 0.9 s for the 1 GB log. Reopen the 1 GB and 4 GB harness logs on disk, from an `.idx` saved on disk. Compare with the first opens measured here: 7.1 s and 24.1 s, both mode-switch parses (U1-7).
- **Cold cache.** Every disk figure here read the log from the page cache, since a 4 GB log fits in 15 GB. Drop the cache and repeat one filter on disk, to separate I/O from CPU. The 20 GB NVMe target (`986-DiskMode.md:13`) remains #1024's measurement, on the owner's PC.
- **One read on Windows, and on a share.** `ScreenRowsBenchmarks` measured 7.6 µs per read on Linux (`1028-OpenAs.md:168-173`). On Windows, each read opens a handle (`LogFileSource.cs:158`), which antivirus may scan. Run it on Windows, on a local SSD and on an SMB share.
- **RAM after filtering, under memory pressure.** The estimate covers the peak while opening, but filters later peaked at 1.79 GB and 6.50 GB, against estimates of 1.25 GB and 5.01 GB. Measure under memory pressure whether that excess is memory the GC can give back.
- **Rebuilding the table of contents after an open.** When a log has no saved sections, the GUI reads it whole again through a `StreamReader` (`NavigationManager.cs:96-107`, called from `FilterViewModel.cs:953-956`). Time this on the 4 GB log; it also holds a handle that doesn't share deletion (U3-4).

## Open Questions

- **After a change, should the CLI and PowerShell reopen only the part that was asked for, or fail?** Should the GUI's Reload after a partial open (#1157 step 5) follow the same rule (U3-3)?
- **Is there an issue for decision 8 of #1025, sharing deletion in `FileHelper.Open` (U3-4)?** Is there one for the "later issues" of decision 7: extracting only the chosen file, and clearing leftovers at startup?
- **Should the Open As window warn when neither mode fits (U3-5)?** And what should Weevil do with a log of more than 2,147,483,647 lines, which disk mode's target size makes possible?
- **Is `IRecord.Content` meant to be readable by plugins on disk (U3-1)?** If so, should the plugin interface say how a plugin reports a failed read, rather than swallowing it?
