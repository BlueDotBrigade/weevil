# Audit Measurements

Raw results for stage 2 of the [Weevil 3.0 audit](../Progress.md). `Findings/Performance.md` interprets them.

> Measured on 2026-11-16 and 2026-11-17 on a Linux cloud container: 4 vCPUs, 15 GB of RAM (14.2 GB available to .NET), Ubuntu 24.04, .NET 10.0.15 (SDK 10.0.115 from Ubuntu's archive). **Linux figures:** compare 2.x with 3.0 only within this machine; Windows timings will differ.

## Versions

| Name | What | Source |
|---|---|---|
| 2.12.1 | Weevil 2.12.1 Core | NuGet package `BlueDotBrigade.Weevil.Core` 2.12.1 |
| `main` | Weevil 3.0 Core | `main` at `e3dee822`, built in Release |

## Logs

Synthetic TSV logs written by the [memory harness](../../MemoryHarness/README.md): 131 bytes per record on average.

| Log | Records | Last record |
|---|---|---|
| 1,000 MB | 7,547,742 | `sequence=7547741` |
| 4.0 GB | 30,014,396 | `sequence=30014395` |

4 GB is the largest log this environment can open (owner, 2026-11-16).

## Memory: Opening a Log

The [memory harness](../../MemoryHarness/README.md), unchanged, built against each version:

```bash
cd Doc/Research/Weevil3/MemoryHarness
dotnet run -c Release -p:WeevilVersion=2.12.1 -- --size-mb 1000,4000 --log-folder <folder> --ram-percent 99 --report Memory-2.12.1.md
dotnet run -c Release -- --size-mb 1000,4000 --log-folder <folder> --reopen --report Memory-Main.md
```

| File | Notes |
|---|---|
| [Memory-2.12.1.md](Memory-2.12.1.md) | Valid. 13.6 GB free at the start |
| [Memory-Main.md](Memory-Main.md) | Valid for memory and reopen times. The first-open times are slow: the logs were not yet in the page cache after a restart |
| [Memory-Main-Run2.md](Memory-Main-Run2.md) | Valid. Repeats the first opens with the logs cached |
| [Memory-2.12.1-Run2.md](Memory-2.12.1-Run2.md) | 1 GB valid. **4 GB invalid:** only 9.2 GB was free at the start, because the page cache held the logs, so the harness stopped it at 9.1 GB. Use [Memory-2.12.1.md](Memory-2.12.1.md) for 4 GB |

## Time: Filter and Find

[OpsHarness](OpsHarness/) opens a log, then times operations through the public API: the median of 5 runs, the managed bytes allocated per run, and on Linux the peak resident memory while each operation ran. It is written for this audit and is not part of `Weevil.sln`. The same source builds against both versions; `WEEVIL2` selects 2.12.1's API where it differs.

```bash
cd Doc/Research/Weevil3/Audit/Measurements/OpsHarness
dotnet build -c Release                                   # main
dotnet build -c Release -p:WeevilVersion=2.12.1 -o bin2x  # 2.12.1
DOTNET_GCHeapHardLimitPercent=0x63 dotnet bin/Release/net10.0/OpsHarness.dll <log> 5 ram|disk sequence=<last>
```

Results: `Ops-*.txt` (one line per operation: name, median seconds, MB allocated, result count, min, max, peak).
