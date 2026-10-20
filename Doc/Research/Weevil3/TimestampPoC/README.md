# Timestamp Proof of Concept

Question for #1008 (step 2 of the [#988 plan](../../../Plans/988-IndexingService.md)): should Weevil keep every record's timestamp in memory, or parse it from the log text each time it is needed?

Keeping it costs 8 bytes per record: about 1.2 GB for a 20 GB log. Parsing it costs time, every time: insights read every record's timestamp on every open, as do the `@Elapsed` filter, the graph and the edge analyzers.

## What It Measures

Four ways of reading a record's timestamp:

| Way | How |
|---|---|
| Column | Stored as today: an 8-byte value per record |
| Plugin parse | Today's plugin contract: decode the record, then Weevil's `TsvRecordParser` builds a whole record |
| Span parse | A timestamp-only parse with Weevil's rules: `DateTime.TryParseExact` on the first field |
| Fast parse | Hand-written digit parsing, falling back to the span parse for anything unexpected |

Each way runs the same workloads, and the program checks that every way gives the same answers:

- **Insights at open:** out-of-order timestamps over every record, then time gaps between UI-thread records. These are two passes, as in Weevil.
- **`@Elapsed` filter:** the time between consecutive records of a previous filter's results, for 10% and for 100% of the records.
- **Go to timestamp:** a binary search, 1,000 times.
- **Rows on screen:** elapsed times for 50 rows, at 10,000 scroll positions.
- **Parallel read:** every timestamp read once, split across threads, to show how fast a whole-log pass could parse in parallel.

The report scales the workloads that read every record to a 20 GB log, then applies the decision rule from the #988 plan: keep the column, unless parsing on demand adds under 5 s to opening a 20 GB log and keeps an `@Elapsed` filter under 10 s.

## Run It

The project is self-contained: it does not reference any Weevil project and is not part of `Weevil-v2.sln`. It needs no NuGet packages. Building it needs the .NET 10 SDK.

```powershell
cd Doc\Research\Weevil3\TimestampPoC
dotnet run -c Release
```

- The first run writes a 1,000 MB synthetic log to the temporary folder, which takes about a minute. Later runs with the same log options reuse it.
- A full run then takes a few minutes. "Plugin parse" is the slowest.
- Copy everything from `# Timestamp proof of concept` to the end, or add `--report results.md` to save it to a file.

### Options

The log:

| Option | Meaning |
|---|---|
| `--size-mb <10-2000>` | Size of the synthetic log, in MB. Default 1000. |
| `--record-bytes <70-4000>` | Average record size in the synthetic log. Default 131, as in the #978 test log. |
| `--ui-percent <0-100>` | Share of synthetic records on the UI thread. Default 20. |
| `--log-folder <path>` | Where the synthetic log is written. Default: the temporary folder. |
| `--file <path>` | Use a real Weevil TSV log instead: UTF-8, under 2 GB. |

The measurements:

| Option | Meaning |
|---|---|
| `--filter-percent <1-100>` | Share of records the previous filter kept, for the `@Elapsed` filter. Default 10. |
| `--threads <1-256>` | Threads for the parallel read. Default: the computer's logical processors. |
| `--ways <list>` | Ways to compare, from `column`, `plugin`, `span` and `fast`. The column is always included. Default all. |
| `--runs <1-20>` | Timed runs per workload; the report shows the median. Default 3. |
| `--report <path>` | Also save the report to this file. |

### Experiments

| Question | Command |
|---|---|
| Do shorter records change the answer? There are more of them per GB. | `dotnet run -c Release -- --record-bytes 80` |
| Does a real log behave like the synthetic one? | `dotnet run -c Release -- --file C:\Logs\Application.log` |
| Would parsing on 4 threads be fast enough? | `dotnet run -c Release -- --threads 4` |
| Does a narrow previous filter make `@Elapsed` cheap? | `dotnet run -c Release -- --filter-percent 1` |
| A quicker run, without the slowest way | `dotnet run -c Release -- --ways span,fast --runs 1` |
| The temporary folder is short of space | `dotnet run -c Release -- --log-folder D:\Temp` |

### On a Computer Without .NET

Build on a computer with the SDK, as a self-contained app that includes the .NET runtime:

```powershell
dotnet publish -c Release -r win-x64 --self-contained -o publish
```

Copy the `publish` folder (about 75 MB) to the other computer, then run `publish\Weevil.TimestampPoC.exe`. Nothing needs installing there.

## Limits

- Single thread, except the parallel read. The insights read records in order, so they would need to parse in a separate parallel pass first; the parallel read shows the cost of that pass.
- The column costs no time at open: the parser already parses every timestamp to recognize where a record starts.
- UTF-8 Weevil TSV logs only. The synthetic log has one line per record.
- Timings vary between runs, and between computers; compare the ways with each other, not with other benchmarks.

