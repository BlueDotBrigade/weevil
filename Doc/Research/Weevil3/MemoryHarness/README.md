# Memory Harness

Question for #1019, before step 1 of the [#988 plan](../../../Plans/988-IndexingService.md): how much RAM does Weevil use after opening a log? #978 aims to use less, so this records "before"; the same harness measures "after" once each step lands.

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

The logs are synthetic Weevil TSV logs averaging 131 bytes per record, as in the #978 test log, generated the same way as the [timestamp proof of concept](../TimestampPoC/README.md)'s.

A log too big for the RAM would make the computer page to disk. The harness stops opening it once the working set passes 90% of the RAM that was free when the run started, and reports it as too big. Close other applications first: whatever they hold is not available to Weevil.

Logs it cannot measure, and the sizes given to `--estimate-gb`, are estimated from the largest log it measured: the memory before opening, plus the memory per GB of log times the log's size. The report labels them as estimates.

Each synthetic log is measured as a first open: the harness deletes the log's saved line index (`.idx`) before opening it.

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
| `--ram-percent <10-100>` | Stop opening a log once it uses this share of the RAM free when the run started. Default 90. |
| `--report <path>` | Also save the report to this file. |

## Limits

- In a container, .NET limits the managed heap to 75% of the container's memory. The harness raises it to 99%, the most .NET accepts, so a log is reported too big by the harness rather than by an `OutOfMemoryException`. The RAM in the report is what .NET may use.
- The harness runs on .NET 10, while Weevil 2.12.1 targets .NET 9. Weevil moves to .NET 10 in #1003, so "after" runs on the same runtime as "before".
- Memory grows with the number of records, not only the size of the log. Shorter records mean more memory per GB.

## Results

### Before: Weevil 2.12.1 (2026-10-20)

Weevil 2.12.1 needs about **3.25 GB of RAM per GB of log** at the peak while opening, and about 3.0 GB per GB once the log is open: about 430 bytes per record, for records averaging 132 bytes. The peak decides whether a log fits. For the 4 GB log the extra 0.8 GB at the peak is temporary, probably from the record lists growing while the log is parsed.

Measured on a 4-vCPU Linux container with 14.2 GB of RAM available to .NET, all of it free, with .NET 10 and `--ram-percent 99`. The full report is in [Results-2026-10-20-Before.md](Results-2026-10-20-Before.md).

| Log | Working set after opening | Peak while opening | Source |
|---|---|---|---|
| 1 GB | 3.3 GB | 3.3 GB | Measured: 7,547,742 records, opened in 41 s |
| 4 GB | 12.2 GB | 13.0 GB | Measured: 30,014,396 records, opened in 158 s |
| 8 GB | 24.3 GB | 26.0 GB | **Estimate.** Too big for this machine |
| 12 GB | 36.4 GB | 39.0 GB | **Estimate** |
| 16 GB | 48.5 GB | 52.0 GB | **Estimate** |
| 20 GB | 60.7 GB | 64.9 GB | **Estimate** |
| 24 GB | 72.8 GB | 77.9 GB | **Estimate** |

- **The estimates hold.** Memory grows in step with the number of records: estimated from the 1 GB log alone, the 4 GB log's peak was 13.0 GB, exactly as measured. The larger sizes are estimated from the 4 GB log.
- **On a PC:** with 16 GB of RAM, a 4 GB log does not fit once Windows and the WPF window take their share. With 32 GB, the largest log that fits is about 8 GB.
- **Free RAM matters.** An earlier run reported the 4 GB log as too big, because other processes held 2.3 GB of the RAM and the log passed the harness's limit. Close other applications before measuring.

### After step 1 of #988: content held as the log's bytes (2026-10-29)

Weevil now needs about **1.75 GB of RAM per GB of log** at the peak while opening, and 1.62 GB once the log is open. That is 46% less than 2.12.1 after opening, and 40% less than `main` before #1133, which was measured the same day on the same machine. The full reports are in [Results-2026-10-29-RawContent.md](Results-2026-10-29-RawContent.md) and, for `main`, [Results-2026-10-29-MainBeforeRawContent.md](Results-2026-10-29-MainBeforeRawContent.md).

| Log | 2.12.1 | `main` before #1133 | After #1133 | Source |
|---|---|---|---|---|
| 1 GB | 3.3 GB | 2.8 GB | 1.7 GB, peak 1.9 GB | Measured: opened in 22 s, against 29 s for `main` |
| 4 GB | 12.2 GB, peak 13.0 GB | 10.9 GB, peak 10.9 GB | 6.5 GB, peak 7.0 GB | Measured: opened in 94 s, against 114 s for `main` |
| 8 GB | 24.3 GB, peak 26.0 GB | | 13.0 GB, peak 14.0 GB | **Estimate** |
| 16 GB | 48.5 GB, peak 52.0 GB | | 25.9 GB, peak 28.1 GB | **Estimate** |
| 24 GB | 72.8 GB, peak 77.9 GB | | 38.9 GB, peak 42.1 GB | **Estimate** |

- **On a PC:** with 32 GB of RAM, the largest log that fits is now about 16 GB, instead of about 8 GB.
- **The peak:** unlike `main`, the peak while opening is above the memory once the log is open, by 0.5 GB for the 4 GB log. The cause has not been investigated.
