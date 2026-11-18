# Memory Harness

Measures how much RAM Weevil uses to open a log: the working set before and after opening, the peak while opening, the managed heap, and how long opening took. Each log is opened through Weevil's `Engine` API in a fresh process. It can measure this repository's Core, or a release of Weevil from NuGet, so the two can be compared.

Written for #1019. The results so far are in [`Doc/Research/Weevil3/MemoryHarness`](../../../Doc/Research/Weevil3/MemoryHarness/README.md#results): add new results there, as a dated section.

## What It Measures

Each log is opened through Weevil's `Engine` API in a fresh process, and the harness reports:

| Figure | Meaning |
|---|---|
| Before opening | The working set with Weevil loaded, before it opens the log |
| Working set | The RAM the process holds after opening the log, as Task Manager shows it |
| Peak | The most it held while opening |
| Managed heap | The .NET objects in that working set |
| Open | How long opening took |

It measures opening a log, not the WPF window, which adds a roughly fixed amount on top.

The logs are synthetic Weevil TSV logs averaging 131 bytes per record, as in the #978 test log, generated the same way as the [timestamp proof of concept](../../../Doc/Research/Weevil3/TimestampPoC/README.md)'s.

A log too big for the RAM would make the computer page to disk. The harness stops opening it once the working set passes 90% of the RAM that was free when the run started, and reports it as too big. Close other applications first: whatever they hold is not available to Weevil.

Logs it cannot measure, and the sizes given to `--estimate-gb`, are estimated from the largest log it measured: the memory before opening, plus the memory per GB of log times the log's size. The report labels them as estimates.

Each synthetic log is measured as a first open: the harness deletes the log's saved line index (`.idx`) before opening it. With `--reopen`, it then opens the log again, from the record table saved with its index (#1023), and opens each synthetic log again after it grew: its first 99% is opened, so its index is saved, then the rest is appended. Weevil saves no index in the temporary folder, so `--reopen` needs `--log-folder` elsewhere.

## Run It

The project is not part of `Weevil.sln`. Building it needs the .NET 10 SDK.

```powershell
cd Dep\TestHarnesses\MemoryHarness

# Before: Weevil 2.12.1 from NuGet.
dotnet run -c Release -p:WeevilVersion=2.12.1 -- --size-mb 1000,4000,8000 --delete-logs --report before.md

# After: this repository's Core.
dotnet run -c Release -- --size-mb 1000,4000,8000 --delete-logs --report after.md
```

- Each synthetic log is generated just before it is opened, which takes about a minute per 4 GB. `--delete-logs` deletes it after measuring, so only one log is on disk at a time. Without it, later runs reuse the logs.
- Switching between the two commands rebuilds the harness, because each references a different Weevil.

## Options

| Option | Meaning |
|---|---|
| `--size-mb <list>` | Synthetic logs to open, in MB, separated by commas. Default 1000. |
| `--record-bytes <70-4000>` | Average record size in the synthetic logs. Default 131. |
| `--log-folder <path>` | Where the synthetic logs are written. Default: the temporary folder. |
| `--delete-logs` | Delete each synthetic log after measuring it. |
| `--reopen` | Also reopen each log from its saved index, and each synthetic log after it grew 1%. Needs logs outside the temporary folder. |
| `--file <path>` | Open a real log instead; repeat for several. |
| `--estimate-gb <list>` | Log sizes to estimate, in GB, separated by commas. Default 12,16,20,24. |
| `--ram-percent <10-100>` | Stop opening a log once it uses this share of the RAM free when the run started. Default 90. |
| `--report <path>` | Also save the report to this file. |

## Limits

- In a container, .NET limits the managed heap to 75% of the container's memory. The harness raises it to 99%, the most .NET accepts, so a log is reported too big by the harness rather than by an `OutOfMemoryException`. The RAM in the report is what .NET may use.
- The harness runs on .NET 10, while Weevil 2.12.1 targets .NET 9. Weevil moves to .NET 10 in #1003, so "after" runs on the same runtime as "before".
- Memory grows with the number of records, not only the size of the log. Shorter records mean more memory per GB.
