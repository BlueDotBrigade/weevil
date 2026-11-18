# Weevil memory after opening a log (#978)

| | |
|---|---|
| Weevil | NuGet package 2.12.1: Core 2.12.1+Release |
| Measured | 2026-11-17, options `--size-mb 1000,4000 --log-folder /home/user/bench-logs --estimate-gb 8 --ram-percent 99 --report <scratch>/results/memory-2.12.1-run2.md` |
| Machine | 4 logical processors, 14.2 GB of RAM available to .NET, 9.2 GB of it free when the run started, Ubuntu 24.04.5 LTS |
| .NET | .NET 10.0.15, workstation garbage collector |

## Measured

- **Before opening:** the working set of the process with Weevil loaded, before it opens the log.
- **Working set:** the RAM the process holds after opening the log, as Task Manager shows it.
- **Peak:** the most it held while opening.
- **Managed heap:** the .NET objects in that working set.
- **Too big:** opening was stopped when the working set passed 9.1 GB: 99% of the RAM free when the run started.

| Log | Case | Records | Bytes per record | Open | Before opening | Managed heap | Working set | Peak |
|---|---|---|---|---|---|---|---|---|
| 1,000 MB | First open | 7,547,742 | 132 | 27.1 s | 0.0 GB | 3.0 GB | 3.3 GB | 3.3 GB |
| 4.0 GB | First open | | | stopped after 66 s | | | | **Too big:** over 9.1 GB |

## Per GB of Log

From the 1,000 MB log: **3.31 GB** of working set per GB of log after opening, and **3.32 GB** at the peak while opening, on top of 0.03 GB before opening.

## Estimated

**These are estimates, not measurements:** the memory before opening, plus the figures per GB above multiplied by the log's size, for records of the same average size.
The last column is the smallest common amount of RAM that holds the peak. Windows and other applications need some of it too.

| Log | Working set after opening (estimate) | Peak while opening (estimate) | RAM that holds the peak |
|---|---|---|---|
| 4 GB | 13.3 GB | 13.3 GB | 16 GB |
| 8 GB | 26.6 GB | 26.6 GB | 32 GB |
