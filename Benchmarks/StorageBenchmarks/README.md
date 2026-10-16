# Weevil storage benchmark — exploratory only

Compares the **same regex** against **the same complete records**:

1. **Text:** `File.ReadAllText` once; keep one UTF-16 string and lightweight record offsets. Regex runs on `ReadOnlySpan<char>` slices (no per-record strings).
2. **Bytes:** `File.ReadAllBytes` once; keep original UTF-8 bytes and lightweight byte offsets. Each filtering pass decodes each record to a **reused pooled UTF-16 char buffer**, then runs the same regex. No per-record string allocation.
3. **Weevil:** Use **the actual** internal `MultilineRecordParser` and `TsvRecordParser` in the referenced Weevil Core assembly (called via a cached delegate). Keep one `IRecord` per record; regex runs on `Record.Content`.

The lightweight indexer recognizes five-column Weevil TSV headers and supports record continuations. Synthetic data is **single-line only**. This is *not* a production parser. For real multiline logs, Weevil normalizes continuation lines to CRLF; the index cases retain their original line endings, so newline-sensitive regexes may produce differing matches. The profiling tool checks record and match counts and errors out on differences.

## Setup

Place this `Benchmarks/StorageBenchmarks/` folder at the **root of your Weevil git checkout** so that the project reference resolves. The program requires .NET 9 SDK, an x64 environment and the `BenchmarkDotNet` NuGet package (restored automatically by `dotnet restore`). Do **not** add the harness to the Weevil shipping application.

```powershell
cd Benchmarks/StorageBenchmarks
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

## Read the results correctly

- `profile` reports **elapsed seconds**, **process CPU seconds**, **allocated MB**, **managed heap retained after full GC**, **private bytes**, **peak working set**, and results from repeated filters. In profile `--case all`, **each case runs as a separate process** so its large allocations do not contaminate another case's memory measurements.
- BenchmarkDotNet `MemoryDiagnoser` reports **total allocated bytes and GC activity**, **not live/retained RAM**. `profile` fills that gap. CPU seconds are the *time spent on CPU*, not hardware CPU utilization percent; they can exceed wall time if work is parallel.
- Initial loading is measured separately from filtering because analysts typically filter many times after opening a file.
- The same compiled regex (`Regex.IsMatch(ReadOnlySpan<char>)`) is reused across all cases. The byte case pays UTF-8-to-UTF-16 decoding **on every filtering pass**; the text case pays decoding only during initial load; Weevil stores already decoded per-record strings.
- A ~1 GB mostly-ASCII UTF-8 log produces a nearly **2 GB UTF-16 string**. A single .NET string has a practical maximum of about 1 billion UTF-16 code units. Running this at 1 GB requires a **64-bit process with substantial RAM** (possibly >8–16 GB when considering indexing, transient allocations and Weevil's record objects). If a mode fails with OOM, that is a useful feasibility result; retry with `--size-mb 100` to inspect relative performance.
- Other sources of variability include OS file cache, disk speed, regex selectivity and record length. Run more than once and compare trends, not single measurements. BenchmarkDotNet is configured for only three iterations to make 1 GB runs practical.
- Byte offsets can be persisted in a future design, but index persistence, disk-backed storage and production optimizations are deliberately **out of scope**.
- This implementation **does not benchmark actual peak CPU %** via OS performance counters. It reports process CPU time instead, which is a more stable CPU cost measure for this exploration.

## Known constraints

- The real input must be valid UTF-8 and compatible with Weevil's current TSV parser; a different plug-in/parser needs a corresponding indexer and parser choice. The byte pathway uses strict UTF-8; errors are surfaced rather than silently replacing characters.
- The comparison includes *record indexing* for both contiguous representations, but no `Record` object is constructed for those two cases. The Weevil baseline runs the actual existing parser (not a reimplementation).
- All three cases filter records separately. Matching against a single regex over the entire file would have different semantics and is intentionally **not** used.
- Multiline handling is exploratory; custom/newline-sensitive regexes or malformed real input might cause differing match counts, which the profiler will detect.
- No persisted record index, disk-backed mapped memory, UI virtualization, production cache, or final architecture is provided.
