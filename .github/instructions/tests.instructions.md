---
applyTo: "Tst/**"
---

# Test Instructions

## Purpose

These instructions apply to all test projects under `Tst/`. Tests must clearly express intent, remain resilient to refactoring, and validate behavior rather than implementation details.

## Test Frameworks

- **Unit tests**: Use MSTest (`[TestClass]`, `[TestMethod]`) with FluentAssertions or standard `Assert` methods.
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

## Test Placement

- Place the majority of tests in `BlueDotBrigade.Weevil.Core-FeatureTests` (60–75% of total tests).
- Add GUI feature tests only for behaviors that cannot be validated through the core library's public API.
- Use core feature tests for: filtering logic, analysis results, navigation, data transformations, persistence.
- Use GUI feature tests for: workflow sequencing requiring UI state, visual concerns, end-user journeys spanning multiple ViewModels.

## Naming Convention

- Unit test method names follow the pattern: `GivenCondition_WhenAction_ThenExpectedResult`.
  - Example: `GivenEmptyCollection_WhenCalculateCalled_ThenReturnsNull`
- Reqnroll scenario titles should read as plain English sentences describing expected behavior.
- Test class names match the class under test with a `Test` suffix (e.g., `MeanCalculatorTest`).

## Test Data

- Reusable test data lives in the `.Daten/` directory within each test project.
- The `Default.log` file contains 387 standard records and is the baseline for feature tests.
- Use the `Daten` helper class to load test data: `new Daten().AsString()`.
- Do not hardcode raw log strings inline; use test data files for maintainability.
- The shared test context object is `Token`; its default configuration sets `IncludePinned` and `IncludeBookmarks` to `false`.

## Writing Tests

- Prefer behavior-oriented assertions over implementation-specific checks.
- Avoid asserting on internal state (private fields, internal methods). Test through public APIs.
- Do not write tests that rely on execution order; each test must be independent.
- Use `Record.Dummy` as the null object when a test requires an absent record reference.
- When testing filters, use the existing `FilterCriteria` and `LoadCriteria` builder patterns.

## Regression Tests

- Every bug fix must be accompanied by a regression test that reproduces the original failure.
- The regression test must fail before the fix is applied and pass after.
- Reference the issue number in a comment near the test (e.g., `// Regression: Issue #123`).

## Reqnroll / BDD Conventions

- Feature files live alongside their step definition files in the same directory.
- Step definition classes derive from a shared base class that holds the `Token` context.
- Use `Given` steps to establish preconditions, `When` to perform actions, and `Then` to verify outcomes.
- Avoid asserting inside `Given` or `When` steps.
- Reuse existing step definitions wherever possible rather than creating duplicates.

## Test Configuration

- Tests run using: `dotnet test Weevil-v2.sln --configuration Debug -p:Platform=x64 --no-build`
- The `InternalsVisibleTo` attribute is only active in `Debug` configuration; always use `Debug` when running tests.
- Do not introduce test-only public APIs or loosen access modifiers solely for test purposes.
