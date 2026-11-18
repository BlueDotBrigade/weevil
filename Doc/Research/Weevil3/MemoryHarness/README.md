# Memory Harness

Question for #1019, before step 1 of the [#988 plan](../../../Plans/988-IndexingService.md): how much RAM does Weevil use after opening a log? #978 aims to use less, so this records "before"; the same harness measures "after" once each step lands.

## The Harness

The harness is in [`Dep/TestHarnesses/MemoryHarness`](../../../../Dep/TestHarnesses/MemoryHarness/README.md) since #1290. Its README says what each figure below means, how to run it, and its limits. The results stay here, as a dated record.

## Results

### Before: Weevil 2.12.1 (2026-10-20)

Weevil 2.12.1 needs about **3.25 GB of RAM per GB of log** at the peak while opening, and about 3.0 GB per GB once the log is open: about 430 bytes per record, for records averaging 132 bytes. The peak decides whether a log fits. For the 4 GB log the extra 0.8 GB at the peak is temporary, probably from the record lists growing while the log is parsed.

Measured on a 4-vCPU Linux container with 14.2 GB of RAM available to .NET, all of it free, with .NET 10 and `--ram-percent 99`. The full report is in [Results-2026-10-20-Before.md](Results-2026-10-20-Before.md).

| Log | Working set after opening | Peak while opening | Source |
|---|---|---|---|
| 1 GB | 3.3 GB | 3.3 GB | Measured: 7,547,742 records, opened in 41 s |
| 4 GB | 12.2 GB | 13.0 GB | Measured: 30,014,396 records, opened in 158 s |
| 8 GB | 24.3 GB | 26.0 GB | **Estimate.** Too big for this machine |
| 12 GB | 36.4 GB | 39.0 GB | **Estimate** |
| 16 GB | 48.5 GB | 52.0 GB | **Estimate** |
| 20 GB | 60.7 GB | 64.9 GB | **Estimate** |
| 24 GB | 72.8 GB | 77.9 GB | **Estimate** |

- **The estimates hold.** Memory grows in step with the number of records: estimated from the 1 GB log alone, the 4 GB log's peak was 13.0 GB, exactly as measured. The larger sizes are estimated from the 4 GB log.
- **On a PC:** with 16 GB of RAM, a 4 GB log does not fit once Windows and the WPF window take their share. With 32 GB, the largest log that fits is about 8 GB.
- **Free RAM matters.** An earlier run reported the 4 GB log as too big, because other processes held 2.3 GB of the RAM and the log passed the harness's limit. Close other applications before measuring.

### After step 1 of #988: content held as the log's bytes (2026-10-29)

Weevil now needs about **1.62 GB of RAM per GB of log**, both at the peak while opening and once the log is open. That is 46% less than 2.12.1 after opening, and 40% less than `main` before #1133, which was measured the same day on the same machine. The full reports are in [Results-2026-10-29-RawContent.md](Results-2026-10-29-RawContent.md) and, for `main`, [Results-2026-10-29-MainBeforeRawContent.md](Results-2026-10-29-MainBeforeRawContent.md).

| Log | 2.12.1 | `main` before #1133 | After #1133 | Source |
|---|---|---|---|---|
| 1 GB | 3.3 GB | 2.8 GB | 1.7 GB, peak 1.7 GB | Measured: opened in 20 s, against 29 s for `main` |
| 4 GB | 12.2 GB, peak 13.0 GB | 10.9 GB, peak 10.9 GB | 6.5 GB, peak 6.5 GB | Measured: opened in 75 s, against 114 s for `main` |
| 8 GB | 24.3 GB, peak 26.0 GB | | 13.0 GB, peak 13.0 GB | **Estimate** |
| 16 GB | 48.5 GB, peak 52.0 GB | | 25.9 GB, peak 25.9 GB | **Estimate** |
| 24 GB | 72.8 GB, peak 77.9 GB | | 38.9 GB, peak 38.9 GB | **Estimate** |

- **On a PC:** with 32 GB of RAM, the largest log that fits is now about 16 GB, instead of about 8 GB.
- **The peak:** a first measurement put the peak 0.5 GB above the memory after opening. The parser kept every record it had read, with its line's text, until the chunk holding them was complete. Keeping only what the parser found (#1134 review) removed it, and opening the 4 GB log fell from 94 s to 75 s.

