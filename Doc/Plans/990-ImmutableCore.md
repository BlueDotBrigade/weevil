# 990: Immutable Core

> **Status:** In progress
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Phase 2 of the [Weevil 3.0 feasibility study](../Research/Weevil3/Feasibility.md). Epic #990.

## Goal

- Records hold no mutable state. Everything that changes lives in the session (annotations) or the view (record scope, filter results, selection, elapsed time).
- One lock guards every write to the session.
- The GUI subscribes to events once per open log, not again after each Clear or Reload.
- Behaviour is unchanged, apart from the decisions below. The sidecar format does not change.
- No `Metadata` object per record: about 12% less memory.
- Filtering is no slower.

[MutableStateInCore.md](../Research/Weevil3/MutableStateInCore.md) lists every value that moves, and what writes it today.

## Engine API

Callers open and use an engine without ever seeing its state. `Session` holds the log, annotations, bookmarks, regions and the sidecar, and is internal to Core. [EngineApi.md](../Research/Weevil3/EngineApi.md) calls it `LogSession`; it was renamed on 2026-10-26. An `IEngine` is one view of it, with its own record scope, filter and selection, so no operation takes a token. This phase has one view per log; #991 adds `CreateView()`. See [EngineApi.md](../Research/Weevil3/EngineApi.md).

## Decisions

