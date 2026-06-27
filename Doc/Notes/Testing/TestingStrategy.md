## Recommendation

Place the bulk of the feature tests in `Tst/BlueDotBrigade.Weevil.Core-FeatureTests`, and keep only a lean, business‑critical subset in `Tst/BlueDotBrigade.Weevil.GUI-FeatureTests`.

---

## Why This Distribution Makes Sense

### 1. Test Pyramid Economics
- Core (non‑UI) feature tests are fast, deterministic, parallelizable, and cheap to maintain.
- GUI (end‑to‑end) feature tests are slower, more brittle (timing, rendering, threading), and costlier to debug.
- Concentrating logic verification in Core preserves a healthy test pyramid and fast feedback loops.

### 2. Clear Responsibility Boundaries
- Domain/application logic (parsing, filtering, state transitions, validation, aggregation) belongs in Core tests.
- GUI tests should cover only behavior added by the UI layer: workflow composition, interaction sequencing, accessibility, conditional visibility/enabling.

### 3. Refactor Safety
- Strong Core coverage lets you freely refactor or even replace the UI framework without losing confidence.
- When a GUI test fails while Core tests pass, triage is narrowed to wiring/presentation concerns.

### 4. Reduced Duplication
- Re‑asserting every logic branch through the GUI adds noise and maintenance overhead.
- GUI tests answer: “Can a real user accomplish X?” not “Does every edge case of X's rule engine work?”

### 5. Flakiness Containment
- UI layers are more susceptible to intermittent failures (focus, timing, animations, environment).
- A smaller GUI suite keeps the signal-to-noise ratio high.

---

## Suggested Proportional Breakdown (Heuristic)

| Layer | Purpose | Approx. Share |
|-------|---------|---------------|
| Core Feature Tests | Domain + application orchestration via public Core APIs | 60–75% |
| Integration / Adapter Tests | Core + real filesystem, persistence, external formats | 10–20% |
| GUI Feature (End‑to‑End) Tests | Critical user journeys + UI-specific behavior | 10–15% |

---

## Decision Heuristics

Put a scenario in **Core** if:
- It is fully expressible through public Core APIs/services.
- It asserts data transformations, invariants, or rule evaluation.
- UI interaction timing/order does not change the outcome.

Keep (or create) a **GUI** feature test only if:
- The user workflow sequence itself is essential to the outcome.
- UI composition or state (enabled/disabled, progressive disclosure) affects behavior.
- It validates cross-cutting integration exactly as a user experiences it.
- It asserts inherently visual/interactional concerns (focus handling, keyboard shortcuts, accessibility semantics, theming/visual sanity).

Avoid:
- Duplicating each Core scenario at the GUI level.
- Asserting micro edge cases (formatting, parsing nuances) via UI automation.

---

## Practical Structuring Tips

### Core Feature Tests
- Organize by domain capability (e.g., `Filtering`, `Parsing`, `Aggregation`, `Export`).
- Use scenario-style naming: `GivenInvalidTimestamp_WhenParsing_ThenErrorIsReturned`.
- Employ test data builders / fixtures to keep Arrange sections terse.
- Keep one primary behavioral assertion per test; support with minimal secondary assertions.

### Integration / Adapter Tests
- Target only risk areas (real file IO, large datasets, concurrency, external formats).
- Use them to detect differences between mocked vs real environments.

### GUI Feature Tests
- Name after user goals: `User_CanFilterResultsByDateRange`.
- Cover only: onboarding/open-first-file, critical editing/processing workflow, save/export, error surfacing, essential accessibility flows.
- Prefer stable selectors (automation IDs) over fragile visual locators.
- Limit per-test assertions to the core success/failure outcome plus essential UI state checks.

---

## Refactoring Guardrail

Workflow:
1. Add/extend logic with Core feature tests first.
2. Only add a GUI test if the user journey itself is novel or the UI layer adds logic.
3. When a regression appears in GUI:
   - If Core tests fail too → logic issue.
   - If only GUI fails → presentation/wiring issue.

---

## Example Categorization

| Scenario | Layer |
|----------|-------|
| Log file parsing edge cases (malformed entries) | Core |
| Filtering by severity with multiple overlapping filters | Core |
| Large dataset performance threshold | Core (possibly Integration if real IO) |
| User applies a filter through menu → list updates | GUI (single journey) |
| Keyboard shortcut triggers refresh | GUI |
| Accessibility: Tab order for main workflow | GUI |
| Export formatting (CSV column ordering) | Core (Integration if real write) |

---

## Migration Strategy (If Current GUI Suite Is Heavy)

1. Inventory existing GUI tests → tag each as Logic, Workflow, or UI-only.
2. For those tagged Logic: extract assertions into new/existing Core feature suites.
3. Keep only one GUI test per unique workflow; remove near-duplicates.
4. Add missing Core tests to cover any removed GUI logic assertions.
5. Introduce a smoke pack: a tiny subset (e.g., 5–10 tests) that runs on every PR; run the full GUI pack nightly.

---

## Anti‑Patterns to Watch For

- GUI test asserting internal data structures or exact serialized JSON unless UI transforms them.
- Multiple GUI tests differing only by input edge cases already covered in Core.
- Overuse of sleeps/timeouts instead of waiting on explicit signals (leading to brittleness).
- Bloated “God” end‑to‑end tests covering many orthogonal behaviors (hard to diagnose failures).

---

## Summary

Concentrate depth and breadth of behavioral verification in Core feature tests; curate a minimal, high-value set of GUI feature tests for genuine end‑user journeys and UI-specific logic. This approach:
- Accelerates feedback
- Reduces flakiness
- Lowers maintenance cost
- Increases refactor confidence

---

## Optional Next Steps

- Provide a sample Core feature test template.
- Suggest a folder and naming taxonomy.
- Help triage existing GUI tests for migration.
- Propose a CI matrix (fast vs full suites).

Let me know which you’d like next.