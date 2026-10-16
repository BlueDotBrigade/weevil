# Large log files: implementation strategy

Status: draft for discussion, 2026-10-16. No production code has changed.

## Summary

- **Direction:** store log content as raw bytes plus a record index (byte offset, line number, timestamp, severity). In the [storage benchmark](StorageBenchmarks/README.md) this filtered as fast as today, used a third of the memory and opened about 7× faster.
- **Phase order:** disk mode cannot work without the record index, so build the index before disk mode: 1) faster parser, 2) record index, 3) disk mode and the Open As option.
- **Clear operations do not block disk mode.** Only six GUI commands call them; no internal class does. Clearing never changes the log file today, and once the index exists it can be rebuilt as "narrow the index", which works in both modes.
- **Biggest obstacle:** arrays of fully loaded records are the currency of the public API (`ICoreEngine.Records`, `IFilter.Results`, analyzers, selection). They must become lists that load records on demand.
- **Second obstacle:** `Metadata` mixes user annotations, parsed facts and per-filter values, and the grid edits it directly. It must be split.
- **Sidecar byte offsets cannot be trusted.** Some are correct; others hold a default value. Line numbers are the only reliable key, and the record index becomes the only source of byte offsets. See [Byte offsets in sidecars today](#byte-offsets-in-sidecars-today).

## Decisions so far

| Topic | Decision |
|---|---|
| Memory | Analyst machines have at most 32 GB of RAM. |
| File size | Design for 64 GB. The largest file seen in practice was under 25 GB. |
| Targets | Open a 20 GB file in under 1 minute; filter it in under 10 seconds. |
| Encoding | No guarantees; any encoding may appear. |
| File changes | Log files only grow by appending. No truncation, rotation or circular logs. |
| Plugins | Compatibility with external plugins is not required. |
| Index file | Raw binary file named like the sidecar: `Application.log.idx` beside `Application.log` and `Application.log.xml`. Rebuilt when missing, stale or unwritable. See [Index file](#index-file). |
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
- Optional bug fix: stop seeking to a sidecar byte offset when opening from a start line. Read from the start of the file and skip to the requested range instead. Partial loads get slower, but line numbers become correct. See [Byte offsets in sidecars today](#byte-offsets-in-sidecars-today).

Done when: existing and new parser tests pass, and the benchmark's Weevil case shows fewer allocations and a faster open. RAM stays about 3× the file size, because `Record` objects remain.

### Phase 2: record index for both modes

Goal: know where every record is, and stop keeping one object per record.

This is the largest phase. It can be split:

- **2a: index alongside today's records.** Build the index during loading, save it as an [index file](#index-file), and reuse it on reopen. Locate records, bookmarks, regions and sections by line number, ignoring any byte offset stored in the sidecar. Write offsets taken from the index when the sidecar is saved.
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

## Index file

User data (comments, pins, bookmarks, regions, filter history) stays in the XML sidecar. The record index goes in its own file, named and placed like the sidecar so that all of a log's files can be copied to another developer together:

```
Application.log        the log file (never modified)
Application.log.xml    sidecar: user data, XML
Application.log.idx    record index: binary
```

- **Format:** raw binary. A header holds the format version, the log's encoding, the parser that built it, the record count and a fingerprint of the log. Each field (offset, line number, timestamp, severity and flags) is stored as its own column, so a severity filter reads 1 byte per record. Text formats would be several times larger and as slow to read as re-indexing the log; SQLite would take minutes to insert hundreds of millions of rows.
- **It is a cache:** if the file is missing, stale, corrupt or from an older format, Weevil rebuilds it from the log. Deleting it is always safe. This is why binary is acceptable here, while persisted user state stays in XML.
- **Fingerprint:** based only on content (the length indexed, plus a hash of the first few KB and of the last indexed record), not on the path or timestamps, so an index copied to another computer stays valid.
- **Growth:** new entries are appended, as described under phase 2's append-only refresh.
- **Crash safety:** write to `Application.log.idx.tmp`, then replace `Application.log.idx`, so a crash never leaves a partial index.
- **When not saved:** if the folder is read-only, or the log was opened from a compressed archive (temporary folder), the index is not saved, matching the sidecar's rule. Weevil rebuilds it on every open.
- **Size:** about 21 bytes per record, roughly 5 GB for a 25 GB log. See phase 2's index contents for how larger indexes are handled.
- **Developers:** a CLI command prints the index as text, since the file itself is not human-readable.

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

## Byte offsets in sidecars today

The sidecar has a byte offset field (`RelatesTo.ByteOffset`) for records, bookmarks, regions and sections, but it is not reliable:

| Item | Byte offset written |
|---|---|
| Records (comments, pins) | Always the default, -1 |
| Bookmarks | Always -1 |
| Regions | Always -1 |
| Sections (table of contents) | Whatever the plugin set: possibly correct, 0 if the section was built without one, or -1 |

Only one code path uses a stored offset: opening from a start line. `CoreEngineBuilder` asks `SidecarManager.GetByteOffsetOrDefault` for the offset of a section starting on that line, seeks there, and numbers the first line read as the start line. Consequences:

- No matching section, or a section saved with offset 0: reading starts at the top of the file, but numbering starts at the start line, so every line number is shifted and the wrong records are kept.
- A wrong non-zero offset: reading starts at the wrong place, possibly mid-line, with wrong numbering.
- An offset of -1: setting the stream position fails, so the file does not open.

This path runs when a plugin's Open As dialog returns a range starting after line 1, or when a sidecar holds a section on line 1 with a non-zero offset. No tests cover it.

Approach: treat every stored offset as unverified. Line numbers are the key; offsets come only from the record index, and are written back to the sidecar from it. Keeping the existing field means no sidecar format change.

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
| Sidecar byte offsets are unreliable | `RelatesTo.ByteOffset` for records, bookmarks, regions and sections | Key on line numbers; take offsets only from the index | 2a |
| Partial loads seek to an unverified offset | `OpenAsResult.Range`; `SidecarManager.GetByteOffsetOrDefault` | Use a sub-range of the index. Optionally fix in phase 1 by reading from the start. | 1 or 2a |
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
- When a sidecar is loaded, Weevil shall locate records, bookmarks, regions and sections by line number, and shall ignore stored byte offsets.
- When a sidecar is saved, Weevil shall write byte offsets taken from the record index.
- When a log file is opened from a start line, Weevil shall find the starting position from the record index.
- When a log file is reopened and its saved index matches, Weevil shall reuse the saved index.
- If the saved index does not match the log file, then Weevil shall rebuild the index.
- Weevil shall save the record index in the log file's folder, named after the log file with an `.idx` extension.
- When a copied index file matches its log file, Weevil shall reuse it, regardless of the file's path or timestamps.
- If the index file cannot be written, then Weevil shall continue without saving it.
- If the log file was opened from a compressed archive, then Weevil shall not save the index file.
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
