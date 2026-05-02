# Issue 252 - Visual Studio 2026 target location strategy

## Objective
Determine the simplest and most reliable way to display a "target location" in Visual Studio 2026 when input is either:

1. A fully qualified method name (FQN), or
2. A `FilePath + LineNumber` pair.

## Recommendation (TL;DR)
Use a **two-path strategy**:

1. **Primary path (most reliable):** resolve to `FilePath + LineNumber`, then open that file and move caret to the line.
2. **Fallback path (when only FQN is available):** ask Visual Studio to navigate to the symbol by FQN.

This keeps behavior deterministic whenever a file location is known, while still supporting symbol-only input.

---

## Why this is the simplest reliable approach

### `FilePath + LineNumber` is stable and deterministic
- It avoids symbol ambiguity (partial classes, overloads, generated code).
- It does not depend on successful symbol indexing.
- It is straightforward to test with integration tests.

### FQN-first navigation is useful but less deterministic
- FQNs can be ambiguous across overloads, generic arity, or multiple projects.
- Symbol databases can be temporarily stale.
- Generated source can alter the physical location experience.

So the design should treat FQN as input that is ideally **resolved to file/line before navigation**.

---

## Proposed behavior contract

### Input normalization
- Accept either:
  - `TargetMethodFqn`, or
  - `TargetFilePath` + `TargetLineNumber`.
- If both are present, prefer `TargetFilePath + TargetLineNumber`.

### Navigation order
1. If file path exists and line number > 0:
   - Navigate directly to file and line.
2. Else if FQN exists:
   - Attempt symbol navigation by FQN.
3. If neither succeeds:
   - Show explicit message with original target value and failure reason.

### User feedback
- On success: report exact resolved location shown in VS (file + line if available).
- On fallback: report that symbol navigation was used.
- On failure: include actionable guidance (e.g., ensure solution is loaded and fully indexed).

---

## Suggested implementation shape (future)

Create a small adapter abstraction for VS navigation:

- `TryNavigateToFileLine(filePath, lineNumber)`
- `TryNavigateToSymbol(fqn)`

And an orchestrator:

- `TryShowTargetLocation(target)` that follows the navigation order above.

This keeps the feature easy to test and allows IDE-specific behavior changes without touching domain logic.

---

## Acceptance criteria for issue 252

1. Given valid `FilePath + LineNumber`, Visual Studio 2026 opens file and positions caret on requested line.
2. Given invalid file path but valid FQN, Visual Studio attempts symbol navigation.
3. Given ambiguous FQN, user receives clear disambiguation/failure message.
4. Telemetry/logging captures which path succeeded (`file-line` or `symbol`).
5. Integration tests cover at least one success and one fallback case.

---

## Non-goals
- Deep refactoring of Weevil core logic.
- Changes to parsing/analyzer behavior unrelated to navigation.
- IDE support beyond Visual Studio 2026 in this issue.
