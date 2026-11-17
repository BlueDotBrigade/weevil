# Performance: Weevil 2.12.1 Against 3.0

Stage 2 of the [Weevil 3.0 audit](../Progress.md). Checks the CPU and RAM claims made for 3.0 against measurements taken for this audit, and lists what is still unmeasured.

> **Status:** Final (no High finding, so no sceptic pass). Measured on 2026-11-16 and 2026-11-17 at `main` e3dee822 against Weevil 2.12.1 from NuGet. Raw results and the tools are in [Measurements/](../Measurements/README.md).

## Summary

**3.0's main claims hold on this machine.** Opening the 4 GB synthetic log peaks at 4.9 GB of RAM instead of 12.9 GB (62% less), and takes 29 s instead of 90 s. Every timed filter on the 1 GB log is 4 to 24 times faster than in 2.12.1, with the same results. A reopen from the saved table takes 8 s for the 4 GB log instead of 29 s. On disk, the log keeps 22 bytes per record at rest, and the 4 GB log filters in 1.1 to 1.4 times RAM's time from the page cache.

Find is the one operation that is not faster: 0.53 s against 0.45 s on the 1 GB log in RAM, and 0.7–1.0 s on disk.

**Three measured caveats:**

- **Disk mode needs more memory while filtering than the documents say** (P-1). A filter on disk churns through 16 MB buffers on the large object heap, about one for every two to five chunks of the log. That forces full collections and makes the working set grow towards the size of the log. With the managed heap capped at 1 GB, two of eight filters on the 4 GB log ran out of memory, although its table needs only 0.67 GB. The cause is confirmed, and a small change in `FilterManager` removes most of it.
- **Changing storage mode costs a full parse** (P-2). It takes 6 s on the 1 GB log, against 1.3–2.3 s for a reopen in the same mode, which confirms U1-7.
- **The RAM figures describe opening, not use** (P-3). After eight filters and a Find, the 4 GB log's working set in RAM had grown from 4.7 GB to 6.3 GB, with a peak of 6.5 GB. The Open As advice sizes RAM from the opening figure, plus a fixed 1 GB.

**Found in passing:** a log placed directly in the temporary folder is not recognised as temporary, so Weevil saves its `.idx` and sidecar there (P-4).

**Every figure is from Linux,** with the log in the page cache and at most 4 GB, the largest log this environment can open. The 20 GB target, cold reads, network shares and Windows itself remain for the owner to measure.

## Method

- **Machine.** A Linux cloud container: 4 vCPUs, 15 GB of RAM (14.2 GB available to .NET), .NET 10.0.15, workstation GC. Compare figures only within this machine.
- **Versions.** Weevil 2.12.1 Core from NuGet against `main` e3dee822, both run on .NET 10 by the same harness source.
- **Logs.** Synthetic TSV logs from the memory harness, 131 bytes per record on average: 1,000 MB (7,547,742 records) and 4.0 GB (30,014,396 records).
- **Tools.**
  - The repo's [memory harness](../../MemoryHarness/README.md), unchanged.
  - Three small programs written for the audit: [OpsHarness](../Measurements/OpsHarness/) (open, eight filters and a Find; the median of 5 runs, allocations, peak working set), [ReopenDiag](../Measurements/ReopenDiag/) (the `.idx` open paths) and [AllocDiag](../Measurements/AllocDiag/) (allocations by type).
- **Noise.**
  - The cleanest run is [Ops-1gb-run1.txt](../Measurements/Ops-1gb-run1.txt), taken with nothing else running.
  - Review agents ran during the other runs. That makes open times noisy (the 4 GB RAM open read 73 s once and 29 s twice), while filter times stayed within about 10%.
  - Treat open times as rough, and filter and allocation figures as solid.

## Results

### Memory After Opening

From [Memory-2.12.1.md](../Measurements/Memory-2.12.1.md), [Memory-Main.md](../Measurements/Memory-Main.md) and [Memory-Main-Run2.md](../Measurements/Memory-Main-Run2.md).

