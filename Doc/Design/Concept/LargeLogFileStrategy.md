# Large log files: design concept

Status: draft, 2026-10-16. No production code has changed.
Related: [storage benchmark](StorageBenchmarks/README.md), [draft user stories](LargeLogFileUserStories.md).

## Problems

Weevil loads every record of a log into memory as an object. For large logs this causes three problems:

- **Slow to open.** A 1 GB log (7.6 million records) takes about 32 s to parse. The parser allocates 12 GB of temporary objects to keep 3 GB.
- **Too much memory.** Records use about 3× the file size in RAM, so on a 32 GB machine logs above roughly 8–10 GB cannot be opened. Analysts have needed logs up to 25 GB.
- **No reliable record locations.** Weevil does not know where each record starts in the file. Only table-of-contents sections save a byte offset, and until #958 it was used without being checked.

Filtering must not get slower: analysts filter the same log many times.

## Constraints

- Analyst machines have up to 32 GB of RAM.
- Design for logs up to 64 GB; the largest seen was under 25 GB.
- Targets: open a 20 GB log in under 1 minute, and filter it in under 10 seconds.
- Logs may use any encoding.
- Logs only grow by appending: no truncation, rotation or circular logs.
- The original log is never modified. User data stays in the XML sidecar.
- Compatibility with external plugins is not required.

## Challenges

### Architecture

- **Record arrays everywhere.** The public API passes `ImmutableArray<IRecord>`: `ICoreEngine.Records`, `IFilter.Results`, analyzers, insights, selection and export. Every consumer assumes all records are in memory, and analyzers and graphs read every filtered record and set flags on them.
- **`Metadata` mixes three kinds of data:** user annotations (comment, pin, flag), facts from parsing (UI thread, multiline) and per-filter values (elapsed time, rewritten for every record on each filter). The grid edits comments and pins on it directly, and saving the sidecar scans every record for them.
- **Selection holds record objects,** and WPF selection calls `IndexOf`, which would load every record if implemented naively.
- **No Weevil Open As dialog.** Only plugins can show one, and the built-in plugins don't.
- **Clear operations.** Six GUI commands rebuild the engine from the records left in memory. Nothing else calls them, and they never change the log file.

### Compatibility

