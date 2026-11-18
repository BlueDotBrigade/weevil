# Weevil 3.0 Audit: Findings

An independent audit of the three 3.0 epics: #986 disk mode, #988 less memory and #990 immutable core. 2026-11-17, `main` at `e3dee822`. Updated 2026-11-18 with the fixes merged and the owner's decisions since.

## Summary

- The three epics work, and the measurements back them.
  - A 4 GB log opens with 62% less memory than in 2.12.1 (4.9 GB, not 12.9 GB), and three times faster.
  - Filters are 4 to 24 times faster.
  - A log larger than RAM can now be opened.
  - Find is no faster.
- Code quality is high within each class, and lower between classes. Some rules are spread over several classes or kept by convention, and most of the large designs are described only in finished plans.
- The one serious bug, #1245, is fixed: F5 froze the window on a large log on disk.
- Nothing needs redesigning before 3.0 ships.

## The Epics

- **#990 Immutable core: done.** Records hold no state, and the selection lives in Core. One regression: #1246.
- **#988 Less memory: done.** A log takes 1.16 GB of RAM per GB, not 3.03 GB as in 2.12.1, and a reopen skips the parse.
- **#986 Disk mode: mostly done.** It works, and gives the same results as RAM. Still open: the 20 GB test (#1024) and the Open As dialog (#1157).

## Bugs

Fixed:

- #1245 F5 freezes the window while it re-runs the filter, and on a large log on disk it can't be stopped
- #1247 Weevil stops other applications from renaming or deleting a log while it reads it
- #1248 A new filter can cancel the Stop meant for the previous one, so a running filter can't always be stopped
- #1249 ISelect's record-based Select and Unselect throw for a record the filter hides
- #1250 GetSectionRecords throws for the last section of a log
- #1251 Closing the Open As dialog during a zip extraction leaves a full copy of the log in %TEMP%
- #1269 A filter on a log opened on disk allocates a new 16 MB buffer again and again, so it can run out of memory
- #998 Go to Record doesn't show an insight's record when the filter hides it (existing)

Open:

- #1246 Clear fails in a folder where the sidecar can't be written
- #1252 A log of more than 2,147,483,647 lines fails to open with an OverflowException
- #1294 The #IPv4 filter alias probably never matches an IP address in a TSV log (to be confirmed)

## Proposed Refactoring

- #1255 Core passes the storage mode as an enum, not as a missing content source
- #1256 Give the saved index's open-and-save rules one home
- #1257 Replace FilterViewModel's seven "is anything running?" flags with one busy state
- #1258 Share the analyzers' common code, so a change to the record model is made once
- #1259 Add a memory budget for a filter, in RAM and on disk
- #1293 Remove the 2.x shapes 3.0 kept, and hide Core's stores, before 3.0's API is published (technical debt)
- #1295 One exception for a log on disk that can no longer be read, documented on IRecord.Content
- #1296 Reopening a UTF-8 or UTF-16 log in the other storage mode reuses its saved index instead of parsing it again

## User Stories

- #1253 Keep the comments, pins and bookmarks of a log opened from a zip
- #1254 Refuse to open a log that another Weevil already has open
- #1219 Reload a partial open (existing; Reload now re-runs the same part against the latest data)

## Documentation

- #1268 Design notes for opening a log, the engine and session, filtering and the record grid: done
- The #991 plan (multiple views, 3.1) now holds the audit's findings for it
- Help doesn't explain the `.idx` file, by the owner's decision

Evidence: [Report.md](Report.md). Finding-to-issue map: [Issues.tsv](Issues.tsv).
