# Weevil 3.0 Audit: Progress

An independent review of the Weevil 3.0 code on `main` against Weevil 2.x. It asks whether the new code captures its **intent** clearly and is **maintainable**, whether the architecture suits large logs in RAM and on disk, and whether the claims about CPU and RAM hold.

> **Status:** Stage 0 done. Next: Stage 1, unit U1.
>
> **AI agents:** Read this file first. It is the authoritative record of the audit. Continue from **Next Steps**. Do not repeat a completed step unless a finding needs checking again.

## Resume Here

1. Read this file, then [Scope.md](Scope.md).
2. Prepare git, since the clone is shallow:
   ```bash
   git fetch --unshallow origin
   git fetch origin "Releases/2.x:refs/remotes/origin/Releases/2.x"
   ```
3. Continue from **Next Steps** below.
4. Commit and push after each unit. Only pushed work survives: the cloud container is deleted when it goes idle.

## Decisions

Agreed with the owner on 2026-11-16.

| Topic | Decision |
|---|---|
| Epics | #986 Disk Mode, #988 Less Memory, #990 Immutable Core. #991 Multiple Views is a draft: check only that the design leaves room for it |
| Main deliverable | Code quality: intent and maintainability. Correctness gets a light check; this is not a bug hunt |
| CPU and RAM | Light: check the claims and the existing benchmarks against the code, and list the evidence gaps |
| Benchmarks | Run here, on Linux, with the .NET 10 SDK installed. Label every result as Linux. Compare `Releases/2.x` with `main` on the same machine |
| Sceptic | A fresh-context agent tries to disprove each **high-priority** finding before it is recorded |
| Rules | No changes to source code. No GitHub issues. Only files in this folder change. Compiler warnings are expected, from the many analyzers turned on; ignore them |

## Plan

| Stage | Work | Output | Status |
|---|---|---|---|
| 0 | Map the changed files to the epics; define review units | [Scope.md](Scope.md), [scope.py](scope.py) | Done |
| 1 | Review units U1 to U7, one at a time, through every lens below | `Findings/U<n>-<Name>.md` | Not started |
| 2 | Check the performance claims: build both branches, run the memory harness and the benchmarks on Linux | `Findings/Performance.md` | Not started |
| 3 | Cross-cutting assessment: how the units fit together, RAM vs disk, room for #991 | `Findings/CrossCutting.md` | Not started |
| 4 | Sceptic pass on high-priority findings not yet challenged; write the report | `Report.md` | Not started |

### Stage 1 Units

| Unit | Area | Status |
|---|---|---|
| U1 | Line index and parsing (#988) | Not started |
| U2 | Record representation (#988) | Not started |
| U3 | Disk mode and the log file source (#986) | Not started |
| U4 | Engine composition and session state (#990) | Not started |
| U5 | Filtering, selection, navigation, analyzers (#990, #988) | Not started |
| U6 | GUI over the new Core (#990, #986) | Not started |
| U7 | Open As dialog and file browser (#1157, light) | Not started |

## How Each Unit Is Reviewed

Read the code itself, not only the diff. Compare with `Releases/2.x` where the unit replaced something. Read the unit's design note in `Doc/Design/` and its plan, as claims to check rather than facts.

### Lenses

1. **Intent.** Can a reader tell what the code is for, and why, from the code alone?
   - Names follow the domain language in `Doc/Design/`.
   - Invariants are enforced by types or guards, not only described in comments: immutability, index validity, a stale `.idx` after the log grows.
   - Ownership and lifetime are visible: who owns file handles, chunks and buffers; `IDisposable` where it matters.
   - Threading is visible: which thread owns which state, and what a lock protects.
2. **Maintainability.**
   - RAM vs disk: is the mode behind one abstraction, or do mode checks spread through layers?
   - Cohesion and size of classes; duplicated logic; abstractions that earn their place, or do not.
   - Layering: logic in Core, screen logic in the GUI; no Core dependency on plugins.
   - Tests read as specifications; feature tests run in both modes where behaviour should match.
   - Design notes match the code.
3. **Architecture.** Boundaries, data flow, extensibility, fitness for very large logs, room for #991.
4. **CPU and RAM (light).** Hot paths, allocations per record, repeated passes, retention. Record suspicions for Stage 2 to measure; do not claim a speed-up or regression without a measurement.
5. **Correctness (light).** Note a defect only when it is seen in passing.

### Recording a Finding

Significant findings use this form. Minor ones get one line in a "Minor" list.

```markdown
### U<n>-<k>. <Short title>

- **Priority:** High | Medium | Low
- **Kind:** Intent | Maintainability | Architecture | CPU | RAM | Correctness
- **New in 3.0?** Regression | New design | Existing weakness
- **Evidence:** file:line references, measurements
- **Impact:** what it costs, and whom
- **Recommendation:** the change, its benefit, its complexity (S/M/L)
- **Sceptic:** Not challenged | Upheld | Revised (why) | Withdrawn (why)
```

High-priority findings go to a sceptic agent with only the code and the claim, not this reasoning.

## Findings So Far

None yet. See [Scope.md](Scope.md#first-observations) for what to look at first.

## Open Questions

- None yet.

## Next Steps

1. **Stage 1, U1: Line index and parsing.** Start with `Doc/Design/RecordTable.md` and `Doc/Plans/988-IndexingService.md`, then `Core/IO/LineIndex.cs` (721 lines), `IndexSection.cs`, `LineIndexFile.cs`, `LogLineReader.cs`, `SavedParse.cs`, `SavedTableReader.cs`, `GrownLogParser.cs` and `Core/Data/MultiLineRecordParser.cs`. Compare parsing with `Releases/2.x`.
2. Write `Findings/U1-LineIndex.md`; update the unit table and **Next Steps**; commit and push.
3. Continue with U2.

## Log

| Date | Session work |
|---|---|
| 2026-11-16 | Agreed the plan with the owner. Stage 0: mapped 454 commits and 113 source files to the epics; defined units U1–U7 |