- **Line numbers must not change,** or existing sidecars point at the wrong records. Today's parser treats `\n`, `\r` and `\r\n` as line ends, skips a byte order mark, and gives up after 256 lines without a record start.
- **Most sidecar byte offsets are unset.** Records, bookmarks and regions always save -1. Section offsets are set by the plugin and are generally correct, but nothing verifies them. See [Sidecar byte offsets today](#sidecar-byte-offsets-today).
- **Filter behaviour must not change.** Multiline records are rejoined with `\r\n`, and the regex sees the whole record, including the header fields.
- **Any encoding.** UTF-16 and UTF-32 newlines span several bytes. Files without a byte order mark are read as UTF-8, and invalid characters are replaced.
- **Growing files.** The last record may be incomplete, or gain more lines later.

### Scale

- **.NET size limits.** A string holds at most about 1 billion characters and an array about 2 GB, so content must be stored in chunks.
- **Index size.** About 21 bytes per record: roughly 5 GB for a 25 GB log, and 13 GB for a 64 GB log.
- **32-bit line numbers.** Fine up to about 2.1 billion lines; a 64 GB log would overflow only if its lines averaged under 30 bytes.
- **Disk speed.** Disk mode re-reads the file on every filter. An NVMe drive reads 20 GB in about 6 s, a SATA SSD in about 36 s; network shares are slower still.

## Options

### Where log content lives

Measured per 1 GB of log in the [benchmark](StorageBenchmarks/README.md). Filtering speed was within 10% across the first three options for every regex tested.

| Option | Pros | Cons |
|---|---|---|
| Record objects (today) | Simple; every feature works | 3.0 GB RAM and 32 s to open |
| UTF-16 text in RAM + index | No decoding when filtering | 2.1 GB RAM |
| Raw bytes in RAM + index | 1.1 GB RAM and 5 s to open | Decodes on every filter (no measurable cost); still limited by RAM |
| Raw bytes on disk + index | Opens logs larger than RAM | Filter speed limited by the drive |
| Memory-mapped log | Windows manages caching | Stops the writing app from truncating the log; poor fit for growing files |

### Record parsing

| Option | Pros | Cons |
|---|---|---|
| One universal parser | One code path | Cannot recognise proprietary formats without configuration, so it becomes a plugin anyway |
| Pluggable parsers, split into a record-start check and field parsing | Supports any format; a start check that looks at one line at a time lets indexing run in parallel | Two parts to write per format |

### Index file format

| Option | Pros | Cons |
|---|---|---|
| JSON, XML or text | Human-readable | Several times larger; reading it is as slow as re-indexing the log |
| SQLite | Queryable; robust | Minutes to insert hundreds of millions of rows; larger file; extra dependency |
| Raw binary | Compact, fast, appendable, can be memory-mapped | Not human-readable; a CLI dump command covers this |

### Clear operations in disk mode

| Option | Pros | Cons |
|---|---|---|
| Disable in disk mode | Small change | Feature missing in disk mode |
| Narrow the index to a subset of records | Works in both modes; phase 2b must rebuild clear for RAM mode anyway | Frees little memory in disk mode |

## Recommendation

Store log content as raw bytes with a binary record index (`Application.log.idx`). RAM mode keeps the bytes in memory; disk mode reads them from the file. Everything else is shared. Use pluggable parsers split into a record-start check and field parsing, and implement clear as narrowing the index.

Why:

- In the benchmark it filtered as fast as today, with a third of the memory and a 7× faster open.
- One design serves both modes; only where the bytes live differs.
- The index is built from the log itself, so line numbers and byte offsets are always right, and sidecar offsets no longer need to be trusted.
- Reopening is fast, and the index can be copied along with the log and sidecar.
- It can be delivered in phases that are each useful on their own, starting with a low-risk one.
- It fits the future immutable core (see [Reference](#future-immutable-core)).

### Phases

The index comes before disk mode, because disk mode depends on it.

1. **Faster parser.** No change in behaviour or API.
   - Skip the extra string copy and second `Record` for single-line records.
   - Parse TSV fields without `Split` or exceptions. This also fixes an uncaught `OverflowException` that stops loading.
   - Remove four forced garbage collections, build the record array once, and use larger read buffers.
   - Done (#958): opening from a start line checks the section's saved byte offset before using it, and reads from the top of the file when it does not check out.
2. **Record index.**
   - 2a: build the index while loading and save it. Locate sidecar items by line number, and write offsets from the index. When a reopened log has grown, re-index from its last record.
   - 2b: keep raw bytes in RAM (in chunks) and create records on demand. Replace record arrays in the API with lists of record numbers. Move annotations into one store with change events, and save the sidecar from it. Calculate elapsed time on display, store analyzer flags in a bitset, and implement clear as narrowing the index.
3. **Disk mode.**
   - Add a Weevil Open As dialog with RAM / Disk, defaulting to RAM.
   - Read content with normal file reads that never block the app writing the log: no memory-mapping, and the file is shared for deletion too. Filter in parallel.
   - Warn when RAM is chosen for a log that won't fit, and when a disk-mode log is on a network drive.
   - First, add a disk case to the benchmark to confirm the 10-second target.

## Reference

### Index file

```
Application.log        log file, never modified
Application.log.xml    sidecar: user data, XML
Application.log.idx    record index: binary
```

- Named and placed like the sidecar, so a log's files can be copied to another developer together.
- Header: format version, encoding, parser, record count and a fingerprint of the log. Then one column per field: byte offset (8 bytes), line number (4), timestamp (8), severity and flags (1).
- It is a cache: if missing, stale, corrupt or from an older format, Weevil rebuilds it. Deleting it is always safe.
- The fingerprint uses only content (the length indexed, plus hashes of the first few KB and of the last record), not the path or timestamps, so a copied index stays valid.
- Written to `Application.log.idx.tmp` first, then swapped in, so a crash never leaves a partial index.
- Not saved when the folder is read-only or the log was opened from a compressed archive, matching the sidecar. Weevil then rebuilds it on every open.
- When a reopened log has grown, check that the start and the last indexed record still match; if not, warn and rebuild.
- For large indexes (13 GB for a 64 GB log), compact the columns or memory-map the `.idx` file. Mapping Weevil's own file is safe, unlike mapping the log.

### Sidecar byte offsets today

| Item | Byte offset saved |
|---|---|
| Records (comments, pins) | Always -1 |
| Bookmarks and regions | Always -1 |
| Sections | Set by the plugin; generally correct (all 11 in the `GlobalDefault.log` test data match), but not verified |

Opening from a start line uses the byte offset of a section on that line. Before #958 the offset was used unchecked:

- No matching section, or offset 0: reading started at the top, so every line number was shifted.
- A wrong offset: reading started at the wrong place, possibly mid-line.
- -1: the file failed to open.

Since #958, the offset is used only when a line begins there; otherwise reading starts at the top of the file and skips to the start line. An offset that is wrong but still lands at the start of a line cannot be detected without a record index.

### Future: immutable core

A future goal is an immutable Core, where opening a log creates a session token and concurrency is handled at that level, so several views can share one log. This design fits if:

- the index and content storage are immutable once built, and shared between views;
- the annotation store is the only mutable part, owned by the session;
- each view owns only its filter results (record numbers) and selection.
