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

Everything about performance lives in `BlueDotBrigade.Weevil.Core-PerformanceTests`, in two kinds (#1177):

| Kind | Where | Runs | Catches |
|---|---|---|---|
| **Test**: a budget that must hold | An MSTest `[TestClass]` | In CI, with `dotnet test` | Large regressions: memory kept per record in RAM and on disk, and filters, export and an analyzer far over a generous time limit, which on disk also catches an analyzer or export reading the log once per record |
| **Benchmark**: a measurement to compare two versions | A BenchmarkDotNet class in `Benchmarks/` | On demand, with `dotnet run` | Smaller regressions in time and memory allocated: opening (without a saved line index), filtering, Find, export and an analyzer, on logs of 100,000 and 1,000,000 records |

Add a test when a limit must never be crossed, and a benchmark when you want to see how a change moves a number. A CI runner is too noisy to fail a build on a 10% slowdown, so that is a benchmark, not a test. CI never runs the benchmarks.

### Running the Benchmarks

Run them in Release, on a quiet computer, from the repository's root folder:

```
dotnet run -c Release -p:Platform=x64 --project Tst/BlueDotBrigade.Weevil.Core-PerformanceTests -- --filter "*"
```

- In Debug, the program only says to run it in Release.
- `--filter "*FilterBenchmarks*"` runs one class's benchmarks; without `--filter`, BenchmarkDotNet asks which to run.
- `--job short` takes a few minutes, for a quick look. The default job takes longer and measures more precisely.
- The first run writes the reference logs to the temporary folder (`Weevil.Benchmarks`), and later runs reuse them. After a change to `ReferenceLog.cs`, delete that folder.
- Results are written to `BenchmarkDotNet.Artifacts/results`, which Git ignores.

### Comparing Two Versions

1. Run the benchmarks on `main`, and keep the Markdown results.
2. Run them on the branch, on the same computer, with the same job.
3. Compare the `Mean` and `Allocated` columns. A change larger than the `Error` column, and larger than about 10%, is worth explaining in the pull request.

The benchmarks use only Weevil's public API, because `InternalsVisibleTo` is limited to Debug builds. They open logs in RAM: disk mode joins them when #1028 makes the storage mode public.
