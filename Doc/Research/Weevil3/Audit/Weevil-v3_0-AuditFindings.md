# Weevil 3.0 Audit: Findings

An independent audit of Weevil 3.0 (`main` at `e3dee822`) against Weevil 2.x. It covers the three 3.0 epics: #986 (disk mode), #988 (less memory) and #990 (immutable core).

> **Status:** Final. 2026-11-17. The issues below were filed on 2026-11-17 with the owner's decisions. Evidence and measurements: [Report.md](Report.md), [Findings/](Findings/) and [Measurements/](Measurements/README.md).

## Executive Summary

**The three epics were delivered, and they work.** On the same machine, 3.0 opens a 4 GB log with 62% less memory than 2.12.1 (4.9 GB instead of 12.9 GB), three times faster (29 s instead of 90 s). It filters 4 to 24 times faster, with the same results. It reopens a log from its saved index without parsing it again. It can open a log larger than RAM, keeping only 22 bytes per record in memory. Find is the one operation that didn't get faster.

**The code is clear in the small and implicit in the large.**
- **Each new class is easy to read:** it says what it's for, why, and which issue created it. Tests grew by 78% and read as specifications.
- **The weakness is between classes.** Some rules are spread over several classes, some are kept only by convention, and three of the four largest designs are described only in finished plans, not in `Doc/Design`.
- **For the next maintainer,** that is the main cost of 3.0.

**One serious bug:** F5 freezes the window while it re-runs the filter, and on a large log on disk it can't be stopped (#1245). It's being fixed for 3.0.

**What came out of the audit:**
- **8 new bugs** and **1 existing bug** moved into 3.0;
- **2 user stories** and **1 decision** added to an existing story;
- **5 proposed refactorings;**
- **4 recommendations awaiting a decision.**

No architectural decision needs reversing before 3.0 ships.

## Were the Three Epics Implemented, With High Code Quality?

| Epic | Implemented? | Code quality |
|---|---|---|
| **#990 Immutable core** | **Yes.** Records hold no state. Every comment, pin, flag and elapsed time has one home, keyed by line number. Clear narrows the view in place. The selection is a set of ranges in Core, so selecting everything no longer freezes a filter | **Good classes, implicit lifetimes.** The session's stores are clear and lock-free to read. What Reload keeps is decided in five places, because the log, a view of it and the session have no types of their own. `FilterViewModel` grew by a third without new seams. One regression: Clear now fails in a read-only folder (#1246) |
| **#988 Less memory** | **Yes, and measured.** 1.16 GB per GB of log after opening (3.03 in 2.12.1); a saved index makes a reopen 3 to 4 times faster than a parse | **Careful and well tested.** Each class validates what it reads, and both line scanners are tested against `StreamReader`. The rules for what an open saves are spread over two classes and flags (#1256) |
| **#986 Disk mode** | **Mostly.** Disk mode works: 22 bytes per record at rest, the same results as RAM, every 64 KB block checked, and a changed log detected. Still open: the 20 GB proof of concept (#1024) and 11 of 12 pieces of the Open As dialog (#1157) | **One small class reads the log back.** Readers never ask the mode. But a read of a record's text can fail only on disk, and its cost differs, which is how #1245 arose |

**Overall:** the epics are implemented properly. Code quality is high at the level of classes and tests, and medium at the level of the design as a whole, where knowledge sits in plans and conventions rather than in code and `Doc/Design`.

## Findings

### Bugs

All in milestone 8 (Weevil 3.0).

| Issue | Title | Severity | Decision |
|---|---|---|---|
| #1245 | F5 freezes the window while it re-runs the filter, and on a large log on disk it can't be stopped | **High** | Fix: re-run the filter in the background, then refresh the UI |
| #1246 | Clear fails in a folder where the sidecar can't be written | Medium (regression) | File only, for now |
| #1247 | Weevil stops other applications from renaming or deleting a log while it reads it | Medium | Fix. The GUI names Ctrl+F5 instead of reloading by itself; the CLI and PowerShell warn and keep their result |
| #1248 | A new filter can cancel the Stop meant for the previous one, so a running filter can't always be stopped | Medium | Fix: one stop signal per filter run |
| #1249 | ISelect's record-based Select and Unselect throw for a record the filter hides | Medium | Fix: remove the 2.x methods |
| #1250 | GetSectionRecords throws for the last section of a log | Low | Fix: delete the dead code |
| #1251 | Closing the Open As dialog during a zip extraction leaves a full copy of the log in %TEMP% | Low (not yet reachable) | Fix before #1157 wires in the dialog |
| #1252 | A log of more than 2,147,483,647 lines fails to open with an OverflowException | Low | A clear message; keep the limit |
| #998 | Go to Record doesn't show an insight's record when the filter hides it | Medium | Existing issue, moved to milestone 8. Fix: apply the filter, then go to the record |

### Proposed Refactoring

No change in behaviour. No milestone; to be scheduled.

| Issue | Title | Why |
|---|---|---|
| #1255 | Core passes the storage mode as an enum, not as a missing content source | "No source means RAM" is read back from a null |
| #1256 | Give the saved index's open-and-save rules one home | What an open saves is spread over two classes and settable flags |
| #1257 | Replace FilterViewModel's seven "is anything running?" flags with one busy state | Each long operation sets seven flags by hand |
| #1258 | Share the analyzers' common code, so a change to the record model is made once | 3.0 changed 10 to 14 near-copies by hand, several times |
| #1259 | Add a memory budget for a filter, in RAM and on disk | Only the memory after opening is guarded |

**Not filed as issues.** These belong to the next release's multiple-views work (#991), and are recorded in its plan:
- give the log and a view their own types;
- save the sidecar from the session's store;
- split `FilterViewModel` along its seams.

### User Stories

| Issue | Title | Release |
|---|---|---|
| #1253 | As an analyst, I want the comments, pins and bookmarks I add to a log opened from a zip to be kept, in order to continue my analysis when I open it again | 3.0, under #1157 |
| #1254 | As an analyst, I want Weevil to refuse to open a log that another Weevil already has open, in order not to lose comments and pins to the other window's saves | 3.0 |
| #1219 | Show a partial open in the status bar, and reload it (existing) | 3.0. Decision added: Reload re-runs the same part (region, section or lines) against the latest data |

### Awaiting a Decision

No issues yet. To discuss one at a time.

| Recommendation | In plain English |
|---|---|
| **Filters on disk churn memory** (P-1) | Each filter on a log opened on disk allocates many 16 MB buffers instead of reusing four, so it briefly needs far more memory than the 22 bytes per record the documents promise. To be explained, and confirmed as a bug, before any work |
| **`IRecord.Content` on disk** (U2-1, U3-1) | On disk, reading a record's text opens the log file and can fail, but the interface doesn't say so. To be explained with an example |
| **Changing storage mode costs a full parse** (U1-7, P-2) | A log opened in RAM, then later on disk (or the reverse), is parsed again: 6 s instead of 1–2 s for a 1 GB log, although the saved table would serve both. To be discussed |
| **Tell users about the `.idx` file** (U1-5) | Every log now gets a `<log>.idx` beside it, about 22 bytes per record (167 MB for a 1 GB log). Help doesn't mention it |

## Where to Find More

- **[Report.md](Report.md):** the technical audit: architecture, code quality, CPU and RAM, with measurements.
- **[Findings/](Findings/):** every finding, with its evidence, file by file.
- **[Progress.md](Progress.md):** the owner's decisions after the audit.
- **[Issues.tsv](Issues.tsv):** each audit finding and the issue filed for it.
