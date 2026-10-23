---
applyTo: "Tst/**"
---

# Test Instructions

## Rules

- **A test written for a filed bug names it:** `[WorkItem(N)]` on an MSTest method, or `@Bug:N` on a Gherkin scenario, where `N` is the bug's GitHub issue number. Add a one-line comment saying what broke.
- **A defect found and fixed before it was filed gets no bug tag.** Add a test for it only if it protects behaviour worth keeping; it is then an ordinary behaviour test.
- **Prefer a few meaningful tests at the right level.** Do not test implementation details, and do not test the same behaviour at two levels.

[Why: a test must earn its keep](#why-a-test-must-earn-its-keep) explains how to decide.

## Purpose

These instructions apply to all test projects under `Tst/`. Tests must clearly express intent, remain resilient to refactoring, and validate behavior rather than implementation details.

## Test Frameworks

- **Unit tests**: Use MSTest (`[TestClass]`, `[TestMethod]`) and prefer `FluentAssertions` for assertions.
- Avoid `Microsoft.VisualStudio.TestTools.UnitTesting.Assert` when an equivalent FluentAssertions expression exists.
- **Feature/scenario tests**: Use the Reqnroll framework (BDD/Gherkin style) with `.feature` files and corresponding step definition classes.
- Do not mix the two frameworks in the same test project.

## Test Projects

| Project | Purpose |
|---------|---------|
| `BlueDotBrigade.Weevil.Core-UnitTests` | Unit tests for core logic (isolated, fast) |
| `BlueDotBrigade.Weevil.Common-UnitTests` | Unit tests for shared infrastructure |
| `BlueDotBrigade.Weevil.Core-FeatureTests` | BDD feature tests for the core engine |
| `BlueDotBrigade.Weevil.Core-FunctionalTests` | Functional tests with real I/O and persistence |
| `BlueDotBrigade.Weevil.Gui-FeatureTests` | Feature tests for genuine GUI-specific user journeys |
| `BlueDotBrigade.Weevil.Cli-FunctionalTests` | Functional tests for the command-line interface's commands, with real I/O |
| `BlueDotBrigade.Weevil.PowerShell-FunctionalTests` | Functional tests for the PowerShell cmdlets, with real I/O |

## Test Placement

- Place the majority of tests in `BlueDotBrigade.Weevil.Core-FeatureTests` (60–75% of total tests).
- Add GUI feature tests only for behaviors that cannot be validated through the core library's public API.
- Use core feature tests for: filtering logic, analysis results, navigation, data transformations, persistence.
- Use GUI feature tests for: workflow sequencing requiring UI state, visual concerns, end-user journeys spanning multiple ViewModels.

## Naming Convention

- Unit test method names follow the pattern: `GivenCondition_WhenAction_ThenExpectedResult`.
  - Example: `GivenEmptyCollection_WhenCalculateCalled_ThenReturnsNull`
- Reqnroll scenario titles should read as plain English sentences describing expected behavior.
- Unit test class names must match the class under test with a `Tests` suffix (e.g., `MeanCalculatorTests`).
- The unit test file name must match the unit test class name (for example: `MeanCalculatorTests.cs` contains `MeanCalculatorTests`).

## Test Data

- Reusable test data lives in the `.Daten/` directory within each test project.
- Use the `Daten` helper class to load test data: `new Daten().AsString()`.
- Do not hardcode raw log strings inline; use test data files for maintainability.
- When fake records are required, use `BlueDotBrigade.Weevil.TestTools.Data.R` to construct records with minimal visual noise.
- Keep `[TestMethod]` bodies focused on intent; avoid verbose inline `new Record(...)` setup when `R` can express the same scenario.
- The shared test context object is `Token`; its default configuration sets `IncludePinned` and `IncludeBookmarks` to `false`.

## Writing Tests

- Prefer behavior-oriented assertions over implementation-specific checks.
- Avoid asserting on internal state (private fields, internal methods). Test through public APIs.
- Do not write tests that rely on execution order; each test must be independent.
- Assert on elapsed time only when a slow algorithm is the risk, and with a generous bound, so the test does not fail on a slow build machine.
- Use `Record.Dummy` as the null object when a test requires an absent record reference.
- When testing filters, construct `FilterCriteria` directly using `new FilterCriteria(expression)` or `FilterCriteria.None`. Load the engine via the `Engine.UsingPath(...)` builder style.

## Regression Tests

A regression test guards a filed bug: a GitHub issue of type Bug.

- Every fix for a filed bug comes with a regression test that reproduces the original failure.
- The regression test must fail before the fix is applied and pass after.
- Name the bug in an attribute or tag, so a reader knows why the test exists, and say what broke in one line:

  ```csharp
  [TestMethod]
  [WorkItem(1066)]
  public void GivenTextSentWhileTyping_WhenCtrlPIsPressedBeforeTheRest_ThenTheRecordStaysPinned()
  {
      // Ctrl+P toggled off the pin that the first words of the comment had set.
  ```

  ```gherkin
  @Bug:1066
  Scenario: Pressing Ctrl+P while typing a comment keeps the record pinned
  ```

- `@Requirement:N` names the user story or requirement a scenario specifies; `@Bug:N` names the bug it guards.
- Keep the test name about behaviour: the bug number goes in the attribute or tag, not in the name.
- Older tests mark their bug with a `// Regression: Issue #N` comment. Switch a test to the attribute or tag when you change it.
- A defect found during development, for example in a review of an unmerged pull request, is not a filed bug, so its tests get no tag.

## Reqnroll / BDD Conventions

- Feature files live alongside their step definition files in the same directory.
- Step definition classes derive from a shared base class that holds the `Token` context.
- Use `Given` steps to establish preconditions, `When` to perform actions, and `Then` to verify outcomes.
- Avoid asserting inside `Given` or `When` steps.
- Reuse existing step definitions wherever possible rather than creating duplicates.
- Gherkin scenarios are the preferred format for capturing test specifications. Write scenarios in plain English that a non-technical stakeholder can read.

## Terminology

- A list of testing terminology can be found here: TBD

## Test Configuration

- Tests run using: `dotnet test Weevil-v2.sln --configuration Debug -p:Platform=x64 --no-build`
- The `InternalsVisibleTo` attribute is only active in `Debug` configuration; always use `Debug` when running tests.
- Do not introduce test-only public APIs or loosen access modifiers solely for test purposes.

## Why: a test must earn its keep

The cost of a test is not writing it: it is every future change that has to keep it passing. A good suite earns its keep in two ways: it catches real regressions, and when it fails, people believe it. Noise works against both. Brittle tests break on harmless refactors, duplicate tests fail in groups, and tests of implementation details lock in the design. After a while, people stop trusting a failed build.

Before adding a test, ask:

1. **Does it protect a behaviour someone relies on?** A user or a caller relies on it, not just the way the code works today.
2. **Could a future change plausibly break it?** Code that is obvious and unlikely to change does not need a test.
3. **Is it at the cheapest level that can see the behaviour?**
   - Gherkin feature tests in `BlueDotBrigade.Weevil.Core-FeatureTests` for what users experience. Most tests belong here (see [Test Placement](#test-placement)).
   - Unit tests for logic with sharp edges, such as the selection's position ranges or keeping the selection across a filter.
   - GUI tests only for what only the GUI can show.
4. **Would its failure tell a developer something useful?** If not, it is noise.

A filed bug passes these questions by definition: it escaped, which shows a gap in the tests. That is why it always gets a regression test, and why that test names it. A defect caught during development passes them only when the behaviour is worth protecting on its own.
