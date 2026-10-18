# 991: Multiple Views of the Same Log

> **Status:** Draft
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Phase 5 of the [Weevil 3.0 feasibility study](../Research/Weevil3/Feasibility.md). Epic #991; user story #195. Planned for the end of the Weevil 3.0 milestone, if time permits. It needs #990; after #988, a view costs much less memory.

## Goal

- An analyst opens another view of an open log. Each view has its own filter, results, selection and elapsed times.
- All views share one copy of the log and one session: a comment, pin, bookmark or region added in one view appears in every view.
- Another view adds only its own state: its results as record numbers, its selection and its active record.
- The sidecar is saved once per log.

## Decisions Needed First

1. **Clear:** does it narrow one view, or the log for every view?
2. **Flags:** do analyzer flags belong to one view, or to the log?
3. **Window or tab:** #195 describes a new window.

## Steps

One sub-issue and one pull request per step, in order.

1. **Views in Core.** A session holds several views. Each view owns its filter, results, selection, active record and metric counts. Filter history stays in the session, because the sidecar saves it per log.
2. **Open another view in the GUI,** as a window or a tab (decision 3). Annotation changes already reach every row through session events (#990), so all views stay in step.
3. **Closing.** Closing a view frees its state. Closing the last view saves the sidecar and closes the session.

No proof of concept is needed: the design follows from #990.

## Risks

- Two views can change the same annotation at once. The session's single lock (#990) orders the writes, and the last write wins, as with two clicks today.
