# Issue 252 - Visual Studio 2026 Target Location Strategy

## Objective
Given either:
1. a fully qualified method name (FQMN), or
2. a `FilePath + LineNumber`,

open Visual Studio 2026 at the target location using the **simplest** and most **reliable** approach.

---

## Recommendation (TL;DR)
Use a **single canonical target format** internally:

```text
ResolvedTarget = { AbsoluteFilePath, LineNumber }
```

Then always launch Visual Studio using:

```powershell
devenv.exe /Edit "<AbsoluteFilePath>" /Command "Edit.GoTo <LineNumber>"
```

This keeps one stable display path and avoids UI-driven search behavior.

---

## Why this is the simplest and most reliable approach

### 1) `FilePath + LineNumber` is deterministic
- It directly identifies a physical source location.
- It does not depend on indexing freshness, naming ambiguities, or current solution filters.
- It is easy to validate before launching Visual Studio.

### 2) `devenv /Edit + Edit.GoTo` is scriptable and explicit
- `/Edit` opens the concrete file target.
- `Edit.GoTo` puts focus on the exact line.
- This avoids fragile keyboard automation or command-palette text entry.

### 3) FQMN can be normalized into the same deterministic format
- FQMN inputs should be resolved to `FilePath + LineNumber` first.
- Once resolved, use the same launch pathway above.
- This creates one tested execution path instead of separate behaviors.

---

## Resolution rules

### Input A: `FilePath + LineNumber`
1. Normalize to absolute path.
2. Validate file exists.
3. Clamp line number to `[1, maxLines]` (or reject if strict mode is desired).
4. Invoke:
   - `devenv.exe /Edit "<path>" /Command "Edit.GoTo <line>"`

### Input B: Fully Qualified Method Name
1. Resolve FQMN against the loaded solution (Roslyn symbol resolution recommended).
2. If exactly one source definition is found:
   - extract `AbsoluteFilePath + LineNumber`.
   - invoke the same command as Input A.
3. If zero or multiple matches:
   - return a structured "cannot uniquely resolve" result.
   - optionally include candidate locations for user selection.

---

## Non-recommended alternatives

### A) Directly searching for FQMN in Visual Studio UI
Examples: Go To All text query injection, Find-in-Files command strings.

Why not:
- Can produce multiple ambiguous hits.
- Results depend on indexing scope/settings.
- Harder to make deterministic in automation.

### B) Keyboard/UI automation
Why not:
- Brittle across Visual Studio versions, themes, and focus states.
- Higher maintenance burden and flaky behavior.

---

## Proposed contract for future implementation

```text
TryDisplayTarget(input) -> DisplayResult

where:
- input is either
  - MethodTarget { FullyQualifiedMethodName }
  - FileTarget { FilePath, LineNumber }
- DisplayResult includes
  - Success/Failure
  - FailureReason (NotFound, Ambiguous, InvalidInput, LaunchError)
  - ResolvedTarget? { FilePath, LineNumber }
  - Candidates? [ResolvedTarget]
```

This keeps the implementation testable while preserving a single display mechanism.

---

## Minimum acceptance criteria
1. `FilePath + LineNumber` opens Visual Studio at the requested file and line.
2. A uniquely resolvable FQMN opens at its definition.
3. Ambiguous FQMN does not guess; it returns candidates.
4. Invalid file path or invalid line number produces clear failure output.
5. All automated tests remain green after integration work.

---

## Summary decision
The best strategy for Visual Studio 2026 is:

1. **Normalize every input to `FilePath + LineNumber`.**
2. **Use `devenv.exe /Edit ... /Command "Edit.GoTo ..."` as the only display path.**

This is the lowest-complexity approach with the highest determinism and easiest long-term maintenance.