| Log | Weevil | Working set after opening | Peak while opening | First open | Reopen | Reopen after 1% growth |
|---|---|---|---|---|---|---|
| 1 GB | 2.12.1 | 3.3 GB | 3.4 GB | 23–27 s | (none) | (none) |
| 1 GB | `main`, RAM | 1.2 GB | 1.4 GB | 8.4 s | 2.4 s | 3.2 s |
| 4 GB | 2.12.1 | 12.2 GB | 12.9 GB | 90 s | (none) | (none) |
| 4 GB | `main`, RAM | 4.7 GB | 4.9 GB | 29 s | 8.0 s | 11.4 s |
| 4 GB | `main`, Disk | 0.69 GB | 0.91 GB | 24 s | (not measured) | (not measured) |

Per GB of log, `main` keeps 1.16 GB after opening and 1.22 GB at the peak. 2.12.1 keeps 3.03 GB and 3.21 GB.

### Filter and Find on the 1 GB Log

The median of 5, from [Ops-1gb-run1.txt](../Measurements/Ops-1gb-run1.txt). Every filter returned the same count in all three columns.

| Operation | 2.12.1 | `main`, RAM | `main`, Disk | `main` RAM vs 2.12.1 |
|---|---|---|---|---|
| Plain text `timeout` (2.9 M results) | 2.78 s | 0.34 s | 0.44 s | 8.1× faster |
| Plain text, rare (11 results) | 2.23 s | 0.21 s | 0.29 s | 10.9× |
| Regex `retry.*connection` | 2.89 s | 0.39 s | 0.46 s | 7.3× |
| Regex with groups and `\w+` | 2.95 s | 0.75 s | 0.86 s | 3.9× |
| Include `timeout`, exclude `retry` | 3.13 s | 0.38 s | 0.47 s | 8.2× |
| Moniker `@Severity=Warning` | 2.38 s | 0.10 s | 0.11 s | 23.5× |
| `Warning\|\|Error` | 3.46 s | 0.37 s | 0.46 s | 9.5× |
| Clear the filter (show all) | 2.00 s | 0.36 s | 0.37 s | 5.6× |
| **Find the last record's text** | **0.45 s** | **0.53 s** | **0.73 s** | **0.85×, slower** |

On the 4 GB log ([Ops-4gb-main-ram-run2.txt](../Measurements/Ops-4gb-main-ram-run2.txt), [Ops-4gb-main-disk-run2.txt](../Measurements/Ops-4gb-main-disk-run2.txt)), filters take 0.41–3.3 s and scale linearly with the log. Disk takes 1.0 to 1.4 times RAM's time, and Find takes 2.0 s in RAM and 2.9 s on disk. 2.12.1 was not timed on the 4 GB log, because it needs 13 of the machine's 15 GB.

### Opening Around the Saved Index

[Reopen-1gb.txt](../Measurements/Reopen-1gb.txt), three runs on the 1 GB log, in RAM unless stated:

| Path | Time |
|---|---|
| First open, saving no `.idx` (temporary subfolder) | 7.3–7.8 s |
| First open, writing a 167 MB `.idx` beside the log | 7.0–7.3 s |
| Reopen, same mode: RAM | 2.1–2.4 s |
| Reopen, same mode: Disk | 1.3–1.5 s |
| **Reopen in the other mode** (RAM to Disk, Disk to RAM) | **6.0–6.8 s** |

### Allocations per Filter

One plain-text filter on the 1 GB log ([AllocDiag-1gb.txt](../Measurements/AllocDiag-1gb.txt), [FilterBuffers-4gb.md](../Measurements/FilterBuffers-4gb.md)):

| Mode | Allocated | Of which `Record` | Of which large `byte[]` | Full collections |
|---|---|---|---|---|
| RAM | 384 MB | 365 MB (48 bytes per row) | none | 0 |
| Disk | 820–881 MB | 364 MB | 436–497 MB | 1–2 |

## Claims Checked

