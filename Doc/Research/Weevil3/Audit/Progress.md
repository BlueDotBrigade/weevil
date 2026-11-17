# Weevil 3.0 Audit: Progress

An independent review of the Weevil 3.0 code on `main` against Weevil 2.x. It asks whether the new code captures its **intent** clearly and is **maintainable**, whether the architecture suits large logs in RAM and on disk, and whether the claims about CPU and RAM hold.

> **Status:** Stage 1 done (all units Final). Stage 2 drafted (`Findings/Performance.md`). Stage 3 drafted (`Findings/CrossCutting.md`, one High: X-1). Stage 4 next.
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
| Benchmarks | Run here, on Linux, with the .NET 10 SDK installed. Label every result as Linux. Compare 2.x with `main` on the same machine |
| Benchmark logs | **4 GB is the largest log this environment can open** (owner, 2026-11-16; uploads are limited to 30 MB). Generate synthetic logs with the [memory harness](../MemoryHarness/README.md); use the 10-million-record droid logs from Git LFS (`git lfs pull`, about 141 MB) for numbers comparable with the repo's. Larger logs, such as the 20 GB disk-mode target (#1024), are a gap for the owner to measure on Windows |
| Pacing | Run every stage without stopping. Commit and push after each unit |
| Agents | At most four background agents at a time (owner, 2026-11-17). Usage limits interrupted two runs, so each agent now writes and pushes its own file |
| Sceptic | A fresh-context agent tries to disprove each **high-priority** finding before it is recorded |
| Rules | No changes to source code. No GitHub issues. Only files in this folder change. Compiler warnings are expected, from the many analyzers turned on; ignore them |

## Plan

| Stage | Work | Output | Status |
|---|---|---|---|
| 0 | Map the changed files to the epics; define review units | [Scope.md](Scope.md), [scope.py](scope.py) | Done |
| 1 | Review units U1 to U7 through every lens below | `Findings/U<n>-<Name>.md` | Done |
| 2 | Check the performance claims: build both branches, run the memory harness and the benchmarks on Linux | `Findings/Performance.md`, [Measurements/](Measurements/README.md) | Drafted |
| 3 | Cross-cutting assessment: how the units fit together, RAM vs disk, room for #991 | `Findings/CrossCutting.md` | Drafted |
| 4 | Sceptic pass on high-priority findings not yet challenged; write the report | `Report.md` | Not started |

### Stage 1 Units

A unit's status is the status note at the top of its findings file:

- **No file:** not started, or its review was lost. Review it again.
- **Draft, sceptic pass pending:** reviewed; its high-priority findings are not yet challenged.
- **Final:** reviewed and challenged.