### After step 3 of #988: records created when they are read (2026-11-13)

Weevil now needs about **1.22 GB of RAM per GB of log**, both at the peak while opening and once the log is open. That is 25% less than after step 1, and 62% less than 2.12.1 at the peak. Records live in a record table, and a record is created only when it is read (#1021). #1086 asks for at most 1.2. The full report is in [Results-2026-11-13-RecordsOnDemand.md](Results-2026-11-13-RecordsOnDemand.md).

| Log | 2.12.1 | After step 1 (#1133) | After step 3 (#1021) | Source |
|---|---|---|---|---|
| 1 GB | 3.3 GB | 1.7 GB | 1.4 GB, peak 1.4 GB | Measured: opened in 6.9 s |
| 4 GB | 12.2 GB, peak 13.0 GB | 6.5 GB, peak 6.5 GB | 4.9 GB, peak 4.9 GB | Measured: opened in 26 s |
| 16 GB | 48.5 GB, peak 52.0 GB | 25.9 GB, peak 25.9 GB | 19.5 GB, peak 19.5 GB | **Estimate** |
| 24 GB | 72.8 GB, peak 77.9 GB | 38.9 GB, peak 38.9 GB | 29.2 GB, peak 29.2 GB | **Estimate** |

- **On a PC:** with 32 GB of RAM, the largest log that fits is now about 20 GB, instead of about 16 GB after step 1.
- **Opening times** are from the same kind of machine, but its speed varies between sessions, so compare them only roughly: the 4 GB log took 75 s on 2026-10-29.

### Reopening, after #1023 (2026-11-14)

A reopen builds the record table saved with the log's index instead of parsing the log, and takes the same memory as a first open. A reopen after the log grew 1% parses only its new part. While it copies the saved rows into the longer table, it holds both, about 22 bytes per record more, so its peak is about 1.37 GB per GB of log; #1201 removes the copy. The full report is in [Results-2026-11-14-Reopen.md](Results-2026-11-14-Reopen.md).

| Log | First open | Reopen | Reopen after growing 1% |
|---|---|---|---|
| 1 GB | 7.8 s, 1.4 GB | 2.7 s, 1.4 GB | 3.4 s, 1.4 GB, peak 1.5 GB |
| 4 GB | 30.8 s, 4.9 GB | 10.4 s, 4.9 GB | 16.2 s, 4.9 GB, peak 5.5 GB |

- **The first measurement kept the saved table** after a grown reopen: 5.5 GB once the 4 GB log was open. The engine's collection ran while the method that opened the log still held the table in its frame, and clearing the `out` variable did not free it. The reopen now runs in a method of its own, never inlined (`CoreEngineBuilder.TryReopen`).
- **The 4 GB reopen takes 10 s,** a third of a first open: it still reads the whole log, to check it against the saved checksums, and reads the 0.66 GB table.

### Reopening a grown log without copying its rows, after #1201 (2026-11-14)

The longer table of a grown log now shares the saved table's full blocks, instead of copying their rows. On the 4 GB log, a reopen after growing 1% peaks at 5.0 GB instead of 5.5 GB, against 4.9 GB for a first open. It takes 3.6 s more than a plain reopen in the same run, against 5.8 s before; compare whole times between the two runs only roughly, since the plain reopen also moved, from 10.4 s to 8.1 s. The full report is in [Results-2026-11-14-SharedBlocks.md](Results-2026-11-14-SharedBlocks.md).

- **What is left of the peak** is estimated at tens of MB, within the report's rounding to 0.1 GB: the saved table's last chunk, which the new part's parse reads again, the rows of the new part, which are copied, and the parse's buffers.

| Log | First open | Reopen | Reopen after growing 1% |
|---|---|---|---|
| 1 GB | 7.3 s, 1.4 GB | 2.3 s, 1.4 GB | 3.0 s, 1.4 GB, peak 1.4 GB |
| 4 GB | 26.1 s, 4.9 GB | 8.1 s, 4.9 GB | 11.7 s, 4.9 GB, peak 5.0 GB |
