# Weevil storage benchmark — exploratory only

Compares the **same regex** against **the same complete records**:

1. **Text:** `File.ReadAllText` once; keep one UTF-16 string and lightweight record offsets. Regex runs on `ReadOnlySpan<char>` slices (no per-record strings).
2. **Bytes:** `File.ReadAllBytes` once; keep original UTF-8 bytes and lightweight byte offsets. Each filtering pass decodes each record to a **reused pooled UTF-16 char buffer**, then runs the same regex. No per-record string allocation.
3. **Weevil:** Use unmodified copies of Weevil's `MultilineRecordParser` and `TsvRecordParser` (see [Copied Weevil classes](#copied-weevil-classes)). Keep one `IRecord` per record; regex runs on `Record.Content`.
4. **Disk:** Keep only where each record starts; every filtering pass reads the bytes from the drive, in parallel. See [Disk case](#disk-case-1024).

The lightweight indexer recognizes five-column Weevil TSV headers and supports record continuations. Synthetic data is **single-line only**. This is *not* a production parser. For real multiline logs, Weevil normalizes continuation lines to CRLF; the index cases retain their original line endings, so newline-sensitive regexes may produce differing matches. The profiling tool checks record and match counts and errors out on differences.

## Setup

The project is self-contained: it does not reference any Weevil project and is not part of `Weevil-v2.sln`. It requires the .NET 10 SDK, an x64 environment and the `BenchmarkDotNet` NuGet package (restored automatically by `dotnet restore`). Do **not** add the harness to the Weevil shipping application.

```powershell
cd Doc/Research/LargeLogFiles/StorageBenchmarks
dotnet restore
# Fast smoke test: 10 MB, four approaches, verifies equivalent results
dotnet run -c Release -- --size-mb 10
# Real experiment: 1,000,000,000 bytes, five filter passes, fresh process per case
dotnet run -c Release -- --mode profile --size-mb 1000 --filters 5
# BenchmarkDotNet timing and allocations, run in isolated benchmark processes
dotnet run -c Release -- --mode bdn --bench filter --size-mb 1000
# Use a real log file (UTF-8, Weevil five-column TSV)
dotnet run -c Release -- --mode profile --file "C:\Logs\my.log" --regex "ERROR_CODE_42"
# If needed, profile a single approach in a fresh process
dotnet run -c Release -- --mode profile --case Bytes --file "C:\Logs\my.log"
```

`--mode bdn --bench load` benchmarks **file read + index** (or full Weevil parse) from disk; `--bench filter` benchmarks **only filtering** after one-time setup. `--bench both` runs both groups. `--regex`, `--file`, `--filters`, `--size-mb`, `--case`, and `--help` are supported. Default input is a persistent generated UTF-8 TSV log file in the system temporary directory; its size is **1,000 decimal MB**, slightly under 1 GiB. Reuse keeps generation out of timings.

## Copied Weevil classes

`WeevilCopies/` holds byte-identical copies of the Weevil source files the parser needs, taken at commit `a68e263`. They are copied rather than referenced so the benchmark builds and runs on its own. `NullLog.cs` stands in for Weevil's `Diagnostics.Log` and discards messages.

| Copy | Source |
|---|---|
| `IRecord.cs`, `Record.cs`, `Metadata.cs`, `IRecordParser.cs`, `SeverityType.cs`, `SeverityTypeHelpers.cs` | `Src/BlueDotBrigade.Weevil.Common/Data/` |
| `LogSeverityType.cs` | `Src/BlueDotBrigade.Weevil.Common/Diagnostics/` |
| `MultiLineRecordParser.cs`, `TsvRecordParser.cs` | `Src/BlueDotBrigade.Weevil.Core/Data/` |
| `LineIndex.cs`, taken at commit `1c3ae16e` for the disk case | `Src/BlueDotBrigade.Weevil.Core/IO/` |

To check whether a copy has drifted from Weevil, run from the repository root, for example:

```powershell
git diff --no-index Src/BlueDotBrigade.Weevil.Core/Data/TsvRecordParser.cs Doc/Research/LargeLogFiles/StorageBenchmarks/WeevilCopies/TsvRecordParser.cs
```

## Read the results correctly

- `profile` reports **elapsed seconds**, **process CPU seconds**, **allocated MB**, **managed heap retained after full GC**, **private bytes**, **peak working set**, and results from repeated filters. In profile `--case all`, **each case runs as a separate process** so its large allocations do not contaminate another case's memory measurements.
- BenchmarkDotNet `MemoryDiagnoser` reports **total allocated bytes and GC activity**, **not live/retained RAM**. `profile` fills that gap. CPU seconds are the *time spent on CPU*, not hardware CPU utilization percent; they can exceed wall time if work is parallel.
- Initial loading is measured separately from filtering because analysts typically filter many times after opening a file.
- The same compiled regex (`Regex.IsMatch(ReadOnlySpan<char>)`) is reused across all cases. The byte case pays UTF-8-to-UTF-16 decoding **on every filtering pass**; the text case pays decoding only during initial load; Weevil stores already decoded per-record strings.
- The Text case's load time is dominated by `File.ReadAllText`, not by UTF-8 decoding. In separate repeated timings of the 1 GB file, `File.ReadAllText` took 5.6–15.9 s, while `File.ReadAllBytes` followed by one `Encoding.GetString` call took 1.7–1.9 s.
- A ~1 GB mostly-ASCII UTF-8 log produces a nearly **2 GB UTF-16 string**. A single .NET string has a practical maximum of about 1 billion UTF-16 code units. Running this at 1 GB requires a **64-bit process with substantial RAM** (possibly >8–16 GB when considering indexing, transient allocations and Weevil's record objects). If a mode fails with OOM, that is a useful feasibility result; retry with `--size-mb 100` to inspect relative performance.
- Other sources of variability include OS file cache, disk speed, regex selectivity and record length. Run more than once and compare trends, not single measurements. BenchmarkDotNet is configured for only three iterations to make 1 GB runs practical.
- Byte offsets can be persisted in a future design, but index persistence and production optimizations are deliberately **out of scope**. The Disk case measures reading from the drive, not Weevil's disk mode itself.
- This implementation **does not benchmark actual peak CPU %** via OS performance counters. It reports process CPU time instead, which is a more stable CPU cost measure for this exploration.

## Known constraints

- The real input must be valid UTF-8 and compatible with Weevil's current TSV parser; a different plug-in/parser needs a corresponding indexer and parser choice. The byte pathway uses strict UTF-8; errors are surfaced rather than silently replacing characters.
- The comparison includes *record indexing* for both contiguous representations, but no `Record` object is constructed for those two cases. The Weevil baseline runs unmodified copies of the existing parser (not a reimplementation).
- The Weevil case measures the parser only. Opening a file in Weevil also copies the record array, forces full garbage collections and applies an initial filter, none of which is measured here.
- The Text, Bytes and Weevil cases filter on a single thread; the Disk case on the threads `--threads` gives. Weevil itself filters with `Parallel.ForEach`.
- All cases filter records separately. Matching against a single regex over the entire file would have different semantics and is intentionally **not** used.
- Multiline handling is exploratory; custom/newline-sensitive regexes or malformed real input might cause differing match counts, which the profiler will detect.
- No persisted record index, memory-mapped storage, UI virtualization, production cache, or final architecture is provided.

## Disk case (#1024)

Step 1 of the [#986 plan](../../../Plans/986-DiskMode.md#steps). **Question:** can a filter read a 20 GB log from the drive in under 10 s on NVMe, how slow is SATA, how many threads should read, and how long does a screen of rows take through the line index? Requirement #1088 sets the 10 s target.

### What it measures

- **Opening** reads the log once and keeps only where each record starts: 8 bytes per record, about 1.2 GB for a 20 GB log. This is not part of any filter time.
- **Each filtering pass** reads the whole log from the drive, as disk mode will:
  - in chunks of about 16 MB (`--chunk-kb`) that end at a record start, like Weevil's content chunks, handed to the threads as they become free;
  - with ordinary reads (`RandomAccess.Read`), sharing the log for reading, writing and deletion, and never memory-mapped;
  - decoding each record and matching it with the thread's own compiled `Regex`, or with Weevil's case-sensitive plain-text rule (`--text`).
- **`--threads`** lists the thread counts to measure, for example `1,2,4,8,16`. Each pass prints its seconds, MB/s and CPU seconds; every pass must find the same matches.
- **`--cache cold`** empties the operating system's file cache before each pass, so every pass reads from the drive:
  - on Windows, by emptying the standby list, which needs an elevated prompt. Without one the run stops and says so;
  - on Linux, with `posix_fadvise`.
- **A screen of rows:** after the passes, `--screens` reads of `--screen-rows` rows (default 200 of 50) at random lines, through a copy of Weevil's line index, which keeps the offset of every 64th line. It prints the median, 95th percentile and maximum, cold when `--cache cold`.
- **Not measured:** creating one record per row, which Weevil's filter does (about 14 ns each on one thread), metrics and the GUI. The engine adds those to the times measured here.

### Decision rule, set before measuring

Proposed on 2026-11-08, to confirm with the owner:

- **10 s is feasible** if a cold filter of the 20 GB log on NVMe takes **8 s or less** at the best thread count. The other 2 s leave room for creating records, which costs about 2 s of CPU for 150 million records on one thread, and less across several.
- **Reader threads:** the smallest count within 10% of the fastest.
- **SATA:** no target; the measured time decides the wording of the warning in step 5 of the plan.
- **A screen of rows** should take well under 50 ms cold, so that scrolling stays smooth.

### Run it on Windows

From an **elevated** prompt, in this folder. Each 20 GB log takes several minutes to generate, and is reused afterwards.

```powershell
# NVMe: the log is generated in the temporary folder, usually on C:
dotnet run -c Release -- --case Disk --size-mb 20000 --threads 1,2,4,8,16 --cache cold --filters 3
dotnet run -c Release -- --case Disk --size-mb 20000 --threads 8 --cache cold --filters 3 --text ERROR_CODE_42 --screens 0
# SATA: generate the log on that drive instead
dotnet run -c Release -- --case Disk --size-mb 20000 --log-folder D:\WeevilBenchmark --threads 1,2,4,8,16 --cache cold --filters 3
```

The first time, check with Sysinternals RAMMap (File Summary) that the log's standby pages are gone after a pass, so that "cold" really is cold. Record the drive models, the free space, and whether antivirus scans the folder.

### Results: 2026-11-08, Linux check of the method

On the 4-vCPU Linux VM, .NET 10. **These are not drive speeds:** the VM's disk is cached by its host, so they only check that the case is correct and that its cache emptying works.

- **Same results:** with `--case all`, all four cases found the same records and matches on a 10 MB synthetic log (76,372 and 4,774) and on the 1 GB droid log with multi-line records (10,000,000 and 546,510).
- **Beyond 4 GB:** on an 8 GB synthetic log, 61,097,852 records, both thread counts found 3,818,616 matches, one record in 16 as generated.
- **Cold is colder:** on the 1 GB synthetic log, 4 threads took 0.21 s cold and 0.11 s warm; 1 thread 0.43 s and 0.41 s.
- **A screen of 50 rows:** 0.10 ms median cold, 0.02 ms warm.
- **Memory:** 1.6 GB peak working set for the 8 GB log, mostly the record starts and the list that collected them.

## Results: 2026-10-16

Measured with `--mode profile --size-mb 1000 --filters 5` on a 4-vCPU Linux VM with 15 GB RAM, .NET 9.0.20. Identical runs on this VM varied by up to 3× in load time; filter times were steadier. Treat the numbers as relative, not absolute.

1 GB synthetic file, 7,637,231 single-line records:

| | Text | Bytes | Weevil (parser only) |
|---|---|---|---|
| Load + index | 22.1 s | 4.6 s | 31.8 s |
| Allocated during load | 4,210 MB | 1,201 MB | 12,041 MB |
| Retained heap after load | 2,101 MB | 1,101 MB | 2,996 MB |
| Peak working set | 4,265 MB | 1,237 MB | 3,193 MB |

Five filter passes, by regex:

| Regex | Text | Bytes | Weevil |
|---|---|---|---|
| `ERROR_CODE_42` | 3.04 s | 2.58 s | 3.53 s |
| `(?i)error_code_42` | 3.36 s | 3.36 s | 3.29 s |
| `ERROR_CODE_42\|sequence=07;\|Timeout\|Disconnected` | 4.12 s | 4.17 s | 4.19 s |
| `source=.*sequence=0[37];` | 4.47 s | 4.82 s | 4.47 s |

Observations:

- Keeping raw UTF-8 bytes did not make repeated filtering meaningfully slower: all three cases were within about 10% for every regex.
- Bytes retained about a third of the memory of today's parser and loaded about 7× faster.
- Today's parser allocated about 4× the memory it retained.
- In a separately timed run, about 3.7 s of the Bytes load was record indexing, mostly validating each line's TSV timestamp.
