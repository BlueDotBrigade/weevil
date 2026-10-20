# Weevil memory after opening a log (#978)

| | |
|---|---|
| Weevil | NuGet package 2.12.1: Core 2.12.1+Release |
| Measured | 2026-10-20, options `--size-mb 1000,4000,8000 --delete-logs` |
| Machine | 4 logical processors, 14.2 GB of RAM available to .NET, Ubuntu 24.04.5 LTS |
| .NET | .NET 10.0.13, workstation garbage collector |

## Measured

- **Working set:** the RAM the process holds after opening the log, as Task Manager shows it.
- **Peak:** the most it held while opening.
- **Managed heap:** the .NET objects in that working set.
- **Too big:** opening was stopped when the working set passed 12.8 GB, 90% of this machine's RAM.

| Log | Records | Bytes per record | Open | Managed heap | Working set | Peak |
|---|---|---|---|---|---|---|
| 1,000 MB | 7,547,742 | 132 | 43.4 s | 3.0 GB | 3.3 GB | 3.3 GB |
| 4.0 GB | | | stopped after 165 s | | | **Too big:** over 12.8 GB |
| 8.0 GB | | | stopped after 120 s | | | **Too big:** over 12.8 GB |

## Per GB of Log

From the 1,000 MB log: **3.26 GB** of working set per GB of log after opening, and **3.26 GB** at the peak while opening.

## Estimated

**These are estimates, not measurements:** the figures per GB above, multiplied by the log's size, for records of the same average size.
The last column is the smallest common amount of RAM that holds the peak. Windows and other applications need some of it too.

| Log | Working set after opening (estimate) | Peak while opening (estimate) | RAM that holds the peak |
|---|---|---|---|
| 4 GB | 13.0 GB | 13.0 GB | 16 GB |
| 8 GB | 26.1 GB | 26.1 GB | 32 GB |
| 12 GB | 39.1 GB | 39.1 GB | 64 GB |
| 16 GB | 52.1 GB | 52.1 GB | 64 GB |
| 20 GB | 65.2 GB | 65.2 GB | 128 GB |
| 24 GB | 78.2 GB | 78.2 GB | 128 GB |
