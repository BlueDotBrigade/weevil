# Disk Mode

How Weevil keeps a log's content on disk instead of in memory (#421, #1025). The plan and its decisions are in [1025-ContentFromFile.md](../Plans/1025-ContentFromFile.md).

## Storage Modes

`StorageMode` (`BlueDotBrigade.Weevil.IO`) says where an opened log's content is kept:

- **RAM**, the default: each record's content is kept in memory until Reload, as bytes in 16 MB chunks, or as a string for UTF-16 in the other byte order, UTF-32 and text a plugin changed.
- **Disk**: only the record table stays in memory, about 22 bytes per record. Content is read back from the log whenever it is needed, whatever the encoding. Only text a plugin's parser changed, which is not in the log, stays in memory.

`IEngineBuilder.UsingStorage` takes the mode, and `IEngine.StorageMode` reports it (#1028). The engine keeps it, and Clear, Reload and a retry after a failed Reload keep it. The CLI's `--storage` and PowerShell's `-Storage` choose it; without them, a log opens in RAM. The GUI's Open As window comes with #1028.

`Engine.EstimateMemory(path)` estimates the memory a log needs in RAM, so a caller can suggest a mode before opening it: the log's size plus 34 bytes per line, counted from the saved line index's header, or from the log's first megabyte.

## Reading a Log on Disk

1. The engine builder opens the log, and `SerializedRecordRepository` reads it through `LogLineReader`.
2. In disk mode, the reader gives every byte it reads, in file order, to a `LogFileSourceBuilder`, which computes a checksum of each 64 KB block. Its chunks are chunks on file: they hold their range of the log, but no bytes.
3. Before the repository returns any record, it completes the `LogFileSource`. Nothing can be read through it before then.
4. Afterwards, a record's content is read through its chunk and the source: each read opens its own handle, reads the blocks that hold the bytes, checks their checksums and closes the handle. A `ContentBuffer` reads a whole chunk at a time, so each reader reads a chunk once when it reaches it. A filter gives its parallel workers whole ranges of records that share a chunk (`RecordRanges`), so it reads each chunk once. A log with at most half as many chunks as cores is the exception: each chunk's range is split, so more cores are busy, and each part's worker reads the whole chunk. "Filtering complete." logs the bytes read from the log while it ran, which includes what other readers, such as the rows on screen, read meanwhile (#1027).

Code that reads many records reads them through one `ContentBuffer` per pass, with `ContentBuffer.Read(record)` or `RecordList.ReadContent(position, buffer)`, as filters, Find, the analyzers, export, the CLI and the graph do (#1027). `record.Content` reads the 64 KB blocks that hold one record, and keeps nothing: about 7 µs per record. Over 10 million records in a row, that is 72 s, against 1.5 s through a buffer.

A buffer reads a whole chunk only when two records in a row come from it, so a sparse list, such as a few filter results spread over a large log, reads only each record's blocks. It reads a chunk whole once at most: a pass that comes back to a chunk, such as records sorted by time, reads only each record's blocks from it. A pass therefore never reads more than the log once, plus 64 KB per record.

## A Log That Changed

A log that has only grown is normal: the first read that finds it longer logs a warning, and reads go on with the bytes parsed.

A log that was truncated, replaced or corrupted must be reloaded (#1168):

1. The read that finds it shorter, or a block changed, throws `LogFileChangedException`, with the length expected and found, or the offset of the changed block. Every later read checks again, and throws again.
2. The first such read logs one warning and sets `LogFileSource.HasChanged`.
3. A filter throws the same exception, not an `AggregateException` from its parallel workers, and does not log it as an unexpected error.
4. The caller shows the warning and calls `Engine.Reload()`. Core never reloads by itself: a reader such as a filter worker cannot swap the engine it is reading.
5. Reload deletes the saved line index first, because the index checks only the first and last 4 KB of the log, so it can still match a log changed in the middle. It keeps comments, pins, bookmarks, the filter and the mode, as every Reload does.
6. If the log changes again before Reload applies the filter, Reload deletes the line index it has just saved, and ends like one that could not read the log: the engine shows the log without records, keeps the filter for the next Reload, and Reload throws what the read threw.

The CLI and PowerShell do step 4 through the extension method `EngineExtensions.RunReloadingOnChange`: it runs their work, such as a filter, and on a change reloads once and runs it again. Opening the log is not part of that work: a log that changes while it opens is read as it was. A second change, or what Reload throws in step 6, reaches them as an error. The GUI does step 4 once per log: each open and each Reload starts a new generation of the log, and the first change a read finds in a generation, from a row on screen, a filter, F5, an analysis or other work, shows one warning and reloads (`FilterViewModel.OnFailedRead`, #1028). A row whose read fails shows a placeholder instead of its text, because WPF does not catch a converter's exception. A log that can no longer be read shows one error from the rows, a filter or F5, and is not reloaded; an analysis, a save, a split or a search shows its own error, because their other I/O errors, such as a save to a locked file, are not the log's. The dashboard's Refresh shows its own message. A graph copies the text of its records when it opens, so it never reads a log. A read that fails because the log can no longer be opened, with another `IOException` or an `UnauthorizedAccessException`, is thrown as it is by a filter, as in step 3, and ends a Reload as in step 6. Unlike a change, the filter logs it as an error, because nothing has logged it before.

## Testing in Disk Mode

In a Debug build, the environment variable `WEEVIL_TEST_STORAGE=Disk` makes disk mode the default for a builder given no mode. A mode given to the builder, or carried by Reload, wins.

[`Tst/Disk.runsettings`](../../Tst/Disk.runsettings) sets it, and marks the run so a test can check that logs really open on disk. CI runs the Core unit, feature and functional tests, and the CLI's functional tests, a second time with it. Use it for those projects only: a Debug GUI started with the variable set opens logs on disk too.
