---
applyTo: "**"
excludeAgent: "coding-agent"
---

# Review Instructions

## Purpose

These instructions guide Copilot during pull request reviews. Every review should help maintain correctness, performance, and safety across the Weevil codebase.

## Behavioral Correctness

- Flag any change that expands the semantics of an existing method beyond its original contract.
- Flag changes to filter or selection logic that could affect which records are displayed to the user.
- Flag changes to the `CoreEngine` composition or manager interactions that could alter observable behavior.
- Confirm that pinned and bookmarked record override logic is preserved when filter changes are present.
- Confirm that the `IRecord` contract and `Record` immutability are not weakened.

## UI Responsiveness

- Flag any code placed on the UI thread that performs I/O, filtering, or other long-running operations.
- Confirm that background operations marshal results back to the UI thread using `IUiDispatcher.Invoke()`.
- Flag removal or bypass of the `UiResponsivenessMonitor`.

## Regression Tests

- Flag any bug fix that does not include a regression test.
- Confirm that the regression test would have caught the original defect.
- Flag feature additions that have no accompanying tests.
- Confirm that test method names follow the `GivenCondition_WhenAction_ThenExpectedResult` pattern.

## Plugin Coupling

- Flag any new direct dependency introduced from `BlueDotBrigade.Weevil.Core` or `BlueDotBrigade.Weevil.Common` toward `BlueDotBrigade.Weevil.Plugins`.
- Confirm that extensibility points use `ICoreExtension` or `IPlugin` interfaces.

## XML Sidecar Compatibility

- Flag any change to sidecar serialization classes that removes or renames persisted properties without a migration strategy.
- Confirm that existing sidecar versions can still be deserialized after the change.

## Log File Safety

- Flag any code path that opens the source log file for writing.
- Confirm that all operations on the log file remain non-destructive.

## Performance

- Flag changes that introduce multiple full enumeration passes over large record collections in the hot path.
- Flag unnecessary per-record allocations in filtering or parsing logic.

## Math Namespace

- Flag any use of unqualified `Math.*` calls inside the `BlueDotBrigade.Weevil.Math` namespace; they must use `System.Math.*`.
- Confirm that statistics calculators round results to 3 decimal places using `System.Math.Round(value, 3)`.
- Confirm that `Calculate()` returns `null` when the input collection is empty.

## Security

- Flag any change that logs record content at a severity level above Debug.
- Flag hard-coded credentials, connection strings, or secrets of any kind.

## Code Quality

- Flag broad refactors when the issue only requires a narrow fix.
- Flag new abstractions or base classes introduced without a clear need.
- Flag loosened access modifiers (`internal` → `public`) made solely to support test access; tests should use `InternalsVisibleTo` in Debug builds instead.
