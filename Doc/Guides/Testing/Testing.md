# Testing

## General

Sample log file: /weevil/Tst/BlueDotBrigade.Weevil.Core-UnitTests/.Daten/.Global/Default.log

## Unit Testing

These tests ensure that a class' methods & properties are working as expected.

### Naming Conventions

Test method names follow Roy Osherove's `Method_Scenario_Expected` pattern (see *The Art of Unit Testing*). The three segments answer:

1. **Method** — the method under test (e.g. `Analyze`).
2. **Scenario** — the input shape or precondition (e.g. `Plateau`, `SharpPyramid`).
3. **Expected** — the observable outcome (e.g. `FlagsNothing`, `FlagsThePeak`).

Example:

```csharp
public void Analyze_SharpPyramid_FlagsThePeak(...)
public void Analyze_PlateauPyramid_FlagsTheLastPeakValue(...)
```

Avoid the `Given_/When_/Then_` ceremony for unit tests — when every test in a fixture shares the same Given and When, those segments are pure visual noise. BDD-style phrasing belongs in scenario tests (Reqnroll), not unit tests.

For a deeper treatment of test design, readability, and what makes a test valuable, see Vladimir Khorikov's *Unit Testing: Principles, Practices, and Patterns*.

## Functional Testing

The Weevil core engine can be accessed via:
1. WPF Application
2. Console Application / Command-Line Interface (CLI)
3. PowerShell

For this reason, it is best to perform the majority of testing at the `BlueDotBrigade.Weevil.Core` level.



This project uses two approaches to functional testing:
1. Gherkin scenarios
2. Integration tests (i.e. non-Gherkin)

### Disk Mode

CI runs the Core unit, feature and functional tests, and the CLI's functional tests, a second time with every log opened on disk (#1025, #1027). To do the same locally, run each of those four projects with `dotnet test --settings Tst/Disk.runsettings`. Do not select the file in Visual Studio's Test Explorer: it would apply to every test project until cleared. See [DiskMode.md](../../Design/DiskMode.md).

## Performance

Everything about performance lives in `BlueDotBrigade.Weevil.Core-PerformanceTests`, as MSTest tests of two kinds (#1177):

| Kind | Where | Runs | Catches |
|---|---|---|---|
| **Budget**: a limit that must hold | An MSTest `[TestClass]` | In CI, with `dotnet test` | Large regressions: memory kept per record in RAM and on disk, and filters, export and an analyzer far over a generous time limit, which on disk also catches an analyzer or export reading the log once per record |
| **Benchmark**: a measurement to compare two versions | A BenchmarkDotNet class in `Benchmarks/`, run by a test in `BenchmarkTests`, in the `Benchmark` category | On demand, in Release: locally, or with the Benchmarks workflow | Smaller regressions in time and memory allocated: opening (without a saved line index), reopening (from the saved record table), filtering, Find, export and an analyzer, on logs of 100,000 and 1,000,000 records |

Add a budget when a limit must never be crossed, and a benchmark when you want to see how a change moves a number. A CI runner is too noisy to fail a build on a 10% slowdown, so that is a benchmark, not a budget. To add a benchmark class, also add a test method for it to `BenchmarkTests`, without which it never runs, and add the class to the `options` of the Benchmarks workflow's `benchmarks` input.

### Running the Benchmarks

Run them in Release, on a quiet computer, from the repository's root folder:

```
dotnet test Tst/BlueDotBrigade.Weevil.Core-PerformanceTests -c Release -p:Platform=x64 --filter TestCategory=Benchmark --logger "console;verbosity=detailed"
```

- `--filter "TestCategory=Benchmark&Name~GivenFilterBenchmarks_"` runs one class's benchmarks. The `Given` and `_` keep `Open` from matching `Reopen` too.
- Set the environment variable `WEEVIL_BENCHMARK_JOB=Short` for a quick look: it takes a few minutes. Without it, the default job takes longer and measures more precisely.
- CI's test runs leave the `Benchmark` category out. So should yours in Release: there, a run without a filter, or Run All in Visual Studio's Test Explorer, includes every benchmark, which takes many minutes. Exclude them with `--filter TestCategory!=Benchmark`.
- The first run writes the reference logs to the temporary folder (`Weevil.Benchmarks`), and later runs reuse them. After a change to `ReferenceLog.cs`, delete that folder. The reopen benchmarks copy them, once per storage mode, to `Weevil.Benchmarks` in the local application data folder, where Weevil saves their index: about 350 MB, which you can delete when you no longer run them.
- The Benchmarks workflow (`.github/workflows/benchmarks.yml`) runs them on demand on a GitHub runner, from the Actions tab. It shows the reports on the run's summary page and keeps them as the `benchmark-reports` artifact. A shared runner is noisy, so compare runs of the same workflow, not a runner with your computer.

### Where the Results Go

Each class's results are written to a Markdown file, beside the test assembly, in a folder Git ignores:

```
Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/bin/x64/Release/net10.0/BenchmarkDotNet.Artifacts/results/BlueDotBrigade.Weevil.<Class>-report-github.md
```

The test also attaches the file to its result, and writes the table to its output: Visual Studio's Test Explorer shows it, and so does `dotnet test` with the detailed console logger. A plain `dotnet test` shows only whether each test passed. BenchmarkDotNet also writes a log for each class, named after the class and the time it started, in `BenchmarkDotNet.Artifacts`, one level up.

### When the Output Stops

- **No progress while a test runs.** The test runner shows a test's output only once the test ends, and one class can take many minutes, so a run can look stuck. To follow it, read the newest log in `BenchmarkDotNet.Artifacts`, which BenchmarkDotNet writes as it goes.
- **A timeout stops the run.** For example, the test runner's `--blame-hang-timeout`, or the Benchmarks workflow's limit of 180 minutes. The class being measured then writes no report. The classes already measured keep theirs, and the class's log shows how far it got. Run fewer classes at a time, or the short job.
- **A BenchmarkDotNet upgrade can stop the benchmarks running.** Its documentation supports console applications only, and running from a test runner works with 0.15.8 without being supported. After an upgrade, run one class, for example `FindBenchmarks` with the short job. If it no longer runs, start the benchmarks from a console application's `Main` with `BenchmarkSwitcher` instead.
- **A Debug build measures nothing.** Each benchmark test is inconclusive, because BenchmarkDotNet's times mean nothing without the optimizer: build in Release.

### Comparing Two Versions

1. Run the benchmarks on `main`, and keep the Markdown reports: the next run overwrites them.
2. Run them on the branch, on the same computer, with the same job.
3. Compare the `Mean` and `Allocated` columns. A change larger than the `Error` column, and larger than about 10%, is worth explaining in the pull request.

The benchmarks use only Weevil's public API, because `InternalsVisibleTo` is limited to Debug builds. They open logs in RAM. Since #1028 made the storage mode public, a benchmark can also open a log on disk, with `UsingStorage(StorageMode.Disk)`.