## Copied Weevil Classes

`WeevilCopies/` holds unmodified copies taken from `main` on 2026-10-20 (after #1004 merged). `NullLog.cs` stands in for Weevil's logger, as in the [storage benchmark](../../LargeLogFiles/StorageBenchmarks/README.md).

| File | Copied from |
|---|---|
| `IRecord.cs`, `IRecordParser.cs`, `Metadata.cs`, `Record.cs`, `SeverityType.cs`, `SeverityTypeHelpers.cs` | `Src/BlueDotBrigade.Weevil.Common/Data/` |
| `LogSeverityType.cs` | `Src/BlueDotBrigade.Weevil.Common/Diagnostics/` |
| `TsvRecordParser.cs` | `Src/BlueDotBrigade.Weevil.Core/Data/` |

## Results

### Decision: Keep the Timestamps (2026-10-20)

Weevil keeps every record's timestamp in an 8-byte column, in RAM and disk mode. The decision rule gave the same answer: even the fastest re-read added 5.2 s to opening a 20 GB log, over the 5 s limit.

Every record has a timestamp. Weevil can remember it, or look it up again each time it is needed:

- **A. Remember it (chosen; today's approach).** Weevil already parses every timestamp to find where records start, so it keeps them. It is the fastest and the simplest. It costs memory: 1.2 GB for a 20 GB log (about 5% more in RAM mode) and 3.9 GB for a 64 GB log in disk mode.
- **B. Look it up again, one record at a time.** It saves the memory, but every open of a 20 GB log takes 5.2 s longer, and every plugin needs a new "parse the timestamp only" method.
- **C. Look it up again, on every processor at once.** It is as fast as A, but much harder to build, and it needs the same memory as A while the insights run.

Timestamps are not only needed when opening. Deciding which records match a filter rarely needs them: only `@Elapsed` compares times. But after every filter, Weevil calculates the Elapsed column for every record the filter shows. With B, a filter on a 20 GB log spends up to 3.8 s on that, of its 10 s target.

### Developer PC: 2026-10-20

An i9-9900K (16 logical processors) with 32 GB of RAM and an NVMe drive, on Windows with .NET 10, using the default options: a 1,000 MB synthetic log averaging 132 bytes per record. The full report is in [Results-2026-10-20.md](Results-2026-10-20.md).

Time spent on timestamps when a log opens, scaled from the 1 GB run. Values marked `~` are estimates: the proof of concept did not read from disk, so the disk-mode times for B and C assume the drive reads about 3.3 GB/s. On a SATA SSD, multiply them by about 6.

| Log | Mode | Open | A: keep the column | B: re-read, 1 thread | C: re-read, 16 threads |
|---|---|---|---|---|---|
| 1 GB | RAM | First or re-open | <0.1 s | 0.3 s | <0.1 s |
| 20 GB | RAM | First | 0.5 s | 5.7 s | 1.0 s |
| 20 GB | RAM | Re-open | 0.9 s | 5.7 s | 1.0 s |
| 20 GB | Disk | First | 0.5 s | ~6–12 s | ~6 s |
| 20 GB | Disk | Re-open | 0.9 s | ~6–12 s | ~6 s |
| 64 GB | Disk | First | 1.5 s | ~19–37 s | ~19 s |
| 64 GB | Disk | Re-open | 2.7 s | ~19–37 s | ~19 s |

Re-opening with A loads the saved timestamps from the `.idx` file, about 0.4 s per 20 GB; nothing else is parsed. 64 GB in RAM mode does not fit in 32 GB with any approach.

| | A: keep the column | B: re-read, 1 thread | C: re-read, 16 threads |
|---|---|---|---|
| Memory for timestamps: 1, 20 and 64 GB logs | 60 MB, 1.2 GB, 3.9 GB | 0 | 0, except as much as A while the insights run |
| Elapsed times after a 20 GB filter that shows every record, RAM | 0.1 s | 3.8 s | ~0.7 s |
| The same, when the filter shows 10% of records | 0.0 s | 1.0 s | ~0.1 s |
| The same, every record, disk | 0.1 s | ~6–10 s | ~6 s |
| Source code | **Simple**: as today | **Medium**: a timestamp-only parse in every plugin, and every feature re-parses | **Complex**: B, plus parallel passes and restructured insights |

C's RAM-mode times add the parallel read to A's single pass. Today's plugin contract re-reads far more slowly than B: 87 s at open and 71 s per filter, because it builds a whole record to get a timestamp.

### Smoke Test: 2026-10-20

On a 4-vCPU Linux VM with .NET 10, using a 100 MB synthetic log, scaled to 20 GB:

| | Extra at open | `@Elapsed` filter, all records |
|---|---|---|
| Plugin parse | +115 s | 91 s |
| Span parse | +51 s | 46 s |
| Fast parse | +9.5 s | 5.4 s |
