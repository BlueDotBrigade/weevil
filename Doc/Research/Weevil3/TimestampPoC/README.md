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

### Smoke Test: 2026-10-20

On a 4-vCPU Linux VM with .NET 10, using a 100 MB synthetic log, scaled to 20 GB:

| | Extra at open | `@Elapsed` filter, all records |
|---|---|---|
| Plugin parse | +115 s | 91 s |
| Span parse | +51 s | 46 s |
| Fast parse | +9.5 s | 5.4 s |

The answer was to keep the column: even the fast parse added more than 5 s at open. The decision waits for the run on the developer PC.
