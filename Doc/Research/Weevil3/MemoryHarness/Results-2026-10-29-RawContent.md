# Weevil memory after opening a log (#978)

| | |
|---|---|
| Weevil | this repository's Core project: Core 3.0.0+Release |
| Measured | 2026-10-29, options `--size-mb 1000,4000 --ram-percent 99 --delete-logs --report after.md` |
| Machine | 4 logical processors, 14.2 GB of RAM available to .NET, 11.9 GB of it free when the run started, Ubuntu 24.04.5 LTS |
| .NET | .NET 10.0.13, workstation garbage collector |

## Measured

- **Before opening:** the working set of the process with Weevil loaded, before it opens the log.
- **Working set:** the RAM the process holds after opening the log, as Task Manager shows it.
- **Peak:** the most it held while opening.
- **Managed heap:** the .NET objects in that working set.
- **Too big:** opening was stopped when the working set passed 11.8 GB: 99% of the RAM free when the run started.

| Log | Records | Bytes per record | Open | Before opening | Managed heap | Working set | Peak |
|---|---|---|---|---|---|---|---|
| 1,000 MB | 7,547,742 | 132 | 20.2 s | 0.0 GB | 1.7 GB | 1.7 GB | 1.7 GB |
| 4.0 GB | 30,014,396 | 133 | 74.7 s | 0.0 GB | 6.5 GB | 6.5 GB | 6.5 GB |

## Per GB of Log

From the 4.0 GB log: **1.62 GB** of working set per GB of log after opening, and **1.62 GB** at the peak while opening, on top of 0.04 GB before opening.

## Estimated

**These are estimates, not measurements:** the memory before opening, plus the figures per GB above multiplied by the log's size, for records of the same average size.
The last column is the smallest common amount of RAM that holds the peak. Windows and other applications need some of it too.

| Log | Working set after opening (estimate) | Peak while opening (estimate) | RAM that holds the peak |
|---|---|---|---|
| 12 GB | 19.5 GB | 19.5 GB | 32 GB |
| 16 GB | 25.9 GB | 25.9 GB | 32 GB |
| 20 GB | 32.4 GB | 32.4 GB | 64 GB |
| 24 GB | 38.9 GB | 38.9 GB | 64 GB |
