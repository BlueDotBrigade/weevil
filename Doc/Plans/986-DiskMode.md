# 986: Open a Log in RAM or on Disk

> **Status:** Draft
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Phase 4 of the [Weevil 3.0 feasibility study](../Research/Weevil3/Feasibility.md). Epic #986; user story #421. It needs #988, which gives records on demand and the record table.

## Goal

- An analyst chooses RAM (the default) or Disk when opening a log (#421).
- A 64 GB log opens on a machine with 32 GB of RAM.
- On an NVMe drive, a 20 GB log opens in under 1 minute and filters in under 10 s.
- Every feature works in disk mode, including Clear, which narrows the record set.
- Weevil never blocks the application that writes the log, and never modifies the log.

## Decision Needed First

**Where the choice is made.** Today only plugins can show an Open As dialog (`IPlugin.ShowOpenAs`), and the built-in plugins don't. The options are: Weevil shows its own dialog and passes the choice on, or the choice is added to `OpenAsResult` so plugin dialogs can offer it. Needed by step 4.

## Steps

One sub-issue and one pull request per step, in order.

1. **Proof of concept: disk filtering.** This adds the disk case to the [storage benchmark](../Research/LargeLogFiles/StorageBenchmarks/README.md), as [LargeLogFileStrategy.md](../Research/LargeLogFiles/LargeLogFileStrategy.md) planned.
   - Measure a parallel streaming filter over a 20 GB log on NVMe and on SATA, and the time to read a screen of rows through the line index.
   - This decides the number of reader threads and confirms the 10 s target. Record the result in `Research/LargeLogFiles/`.
2. **Content from the file.** The log source reads content either from RAM chunks or from the file.
   - File reads are ordinary reads, shared for reading, writing and deletion, so the writing application is never blocked.
   - Never memory-map the log. Weevil's own `.idx` may be mapped if the record table is too large to hold.
3. **Streaming work.** In disk mode, filters, analyzers and export read the log in parallel ranges split at record boundaries. Abort stops them.
4. **Choose the mode when opening.** RAM / Disk, defaulting to RAM, as decided above. Weevil warns when RAM is chosen for a log that won't fit, and when a disk-mode log is on a network drive.

## Compatibility

- The sidecar and `.idx` formats do not change in this phase.
- RAM mode behaves exactly as after #988.

## Risks

- On a SATA SSD or a network drive, filtering is several times slower than the target. The warning in step 4 sets expectations; nothing else can fix it.
- A log that grows while open in disk mode: Reload scans only the new content (#977), but the record table must grow with it.
