# 986: Open a Log in RAM or on Disk

> **Status:** In progress
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Phase 4 of the [Weevil 3.0 feasibility study](../Research/Weevil3/Feasibility.md). Epic #986; user stories #421 and, for the Open dialog, #1157. It needs #988, which gives records on demand and the record table.

## Goal

- An analyst chooses RAM or Disk when opening a log (#421). The default is the mode that fits: RAM for the largest log the computer's memory can hold, Disk above that.
- A 64 GB log opens on a machine with 32 GB of RAM.
- On an NVMe drive, a 20 GB log filters in under 10 s. A first open may take about 2 minutes, as today (accepted 2026-10-20); opening it again later uses the saved index (#988).
- Every feature works in disk mode, including Clear, which narrows the view's record scope.
- Reload (Ctrl+F5) brings cleared records back, in both modes, as today: released content is read back from the file.
- Weevil never blocks the application that writes the log, and never modifies or deletes the log, or the zip it came from (#1156).

## Decisions

- Decided 2026-10-19: RAM or Disk is chosen in Weevil's Open As dialog, which appears on every open: the user clicks Open, picks the log in the Windows file dialog, and then Weevil shows Open As.
  - Confirmed 2026-11-07 for the button, Ctrl+O and the menu's Open…. Reload (Ctrl+F5) keeps the mode and never asks again.
  - Changed 2026-11-07: step 6 (#1157) later replaces the Windows file dialog, because it is a second dialog in a different style. Open As then holds the file browser too.
- Decided 2026-10-20: there is no separate "Restore cleared records" command. Reload (Ctrl+F5) already brings cleared records back.
- Decided 2026-11-07:
  - **Order of work in milestone 8 (Weevil 3.0):** #1156 first, then the steps in order, with step 6 (#1157) last.
  - **The default comes from the log's size and the computer's memory**, no longer always RAM: it is RAM for the largest log that fits. Weevil assumes no other application runs apart from the operating system. A percentage of installed RAM does not work, because what matters is the memory the operating system leaves free. Memory is read through `ComputerSnapshot`, as Weevil reads its other facts about the computer.
  - **Zipped logs work in both modes**, from a copy extracted to a temporary folder and deleted later. The zip itself is never deleted (#1156).
  - **The CLI and PowerShell** open in RAM unless an optional parameter asks for Disk.
- Proposed 2026-11-07, to confirm when step 5 starts: default to RAM when the log's estimated memory fits in the available memory, less a small fixed reserve.
  - **Available:** Windows' available physical memory (free and standby pages) when the log is chosen. Applications already running count against it; no room is kept for applications started later. Check on Windows that `ComputerSnapshot.RamTotalFree`, WMI's `FreePhysicalMemory`, reports it; otherwise read it there with `GlobalMemoryStatusEx`.
  - **Estimate:** the log's size plus about 34 bytes per record: about 30 for the record table (1.22 GB of working set per GB of log, #1150) and 4 for a filter's temporary results. The record count comes from the saved index's line count, which is never lower, or from the average line length in the log's first megabyte.
  - **Reserve:** about 1 GB, for Weevil's window and headroom. Tune it once #1141 measures the peak while opening.
  - **Where:** Core estimates the memory a log needs, so the CLI, PowerShell and tests can use it. The GUI compares it with the memory it reads through `ComputerSnapshot`.
  - Open builds the new log while the current one is still in memory (`FilterViewModel.OpenAsync(string)`), so the available memory measured then already excludes it. If Open released the current log first, as Reload does since #1126, the rule could count that log's memory too, but a failed open would then leave no log open.

## Steps

Steps 1 – 5 are sub-issues of #421, created on 2026-10-20, so a change to the plan can be traced through them. Step 6 is user story #1157, under #986, created on 2026-11-07. One pull request per step, in order.

1. **Proof of concept: disk filtering (#1024).** This adds the disk case to the [storage benchmark](../Research/LargeLogFiles/StorageBenchmarks/README.md), as [LargeLogFileStrategy.md](../Research/LargeLogFiles/LargeLogFileStrategy.md) planned.
   - Measure a parallel streaming filter over a 20 GB log on NVMe and on SATA, and the time to read a screen of rows through the line index.
   - This decides the number of reader threads and confirms the 10 s target. Record the result in `Research/LargeLogFiles/`.
   - 2026-11-08: the disk case is in the storage benchmark, checked on Linux. The measurements on NVMe and SATA, on the owner's PC, are still to do; its README says how to run them and proposes the decision rule.
2. **Content from the file (#1025).** One content store with two caching rules: RAM mode keeps every chunk the view's scope includes, and disk mode keeps only the chunks being read. See [EngineApi.md](../Research/Weevil3/EngineApi.md#freeing-ram).
   - A chunk that is not in memory, released by Clear (#988) or never loaded in disk mode, is read back from the file when needed.
   - File reads are ordinary reads, shared for reading, writing and deletion, so the writing application is never blocked. Today Weevil shares the log for reading and writing only (`FileHelper.cs`), which is harmless while it reads the file once; disk mode keeps the file open, so add deletion.
   - Never memory-map the log. Weevil's own `.idx` may be mapped if the record table is too large to hold.
   - Each parallel reader opens its own handle (found in #1024). On Windows, the reads of a handle opened for synchronous I/O are serialized, and read-ahead is kept per handle, so readers sharing one handle read one at a time.
   - A log opened from a zip reads back from its extracted copy, so Weevil keeps that copy while the log is open: the plan's reading of "deleted later", to confirm. Today the copy is deleted right after opening, so Reload of a zipped log fails (#1005, #1156).
3. **Reload brings cleared records back (#1026).** Reload resets the view's scope to the whole log, as Ctrl+F5 does today, and content released by Clear is read back from the file.
4. **Streaming work (#1027).** In disk mode (decided 2026-10-20):
   - Filters read the log in parallel ranges split at record boundaries.
   - Analyzers, export, Find, the graph and the dashboard read it once, in order, because most analyzers carry state from one record to the next. An NVMe drive reads 20 GB in about 6 s.
   - Filters on monikers that never read content, such as `@Pinned` or `@Flagged`, do not read the file, so an expression must say whether it needs content.
   - Abort stops them.
5. **Open As dialog (#1028).** The simplest version (decided 2026-11-07): after the Windows file dialog, Weevil shows a small Open As window offering RAM or Disk, with the default already chosen.
   - For a zip, Open As follows the Select File list, so the default uses the chosen log's size.
   - Warnings appear in that window, not in message boxes: RAM chosen for a log that may not fit, and a disk-mode log on a network drive (suggested 2026-11-07; the owner did not object).
   - #1028 settles what drag-and-drop and opening from Explorer or the command line do, since they skip the Windows file dialog.
   - Today only plugins can show an Open As dialog (`IPlugin.ShowOpenAs`), and the built-in plugins don't. A plugin's own window still follows Weevil's for now.
   - The CLI and PowerShell gain their optional mode parameter.
6. **One Open dialog (#1157).** A generic file-browser user control in `BlueDotBrigade.Weevil.Windows`, which will later move to a library shared by Blue Dot Brigade WPF applications, replaces the Windows file dialog. Weevil's Open As window hosts it beside RAM and Disk. Its design is revisited when it starts (decided 2026-11-07). Details and open questions: #1157.

## Compatibility

- The sidecar format does not change. The `.idx` format, the record table and the checksums are new in Weevil 3.0, so they can change without keeping compatibility (decided 2026-11-08).
- RAM mode behaves exactly as after #988.
- Recommended 2026-11-07, not yet confirmed: the MaterialDesign upgrade (#428) is not a prerequisite, because neither Open As nor #1157 needs MaterialDesign 5 (#1157).

## Risks

- On a SATA SSD or a network drive, filtering is several times slower than the target. The warning in step 5 sets expectations; nothing else can fix it.
- A log that grows while open in disk mode: Reload scans only the new content (#977), but the record table must grow with it.
- A zipped log is extracted to the temporary folder and kept while open, so it needs its full size free there. Weevil saves no index for a file in the temporary folder, so every open parses it (#1005).
