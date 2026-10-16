# Large log files: implementation strategy

Status: draft for discussion, 2026-10-16. No production code has changed.

## Summary

- **Direction:** store log content as raw bytes plus a record index (byte offset, line number, timestamp, severity). In the [storage benchmark](StorageBenchmarks/README.md) this filtered as fast as today, used a third of the memory and opened about 7× faster.
- **Phase order:** disk mode cannot work without the record index, so build the index before disk mode: 1) faster parser, 2) record index, 3) disk mode and the Open As option.
- **Clear operations do not block disk mode.** Only six GUI commands call them; no internal class does. Clearing never changes the log file today, and once the index exists it can be rebuilt as "narrow the index", which works in both modes.
- **Biggest obstacle:** arrays of fully loaded records are the currency of the public API (`ICoreEngine.Records`, `IFilter.Results`, analyzers, selection). They must become lists that load records on demand.
- **Second obstacle:** `Metadata` mixes user annotations, parsed facts and per-filter values, and the grid edits it directly. It must be split.

## Decisions so far

| Topic | Decision |
|---|---|
| Memory | Analyst machines have at most 32 GB of RAM. |
| File size | Design for 64 GB. The largest file seen in practice was under 25 GB. |
| Targets | Open a 20 GB file in under 1 minute; filter it in under 10 seconds. |
| Encoding | No guarantees; any encoding may appear. |
| File changes | Log files only grow by appending. No truncation, rotation or circular logs. |
| Plugins | Compatibility with external plugins is not required. |
| Index file | Weevil may save the index next to the log; rebuild it when the folder is read-only. |
| Network drives | Allowed, with a warning in disk mode. |
| Default mode | RAM. |

## Phases

### Phase 1: faster existing parser

Goal: open files faster and allocate less, with no change in behaviour or API.

- Skip the `StringBuilder` and second `Record` for single-line records (`MultilineRecordParser.EndReadNext`).
- Parse TSV fields with spans and `TryParseExact` instead of `Split` and exceptions (`TsvRecordParser`). This also fixes a bug: an oversized number in the process or thread field throws an uncaught `OverflowException` and stops the load.
- Remove the forced full garbage collections in `SerializedRecordRepository.Get`, the `CoreEngine` constructor, `InMemoryRecordRepository.Get` and `FilterManager.ApplyInternal`.
- Build the record array once instead of copying it twice (`SerializedRecordRepository.Get`).
- Use a larger read buffer and a sequential-scan hint.

Done when: existing and new parser tests pass, and the benchmark's Weevil case shows fewer allocations and a faster open. RAM stays about 3× the file size, because `Record` objects remain.

### Phase 2: record index for both modes

Goal: know where every record is, and stop keeping one object per record.

This is the largest phase. It can be split:

- **2a: index alongside today's records.** Build the index during loading and fill the byte offsets that bookmarks, regions and sections already have room for (`RelatesTo.ByteOffset`, currently -1). Save it and reuse it on reopen.
- **2b: records on demand.** RAM mode keeps the raw bytes in memory (in chunks; one .NET array cannot exceed about 2 GB) and creates `Record` objects only when needed.

Design notes:

- **Index contents:** byte offset (8 bytes), line number (4), timestamp (8), severity and flags (1): about 21 bytes per record. A 25 GB log needs about 5 GB; a 64 GB log about 13 GB, which is too much for 32 GB machines. Start simple and keep the index behind an interface so it can later be compacted, or the saved index file memory-mapped. Memory-mapping Weevil's own index file is safe, unlike mapping the log.
- **Content storage:** one interface with two implementations, in-memory chunks (RAM) and file reads (disk). Both return the same text for a record.
- **Record lists:** replace `ImmutableArray<IRecord>` in the public API with a read-only list backed by record numbers. Filter results become arrays of record numbers (4 bytes each).
- **Metadata split:** comments, pins and flags move to one store keyed by line number. It raises change events so the grid stays in sync. Parsed facts (UI thread, multiline) move into the index. Elapsed time is calculated when displayed instead of written to every record on every filter.
- **Append-only refresh:** when a reopened file has grown, index from the start of the last indexed record, because that record may have gained lines. As a tripwire, check that the file is no shorter and that a few KB at the start and at the last record still match; if not, warn and rebuild.

Done when: line numbers, record boundaries and content match today's parser on the same files, and the saved index is reused on reopen.

### Phase 3: disk mode and the Open As option

Goal: open files larger than RAM.

- Add a Weevil Open As dialog with a RAM / Disk choice, defaulting to RAM. Weevil has no dialog of its own today: only plugins can show one, and the built-in TSV and default plugins do not.
- Disk mode reads record content from the log with normal file reads, never memory-mapping, so the app writing the log is unaffected.
- Filter in parallel: each worker reads a chunk, decodes it and runs the regex.
- Warn when RAM is chosen but the file plus its index would not fit, and when a disk-mode file is on a network drive.
- Before starting, add a disk-backed case to the benchmark to confirm the 10-second target.

## Investigation: clear operations

Findings:

- Clearing never changes the log file. `Engine.Clear` builds a new engine from the records that remain in memory (`InMemoryRecordRepository`). Its purpose is to free RAM.
- The only callers are six GUI commands, which go through `FilterViewModel.ClearRecords` to `IEngine.Clear`, plus functional and unit tests. No class in Core calls it.
- Side effects: it sets `HasBeenCleared`, and the GUI re-subscribes to filter history events afterwards. Saving after a clear keeps comments on cleared records, because the sidecar save merges old and new data.

