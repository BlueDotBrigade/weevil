# Filter Timing

Times Weevil's filters on one log, in RAM or on disk, with a warm or a cold cache. For each filter it reports the median, fastest and slowest time, the bytes one filter allocates, and the number of results. Written for #1287, to check that a filter's buffer pool and its cap of one worker per core cost no speed: [its results](https://github.com/BlueDotBrigade/weevil/pull/1287) are in that pull request.

The [benchmarks](../../../Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/Benchmarks/) already time filters in RAM, and the Benchmarks workflow compares branches. Use this harness for what they don't cover:
- a log on disk;
- a cold cache;
- any log, such as the 10-million-record Droid log of the performance tests;
- two versions of Weevil timed by turns on the same computer.

## Run It

The project is not part of `Weevil.sln`. Building it needs the .NET 10 SDK.

```powershell
cd Dep\TestHarnesses\FilterTiming

# This repository's Core, on a 1-million-record reference log, on disk.
dotnet run -c Release -- --storage Disk

# Any log, with your own filters.
dotnet run -c Release -- --log C:\Logs\Big.log --storage Disk --plain "Blaster" --regex "Bl[a-z]ster"
```

| Option | Meaning |
|---|---|
| `--log <path>` | The log to filter. By default, a reference log of `--reference` records. |
| `--reference <records>` | The size of the reference log, which is written once and kept. Default 1000000. |
| `--log-folder <path>` | Where the reference log is written. Default: `Weevil.FilterTiming` in the temporary folder. |
| `--storage Ram\|Disk` | Where Weevil keeps the log's content. Default Ram. |
| `--iterations <n>` | How many times each filter is timed, after one filter to warm up. Default 7. The median of an even number is the mean of the middle two. |
| `--cold` | Drop the log from the operating system's cache before each filter, so it is read from the disk. Linux only, with `--storage Disk`. |
| `--plain <text>` | The plain text filter. Default `timeout`, which the reference log holds. |
| `--regex <pattern>` | The regular expression filter. Default `retry.*connection`. |

Weevil saves no line index or sidecar in the temporary folder, so a log there is opened from the same state every run. A log elsewhere is opened from its saved index after the first run.

## Compare Two Versions

Build the harness once for each version, from a git worktree of each commit, then run the two builds by turns, several times: a computer's speed drifts as it runs, and taking turns spreads that drift over both.

Both commits must include #1166, from 2026-11-09: older ones have no disk mode, so the harness doesn't build against them.

```powershell
# From the repository's root folder: each worktree goes beside the repository.
git worktree add ..\weevil-before <commit>
git worktree add ..\weevil-after <commit>

# From the harness's folder. WeevilRoot is relative to the project file.
cd Dep\TestHarnesses\FilterTiming
dotnet build -c Release -p:WeevilRoot=..\..\..\..\weevil-before -o bin\before
dotnet build -c Release -p:WeevilRoot=..\..\..\..\weevil-after -o bin\after

foreach ($round in 1..3) {
    foreach ($version in 'before', 'after') {
        dotnet "bin\$version\Weevil.FilterTiming.dll" --storage Disk | Select-Object -Last 2
    }
}
```

The two builds must find the same number of results; if not, the change altered what the filter keeps.

## Limits

- **The cold cache needs Linux, and a log on disk.** `--cold` writes the log's pending pages to the disk, then drops the log's pages from the cache, with `fsync` and `posix_fadvise`, which need no administrator. On Windows, empty the standby list with Sysinternals RAMMap, as an administrator, before each run instead.
- **A cold read here is not a slow disk.** On a virtual disk, a hard drive or a network share may behave differently.
- **One computer.** The number of cores changes how a filter shares its work: a cap on workers only matters on a computer with more cores than the thread pool would use.
