# 986: Open a Log in RAM or on Disk

> **Status:** Draft
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Phase 4 of the [Weevil 3.0 feasibility study](../Research/Weevil3/Feasibility.md). Epic #986; user story #421. It needs #988, which gives records on demand and the record table.

## Goal

- An analyst chooses RAM (the default) or Disk when opening a log (#421).
- A 64 GB log opens on a machine with 32 GB of RAM.
- On an NVMe drive, a 20 GB log filters in under 10 s. A first open may take about 2 minutes, as today (accepted 2026-10-20); reopening uses the saved index (#988).
- Every feature works in disk mode, including Clear, which narrows the view's record scope.
- Reload (Ctrl+F5) brings cleared records back, in both modes, as today: released content is read back from the file.
- Weevil never blocks the application that writes the log, and never modifies the log.

## Decisions

- Decided 2026-10-19: RAM or Disk is chosen in Weevil's Open As dialog, which appears on every open: the user clicks Open, picks the log in the Windows file dialog, and then Weevil shows Open As.
- Decided 2026-10-20: there is no separate "Restore cleared records" command. Reload (Ctrl+F5) already brings cleared records back.

## Steps

One sub-issue of #421 and one pull request per step, in order. The step issues were created on 2026-10-20, so a change to the plan can be traced through them.

1. **Proof of concept: disk filtering (#1024).** This adds the disk case to the [storage benchmark](../Research/LargeLogFiles/StorageBenchmarks/README.md), as [LargeLogFileStrategy.md](../Research/LargeLogFiles/LargeLogFileStrategy.md) planned.
   - Measure a parallel streaming filter over a 20 GB log on NVMe and on SATA, and the time to read a screen of rows through the line index.
   - This decides the number of reader threads and confirms the 10 s target. Record the result in `Research/LargeLogFiles/`.
2. **Content from the file (#1025).** One content store with two caching rules: RAM mode keeps every chunk the view's scope includes, and disk mode keeps only the chunks being read. See [EngineApi.md](../Research/Weevil3/EngineApi.md#freeing-ram).
   - A chunk that is not in memory, released by Clear (#988) or never loaded in disk mode, is read back from the file when needed.
   - File reads are ordinary reads, shared for reading, writing and deletion, so the writing application is never blocked. Today Weevil shares the log for reading and writing only (`FileHelper.cs`), which is harmless while it reads the file once; disk mode keeps the file open, so add deletion.
   - Never memory-map the log. Weevil's own `.idx` may be mapped if the record table is too large to hold.
   - A log opened from a zip file has no file to read back from: Weevil deletes the extracted copy after opening it. Out of scope for this release (#1005).
3. **Reload brings cleared records back (#1026).** Reload resets the view's scope to the whole log, as Ctrl+F5 does today, and content released by Clear is read back from the file.
4. **Streaming work (#1027).** In disk mode (decided 2026-10-20):
   - Filters read the log in parallel ranges split at record boundaries.
   - Analyzers, export, Find, the graph and the dashboard read it once, in order, because most analyzers carry state from one record to the next. An NVMe drive reads 20 GB in about 6 s.
   - Filters on monikers that never read content, such as `@Pinned` or `@Flagged`, do not read the file, so an expression must say whether it needs content.
   - Abort stops them.
5. **Open As dialog (#1028).** After the Windows file dialog, Weevil always shows its own Open As dialog, with the ways to open the log, starting with RAM (the default) or Disk. Today only plugins can show an Open As dialog (`IPlugin.ShowOpenAs`), and the built-in plugins don't. A plugin's own options, such as a line range, join Weevil's dialog rather than opening a second one; settle how when writing this step's sub-issue. Weevil warns when RAM is chosen for a log that won't fit, and when a disk-mode log is on a network drive.

## Compatibility

- The sidecar and `.idx` formats do not change in this phase.
- RAM mode behaves exactly as after #988.

## Risks

- On a SATA SSD or a network drive, filtering is several times slower than the target. The warning in step 5 sets expectations; nothing else can fix it.
- A log that grows while open in disk mode: Reload scans only the new content (#977), but the record table must grow with it.