Options:

| Option | Effort | Result |
|---|---|---|
| Disable in disk mode | Small: disable the six commands; `Engine.Clear` rejects the call in disk mode. | Disk mode lacks clear. |
| Narrow the index | Comes with phase 2b, which must rebuild clear for RAM mode anyway. | Clear works in both modes and stays non-destructive. In disk mode it hides records rather than freeing much memory. |

Recommendation: implement clear as "narrow the index" in phase 2b. If disk mode ships before 2b, disable clear in disk mode until then.

## Other disk-mode challenges

| Challenge | Where | Approach | Phase |
|---|---|---|---|
| Record arrays in the public API | `ICoreEngine.Records`, `IFilter.Results`, `ResultsChangedEventArgs`, `IRecordAnalyzer.Analyze`, `ISelect`, `IInsight`, `IWrite` | Read-only record list loaded on demand | 2b |
| Grid edits `Metadata` directly | `FilterView.xaml` binds two-way to `Metadata.Comment` and `Metadata.IsPinned` | User-annotation store with change events | 2b |
| Elapsed time written to every record on every filter | `FilterManager`, `ElapsedTimeAnalyzer` | Calculate when displayed | 2b |
| Sidecar save scans every record | `SidecarLoader` save | Scan the annotation store instead | 2b |
| Line numbers must match today's, or existing sidecars point at the wrong records | `StreamReader.ReadLine` counts `\n`, `\r` and `\r\n`; 256-line search limit; byte order mark | Replicate exactly; compare against today's parser on real files | 2a |
| Filter behaviour | Multiline records are rejoined with `\r\n`; the regex sees the whole line | Rejoin the same way when loading a record | 2b |
| Unknown encodings | UTF-16 and UTF-32 newlines span several bytes; files without a byte order mark are read as UTF-8 | Encoding-aware line scanner; keep today's detection and replacement of invalid characters | 2a |
| Index size at 64 GB | About 13 GB at 21 bytes per record | Compact columns or memory-map the saved index if needed | Later |
| Line numbers are 32-bit | `IRecord.LineNumber` | Fine up to about 2.1 billion lines; a 64 GB file would overflow only if lines averaged under 30 bytes | Accept |
| Selection holds record objects | `SelectionManager`; WPF `ListView` | Store line numbers; make `IndexOf` a binary search so WPF selection doesn't load every record | 2b |
| Analyzers and graphs read every filtered record and set flags | `Analysis/*`, `GraphViewModel` | Iterate the record list; store flags in a bitset | 2b |
| Export and save selected | `DiskWriter`, `LogFileSplitter` | Stream from content storage | 2b |
| Partial loads | `OpenAsResult.Range`, start line via sidecar section offsets | Use a sub-range of the index | 2a |
| No Weevil Open As dialog | `FilterViewModel.OpenAsync` shows only plugin dialogs | New dialog | 3 |
| Filter speed depends on the drive | Disk mode | An NVMe drive reads 20 GB in about 6 s, so 10 s is reachable with parallel filtering. A SATA SSD (about 36 s), hard drive or network share will miss it; warn. | 3 |
| File handle | `FileHelper.Open` shares read/write but not delete | Reopen per read, or add delete sharing | 3 |
| Forced garbage collections on multi-GB heaps | Four call sites | Remove | 1 |

## Draft user stories

### Phase 1

- As an analyst, I want large log files to open faster, so that I can start investigating sooner.
- As an analyst, I want Weevil to use less memory while opening a file, so that I can open larger files.

Requirements:

- When a log file is opened, Weevil shall produce the same records, line numbers and content as version 2.12.1.
- If a numeric field in a record header cannot be parsed, then Weevil shall treat the line as part of the previous record and continue loading.

### Phase 2

- As an analyst, I want comments, bookmarks and regions to stay attached to the right records, so that my annotations remain correct.
- As an analyst, I want a log file I have opened before to reopen quickly, so that I do not wait for it to be read again.

Requirements:

- When a log file is opened, Weevil shall record the byte offset and line number of every record.
- When a log file is reopened and its saved index matches, Weevil shall reuse the saved index.
- If the saved index does not match the log file, then Weevil shall rebuild the index.
- If the index file cannot be written, then Weevil shall continue without saving it.
- When a reopened log file has grown, Weevil shall index only the content after the start of the last indexed record.

### Phase 3

- As an analyst, I want to choose whether a log file is kept in RAM or read from disk, so that I can open log files larger than my computer's memory.

Requirements:

- When the Open As dialog is shown, Weevil shall preselect RAM.
- While a log file is open in disk mode, Weevil shall read record content from the log file on demand.
- If RAM is selected and the file plus its index would exceed available memory, then Weevil shall warn the analyst and suggest disk mode.
- If disk mode is selected for a file on a network drive, then Weevil shall warn that filtering may be slow.
- While a log file is open in disk mode, Weevil shall filter a 20 GB log file in under 10 seconds on an NVMe drive.

## Keep in mind: immutable core

A future goal is an immutable Core, where opening a log creates a session token and concurrency is handled at that level, so several views can share one log file. This plan fits that goal if:

- the index and content storage are immutable once built, and shareable between views;
- the annotation store is the single mutable place, owned by the session;
- each view owns only its filter results (record numbers) and selection.
