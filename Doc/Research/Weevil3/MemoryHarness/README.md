# Memory Harness

Question for #1019, before step 1 of the [#988 plan](../../../Plans/988-IndexingService.md): how much RAM does Weevil use after opening a log? #978 aims to use less, so this records "before"; the same harness measures "after" once each step lands.

## What It Measures

Each log is opened through Weevil's `Engine` API in a fresh process, and the harness reports:

| Figure | Meaning |
|---|---|
| Working set | The RAM the process holds after opening the log, as Task Manager shows it |
| Peak | The most it held while opening |
| Managed heap | The .NET objects in that working set |
| Open | How long opening took |

It measures opening a log, not the WPF window, which adds a roughly fixed amount on top.

The logs are synthetic Weevil TSV logs averaging 131 bytes per record, as in the #978 test log, generated the same way as the [timestamp proof of concept](../TimestampPoC/README.md)'s.

A log too big for the RAM would make the computer page to disk. The harness stops opening it once the working set passes 90% of the RAM, and reports it as too big. Logs it cannot measure, and the sizes given to `--estimate-gb`, are estimated from the figures per GB of the largest log it measured. The report labels them as estimates.

## Run It

The project is not part of `Weevil-v2.sln`. Building it needs the .NET 10 SDK.

```powershell
cd Doc\Research\Weevil3\MemoryHarness

# Before: Weevil 2.12.1 from NuGet.
dotnet run -c Release -p:WeevilVersion=2.12.1 -- --size-mb 1000,4000,8000 --delete-logs --report before.md

# After: this repository's Core.
dotnet run -c Release -- --size-mb 1000,4000,8000 --delete-logs --report after.md
```

- Each synthetic log is generated just before it is opened, which takes about a minute per 4 GB. `--delete-logs` deletes it after measuring, so only one log is on disk at a time. Without it, later runs reuse the logs.
- Switching between the two commands rebuilds the harness, because each references a different Weevil.

### Options

| Option | Meaning |
|---|---|
| `--size-mb <list>` | Synthetic logs to open, in MB, separated by commas. Default 1000. |
| `--record-bytes <70-4000>` | Average record size in the synthetic logs. Default 131. |
| `--log-folder <path>` | Where the synthetic logs are written. Default: the temporary folder. |
| `--delete-logs` | Delete each synthetic log after measuring it. |
| `--file <path>` | Open a real log instead; repeat for several. |
| `--estimate-gb <list>` | Log sizes to estimate, in GB, separated by commas. Default 12,16,20,24. |
| `--ram-percent <10-100>` | Stop opening a log once it uses this share of the RAM. Default 90. |
| `--report <path>` | Also save the report to this file. |

## Limits

- In a container, .NET limits the managed heap to 75% of the container's memory. The harness raises it to 99%, the most .NET accepts, so a log is reported too big by the harness rather than by an `OutOfMemoryException`. The RAM in the report is what .NET may use.
- The harness runs on .NET 10, while Weevil 2.12.1 targets .NET 9. Weevil moves to .NET 10 in #1003, so "after" runs on the same runtime as "before".
- Memory grows with the number of records, not only the size of the log. Shorter records mean more memory per GB.

## Results

### Before: Weevil 2.12.1 (2026-10-20)

Weevil 2.12.1 holds about **3.3 GB of RAM per GB of log**: about 430 bytes per record, for records averaging 132 bytes. The peak while opening equals the working set afterwards: memory only grows while a log opens.

Measured on a 4-vCPU Linux container with 14.2 GB of RAM available to .NET, with .NET 10. The 4 and 8 GB logs did not fit, so they are estimated like the larger sizes. The full report is in [Results-2026-10-20-Before.md](Results-2026-10-20-Before.md).

| Log | Working set after opening | Peak while opening | Source |
|---|---|---|---|
| 1 GB | 3.3 GB | 3.3 GB | Measured: 7,547,742 records, opened in 43 s |
| 4 GB | 13.0 GB | 13.0 GB | **Estimate.** Too big for this machine: stopped at 12.9 GB after 165 s |
| 8 GB | 26.1 GB | 26.1 GB | **Estimate.** Too big for this machine: stopped at 12.8 GB after 120 s |
| 12 GB | 39.1 GB | 39.1 GB | **Estimate** |
| 16 GB | 52.1 GB | 52.1 GB | **Estimate** |
| 20 GB | 65.2 GB | 65.2 GB | **Estimate** |
| 24 GB | 78.2 GB | 78.2 GB | **Estimate** |

The estimates assume memory grows in step with the number of records. On a PC with 32 GB of RAM, the largest log that fits is about 8 GB, before Windows and the WPF window take their share. Running the harness there would measure the 4 and 8 GB logs instead of estimating them.
