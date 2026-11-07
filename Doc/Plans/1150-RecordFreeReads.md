# 1150: Read the Record Table Without Creating Records

> **Status:** In progress
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Follow-up to #1140 (step 3.6 of [plan 988](988-IndexingService.md)). Since #1140 a record is created each time a row is read, so code that reads every row and needs only a few facts pays for a record per row. Branch `Feature/1150-RecordFreeReads`, pull request #1152.

## Decisions

- **2026-11-07, the user:** elapsed times are mostly displayed, and only sometimes filtered on or searched. The analyzers and insights may take slightly longer if not excessively. Later the same day the user asked for the analyzers and insights to be finished in this pull request too, starting from the prototypes.

## Steps

1. **Elapsed times are calculated when read.** Done (b40b23ed). `ElapsedTimes.Measure(records, position)` for a row; `Get(lineNumber)` calculates every line's on its first call.
2. **The elapsed-time search measures by position.** Done (08880e67).
3. **Find reads content by position.** Done (7858658b). `RecordList.ReadContent`, `ContentBuffer.Read(chunk, ...)`.
4. **The memory budgets fall.** Done (e6bd2b14). 160 bytes per record; 1.35 and 1.25 GB per GB of the droid logs.
5. **The time-gap and out-of-order analyzers create records only for the rows they flag.** Done (9e6a54cd).
   - Clear the analyzed rows' flags with one bulk call by line number, not `FlagSet.FlagOnly`, which would clear hidden lines too.
   - Read timestamps with `GetCreatedAt` and the UI-thread fact with a new `RecordList.GetFacts(position)`.
   - The prototype measured the elapsed-time analyzer at 0.07 s on 10 million records (0.57 s before, `main` 0.25 – 0.30 s, 2.12.1 0.41 s).
6. **The insights read columns.** Done in the commit after 9e6a54cd: `SeverityMetrics.Count(severity, line, createdAt)`; `TimeGapInsight` needed nothing more. `CriticalErrorsInsight` (severity), `TemporalAnomalyInsight` (timestamps) and `TimeGapInsight` (through `TimeGapUiAnalyzer`) run after every open and read every record. Time `GetInsights` before and after.
7. **Documentation and the pull request.** The Core and Common rules, plan 988 and the description of #1152.

## How to Measure

On the 10-million-record UTF-8 droid log (`Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/.Daten/.Global/10MillionRecords_UTF8~ZIP`), in Release, alternating this branch and `main` in one session: each operation after 28 filters, median of 5 runs after a warm-up. The machine's speed varies widely between sessions, so compare only numbers from the same session.
