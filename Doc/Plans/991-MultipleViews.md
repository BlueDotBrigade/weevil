# 991: Multiple Views of the Same Log

> **Status:** Draft
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Phase 5 of the [Weevil 3.0 feasibility study](../Research/Weevil3/Feasibility.md). Epic #991; user story #195. It needs #990, #988 and #986.

**Implemented last,** so the release does not depend on it. No earlier phase needs anything from this plan. Freeing RAM with one view (#988) and reading cleared content back from the file (#986) work without it; this plan only extends them to several views.

**This milestone builds the Core only** (decided 2026-10-20). The GUI for several views comes in the next milestone, built with the latest [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit).

The API, Clear and memory design is in [EngineApi.md](../Research/Weevil3/EngineApi.md).

## Goal

- Core can open another view of an open log. Each view has its own record scope, filter, results, selection and elapsed times.
- Two views can clear different records, and run different analyzers: each view has its own flags.
- All views share one copy of the log and one session: a comment, pin, bookmark or region added in one view appears in every view.
- In RAM mode, content is released only when no view still includes it.
- Another view adds only its own state: its scope, its results as record numbers, its selection and its active record.
- The sidecar is saved once per log.

## Decisions

- Decided 2026-10-19: Clear and flags belong to each view.
- Decided 2026-10-20: the GUI questions wait for the next milestone: a window or a tab (#195 describes a window), what a new view starts with, and how Reload works with several views.
- Whether `IEngine` is `IDisposable`, and what closing a view means: #1030.
- Decided 2026-11-17, after the [Weevil 3.0 audit](../Research/Weevil3/Audit/Findings/CrossCutting.md#x-3-991-will-find-the-data-model-ready-and-the-object-model-not) (X-3): multiple views are for Weevil 3.1.
  - **One immutable Log per file, one Session, N Views.** Reload builds a new Log and replaces the old one.
  - **Each view has its own `FilterManager`** and one owner thread.
  - **Core stays synchronous and cancellable, with no `async`/`await`.** Long operations take a `CancellationToken`.
- Decided 2026-11-17 (#1219): **Reload re-runs each view's request against the latest data.** A region or section is found again by name, "from line N" grows to include appended lines, and a fixed range stays fixed.

## Steps

One sub-issue and one pull request per step, in order.

0. **Make room for views** (audit finding X-3). The data model is ready; the object model is not. Before step 1:
   - **Extract the Log from `CoreEngine`** (U4-2). Give a read of the log (record table, content chunks, line index, source) its own type, apart from a view (scope, filter, results, selection, flags). Decide what Reload keeps in one place, not five.
   - **Save the sidecar from the session's stores,** not from one view's scope (U4-1, #1246).
   - **One cancellation per filter run** (U5b-2): done in 3.0 by #1248. Each view's `FilterManager` has its own.
   - **Split `FilterViewModel`** into the open log's lifetime and one view's state (U6a-2), with one busy state (U6a-1, #1257).
   - **Views keep line numbers, not records** (U2-2). A record keeps its whole 16 MB chunk alive, so a view or window that keeps records keeps an old Log's content after Reload.

1. **Views in Core.**
   - `IEngine.CreateView()` returns another engine that shares the internal `Session`. Callers never see the session.
   - Each engine owns its record scope, filter, results, flags, selection, active record and metric counts. Flags already live in the engine (#990 step 3).
   - Filter history stays in the session, because the sidecar saves it per log.
   - A new view starts with the whole log and no filter; the caller can apply a filter. The GUI may choose otherwise in the next milestone.
2. **Free RAM across views.**
   - A content chunk is released only when it lies outside every view's scope. The combined scope is recalculated whenever a view clears, restores, opens or closes; scopes are short range lists, so this is cheap.
   - A view whose scope grows, through Reload or as a new view, reads the chunks it needs back from the file (#986).
   - Test: two views clear different ranges; only the chunks both exclude are released; closing one view releases the chunks only it kept.
3. **Open another view in the GUI:** next milestone. Annotation changes already reach every row through session events (#990), so all views will stay in step.
4. **Closing.** Closing a view frees its state and recalculates which chunks are still needed. Closing the last view closes the session. Saving stays explicit, as today. Whether closing means `Dispose` is #1030.

No proof of concept is needed: the design follows from #990, #988 and #986.

## Risks

- Every long operation in Core must take a token and leave nothing half done when cancelled (audit X-5). Several views multiply the operations that run at once.

- Two views can change the same annotation at once. The session's single lock (#990) orders the writes, and the last write wins, as with two clicks today.
- Views that each keep a different small range hold whole chunks: up to about 16 MB at each edge of each view's ranges.