| Unit | Area | Findings file |
|---|---|---|
| U1 | Line index and parsing (#988) | `Findings/U1-LineIndex.md` |
| U2 | Record representation (#988) | `Findings/U2-RecordRepresentation.md` |
| U3 | Disk mode and the log file source (#986) | `Findings/U3-DiskMode.md` |
| U4 | Engine composition and session state (#990) | `Findings/U4-EngineAndSession.md` |
| U5a | Selection (#990) | `Findings/U5a-Selection.md` |
| U5b | Filtering, navigation, analyzers (#990, #988) | `Findings/U5b-FilterNavigationAnalysis.md` |
| U6a | `FilterViewModel` (#990, #986) | `Findings/U6a-FilterViewModel.md` |
| U6b | The grid over Core (#990) | `Findings/U6b-Grid.md` |
| U7 | Open As dialog and file browser (#1157, light) | `Findings/U7-OpenAsDialog.md` |

U5 and U6 are split in two because of their size.

How units were reviewed:

- **U1:** two reviewers, one for design and intent, one for runtime, tests and documents. The main session merged their findings and checked the key references.
- **U2 to U7:** after two usage-limit interruptions, each unit has one reviewer covering every lens, who writes and pushes the file.
- **All units:** a sceptic then challenges each High finding and records its verdict in the file.

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

## Existing Measurements

Stage 2 checks these; it does not start from nothing.

- **Memory harness:** [`../MemoryHarness/`](../MemoryHarness/README.md) has dated results from 2026-10-20 (Weevil 2.12.1, the baseline) to 2026-11-14 (reopen, shared blocks). `-p:WeevilVersion=2.12.1` builds it against 2.12.1 from NuGet, so both versions open the same synthetic logs. Baseline: 2.12.1 opened a 4 GB log with a 13.0 GB peak on a 14.2 GB Linux container.
- **Benchmarks:** `Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/Benchmarks/` (BenchmarkDotNet, #1177) exists only on `main`, so it measures 3.0 alone.
- **Budget tests:** `Core-PerformanceTests` holds memory-per-record and time-limit tests (#1086, #1091, #1103).
- **Research:** `../RawContent/`, `../RecordsOnDemand/`, `../TimestampPoC/`, `../VirtualListPoC/` each record timings behind a decision.

## External Evidence: SonarCloud

[SonarCloud](https://sonarcloud.io/project/overview?id=BlueDotBrigade_weevil) analysed `main` at `7c6a061c` on 2026-11-16. Read through its public API, for example `https://sonarcloud.io/api/issues/search?componentKeys=BlueDotBrigade_weevil&resolved=false&facets=rules`.

| Measure | Value |
|---|---|
| Lines of code | 28,509 |
| Open issues | 1,008: 944 code smells, 34 bugs, 30 vulnerabilities |
| Ratings | Maintainability A (2,250 min of debt); Reliability D (from the bugs) |
| Duplication | 2.3% |
| Coverage | 67.8% |
| Cognitive complexity | 3,527 in total |

**Most issues come from the Roslyn rules that the build turns on, not from Sonar's own rules.** At least 622 of the 1,008 are `external_roslyn:*`, mostly style: `IDE0008` (explicit type, 128), `CA1704` (spelling, 73), `MSTEST0049` (60), `IDE0028` and `IDE0009`. Sonar's own rules (`csharpsquid:*`) raise about 340. Treat the total as noise. The few Sonar rules below are worth using as evidence.

Sonar cannot tell new code from code inherited from 2.x. Before citing one of its issues as a 3.0 problem, check `git blame` or `Releases/2.x`.

Leads by unit:

| Unit | Sonar lead |
|---|---|
| U1 | Cognitive complexity (S3776): `LineIndex.cs:349` 28, `LogLineReader.cs:133` 21 and `:289` 16 |
| U2 | `RecordTable.cs:333` 19, `InMemoryRecordRepository.cs:303` 16; a possible null `nextSection` (S2259) at `RecordCollectionExtensions.cs:440` |
| U4 | `SidecarLoader.cs:194` 36 and `:57` 23; a possible null `_sidecar` (S2259) at `SidecarLoader.cs:62,70` |
| U5 | `FilterManager.cs:134` 20, `FilterStrategy.cs:129` 20, `RecordSearch.cs:52` 22; timeline analyzers 18–46, which #990 and #988 changed only mechanically, so the complexity is probably inherited; regular expressions without a timeout (S6444) in `ContentNavigator.cs:55` and `CommentNavigator.cs:54,90` |
| U6 | `FilterViewModel.cs` has six methods over 15 (the worst is 35, at line 769) and 11 calls that ignore the available cancellation token (S8949, lines 591–2151) |

## Findings So Far

**No unit has a High finding.** Two High candidates were raised; the sceptic lowered U1-1 to Medium, and no other unit raised one.

| Unit | Counts (High / Medium / Low) | Headline |
|---|---|---|
| U1 | 0 / 7 / 5 | Careful classes; the open-and-save policy is spread across classes and flags (U1-1); "no source means RAM" is a null protocol (U1-2); users aren't told about the `.idx` (U1-5) |
| U2 | 0 / 3 / 3 | `Record` is now truly immutable; `IRecord.Content` reads the log on disk and can throw, and its contract doesn't say so (U2-1); a record keeps its 16 MB chunk alive (U2-2) |
| U3 | 0 / 4 / 3 | One small class owns reads from the log; a read can fail only on disk, documented in one paragraph (U3-1); disk filters allocate far more than the table (U3-2, confirmed as P-1) |
| U4 | 0 / 3 / 4 | Records hold no state, stores have one home; Clear now saves the sidecar first and fails where it can't (U4-1); no type for "the log" against "a view", the first obstacle for #991 (U4-2) |
| U5a | 0 / 2 / 4 | Ranges are sound; `ISelect` keeps two ways to select with different rules (U5a-1) |
| U5b | 0 / 2 / 3 | Filters keep 2.x semantics and are far faster; analyzers are near-copies changed by hand (U5b-1); a filter is stopped by a shared 2.x flag and no contract says which thread owns the engine (U5b-2) |
| U6a | 0 / 3 / 2 | Epics put logic in Core, not here; "is anything running" is seven flags (U6a-1); log lifetime and view state share one class (U6a-2); F5 still filters on the UI thread, which on disk reads the log (U6a-3) |
| U6b | 0 / 3 / 2 | One of the clearest parts of 3.0: Core owns the selection, the GUI owns the caret; the property that lets it scale (WPF never walks `RowList`) is neither tested nor detected (U6b-1); no design note, and the GUI instructions contradict three choices (U6b-3) |
| U7 | 0 / 2 / 3 | A sound foundation that nothing uses yet; `Location.Parse` checks the disk on the UI thread (U7-1); nothing guards the "knows nothing about Weevil" boundary (U7-2) |
| Stage 2 | 0 / 2 / 3 | Claims confirmed: 62% less memory at the peak, filters 4–24× faster; disk filters churn 16 MB buffers (P-1); a mode switch is a full parse (P-2) |

Themes that recur across units, for Stage 3:

- **A read can fail only on disk, and the contract doesn't say so:** U2-1, U3-1, U6a-3.
- **The log, the view and the session have no types of their own:** U4-2, U6a-2. This is the first obstacle for #991.
- **No design note** for the open sequence (U1-1), the engine and session (U4-3) or filtering (U5b-5). The finished plans are the only descriptions.
- **Concurrency is a GUI convention:** U5b-2, U6a-1.
- **2.x API kept beside the 3.0 API:** U5a-1, U5a-2.

## Open Questions

- None yet.

## Next Steps

1. **Stage 3.** Write `Findings/CrossCutting.md`. Read every findings file's Summary and Findings headings (not the full files), test the themes listed under **Findings So Far**, decide whether any combination deserves High (for example U6a-3 with U2-1 and U3-1: disk reads on the UI thread), and assess RAM against disk mode as a whole and the room for #991.
2. **Stage 4.** One task at a time (owner, 2026-11-17): a sceptic on each High finding still unchallenged; then `Report.md` with the structure in the owner's brief (executive summary, architecture, code quality, CPU, RAM, trade-offs, prioritised recommendations, final assessment).

## Log

| Date | Session work |
|---|---|
| 2026-11-17 (later) | The usage limit stopped U5b, U6a, U6b and U7; relaunched two at a time. Stage 2: timed the `.idx` open paths, filtered the 4 GB log on disk with the heap capped, confirmed the cause of P-1 with an instrumented scratch build and measured a prototype fix. Found P-4 (temporary-folder check) in passing. Wrote `Performance.md` |
| 2026-11-17 | Two workflow runs stopped at the usage limit; only U1's two reviews survived. Wrote U1 from them. Installed the .NET 10 SDK from Ubuntu's archive (`apt-get install dotnet-sdk-10.0`; Microsoft's download hosts are blocked here). Measured memory of 2.12.1 and `main` on 1 GB and 4 GB logs, and filter and Find times on 1 GB. Relaunched U2 to U7 with one reviewer each |
| 2026-11-16 | Agreed the plan with the owner. Stage 0: mapped 454 commits and 113 source files to the epics; defined units U1–U7. Recorded SonarCloud's leads. Started Stage 1 |