1. **Elapsed time** is the time between two consecutive visible records, the records that meet the filter, skipping records without a timestamp as today (decided 2026-10-19). After each filter, the view calculates elapsed times for all its results and keeps them; records no longer store them (decided 2026-10-20). An `@Elapsed` filter therefore compares the records visible when it is applied, and iterating by elapsed time (#827, #828) covers all results, as today.
2. **Flags belong to the view** (decided 2026-10-19 for #991), so different analyzers can run in different views. With one view in this phase, nothing changes for the user.
3. **Annotations and flags are kept by line number** (decided 2026-10-20). Clear changes record positions but never line numbers.
4. **Grid updates never block** (decided 2026-10-20). A row learns of a change asynchronously, never through a synchronous `Invoke`. A bulk operation (Unpin All, Remove Comments, an analyzer) raises one event, and the grid then refreshes the rows on screen.
5. **Selection is stored as ranges, and Copy is limited to 100,000 records** (decided 2026-10-20). Selecting every record still works, so Clear Selected, Export and analyzers on a selection are unaffected. Copying more shows a message that points to Export.
6. **A `Session` holds the log's mutable state in child stores** (decided 2026-10-26). Records hold none, so the state that changes stays together and fewer threads write to it. `Session` is internal to Core; each store is a child class, such as the comment and pin store. Flags and elapsed times stay with the view (decisions 1 and 2).

## Steps

One sub-issue of #990 and one pull request per step, in order. Each step keeps every test passing. The step issues were created on 2026-10-20, so a change to the plan can be traced through them.

Step 6 comes before step 5 (decided 2026-10-22): it needs nothing from the row design, while step 5 waits for the results of the proof of concept in step 4. Until step 5, `Metadata.ElapsedTime` reads the view's elapsed times, as step 3 made `Metadata` read comments, pins and flags from their stores, so the grid binds as before.

1. **Subscribe once (#1011).**
   - `Engine` forwards the filter's `HistoryChanged` and `ResultsChanged` through new events on `IEngine`, which is public, in Common.
   - On Clear and Reload, `Engine` moves its subscription from the old filter to the new one, before Reload re-applies the filter.
   - The forwarded `HistoryChanged` keeps the history list as its sender; `OnFilterHistoryChanged` relies on it.
   - The GUI's re-subscriptions after Clear and Reload (`FilterViewModel.cs`) go away. Open still builds a new `Engine`, so the GUI subscribes once per Open.
2. **Parsing facts are immutable (#1012).** Multi-line and UI thread are set when a record is created, in a facts byte with the severity, and cannot change afterwards.
3. **Annotation store (#1013).**
   - Comments and pins move to a store in the session, kept by line number (decision 3): an annotation byte per line (pinned, has comment) and a comment table.
   - Flags move to the engine, one bit per line, because they belong to the view.
   - `Metadata` forwards to both, so callers and bindings keep working.
   - The store keeps both comment rules: adding a comment pins the record, and removing it unpins it.
   - The store loads every sidecar entry, including lines not in memory (an open from a start line, or a cleared line), so the sidecar can be saved from the store without losing them.
   - The store and the flags carry across Clear and Reload, as bookmarks and regions do today.
   - Unpin All, Remove All Flags and Remove Comments touch only records in memory today. The sub-issue decides whether they include cleared lines.
4. **Proof of concept: WPF virtual list (#1014). Done 2026-10-22: row view-models only for the rows WPF asks for, and the selection as ranges.**
   - At 100 M rows it met 21 of its 23 targets, and missed the other two by less than 10 ms: Ctrl+A took 21 ms with no extra memory, and keeping a selection across a filter at most 281 ms. WPF's own selection, as Weevil 2.12 uses it, does not scale: selecting rows again after a filter, one `Add` each, takes time that grows with the square of the count. See the [results](../Research/Weevil3/VirtualListPoC/README.md#results).
   - The proof of concept, as planned: on Windows, outside the product: a `ListView` bound to a virtual list of row view-models with 100 M rows, using the real row template, whose row heights vary.
   - Check scrolling, Shift and Ctrl selection (keeping the #806 behaviour), Ctrl+A, Shift+End, filtering with a large selection, comment editing, and the cost of `IndexOf`.
   - The list implements the non-generic `IList`, or WPF copies every item. Rows are equal by line number, because WPF finds items with `IndexOf`.
   - This decides the row design for step 5 and for #988. Record the result in `Research/Weevil3/`.
5. **Row view-models and selection (#1015). Done 2026-10-26.** Split into four pull requests: Core keeps the selection as ranges (#1055, done 2026-10-22), GUI row view-models (#1057, done 2026-10-23), GUI selection input, GUI feature tests (#1080, done 2026-10-26). Selection input is itself three pull requests: Core selects ranges of lines and keeps the anchor (#1062, done 2026-10-23), the grid handles clicks and keys (#1064, done 2026-10-23), then UI Automation and the Copy limit (#1075, done 2026-10-23; its tests fixed by #1077, done 2026-10-24).
   - The grid binds to one view-model per realized row: pin check box, comment box and elapsed-time column. A row writes to the session, and refreshes when the session reports a change to its line (decision 4).
   - A row ignores its own writes, so the comment box (two-way, with a 250 ms delay) does not overwrite text still being typed.
   - Larger than it looks: the row style binds `Severity`, four converters expect an `IRecord`, and code-behind casts items to `IRecord`.
   - Core's `SelectionManager` stores ranges instead of one record per selected row (decision 5).
   - Keeping the selection after a filter uses binary searches, or a merge of the old and new results where the old ones had gaps, as in the proof of concept, instead of comparing every result with every selected record.
   - WPF never selects a row: the grid handles clicks and the selection keys itself, as the proof of concept does for click, Shift, Ctrl, the arrow keys, Page Up, Page Down, Home, End and Ctrl+A. It also needs what the proof of concept left out: a right-click that selects its row before the context menu opens, keeping the anchor across a filter, and reporting the selection to UI Automation. Decided 2026-10-23: dragging the mouse selects nothing more than the press does, because nobody uses it; Ctrl+Shift+arrow adds to the selection, as in Weevil 2.12; typing a letter in the grid does nothing.
   - Decided 2026-10-22: a selected record that a filter hides is no longer selected, and stays unselected when a later filter shows it again. After the filter, a bulletin tells listeners that the selection count changed.
   - GUI feature tests cover the selection journeys, because the Core tests cannot see focus or key handling.
   - Copy stops at 100,000 records (decision 5).
6. **Elapsed time comes from the view (#1016). Done 2026-10-22.**
   - After each filter, the view calculates elapsed times for all its results (decision 1).
   - An `@Elapsed` filter reads the elapsed times of the previous results, which the view passes in. Iterating by elapsed time reads the current ones.
   - This also fixes an aborted filter erasing elapsed times that are still shown. That fix needs its regression test.
   - Add a test for `@Elapsed` applied after another filter, which nothing covers today.
7. **`Metadata` removed from `IRecord` (#1017).** Split into four pull requests (decided 2026-10-26). Until the last one, `Metadata` forwards to the stores, so callers not yet moved keep working.
   - **Core reads the session (#1082).** `Session` holds the comment and pin store (decision 6); the view keeps its flags and elapsed times. Filters, monikers, navigators, reports, export, the sidecar and the selection read and write the stores, which are passed in the way bookmarks already are. Unpin All, Remove Comments and Remove All Flags each become one operation on a store: one lock and one change event (decision 4), covering the same lines as today.
   - **Analyzers.** `IRecordAnalyzer.Analyze` receives `IAnnotations` and `IFlags`, so an analyzer can comment, pin and flag, but reach nothing else. The built-in analyzers and the TSV and default plugins move with it.
   - **GUI.** Copy, Paste, the rows' elapsed times and Go to Record read the stores.
   - **`Metadata` removed.** `IRecord`, `Record`, `Metadata` and `ViewAnnotations` go. Tests move to the stores (169 uses of `.Metadata.` in 35 files). Memory is measured against the goal.
   - External plugins built for 2.x already fail to load in 3.0; they are fixed later (see Compatibility).
   - Later, in a step of its own: bookmarks, regions, remarks, the table of contents and the filter history move into `Session`, under the same lock.
8. **Clear without rebuilding (#1018).**
   - Clear narrows the engine's record scope, using today's rules for each clear operation. The engine, its events and the annotations stay.
   - Clear keeps doing what it does today: it resets the filter (the GUI re-applies its own), drops cleared records from the selection, recounts the metrics and sets `HasBeenCleared`.
   - Every manager holds the record array, set in its constructor, so each reads the scope instead.
   - Clear Selected and Clear Unselected make one range per selected record, so the scope must handle millions of ranges, for example as a bitmap.
   - Memory: as today, records outside the scope are dropped. This is safe because records are immutable and annotations live in the session. #988 replaces it with releasing content chunks.
   - Bug #1004 (unsaved comments and pins on cleared records are lost on save) is fixed before this phase. Its tests must keep passing.

## Compatibility

- The sidecar format and the `.idx` format do not change. Every sidecar entry is kept, including lines not in memory.
- Plugins: the built-in TSV and default plugins move with each step. External plugins need not stay compatible.
- Line numbers, content and filter results stay identical.

## Risks

- Steps 3 and 7 touch every analyzer (13 set flags or comments) and the filter monikers. Keep each change mechanical.
- Session events fire on worker threads: from the parallel loops in `AnalysisManager` (Unpin All, Remove Comments), the filter and parsing. Analyzers themselves run on the UI thread today (#996). A synchronous `Invoke` from those threads can freeze the GUI; decision 4 avoids it.
- If the proof of concept shows WPF cannot handle the row design at 100 M rows, step 5 needs another approach before #988 can create records on demand.
