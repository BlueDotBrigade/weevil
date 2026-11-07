# 986: Open a Log in RAM or on Disk

> **Status:** Draft
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Phase 4 of the [Weevil 3.0 feasibility study](../Research/Weevil3/Feasibility.md). Epic #986; user story #421. It needs #988, which gives records on demand and the record table.

## Goal

- An analyst chooses RAM or Disk when opening a log (#421). The default is the mode that fits: RAM for the largest log the computer's memory can hold, Disk above that.
- A 64 GB log opens on a machine with 32 GB of RAM.
- On an NVMe drive, a 20 GB log filters in under 10 s. A first open may take about 2 minutes, as today (accepted 2026-10-20); reopening uses the saved index (#988).
- Every feature works in disk mode, including Clear, which narrows the view's record scope.
- Reload (Ctrl+F5) brings cleared records back, in both modes, as today: released content is read back from the file.
- Weevil never blocks the application that writes the log, and never modifies the log, or the zip it came from (#1156).

## Decisions

- Decided 2026-10-19: RAM or Disk is chosen in Weevil's Open As dialog, which appears on every open: the user clicks Open, picks the log in the Windows file dialog, and then Weevil shows Open As.
  - Changed 2026-11-07: this holds until #1157, the last item of milestone 8, replaces the Windows file dialog and Open As with one Weevil dialog. Weevil's file dialog is a generic user control, because the Windows one is another dialog in a different style.
- Decided 2026-10-20: there is no separate "Restore cleared records" command. Reload (Ctrl+F5) already brings cleared records back.
- Decided 2026-11-07:
  - **Order of work:** #1156, then steps 1 – 4, then step 5, then #1157.
  - **Open always offers the choice.** Open (the button, Ctrl+O and the menu's Open…) always lets the analyst choose RAM or Disk. Reload (Ctrl+F5) keeps the mode and never asks again.
  - **The default comes from the log's size and the computer's memory**, no longer always RAM. Weevil assumes no other application runs apart from the operating system, and opens in RAM the largest log that fits. A percentage of installed RAM does not work, because what matters is the memory the operating system leaves free. Installed and available memory are read through `ComputerSnapshot`, as Weevil reads its other facts about the computer.
  - **Zipped logs work in both modes.** Weevil keeps the extracted copy until the log is closed, instead of deleting it after opening, and never deletes the zip itself (#1156).
  - **The CLI and PowerShell** open in RAM unless an optional parameter asks for Disk.
- Proposed 2026-11-07, to confirm when step 5 starts: open in RAM when the log's estimated memory fits in the memory available to Weevil, less a small fixed reserve.
  - **Available:** Windows' available physical memory (free and standby pages) when the log is chosen. Check on Windows that `ComputerSnapshot.RamTotalFree`, WMI's `FreePhysicalMemory`, reports it; otherwise read it there with `GlobalMemoryStatusEx`.
  - **Estimate:** the log's size plus about 30 bytes per record (153 bytes per record were measured on records of 123 bytes, #1150). The record count comes from the saved index, or from the average line length in the log's first megabyte.
  - **Reserve:** about 1 GB, for Weevil's window, a filter's temporary results (4 bytes per record) and headroom. Tune it once #1141 measures the peak while opening.
  - Open builds the new log while the current one is still in memory (`FilterViewModel.OpenAsync`). Either release the current log first, as Reload does since #1126, or do not count its memory as available.

## Steps

One sub-issue of #421 and one pull request per step, in order. The step issues were created on 2026-10-20, so a change to the plan can be traced through them.

1. **Proof of concept: disk filtering (#1024).** This adds the disk case to the [storage benchmark](../Research/LargeLogFiles/StorageBenchmarks/README.md), as [LargeLogFileStrategy.md](../Research/LargeLogFiles/LargeLogFileStrategy.md) planned.
   - Measure a parallel streaming filter over a 20 GB log on NVMe and on SATA, and the time to read a screen of rows through the line index.
   - This decides the number of reader threads and confirms the 10 s target. Record the result in `Research/LargeLogFiles/`.
2. **Content from the file (#1025).** One content store with two caching rules: RAM mode keeps every chunk the view's scope includes, and disk mode keeps only the chunks being read. See [EngineApi.md](../Research/Weevil3/EngineApi.md#freeing-ram).
   - A chunk that is not in memory, released by Clear (#988) or never loaded in disk mode, is read back from the file when needed.
   - File reads are ordinary reads, shared for reading, writing and deletion, so the writing application is never blocked. Today Weevil shares the log for reading and writing only (`FileHelper.cs`), which is harmless while it reads the file once; disk mode keeps the file open, so add deletion.
   - Never memory-map the log. Weevil's own `.idx` may be mapped if the record table is too large to hold.
   - A log opened from a zip file reads back from the extracted copy, which Weevil keeps until the log is closed (decided 2026-11-07). Today Weevil deletes that copy right after opening it, which is also why Reload of a zipped log fails (#1005).
3. **Reload brings cleared records back (#1026).** Reload resets the view's scope to the whole log, as Ctrl+F5 does today, and content released by Clear is read back from the file.
4. **Streaming work (#1027).** In disk mode (decided 2026-10-20):
   - Filters read the log in parallel ranges split at record boundaries.
   - Analyzers, export, Find, the graph and the dashboard read it once, in order, because most analyzers carry state from one record to the next. An NVMe drive reads 20 GB in about 6 s.
   - Filters on monikers that never read content, such as `@Pinned` or `@Flagged`, do not read the file, so an expression must say whether it needs content.
   - Abort stops them.
5. **Open As dialog (#1028).** The simplest version (decided 2026-11-07): after the Windows file dialog, Weevil always shows a small Open As window offering RAM or Disk, with the default already chosen.
   - Warnings appear in that window, not in message boxes: RAM chosen for a log that may not fit, and a disk-mode log on a network drive.
   - Today only plugins can show an Open As dialog (`IPlugin.ShowOpenAs`), and the built-in plugins don't. A plugin's own window still follows Weevil's for now; whether its options join Weevil's dialog is an open question of #1157.
   - The CLI and PowerShell gain their optional mode parameter.
6. **One Open dialog (#1157).** Last in milestone 8. A generic file-browser user control in `BlueDotBrigade.Weevil.Windows` replaces the Windows file dialog. Weevil's Open As window hosts it beside RAM and Disk, so opening a log takes one dialog. The control uses standard WPF controls and no MaterialDesign style names, so Weevil's theme restyles it, and it can later move to a library shared by other Blue Dot Brigade WPF applications.

## Compatibility

- The sidecar and `.idx` formats do not change in this phase.
- RAM mode behaves exactly as after #988.
- Neither dialog needs MaterialDesign 5, so the upgrade (#428) is not a prerequisite. MaterialDesign has no file dialog in any version.

## Risks

- On a SATA SSD or a network drive, filtering is several times slower than the target. The warning in step 5 sets expectations; nothing else can fix it.
- A log that grows while open in disk mode: Reload scans only the new content (#977), but the record table must grow with it.
