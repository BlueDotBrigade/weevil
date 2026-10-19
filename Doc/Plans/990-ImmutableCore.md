# 990: Immutable Core

> **Status:** Draft
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Phase 2 of the [Weevil 3.0 feasibility study](../Research/Weevil3/Feasibility.md). Epic #990.

## Goal

- Records hold no mutable state. Everything that changes lives in the session (annotations) or the view (record scope, filter results, selection, elapsed time).
- One lock guards every write to the session.
- The GUI subscribes to events once per window, not again after each Open, Clear or Reload.
- Behaviour is unchanged, apart from the decisions below. The sidecar format does not change.
- No `Metadata` object per record: about 12% less memory.
- Filtering is no slower.

[MutableStateInCore.md](../Research/Weevil3/MutableStateInCore.md) lists every value that moves, and what writes it today.

## Engine API

Callers open and use an engine without ever seeing its state. `LogSession` holds the log, annotations, bookmarks, regions and the sidecar, and is internal to Core. An `IEngine` is one view of it, with its own record scope, filter and selection, so no operation takes a token. This phase has one view per log; #991 adds `CreateView()`. See [EngineApi.md](../Research/Weevil3/EngineApi.md).

## Decisions

1. **Elapsed time** is the time between two consecutive visible records, the records that meet the filter (decided 2026-10-19). An `@Elapsed` filter therefore compares the records visible when it is applied, as it does today; afterwards, elapsed times are calculated for the new results.
2. **Flags belong to the view** (decided 2026-10-19 for #991), so different analyzers can run in different views. With one view in this phase, nothing changes for the user.
3. **Analyzers and insights replacing each other's flags.** Proposed: keep today's behaviour, awaiting confirmation. An analyzer clears the flags on the records it analyzes before setting its own, and opening an insight clears every flag, so the latest one wins.

## Steps

One sub-issue and one pull request per step, in order. Each step keeps every test passing.

1. **Subscribe once.** `Engine` forwards the filter's `ResultsChanged` and `HistoryChanged` events. Clear and Reload already swap the engine behind it, so the GUI's three re-subscriptions (`FilterViewModel.cs`) go away.
2. **Parsing facts are immutable.** Multi-line and UI thread are set when a record is created, in a facts byte with the severity, and cannot change afterwards.
3. **Annotation store.** Comments and pins move to a store in the session: an annotation byte per record (pinned, has comment) and a comment table keyed by line number. Flags move to the engine, one bit per record, because they belong to the view. `Metadata` forwards to both, so callers and bindings keep working. The sidecar is saved from the store, not by scanning every record. The store keeps the rule that adding a comment pins the record.
4. **Proof of concept: WPF virtual list.** On Windows, outside the product: a `ListView` bound to a virtual list of row view-models with 100 M rows. Check scrolling, Shift and Ctrl selection (keeping the #806 behaviour), comment editing, and the cost of `IndexOf`. This decides the row design for step 5 and for #988. Record the result in `Research/Weevil3/`.
5. **Row view-models.** The grid binds to one view-model per realized row: pin check box, comment box and elapsed-time column. A row writes to the session, and refreshes when the session reports a change to its line.
6. **Elapsed time comes from the view.** It is calculated for shown rows from the record before it in the results, as decision 1 describes. This also fixes an aborted filter erasing elapsed times that are still shown. That fix needs its regression test.
7. **`Metadata` removed from `IRecord`.** Filters, analyzers, navigators and reports use the session's annotation API, which is injected the way bookmarks already are. Records are now immutable.
8. **Clear without rebuilding.**
   - Clear narrows the engine's record scope, a short list of record-number ranges, using today's rules for each clear operation. The engine, its events and the annotations stay.
   - Memory: as today, records outside the scope are dropped. This is safe because records are immutable and annotations live in the session. #988 replaces it with releasing content chunks.
   - Today, unsaved comments and pins on cleared records appear to be lost on save (from reading `SidecarLoader.PackSidecar`). Confirm this with a test first, and keep that test as the regression test.

## Compatibility

- The sidecar format and the `.idx` format do not change.
- Plugins: the built-in TSV and default plugins move with each step. External plugins need not stay compatible.
- Line numbers, content and filter results stay identical, except for `@Elapsed` (decision 1).

## Risks

- Steps 3 and 7 touch every analyzer (13 set flags or comments) and the filter monikers. Keep each change mechanical.
- Session events fire on worker threads, for example when an analyzer runs. Row view-models must marshal them to the UI thread through `IUiDispatcher`.
- If the proof of concept shows WPF cannot handle the row design at 100 M rows, step 5 needs another approach before #988 can create records on demand.
