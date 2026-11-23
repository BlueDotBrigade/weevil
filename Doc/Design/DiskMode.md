# Disk Mode

How Weevil keeps a log's content on disk instead of in memory (#421, #1025). The plan and its decisions are in [1025-ContentFromFile.md](../Plans/1025-ContentFromFile.md).

## Storage Modes

`StorageMode` (`BlueDotBrigade.Weevil.IO`) says where an opened log's content is kept:

- **RAM**, the default: each record's content is kept in memory until Reload, as bytes in 16 MB chunks, or as a string for UTF-16 in the other byte order, UTF-32 and text a plugin changed.
- **Disk**: only the record table stays in memory, about 22 bytes per record. While a filter runs, each of its workers, one per core, also holds a buffer of about 17 MB for the chunk it reads (#1269). Content is read back from the log whenever it is needed, whatever the encoding. Only text a plugin's parser changed, which is not in the log, stays in memory.

`IEngineBuilder.UsingStorage` takes the mode, and `IEngine.StorageMode` reports it (#1028). The engine keeps it, and Clear, Reload and a retry after a failed Reload keep it. The CLI's `--storage` and PowerShell's `-Storage` choose it; without them, a log opens in RAM. In Weevil, Open As chooses it on every open. The Open button, Ctrl+O and the menu's Open… show one dialog, which finds the log and chooses the mode for the log selected (#1215); a mode the analyst picks stays as other logs are selected. A log or zip dropped on the window, or given by Explorer or on the command line, shows the same dialog, with the log selected or the zip opened (#1217). It suggests RAM when the log's estimate fits in the available memory, read with `GlobalMemoryStatusEx`, less 1 GB, and Disk otherwise (`OpenAsAdvice`). A log in a zip, or one OneDrive keeps online only, isn't read before it opens: its estimate comes from its size alone, assuming lines of 100 bytes, which errs high (`Engine.EstimateMemory(StorageUnit)`, #1209). It warns when RAM may not fit, and when Disk would read a log over the network. Reload never asks again, and the status bar shows the mode.

`Engine.EstimateMemory(path)` estimates the memory a log needs in RAM, so a caller can suggest a mode before opening it: the log's size plus 34 bytes per line, counted from the saved line index's header, or from the log's first megabyte.

**Memory** comes in two figures (#1259), each kept within a budget by `Tst/BlueDotBrigade.Weevil.Core-PerformanceTests`. Both are for the 10-million-record droid log (`DroidLog`):

- **At rest,** once the log is open: in RAM, about 123 bytes per record; on disk, about 22 bytes per record, the record table (`EngineTests`).
- **What a filter needs on top,** while it runs: about 49 bytes per record in either mode, almost all the 48-byte `Record` it creates for each row (#1140), which the garbage collector reclaims afterwards. On disk, also a buffer of about 17 MB per core (#1269). On 4 cores, one filter allocates about 470 MB in RAM and 540 MB on disk (`FilterManagerTests`).

So the memory in use peaks above the figure at rest, by an amount that grows with the log. Open As adds a fixed 1 GB to its estimate (`OpenAsAdvice.Reserve`), which a large log's filters can exceed: the audit measured a 4 GB log in RAM, estimated at 5.0 GB, peaking at 6.5 GB after eight filters and a Find ([Performance.md](../Research/Weevil3/Audit/Findings/Performance.md), 2026-11-17).

## Reading a Log on Disk

1. The engine builder opens the log, and `SerializedRecordRepository` reads it through `LogLineReader`.
2. In both modes, the reader gives every byte it reads, in file order, to a `LogFileSourceBuilder`, which computes a checksum of each 64 KB block ([OpeningALog.md](OpeningALog.md#the-kinds-of-open)). In disk mode its chunks are chunks on file: they hold their range of the log, but no bytes.
3. Before the repository returns any record, it completes the `LogFileSource`. Nothing can be read through it before then.
4. Afterwards, a record's content is read through its chunk and the source: each read opens its own handle, reads the blocks that hold the bytes, checks their checksums and closes the handle. A `ContentBuffer` reads a whole chunk at a time, so each reader reads a chunk once when it reaches it. A filter gives its parallel workers whole ranges of records that share a chunk (`RecordRanges`), so it reads each chunk once. A log with at most half as many chunks as cores is the exception: each chunk's range is split, so more cores are busy, and each part's worker reads the whole chunk. "Filtering complete." logs the bytes read from the log while it ran, which includes what other readers, such as the rows on screen, read meanwhile (#1027).

Code that reads many records reads them through one `ContentBuffer` per pass, with `ContentBuffer.Read(record)` or `RecordList.ReadContent(position, buffer)`, as filters, Find, the analyzers, export, the CLI and the graph do (#1027). `record.Content` reads the 64 KB blocks that hold one record, and keeps nothing: about 7 µs per record. Over 10 million records in a row, that is 72 s, against 1.5 s through a buffer.

A buffer reads a whole chunk only when two records in a row come from it, so a sparse list, such as a few filter results spread over a large log, reads only each record's blocks. It reads a chunk whole once at most: a pass that comes back to a chunk, such as records sorted by time, reads only each record's blocks from it. A pass therefore never reads more than the log once, plus 64 KB per record.

## Reopening a Log on Disk

A log opened before is reopened from the record table saved with its index ([RecordTable.md](RecordTable.md#saved-with-the-index)), in disk mode as in RAM. A UTF-8 log, or a UTF-16 log in the computer's byte order, is reopened from the table either mode saved, so an analyst who finds a log too large for RAM reopens it on disk without parsing it again (#1296). Other encodings keep a string for each record in RAM, so switching modes parses them again ([OpeningALog.md](OpeningALog.md#the-kinds-of-open)).

On disk, the reopen still reads the whole log once, to check every 64 KB block against the checksums saved with the table, but parses nothing. The chunks on file then read back through a `LogFileSource` made from those checksums (`LogFileSource.Create`), as they would through one a parse completed. The 1 GB, 10-million-record UTF-8 log reopens in 0.9 s, against about 7.6 s for a first open on disk.

## A Log That Changed

Every read of the log shares writing and deletion, so Weevil never stops another application from writing, renaming or deleting it (`FileHelper.Open`, #1247). A read that is open, such as a parse, finishes with the data it started with. A later read goes back to the log by its path.

Every read of a record's text from a log on disk that fails throws one type, `LogFileChangedException`, so a caller, such as a script or a plugin, catches one exception (#1295). An open or a Reload still throws the log's own `IOException`, such as `FileNotFoundException`, as it documents. `IRecord.Content` says so, and that on disk each read opens the log, so many records are read through a `ContentBuffer`.

A log that has only grown is normal: the first read that finds it longer logs a warning, and reads go on with the bytes parsed.

A log that was truncated, replaced or corrupted must be reloaded (#1168):

1. The read that finds it shorter, or a block changed, throws `LogFileChangedException`, with the length expected and found, or the offset of the changed block. Every later read checks again, and throws again.
2. The first such read logs one warning and sets `LogFileSource.HasChanged`.
3. A filter throws the same exception, not an `AggregateException` from its parallel workers, and does not log it as an unexpected error.
4. The caller shows the warning. Core never reloads by itself: a reader such as a filter worker cannot swap the engine it is reading. The analyst reloads with Ctrl+F5, which calls `Engine.Reload()` (#1247).
5. Reload deletes the saved index first, with the record table saved with it, because the line index checks only the first and last 4 KB of the log, so it can still match a log changed in the middle. It keeps comments, pins, bookmarks, the filter and the mode, as every Reload does.
6. If the log changes or goes again before Reload applies the filter, Reload ends like one that could not read the log: the engine shows the log without records, keeps the filter for the next Reload, and Reload throws what the read threw. If the log changed, Reload also deletes the line index it has just saved.

A log that can no longer be opened, because it was renamed or deleted, or Weevil may no longer read it, throws the same exception with `IsUnreadable` set and what the read threw inside. The first such read logs one warning, and later reads try again, so a log renamed back is read again. It does not set `HasChanged`: Reload could not read the log either.

Whatever is running stops, such as a filter, an analysis, a save or a split, and the exception reaches the caller:

- **CLI and PowerShell.** They run their work, such as a filter, through the extension method `EngineExtensions.RunOnce`, and never reload or retry (#1247). It catches `LogFileChangedException` and stops the command with one that says to run it again: the CLI exits with code 1, and PowerShell writes an error. Work that finished, for example in RAM mode, keeps its result, with a warning when the log is gone from its path or shorter than it was read.
- **GUI.** It tells the analyst once per log: each open and each Reload starts a new generation of the log. The first change a read finds in a generation, from a row on screen, a filter, F5, an analysis, a search, a save, a split or other work, shows one warning that asks the analyst to press Ctrl+F5. The first read that finds the log unreadable shows one error instead, and is not reloaded (`FilterViewModel.OnFailedRead`, #1028). While an operation runs, such as a filter or an open, the message waits for it to end (#1257). Other I/O errors of that work, such as a save to a locked file, are not the log's, so the work shows its own error. A row whose read fails shows a placeholder instead of its text, because WPF does not catch a converter's exception. The dashboard's Refresh shows its own message. A graph on disk copies the text of its records in the background when it opens, where Escape stops the copy, so the graph never reads a log.

## Testing in Disk Mode

In a Debug build, the environment variable `WEEVIL_TEST_STORAGE=Disk` makes disk mode the default for a builder given no mode. A mode given to the builder, or carried by Reload, wins.

[`Tst/Disk.runsettings`](../../Tst/Disk.runsettings) sets it, and marks the run so a test can check that logs really open on disk. CI runs the Core unit, feature and functional tests, and the CLI's functional tests, a second time with it. Use it for those projects only: a Debug GUI started with the variable set opens logs on disk too.
