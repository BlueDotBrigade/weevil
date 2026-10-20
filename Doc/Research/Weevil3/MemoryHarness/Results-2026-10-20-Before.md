# Weevil memory after opening a log (#978)

| | |
|---|---|
| Weevil | NuGet package 2.12.1: Core 2.12.1+Release |
| Measured | 2026-10-20, options `--size-mb 1000,4000,8000 --delete-logs` |
| Machine | 4 logical processors, 14.2 GB of RAM available to .NET, 11.9 GB of it free when the run started, Ubuntu 24.04.5 LTS |
| .NET | .NET 10.0.13, workstation garbage collector |

## Measured

- **Before opening:** the working set of the process with Weevil loaded, before it opens the log.
- **Working set:** the RAM the process holds after opening the log, as Task Manager shows it.
- **Peak:** the most it held while opening.
- **Managed heap:** the .NET objects in that working set.
- **Too big:** opening was stopped when the working set passed 10.7 GB: 90% of the RAM free when the run started.

| Log | Records | Bytes per record | Open | Before opening | Managed heap | Working set | Peak |
|---|---|---|---|---|---|---|---|
| 1,000 MB | 7,547,742 | 132 | 31.6 s | 0.0 GB | 3.0 GB | 3.3 GB | 3.3 GB |
| 4.0 GB | | | stopped after 90 s | | | | **Too big:** over 10.7 GB |
| 8.0 GB | | | stopped after 100 s | | | | **Too big:** over 10.7 GB |

## Per GB of Log

From the 1,000 MB log: **3.23 GB** of working set per GB of log after opening, and **3.23 GB** at the peak while opening, on top of 0.03 GB before opening.

## Estimated

**These are estimates, not measurements:** the memory before opening, plus the figures per GB above multiplied by the log's size, for records of the same average size.
The last column is the smallest common amount of RAM that holds the peak. Windows and other applications need some of it too.

| Log | Working set after opening (estimate) | Peak while opening (estimate) | RAM that holds the peak |
|---|---|---|---|
| 4 GB | 12.9 GB | 13.0 GB | 16 GB |
| 8 GB | 25.9 GB | 25.9 GB | 32 GB |
| 12 GB | 38.8 GB | 38.8 GB | 64 GB |
| 16 GB | 51.7 GB | 51.7 GB | 64 GB |
| 20 GB | 64.6 GB | 64.6 GB | 128 GB |
| 24 GB | 77.5 GB | 77.5 GB | 128 GB |
