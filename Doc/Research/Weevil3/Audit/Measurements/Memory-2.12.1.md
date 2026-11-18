# Weevil memory after opening a log (#978)

| | |
|---|---|
| Weevil | NuGet package 2.12.1: Core 2.12.1+Release |
| Measured | 2026-11-16, options `--size-mb 1000,4000 --log-folder <scratch>/logs --estimate-gb 8,16,20,24 --ram-percent 99 --report <scratch>/results/memory-2.12.1.md` |
| Machine | 4 logical processors, 14.2 GB of RAM available to .NET, 13.6 GB of it free when the run started, Ubuntu 24.04.5 LTS |
| .NET | .NET 10.0.15, workstation garbage collector |

## Measured

- **Before opening:** the working set of the process with Weevil loaded, before it opens the log.
- **Working set:** the RAM the process holds after opening the log, as Task Manager shows it.
- **Peak:** the most it held while opening.
- **Managed heap:** the .NET objects in that working set.
- **Too big:** opening was stopped when the working set passed 13.5 GB: 99% of the RAM free when the run started.

| Log | Case | Records | Bytes per record | Open | Before opening | Managed heap | Working set | Peak |
|---|---|---|---|---|---|---|---|---|
| 1,000 MB | First open | 7,547,742 | 132 | 22.6 s | 0.0 GB | 3.0 GB | 3.4 GB | 3.4 GB |
| 4.0 GB | First open | 30,014,396 | 133 | 90.4 s | 0.0 GB | 12.0 GB | 12.2 GB | 12.9 GB |

## Per GB of Log

From the 4.0 GB log: **3.03 GB** of working set per GB of log after opening, and **3.21 GB** at the peak while opening, on top of 0.03 GB before opening.

## Estimated

**These are estimates, not measurements:** the memory before opening, plus the figures per GB above multiplied by the log's size, for records of the same average size.
The last column is the smallest common amount of RAM that holds the peak. Windows and other applications need some of it too.

| Log | Working set after opening (estimate) | Peak while opening (estimate) | RAM that holds the peak |
|---|---|---|---|
| 8 GB | 24.3 GB | 25.7 GB | 32 GB |
| 16 GB | 48.6 GB | 51.5 GB | 64 GB |
| 20 GB | 60.7 GB | 64.3 GB | 128 GB |
| 24 GB | 72.8 GB | 77.2 GB | 128 GB |
