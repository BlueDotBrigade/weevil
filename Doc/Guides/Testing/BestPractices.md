# Testing: Best Practices

## How to test each layer?

The majority of functional tests should target the `BlueDotBrigade.Weevil.Core.dll` assembly, as it contains the critical business logic driving the application. Testing at this layer ensures stability across all interfaces (e.g command-line & WPF applications) and are less fragile compared to UI-focused tests.

**UI layer** functional tests of `ViewModels` in the `BlueDotBrigade.Weevil.Gui.dll` assembly should focus on UI-specific logic, like validation or state changes, and critical end-to-end scenarios to validate integration with the core assembly. However, these should remain a smaller portion due to their higher maintenance cost and redundancy with core tests.

**Recommended Ratio**: `~70-80%` of tests for the core assembly, `~20-30%` for the UI layer, ensuring robust coverage while minimizing redundancy.

## Which Tests Are Worth Having

The rules live in [`.github/instructions/tests.instructions.md`](../../../.github/instructions/tests.instructions.md), which developers and AI agents share. This section explains them.

The cost of a test is not writing it: it is every future change that has to keep it passing. A good suite earns its keep in two ways: it catches real regressions, and when it fails, people believe it. Noise works against both. Brittle tests break on harmless refactors, duplicate tests fail in groups, and tests of implementation details lock in the design. After a while, people stop trusting a failed build.

Before adding a test, ask:

1. **Does it protect a behaviour someone relies on?** A user or a caller relies on it, not just the way the code works today.
2. **Could a future change plausibly break it?** Code that is obvious and unlikely to change does not need a test.
3. **Is it at the cheapest level that can see the behaviour?**
   - Gherkin feature tests in `BlueDotBrigade.Weevil.Core-FeatureTests` for what users experience. Most tests belong here.
   - Unit tests for logic with sharp edges, such as the selection's position ranges or keeping the selection across a filter.
   - GUI tests only for what only the GUI can show.
4. **Would its failure tell a developer something useful?** If not, it is noise.

### Bugs

A filed bug passes these questions by definition: it escaped, which shows a gap in the tests. That is why it always gets a regression test, and why that test names it with `[WorkItem(N)]` or `@Bug:N`: a reader can then see that the test guards a known failure, and is less likely to simplify it away.

A defect caught during development, for example in a review of an unmerged pull request, is different. Developers fix many of these, and a test for each would be noise. Add one only when the behaviour is worth protecting on its own; it is then an ordinary behaviour test, with no bug tag.
