# Core Test Suite Review: Feature Tests vs Functional Tests

## Scope
This review compares:
- `Tst/BlueDotBrigade.Weevil.Core-FeatureTests`
- `Tst/BlueDotBrigade.Weevil.Core-FunctionalTests`

Goal: decide where most **new** automated tests should be added.

## Libraries

### Feature tests (`Core-FeatureTests`)
- Style: behavior-driven development (BDD) using Reqnroll + Gherkin (`Given/When/Then`).
- Primary value: readable acceptance criteria that non-developers can follow.
- Best use: protect high-level business behavior and user-visible workflows.

### Functional tests (`Core-FunctionalTests`)
- Style: direct C# tests (`[TestMethod]`) that execute business logic and assert outcomes.
- Primary value: fast, focused regression coverage for developers.
- Best use: verify rules, edge cases, and bug fixes close to the code.

## Key tradeoffs (including AI-assisted development)

### What AI changes
With a paid GPT/Codex workflow, AI can reduce the authoring cost of both styles:
- generate Gherkin scenarios and step scaffolding,
- generate targeted C# regression tests,
- quickly expand coverage after bugs are found.

So yes: AI makes Feature tests more practical than they were before.

### What AI does **not** remove
AI does not remove core differences in test economics:
- **Feature tests** still carry a shared step-binding layer that can hide mistakes and create cross-scenario coupling.
- **Functional tests** are still easier to debug and usually cheaper to maintain per test over time.

In short: AI lowers creation cost, but long-term maintenance and debugging cost still matter.

## Recommendation
Use a **hybrid strategy** with a clear default:

1. **Default for most new tests: `Core-FunctionalTests`**
   - highest signal-to-maintenance ratio for a resource-constrained open-source project.
2. **Curated Feature test layer: `Core-FeatureTests`**
   - keep and expand scenarios that represent critical acceptance behavior and business contracts.
3. **AI-first workflow**
   - use AI to draft both test types,
   - require human review for correctness and readability,
   - promote only high-value Gherkin scenarios into the acceptance layer.

## Why this recommendation
- The project has limited human developer time, so maintenance cost must stay low.
- Feature tests are valuable for communication and acceptance confidence, especially with AI helping authoring.
- Functional tests remain the most efficient place for the **bulk** of regression depth.
- This balance gives strong protection without over-investing in high-overhead test plumbing.

## Practical decision rule for contributors
When adding a new test, ask:
1. Is this primarily a user-visible behavior/contract that should be readable in plain English?
   - **Yes** → add/update a Feature test.
2. Is this primarily a logic rule, edge case, or bug regression?
   - **Yes** → add a Functional test.
3. Unsure?
   - Add Functional first, then add Feature only if it improves acceptance-level clarity.

## Short answer
- **Where should most future tests go?** 
    - `BlueDotBrigade.Weevil.Core-FunctionalTests`.
- **Do Feature tests still have real benefit (especially with AI)?** 
  - Yes—use them as a focused acceptance layer.
- **Final approach:** 
  - Functional-first for volume, Feature-tests for critical behavior narratives.