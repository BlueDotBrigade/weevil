# Weevil memory after opening a log (#978)

| | |
|---|---|
| Weevil | this repository's Core project: Core 3.0.0+Release |
| Measured | 2026-11-17, options `--size-mb 1000,4000 --log-folder /home/user/bench-logs --reopen --estimate-gb 8,16,20,24 --report <scratch>/results/memory-main.md` |
| Machine | 4 logical processors, 14.2 GB of RAM available to .NET, 14.2 GB of it free when the run started, Ubuntu 24.04.5 LTS |
| .NET | .NET 10.0.15, workstation garbage collector |

## Measured

- **Before opening:** the working set of the process with Weevil loaded, before it opens the log.
- **Working set:** the RAM the process holds after opening the log, as Task Manager shows it.
- **Peak:** the most it held while opening.
- **Managed heap:** the .NET objects in that working set.
- **Too big:** opening was stopped when the working set passed 12.8 GB: 90% of the RAM free when the run started.
- **Reopen:** the log opened again, from the record table saved with its index (#1023). **Reopen after growing 1%:** its first 99% opened, then the rest appended, and opened again: only the new part is parsed.

| Log | Case | Records | Bytes per record | Open | Before opening | Managed heap | Working set | Peak |
|---|---|---|---|---|---|---|---|---|
| 1,000 MB | First open | 7,547,742 | 132 | 18.0 s | 0.0 GB | 1.2 GB | 1.2 GB | 1.4 GB |
| 1,000 MB | Reopen | 7,547,742 | 132 | 2.4 s | 0.0 GB | 1.2 GB | 1.2 GB | 1.4 GB |
| 1,000 MB | Reopen after growing 1% | 7,547,742 | 132 | 3.2 s | 0.0 GB | 1.2 GB | 1.2 GB | 1.5 GB |
| 4.0 GB | First open | 30,014,396 | 133 | 64.2 s | 0.0 GB | 4.7 GB | 4.7 GB | 4.9 GB |
| 4.0 GB | Reopen | 30,014,396 | 133 | 8.0 s | 0.0 GB | 4.7 GB | 4.7 GB | 4.9 GB |
| 4.0 GB | Reopen after growing 1% | 30,014,396 | 133 | 11.4 s | 0.0 GB | 4.7 GB | 4.7 GB | 5.0 GB |

## Per GB of Log

From the 4.0 GB log: **1.16 GB** of working set per GB of log after opening, and **1.22 GB** at the peak while opening, on top of 0.04 GB before opening.

## Estimated

**These are estimates, not measurements:** the memory before opening, plus the figures per GB above multiplied by the log's size, for records of the same average size.
The last column is the smallest common amount of RAM that holds the peak. Windows and other applications need some of it too.

| Log | Working set after opening (estimate) | Peak while opening (estimate) | RAM that holds the peak |
|---|---|---|---|
| 8 GB | 9.3 GB | 9.8 GB | 16 GB |
| 16 GB | 18.7 GB | 19.5 GB | 32 GB |
| 20 GB | 23.3 GB | 24.4 GB | 32 GB |
| 24 GB | 28.0 GB | 29.2 GB | 32 GB |
