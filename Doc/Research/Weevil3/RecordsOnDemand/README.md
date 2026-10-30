# Records on Demand: Proposed Split (#1021)

**Question:** how to deliver step 3 of the [#988 plan](../../../Plans/988-IndexingService.md), records created on demand from a record table, as a series of pull requests.

**Status (2026-10-30):** proposal, not yet decided. An architect agent wrote the design below from four maps of the code (Core internals, public contracts and callers, GUI, research constraints), and a sceptic agent challenged it. The user decides the open questions before any sub-issue is filed.

## Proposal


### 1. The record table

**Columns.** Each column is its own array.

| Column | Type | Bytes |
|---|---|---|
| Line number | `int` | 4 |
| Content offset inside its chunk | `int` | 4 |
| Content length | `int` | 4 |
| Facts (severity, multi-line, UI thread, keeps-string) | `byte` | 1 |
| Created at | `DateTime` | 8 |
| **Total** | | **21** |

This is the plan's 21 bytes, with different columns: a chunk-relative offset plus a stored length, instead of an 8-byte file offset with no length.
- Deriving the length from the next row fails in three cases: a rejected line ends a record, `Get(range, max)` stops early, and end of file. Each would need an exception table that #1023 must also persist.
- The `int` offset is exactly what `Record`'s chunk constructor takes today.
- The file offset is `chunk.FileOffset + offset` when #986 or #1023 needs it.

**Where it lives.** `internal sealed class RecordTable` in `BlueDotBrigade.Weevil.Data` (Core), with an internal `Builder`. It is immutable once built and shared by future views (#991).

**Chunks.**
- The table holds one directory entry per chunk: the chunk's first record number and the `ContentChunk`. That is about 64 entries per GB, at about 0 bytes per record.
- `CreateRecord(recordNumber)` binary-searches the directory. Rows never reference a chunk, so #1022 can swap the directory.

**Blocks.** The columns are stored in blocks of 65,536 rows, and the last block is trimmed when parsing ends. This:
- removes today's `List` + `ToImmutableArray` copy at the end of parsing;
- keeps every array far below 2 GB;
- lets #986 append rows when the log grows, without copying.

**Records that keep a string** (UTF-16 in the other byte order, UTF-32, or plugin-altered text):
- They set the keeps-string facts bit.
- Their text goes in a `string[]` that a block allocates only if it holds such rows. UTF-8 logs pay nothing.

**Facts layout.** It is defined once and shared by `Record`, the table and later `.idx`. Today it is private to `Record`.

**How the parser builds it.**
- `MultiLineRecordParser`'s `ReadRecord` struct is already a row, so `CompleteChunk` hands rows plus their chunk to the builder instead of calling `new Record`.
- `SerializedRecordRepository.Get` checks range and max per row, then appends.
- Plugins' `TryParse(out IRecord)` is unchanged; it stays a temporary object per line.

**Cost.** 21 bytes per record, against today's 48-byte `Record` plus 8-byte scope slot plus 8-byte results slot (64).

### 2. The record list

**Shape.** `public abstract class RecordList : IReadOnlyList<IRecord>` in `BlueDotBrigade.Weevil.Data` (Common).
- It is abstract because Common cannot see Core's internal table, and `InternalsVisibleTo` is Debug-only.
- There are two implementations:
  - `ArrayRecordList` (Common, internal): the adapter over `ImmutableArray<IRecord>`.
  - `TableRecordList` (Core, internal): the table plus an `int[]` of record numbers, or null for every row.

**Members.**
- Abstract: `Count`, `IRecord this[int position]`, `GetLineNumber(int position)`, `GetCreatedAt(int position)`. The last two create no record.
- Built on those in the base class:
  - `IndexOf(int lineNumber)`: binary search, −1 if absent.
  - `BinarySearch(int lineNumber)`: returns the complement of the insertion point when absent, which the nearest-match modes of `RecordSearch` need.
  - `GetEnumerator`: creates one record at a time.
  - `static Empty`.
  - `implicit operator RecordList(ImmutableArray<IRecord>)`: this keeps `R`, test fixtures and plugin-built arrays compiling.
- Temporary: `Length` forwards to `Count` until piece 5.
- Left out on purpose:
  - **`IList<IRecord>`.** Its `Contains`, `IndexOf(IRecord)` and `CopyTo` invite creating every record.
  - **`IsDefault`.** A null check replaces it.

**Snapshot.** A list never changes. Filters and Clear publish new lists, so `FilterViewModel`'s "read Results and ElapsedTimes together" still holds.

**No cache.** A new `Record` is created on each access.
- A cache costs 8 bytes per record.
- WPF rows are already cached weakly by `RowList`.
- The benchmark shows a per-row record in the filter is no slower (80 ms against 106 ms).
- `ContentBuffer`'s reference cache stays correct: a filter passes one object per row to all its expressions. A reused cursor object is ruled out, because of `ContentBuffer` and the metric collectors that keep references.

**Equality by line number.**
- `Record.Equals` is true for any `IRecord` with the same `LineNumber`. `GetHashCode` returns `LineNumber`, and `Record` implements `IEquatable<IRecord>`.
- `Record.Dummy` is line −1, so `IsDummyOrNull` is unchanged.
- Code that depends on identity:
  - `SelectedRecords` uses `ReferenceEquals` at :35 and :63; both must compare line numbers.
  - The `BeSameAs` assertion at `SelectedRecordsTests.cs:23-24` must become `Be`.
- **FluentAssertions trap.** FluentAssertions 7 compares any type that overrides `Equals` by `Equals`, in `Equal`, `BeEquivalentTo` and `Contain`. A test that compares records would silently compare only line numbers, so record assertions must be audited.
- Records from two different logs with the same line number become equal (see open question 4).

### 3. Filter results and the Clear set

- **Filter results.** An `int[]` of table record numbers in ascending order, wrapped in a `TableRecordList`.
  - Record numbers, not positions in the scope, so they survive Clear and are shared across views.
  - `FilterManager` changes `new IRecord[N]` plus `Compact` into an `int[]` with in-place compaction: 4N temporarily, against 8N today. `Compact<T>` is then deleted.
  - When the filter keeps every record, the results are the scope's own list, with no array: −4 bytes per record.
- **The Clear set.** `RecordScope` holds the table plus an `int[]` of kept record numbers, or null before any Clear (0 bytes).
  - `InMemoryRecordRepository` computes the kept numbers from `GetLineNumber` alone.
  - Ranges are left to #1022 if releasing chunks needs them.
- **Selection.** `SelectionManager.GetSelected` returns a `TableRecordList` over the selected record numbers, so Ctrl+A followed by Clear creates no records.
- **Elsewhere:** `CoreEngine` (Records, indexer, Clear) and `NavigationManager`, which receives the scope rather than the initial array (`CoreEngine.cs:158`).

### 4. The split (sub-issues of #1021)

#### Piece 1: Records are equal by line number
- **Scope:**
  - `Record.Equals`, `GetHashCode` and `IEquatable<IRecord>`.
  - `SelectedRecords.Contains` and `Values.Contains` compare line numbers.
- **Forwarding:** none needed.
- **Tests:**
  - `RecordTests`: equal by line, unequal by line, `Dummy` equal only to line −1.
  - `SelectedRecordsTests`: `BeSameAs` becomes `Be`.
  - Audit every record assertion for the FluentAssertions behaviour above.
- **Risk:** something compares records across Reload. It is small and comes first because it teaches us cheaply where identity is relied on.
- **Memory:** none.

#### Piece 2: Callers read records from a record list
- **Scope:**
  - `RecordList` and its array adapter.
  - Every Common contract: `ICoreEngine`, `IFilter.Results`, `ResultsChangedEventArgs`, `ISelect.GetSelected`/`ClearAll`, `IRecordAnalyzer`, `IInsight`, `ICoreExtension`, `IWrite`.
  - `RecordCollectionExtensions`, `RecordSearch` (searches columns, not probe `Record`s) and `ToIndexUsing`.
  - `ElapsedTimes.Calculate`: one pass over the line-number and timestamp columns.
  - Lookups that need only line numbers stop creating records: `SelectedRecords.Keys`/`ContainsKey`, `PositionRanges`, `LowerBound`, `ActiveRecord`, the sidecar save pass, the bulk operations in `AnalysisManager`, and `RowList`'s binary search.
  - The GUI's `VisibleItems` and `SelectedItems` become `RecordList`; the CLI, PowerShell and TestTools are retyped.
- **Forwarding:** `Length`, the implicit conversion, and null checks in place of `IsDefault`.
- **Tests:**
  - New: `RecordListTests`.
  - About 20 test lines that break: `Token.cs:47`, the extension receivers and `IsDefault` in `RecordCollectionExtensionsTests`, and the `IndexOfLineNumber` receivers in `FilteringSteps`, `NavigationSteps` and `ClearingShould`.
- **Risk:** a diff of about 300 lines, but the compiler checks it. External plugins must be rebuilt, which #990 accepted.
- **Memory:** unchanged.

#### Piece 3: Core reads records from a record table (riskiest)
- **Scope:**
  - `RecordTable`, its builder and the shared facts definition.
  - The parser and the repository; `CoreEngineBuilder`.
  - `RecordScope`, `TableRecordList`, Clear as kept record numbers.
  - Filter results as record numbers, and keep-all reusing the scope.
  - `GetSelected` and `NavigationManager`.
- **Forwarding:** everything outside Core already reads `RecordList`, so nothing else changes.
- **Tests:**
  - New: `RecordTableTests`: round trip including `CreationTimeUnknown`, string rows, a block boundary, chunk edges, and the trimmed last block.
  - Rerun #1133's 120-log identity check: every record must be identical to `main`.
  - Rerun the filter timings on the 10-million-record log: same results, no slower.
  - Also time Ctrl+A then Clear, Find, navigation, sidecar save and the analyzers.
  - Rewrite the #1126 test to watch a `ContentChunk`. An on-demand record proves nothing, so the current test would pass vacuously.
  - Lower the #1086 budget from 250 to 175.
- **Risks:**
  - Content identity: string rows, multi-line records, `Get(range, max)`.
  - Paths that now allocate a record per access.
  - Retained records still pin their chunks, as they do today.
- **Memory:** **this is the first saving: about 204 → 161 bytes per record (−43, −21%).**
  - It breaks down as 131 content + 21 table + 8 elapsed times + 1 annotations, with no scope or results array.
  - On the 4 GB harness log, the working set after opening should fall from about 1.62 to about 1.3 GB per GB of log. The opening peak should also fall, because the end-of-parse copy is gone.
- **If review size demands it,** split this piece: (a) the table behind today's array, which costs about +21 bytes per record in the interim but stays under 250; then (b) the scope and results.

#### Piece 4: The GUI and insights keep line numbers, not records
- **Scope:**
  - `IInsight.RelatedRecords` becomes `IReadOnlyList<int>` of line numbers: the three insights, `SeverityMetrics` and `TemporalAnomalyMetrics`, and the GUI callers: `FilterViewModel` lines 1660-1729, `DashboardDialog`, `NavigateToInsightRecordBulletin`, and Get-Insight.
  - Copy Comments walks line numbers and checks `HasComment` before creating any record.
  - `NavigateToSourceCode` uses `Selected.Count`.
  - The dead read at `FilterViewModel.cs:671` is removed.
  - Clear and Open drop the old rows before Core runs, as Reload does.
  - The graph: see open question 2.
- **Tests:** a new test that insights hold no records after Reload; the existing GUI feature tests stay green.
- **Memory:** the budget does not change. This piece stops the GUI and metrics pinning chunks, which #1022 needs.

#### Piece 5: Tests read record lists; forwarders go
- **Scope:**
  - Remove `Length`.
  - `R.GetRecords()` and `R.WithLineNumbers()` return `RecordList`.
  - Tests stop declaring `ImmutableArray<IRecord>` where they mean a contract.
  - Update `Doc/Design`, the done note for step 3 in plan 988, and a `Doc/LessonsLearned` note on FluentAssertions value semantics.
- **Measure:**
  - The budget test at `Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/EngineTests.cs`.
  - The memory harness (`Doc/Research/Weevil3/MemoryHarness`) on the 4 GB log: working set after opening and the peak while opening, recorded as a new results section, as was done after step 1.

### 5. Open questions for the user

1. **The last 4 bytes to reach 157.** Piece 3 lands at about 161 (about 1.23 GB per GB). The options are:
   - derive the length from the next row, with an exception bit and a side table (−4; #1023 must persist the exceptions);
   - compute elapsed times on demand from the timestamp column (−8; `@Elapsed` and elapsed-time navigation get slower, and it degrades on logs with few timestamps);
   - accept 161.

   I recommend accepting 161 now and revisiting when #1023 designs the `.idx` layout.
2. **The graph window.** It keeps an unlimited selection across Open, Reload and Clear. Should it be limited to 100,000 records, like Copy, or keep only the values it plots?
3. **Changing the public `IInsight.RelatedRecords` to line numbers.** This is in piece 4; external plugins break, as #990 already accepts.
4. **Records from different logs with the same line number compare equal.** Is that acceptable?
5. **The Clear set.** I chose `int[]`, following the plan's wording. EngineApi.md and decision 1 of #990 said ranges or a bitmap. Should #1022 own that change?
6. **Filter contracts keep `IRecord`.** `IExpression`, `IFilterStrategy` and `IMetricCollector` keep taking a record per row, which measured no slower. Monikers that read columns directly measured about 2.5 times faster for `@Severity`. File that as a later story?
7. **The #1134 review defects** are still open (`RawContent/README.md:5`). Fix them before piece 3, since piece 3 builds on that parser?
8. **Unrelated bug:** `SeverityMetrics`' "first fatal" is whichever thread counts first, not the lowest line. File it as a bug?
9. **Plan 988 step 3 needs updating:** the columns (chunk-relative offset plus length), the reference counts (now 169/61 and 128/28), and this split, in the style of #990 step 7.

## Challenge

The split is sound in direction, but not as written. Six things must change before it is filed: keep `IList<IRecord>`, split piece 2, fix the paths that build every record before piece 3, decide the graph and insight retention no later than piece 3, make the 3a/3b split mandatory, and fix the #1134 defects first. I checked the findings below against `main` at 39c2cbcc. Its .NET 10 probe was a throwaway program, not kept.

### Problems, each with a fix

**1. Leaving out `IList<IRecord>` makes things slower and creates more records, not fewer.**
- In the probe, a type that is only an `IReadOnlyList<T>` loses LINQ's fast paths. `Last()` and `Count()` created all 1,000 elements, `ElementAt(500)` created 501 and `Skip(990).First()` created 991.
- `Partitioner.Create` returns `DynamicPartitionerForIEnumerable`, which hands out items under a lock. So `AsParallel` and `Parallel.ForEach` lose their indexed partitioning.
- Places hit:
  - `AnalysisManager.FindLines` (:200).
  - `SelectionManager.ToggleIsPinned` and `SetComment` (:586, :608): `AsParallel` over `GetSelected()`. Piece 2's list of line-number lookups leaves them out.
  - `InMemoryRecordRepository` (:107, :143): `.Last()`.
  - Tests: `Results.Last()` (ClearingShould ×6) and `GetSelected().Last()` (NavigationSteps ×2).
- **Fix:** implement a read-only `IList<IRecord>`.
  - `IndexOf` and `Contains` use the line-number binary search; mutators throw.
  - Leaving the interface out prevents nothing: `ToArray` and `foreach` create every record anyway.
- Also add `ToggleIsPinned` and `SetComment` to piece 2.

**2. Several paths would build and hold every record, and the proposal leaves them alone.**
- `SaveSelection` (SelectionManager.cs:338), with one record or none selected, runs `_scope.Records.OrderBy(...).ToImmutableArray()`.
- `AnalysisHelper.OrderRecordsForAnalysis` (:68-80) does the same, sorted by timestamp.
- `MatchingRecordRunsAnalyzer` (:68) sorts by line, which is already the order.
- Today these copies cost 8 B per record. After piece 3 they cost about 56 B per record, and they keep every chunk alive while they run.
- **Fix, in piece 2:**
  - `SaveSelection` exports the scope directly.
  - `MatchingRecordRuns` drops its sort.
  - `OrderRecordsForAnalysis` sorts positions by `GetCreatedAt` and returns a list over them.

**3. Piece 3 makes the graph keep the whole log alive.**
- `GraphViewModel._records` (:65) keeps `GetSelected()` in a modeless window. That window survives Open, Reload and Clear.
- Today that holds the selected records and their chunks. Once `GetSelected` returns a `TableRecordList`, it holds the table, its chunk directory and therefore every chunk of the previous log.
- **Fix:** decide open question 2 before piece 3. The minimum is that piece 2 makes the graph copy its selection (`ToArray`).

**4. Piece 3's rewritten #1126 test will fail.**
- `TrackFirstRecord`'s remarks say that insights hold other records of A.log.
- `FVM._insights` is not refreshed by Reload.
- The test log fits in one chunk.
- So "watch a `ContentChunk`" stays alive until piece 4 removes records from insights.
- **Fix:** in piece 3, watch the `RecordTable`. Switch the test to the chunk in piece 4, or move the `IInsight` change ahead of piece 3.

**5. Piece 1 cannot show where code relies on identity.**
- Within one load, each line has exactly one record object, so equality by line number behaves exactly like reference equality. Nothing changes until piece 3.
- **Fix:** give the array adapter a test-only mode that returns a new copy on every indexer access and every enumeration. Run the whole suite in that mode in piece 2. That catches identity dependencies before the table exists: `BeSameAs`, `ContentBuffer`'s cache, and the references metric collectors keep.
- Also confirm that #1133's 120-log identity check compares fields, not `Equals`. Otherwise piece 1 makes that check meaningless.

**6. Piece 2 is not "about 300 lines".**
- My counts: 172 references in 61 source files, 130 in test files, and 24 source files that mention `IRecordAnalyzer`.
- It also bundles algorithm rewrites: `RecordSearch`'s nearest-match modes, `ElapsedTimes`, `PositionRanges`, `SelectedRecords.Keys` and the sidecar pass.
- **Fix:** split it.
  - **2a:** `RecordList`, the adapter and a mechanical retype, with no change in behaviour.
  - **2b:** the line-number lookups, plus the fixes from problems 1 and 2.
- 2a must also update `common.instructions.md`, which still says to use `ImmutableArray<IRecord>` for collections passed across boundaries.
- Piece 3 also breaks a rule in `core.instructions.md`: "Avoid allocating per-record objects in the hot path of filtering". Record the benchmark as the decision and amend the rule in the same pull request.

**7. Piece 3 is too large to review.**
- **Fix:** make the (a)/(b) split mandatory.
  - **3a:** the table, the shared facts definition and the parser. Core still materializes today's array; that costs +21 B per record, under the budget of 250.
  - **3b:** the scope, the results and Clear.
- The #1134 review defects are a prerequisite for 3a, not an open question (open question 7).

**8. The benchmark did not measure the real access path.**
- `bench/Program.cs` uses one chunk and `Parallel.For` directly over the raw columns.
- It skips the chunk-directory binary search (about 11 steps at 20 GB), the block lookup and the `int[]` lookup from position to record number.
- **Fix:**
  - Give each block its first chunk index. A block of 65,536 rows averages about 8.6 MB, so it spans at most two chunks and the lookup is O(1).
  - Before merging piece 3, rerun the benchmark through `TableRecordList` with 64 or more chunks.

**9. The keep-all shortcut must not skip the filter loop.**
- On Open, `SeverityMetrics` and `TemporalAnomalyMetrics` feed the insights, and they only see records inside the loop.
- **Fix:** run the loop, and drop only the results array (keep-all, or a count equal to N).

**10. `GetSelected` copies record numbers.**
- After Ctrl+A on 100 million records, that is 400 MB of temporary memory. Clear always calls `GetSelected` (CoreEngine.cs:315).
- **Fix:** a list over the results plus the `PositionRanges`, with no copy. Clear takes the ranges.

**11. A dense `string[]` per block is costly when strings are rare.**
- One plugin-altered line in a block allocates 512 KB, which is 8 B for every row in that block.
- **Fix:** use a sparse list (row, string) unless most rows in the block are strings.

**12. The implicit conversion of a default `ImmutableArray` needs a defined result.**
- Today `ActiveRecord` (:24, :77), `ElapsedTimes` (:42) and `GraphViewModel` (:541) check `IsDefault`.
- **Fix:** convert a default array to `Empty`, not to a wrapper that throws `NullReferenceException`, and test it.

**13. Sharing the facts byte crosses assemblies.**
- Common's `InternalsVisibleTo` is Debug-only and names only Common-UnitTests. So a `Record` constructor that takes the facts byte would have to be public.
- Keep the keeps-string bit in the table only, never in `Record`'s facts.

**14. "Open drops the old rows before Core runs" (piece 4) breaks a failed Open.**
- FVM :633-634 keeps the open log when an Open fails.
- **Fix:** drop the rows early only for Clear. For Open, either accept the peak with two logs in memory or rebuild the rows on failure.

**15. The sidecar pass must keep walking the scope.**
- Walking the `AnnotationStore` instead, as the core-internals map suggests, would save annotations on cleared lines. That changes what the sidecar saves.
- **Fix:** say so in piece 2.

**16. Later steps.**
- **#986:** design each column as an array that can be absent. Disk mode drops the offset column, which brings a row down to 13 bytes.
- **#1023:** a stored length only moves the exceptions problem. #1023 must then save 4 bytes per record or derive the length after all. Say this in open question 1.
- **#990 decision 1** keeps elapsed times per view. Open question 1's "on demand" option reverses that decision, so it needs the user's explicit approval.

### Is the split sound?

Yes in direction: equality, then the contracts behind an adapter, then the table, then the GUI's retained records, then the tests. As written, the ordering is wrong in two places. Problems 3 and 4 need the graph and insight decisions no later than piece 3. Problems 1 and 2 need to land before piece 3, or it creates and holds a record for every line.

With problems 1–7 fixed, the split is sound and nothing in it blocks #1022, #1023, #986 or #991. The new pieces would be 1, 2a, 2b, 3a, 3b, 4 and 5, with the graph and insight changes no later than 3b.