| Claim | Source | Measured here | Verdict |
|---|---|---|---|
| About 1.22 GB of RAM per GB of log, 62% less than 2.12.1 at the peak | `Doc/Research/Weevil3/MemoryHarness/README.md` (2026-11-13) | 1.16 after opening, 1.22 at the peak; 4.9 GB against 12.9 GB | **Confirmed** |
| 4 GB log: first open about 26–31 s, reopen 8.1 s, after 1% growth 11.7 s | Same, 2026-11-14 | 29 s, 8.0 s, 11.4 s | **Confirmed** |
| Disk keeps only the record table, about 22 bytes per record | `Doc/Design/DiskMode.md:10`; `Help.md:79`; `Gui/IO/OpenAsAdvice.cs:63` | 22.3 bytes per record at rest; the `.idx` is 22.1 bytes per record. While filtering, far more (P-1) | **True at rest only** |
| The 1 GB, 10-million-record log reopens on disk in 0.9 s, against 7.6 s for a first open | `Doc/Design/DiskMode.md:31` | 1.3–1.5 s for the 1 GB, 7.5-million-record log; a first open on disk took 6.0–7.1 s | **Consistent** (different log and machine) |
| Saving the table adds about 1 s to a first open (9.2–9.5 s against 8.4 s) | `Doc/Design/RecordTable.md:50-51` | No measurable cost: 7.0–7.3 s with the save, 7.3–7.8 s without | **Not reproduced**; the cost is lower than claimed |
| A filter creates one record per row; about 0.15–0.2 s per 10 million records | `.github/instructions/core.instructions.md` (Performance) | 48 bytes of `Record` per row per filter, in both modes | **Consistent** (time not isolated) |
| Filters faster than 2.12.1 | Epic #988 goal | 4–24× faster on the 1 GB log, the same counts | **Confirmed** |
| A 20 GB log filtered in under 10 s from an NVMe drive | `Doc/Research/Weevil3/Feasibility.md` | The 4 GB log on disk from the page cache: 1.3–3.3 s per filter. Linear to 20 GB: 6.5–16 s on 4 vCPUs, before any disk reads | **Unverified** (#1024 is open) |
| Opening needs the log's size plus 34 bytes per line in RAM | `Core/IO/MemoryEstimator.cs:18`, `:46` | Estimate for the 4 GB log: 5.0 GB. Peak while opening: 4.9 GB. Peak in use: 6.5 GB (P-3) | **True for opening** |

## Findings

### P-1. A filter on disk churns through 16 MB buffers, so it needs far more memory than the table

- **Priority:** Medium
- **Kind:** RAM
- **New in 3.0?** New design
- **Evidence:**
  - **Measured.** One plain filter on the 1 GB log allocated 436–497 MB of large `byte[]` on disk, and none in RAM. On the 4 GB log it allocated 1,349 MB. It forced one or two full (gen 2) collections, against none in RAM ([FilterBuffers-4gb.md](../Measurements/FilterBuffers-4gb.md)).
  - **Footprint.** After eight filters and a Find, the 4 GB log's working set on disk was 4.8 GB, against 0.69 GB after opening. The managed heap after a full collection was still 0.67 GB, so the excess is garbage the GC has not returned ([Ops-4gb-main-disk-run2.txt](../Measurements/Ops-4gb-main-disk-run2.txt)).
  - **Under a cap.** With the managed heap capped at 1.5 GB, all filters finished, 20–50% slower. At 1 GB, two of the eight filters on the 4 GB log threw `OutOfMemoryException` ([Ops-4gb-disk-heap-capped.txt](../Measurements/Ops-4gb-disk-heap-capped.txt)).
  - **Cause, confirmed with a counter in a throwaway build.**
    - `Parallel.ForEach` created 18 thread-local states for 4 workers on the 1 GB log, and 47 on the 4 GB log (`Src/BlueDotBrigade.Weevil.Core/Filter/FilterManager.cs:163`).
    - Each state holds a new `ContentBuffer` (`Filter/FilterMapReduceState.cs:19`).
    - Each buffer's first whole-chunk read allocates a new array the size of the chunk (`Src/BlueDotBrigade.Weevil.Common/Data/ContentBuffer.cs:120-123`).
    - Reused buffers would still grow, because chunks differ in length by a few bytes.
  - **Already found in review.** U3-2 found the effect and suspected this cause; this confirms it.
- **Impact:** Disk mode exists for logs that don't fit in RAM. The documents, Help and the Open As window promise 22 bytes per record. While filtering, a log on disk needs the table plus 0.5–0.8 GB on this machine, and every filter adds large-object garbage in proportion to the log's size. Two costs follow: full collections that pause every thread, and a working set that grows towards the log's size until the GC is pressed to return memory. The footprint on a Windows machine under real memory pressure is unmeasured.
- **Recommendation:**
  - **Pool the buffers per filter, capped at the worker count.** Create the content buffers once for the filter, take one in `localInit` and give it back in `localFinally`.
  - **Size them so they don't regrow:** the largest chunk, or the 16 MB chunk size plus room for one record. Don't round up to a power of two: the prototype did, which doubled the live footprint and made the tightest cap worse.
  - **Pin it** with an allocation budget in the disk filter test (`Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/Filter/FilterManagerTests.cs`).
  - **Say in `DiskMode.md`** that a filter needs one chunk per worker, beyond the table.
  - **Expected benefit,** measured with the prototype ([Prototypes/FilterBufferPool.patch](../Measurements/Prototypes/FilterBufferPool.patch)): large-object allocation per filter becomes constant (about 200 MB) instead of growing with the log, and the full collections stop.
  - **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### P-2. Changing storage mode costs a full parse

- **Priority:** Medium
- **Kind:** CPU
- **New in 3.0?** New design (deliberate)
- **Evidence:** On the 1 GB log, a reopen in the same mode took 2.1–2.4 s (RAM) and 1.3–1.5 s (Disk). Reopening in the other mode took 6.0–6.8 s, close to a first open's 7.0–7.8 s, and rewrote the 167 MB `.idx` ([Reopen-1gb.txt](../Measurements/Reopen-1gb.txt)). U1-7 explains why: the storage mode is part of a saved parse's identity (`Src/BlueDotBrigade.Weevil.Core/IO/SavedParse.cs:98`), although for UTF-8 and native UTF-16 the table is the same in both modes.
- **Impact:** The user #986 serves opens a large log, finds it too big for RAM, and reopens it on disk. That user pays a full parse, about 3 to 4 times a reopen, at the moment memory is already short. On a 20 GB log that is minutes instead of seconds.
- **Recommendation:** As U1-7: compare the mode only for logs whose encoding makes the table differ. This needs the owner's agreement (decision 6 of the #1023 plan). **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### P-3. The RAM figures describe opening, not use, and the advice sizes RAM from them

- **Priority:** Low
- **Kind:** RAM
- **New in 3.0?** New design
- **Evidence:**
  - **Measured.** In RAM, the 4 GB log's working set grew from 4.69 GB after opening to 6.34 GB after eight filters and a Find, with a peak of 6.50 GB. The live heap stayed at 4.67 GB. The 1 GB log grew from 1.22 GB to 1.37 GB, with a peak of 1.79 GB ([Ops-4gb-main-ram-run2.txt](../Measurements/Ops-4gb-main-ram-run2.txt), [Ops-1gb-run1.txt](../Measurements/Ops-1gb-run1.txt)).
  - **The advice.** It estimates the log's size plus 34 bytes per line: 5.0 GB for the 4 GB log (`Src/BlueDotBrigade.Weevil.Core/IO/MemoryEstimator.cs:18`, `:46`). It suggests RAM when that, plus a fixed 1 GB reserve, fits in the available memory (`Src/BlueDotBrigade.Weevil.Gui/IO/OpenAsAdvice.cs:20`, `:52`). The 1 GB reserve covered the 1.5 GB of growth here, with 0.5 GB to spare.
  - **Where the growth comes from.** Each filter allocates 48 bytes of `Record` per row (1.5 GB on the 4 GB log). That's the documented cost of records created on demand. The GC keeps part of it as working set.
- **Impact:** None measured at 4 GB. Whether a fixed 1 GB reserve still covers the growth for a 16 GB log is unknown. If it doesn't, the window suggests RAM for a log that then pages while being filtered.
- **Recommendation:** Measure the working set over a few filters on the largest log the owner's machines open in RAM. If the growth scales with the log, make the reserve a share of the estimate. Say in `DiskMode.md` what the figures measure. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### P-4. A log directly in the temporary folder is not recognised as temporary

- **Priority:** Low
- **Kind:** Correctness
- **New in 3.0?** New design (the helper was added in #977)
- **Evidence:**
  - **The comparison is off by a separator.** `FileHelper.IsInTemporaryDirectory` compares `Path.GetDirectoryName(path)`, which has no trailing separator (`/tmp`, `C:\Users\x\AppData\Local\Temp`), with `Path.GetTempPath()`, which has one (`/tmp/`, `...\Temp\`). It is false for a file directly in the temporary folder, and true only for files in a subfolder (`Src/BlueDotBrigade.Weevil.Common/IO/FileHelper.cs:16-29`).
  - **Seen happening.** Opening `/tmp/Weevil.MemoryHarness.v1.1000MB.131B.log` saved a 167 MB `/tmp/...log.idx`, and the next open reused it: 2.8 s instead of 7.3 s ([Reopen-1gb.txt](../Measurements/Reopen-1gb.txt) notes).
  - **Two callers.** The same helper decides whether the sidecar is saved (`Src/BlueDotBrigade.Weevil.Core/Configuration/Sidecar/SidecarManager.cs:143`) and whether the `.idx` is saved (`Src/BlueDotBrigade.Weevil.Core/IO/LineIndexFile.cs:145`). No test covers it.
  - **Who is affected.** The repo's benchmarks keep their logs in a subfolder, so they aren't. The memory harness's default folder is the temporary folder itself (`Doc/Research/Weevil3/MemoryHarness/Program.cs:543`), and its README says Weevil saves no index there, which is wrong. Its first-open figures still hold, because it deletes the `.idx` first.
- **Impact:** Small. Zips extract to a subfolder, so the main case works. A log saved straight into `%TEMP%` gets an `.idx` and a sidecar beside it, against the stated rule. The `OrdinalIgnoreCase` comparison is also wrong on Linux, where paths are case-sensitive.
- **Recommendation:** Compare against `Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()))` with a separator appended to both sides, or test `Path.GetRelativePath`. Add a unit test for a file directly in the folder and one in a subfolder. Fix the harness README. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### P-5. Find got no faster, and on a large log it is slower than a whole filter

- **Priority:** Low
- **Kind:** CPU
- **New in 3.0?** Existing weakness (Find stays on one thread by decision; see U5b-3)
- **Evidence:** Find for the last record's text took 0.53 s on `main` against 0.45 s on 2.12.1 for the 1 GB log in RAM, and 0.73 s on disk. On the 4 GB log it took 2.0 s in RAM and 2.9 s on disk, while a plain filter took 1.5 s and 1.7 s.
- **Impact:** Find is now the slowest common operation on a large log, and it's the one users repeat.
- **Recommendation:** See U5b-3. **Complexity:** M.
- **Sceptic:** Not challenged (Low)

## Not Measured: For the Owner on Windows

| Gap | Why it matters | How |
|---|---|---|
| A 20 GB log on disk, from NVMe and from SATA | The disk-mode target (#1024 is open) | The harness with `--size-mb 20000`; OpsHarness with `disk` |
| Disk mode with the page cache cold | Every disk figure here read from the cache | Restart, or open a log larger than RAM, then filter once |
| Disk mode under real memory pressure (P-1) | The footprint while filtering, where disk mode matters | Filter a log on disk on a machine whose free RAM is near the table's size; watch the working set and the time |
| A network share | Reads open the log for each chunk, and Find and Copy read per record (U2-1) | OpsHarness on a log on a share, in both modes |
| GUI responsiveness | F5 filters on the UI thread (U6a-3); Copy reads per record | Time F5 and Copy on a 4 GB log on disk in the WPF app |
| 2.12.1 filters at 4 GB | Not run here: 2.12.1 needs 13 of this machine's 15 GB | OpsHarness built with `-p:WeevilVersion=2.12.1` |
| A grown reopen, split into reparse and rewrite (U1-11) | Each Reload of a live log rewrites the whole table | The durations in Weevil's own log for "the new part was parsed" |
| The last region of a log by name, against by start line (U1-8) | The CLI and PowerShell parse from line 1 | The CLI with `--region` on the 10-million-record log |

## Open Questions

- **Should Find use the filter's parallel scan?** Find now scans more slowly than a filter (see U5b-3 for why it stays on one thread).
- **Are budget tests needed for use, not only for opening?** The memory budget tests and the documents measure memory after opening. A budget for a filter, especially on disk, would have caught P-1.
