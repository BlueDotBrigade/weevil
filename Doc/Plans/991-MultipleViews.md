# 991: Multiple Views of the Same Log

> **Status:** Draft
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Phase 5 of the [Weevil 3.0 feasibility study](../Research/Weevil3/Feasibility.md). Epic #991; user story #195. It needs #990, #988 and #986.

**Implemented last,** so the release does not depend on it. No earlier phase needs anything from this plan. Freeing RAM with one view (#988) and reading cleared content back from the file (#986) work without it; this plan only extends them to several views.

The API, Clear and memory design is in [EngineApi.md](../Research/Weevil3/EngineApi.md).

## Goal

- An analyst opens another view of an open log. Each view has its own record scope, filter, results, selection and elapsed times.
- Two views can clear different records.
- All views share one copy of the log and one session: a comment, pin, bookmark or region added in one view appears in every view.
- In RAM mode, content is released only when no view still includes it.
- Another view adds only its own state: its scope, its results as record numbers, its selection and its active record.
- The sidecar is saved once per log.

## Decisions Needed First

1. **Flags:** do analyzer flags belong to one view, or to the log?
2. **Window or tab:** #195 describes a new window.
3. **What a new view starts with:** the whole log and no filter (suggested), or a copy of the current view's scope and filter.

## Steps

One sub-issue and one pull request per step, in order.

1. **Views in Core.**
   - `IEngine.CreateView()` returns another engine that shares the internal `LogSession`. Callers never see the session.
   - Each engine owns its record scope, filter, results, selection, active record and metric counts.
   - Filter history stays in the session, because the sidecar saves it per log.
   - A new view starts as decided in decision 3.
2. **Free RAM across views.**
   - A content chunk is released only when it lies outside every view's scope. The combined scope is recalculated whenever a view clears, restores, opens or closes; scopes are short range lists, so this is cheap.
   - A view whose scope grows, through `RestoreRecords()` or as a new view, reads the chunks it needs back from the file (#986).
   - Test: two views clear different ranges; only the chunks both exclude are released; closing one view releases the chunks only it kept.
3. **Open another view in the GUI,** as a window or a tab (decision 2). Annotation changes already reach every row through session events (#990), so all views stay in step.
4. **Closing.** Disposing a view frees its state and recalculates which chunks are still needed. Disposing the last view closes the session. Saving stays explicit, as today.

No proof of concept is needed: the design follows from #990, #988 and #986.

## Risks

- Two views can change the same annotation at once. The session's single lock (#990) orders the writes, and the last write wins, as with two clicks today.
- Views that each keep a different small range hold whole chunks: up to about 16 MB at each edge of each view's ranges.
