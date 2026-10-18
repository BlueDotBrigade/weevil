# Weevil storage benchmark — exploratory only

Compares the **same regex** against **the same complete records**:

1. **Text:** `File.ReadAllText` once; keep one UTF-16 string and lightweight record offsets. Regex runs on `ReadOnlySpan<char>` slices (no per-record strings).
2. **Bytes:** `File.ReadAllBytes` once; keep original UTF-8 bytes and lightweight byte offsets. Each filtering pass decodes each record to a **reused pooled UTF-16 char buffer**, then runs the same regex. No per-record string allocation.
3. **Weevil:** Use unmodified copies of Weevil's `MultilineRecordParser` and `TsvRecordParser` (see [Copied Weevil classes](#copied-weevil-classes)). Keep one `IRecord` per record; regex runs on `Record.Content`.

The lightweight indexer recognizes five-column Weevil TSV headers and supports record continuations. Synthetic data is **single-line only**. This is *not* a production parser. For real multiline logs, Weevil normalizes continuation lines to CRLF; the index cases retain their original line endings, so newline-sensitive regexes may produce differing matches. The profiling tool checks record and match counts and errors out on differences.

## Setup

The project is self-contained: it does not reference any Weevil project and is not part of `Weevil-v2.sln`. It requires the .NET 9 SDK, an x64 environment and the `BenchmarkDotNet` NuGet package (restored automatically by `dotnet restore`). Do **not** add the harness to the Weevil shipping application.

```powershell
cd Doc/Design/Concept/StorageBenchmarks
dotnet restore
# Fast smoke test: 10 MB, three approaches, verifies equivalent results
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

To check whether a copy has drifted from Weevil, run from the repository root, for example:

```powershell
git diff --no-index Src/BlueDotBrigade.Weevil.Core/Data/TsvRecordParser.cs Doc/Design/Concept/StorageBenchmarks/WeevilCopies/TsvRecordParser.cs
```

## Read the results correctly

- `profile` reports **elapsed seconds**, **process CPU seconds**, **allocated MB**, **managed heap retained after full GC**, **private bytes**, **peak working set**, and results from repeated filters. In profile `--case all`, **each case runs as a separate process** so its large allocations do not contaminate another case's memory measurements.
- BenchmarkDotNet `MemoryDiagnoser` reports **total allocated bytes and GC activity**, **not live/retained RAM**. `profile` fills that gap. CPU seconds are the *time spent on CPU*, not hardware CPU utilization percent; they can exceed wall time if work is parallel.
- Initial loading is measured separately from filtering because analysts typically filter many times after opening a file.
- The same compiled regex (`Regex.IsMatch(ReadOnlySpan<char>)`) is reused across all cases. The byte case pays UTF-8-to-UTF-16 decoding **on every filtering pass**; the text case pays decoding only during initial load; Weevil stores already decoded per-record strings.
- The Text case's load time is dominated by `File.ReadAllText`, not by UTF-8 decoding. In separate repeated timings of the 1 GB file, `File.ReadAllText` took 5.6–15.9 s, while `File.ReadAllBytes` followed by one `Encoding.GetString` call took 1.7–1.9 s.
- A ~1 GB mostly-ASCII UTF-8 log produces a nearly **2 GB UTF-16 string**. A single .NET string has a practical maximum of about 1 billion UTF-16 code units. Running this at 1 GB requires a **64-bit process with substantial RAM** (possibly >8–16 GB when considering indexing, transient allocations and Weevil's record objects). If a mode fails with OOM, that is a useful feasibility result; retry with `--size-mb 100` to inspect relative performance.
- Other sources of variability include OS file cache, disk speed, regex selectivity and record length. Run more than once and compare trends, not single measurements. BenchmarkDotNet is configured for only three iterations to make 1 GB runs practical.
- Byte offsets can be persisted in a future design, but index persistence, disk-backed storage and production optimizations are deliberately **out of scope**.
- This implementation **does not benchmark actual peak CPU %** via OS performance counters. It reports process CPU time instead, which is a more stable CPU cost measure for this exploration.

## Known constraints

- The real input must be valid UTF-8 and compatible with Weevil's current TSV parser; a different plug-in/parser needs a corresponding indexer and parser choice. The byte pathway uses strict UTF-8; errors are surfaced rather than silently replacing characters.
- The comparison includes *record indexing* for both contiguous representations, but no `Record` object is constructed for those two cases. The Weevil baseline runs unmodified copies of the existing parser (not a reimplementation).
- The Weevil case measures the parser only. Opening a file in Weevil also copies the record array, forces full garbage collections and applies an initial filter, none of which is measured here.
- All three cases filter on a single thread. Weevil itself filters with `Parallel.ForEach`.
- All three cases filter records separately. Matching against a single regex over the entire file would have different semantics and is intentionally **not** used.
- Multiline handling is exploratory; custom/newline-sensitive regexes or malformed real input might cause differing match counts, which the profiler will detect.
- No persisted record index, disk-backed mapped memory, UI virtualization, production cache, or final architecture is provided.

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
