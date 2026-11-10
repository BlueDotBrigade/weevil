# Disk Mode

How Weevil keeps a log's content on disk instead of in memory (#421, #1025). The plan and its decisions are in [1025-ContentFromFile.md](../Plans/1025-ContentFromFile.md).

## Storage Modes

`StorageMode` (`BlueDotBrigade.Weevil.IO`) says where an opened log's content is kept:

- **RAM**, the default: each record's content is kept in memory until Reload, as bytes in 16 MB chunks, or as a string for UTF-16 in the other byte order, UTF-32 and text a plugin changed.
- **Disk**: only the record table stays in memory, about 22 bytes per record. Content is read back from the log whenever it is needed, whatever the encoding. Only text a plugin's parser changed, which is not in the log, stays in memory.

Until the Open As window lets the user choose (#1028), only Core's internal `EngineBuilder.UsingStorage` takes the mode. The engine keeps it, and Clear, Reload and a retry after a failed Reload keep it.

## Reading a Log on Disk

1. The engine builder opens the log, and `SerializedRecordRepository` reads it through `LogLineReader`.
2. In disk mode, the reader gives every byte it reads, in file order, to a `LogFileSourceBuilder`, which computes a checksum of each 64 KB block. Its chunks are chunks on file: they hold their range of the log, but no bytes.
3. Before the repository returns any record, it completes the `LogFileSource`. Nothing can be read through it before then.
4. Afterwards, a record's content is read through its chunk and the source: each read opens its own handle, reads the blocks that hold the bytes, checks their checksums and closes the handle. A `ContentBuffer` reads a whole chunk at a time, so each reader reads a chunk once when it reaches it: a filter's parallel workers may each read the chunk where their ranges meet.

## A Log That Changed

A log that has only grown is normal: the first read that finds it longer logs a warning, and reads go on with the bytes parsed.

A log that was truncated, replaced or corrupted must be reloaded (#1168):

1. The read that finds it shorter, or a block changed, throws `LogFileChangedException`, with the length expected and found, or the offset of the changed block. Every later read checks again, and throws again.
2. The first such read logs one warning and sets `LogFileSource.HasChanged`.
3. A filter throws the same exception, not an `AggregateException` from its parallel workers, and does not log it as an unexpected error.
4. The caller shows the warning and calls `Engine.Reload()`. Core never reloads by itself: a reader such as a filter worker cannot swap the engine it is reading.
5. Reload deletes the saved line index first, because the index checks only the first and last 4 KB of the log, so it can still match a log changed in the middle. It keeps comments, pins, bookmarks, the filter and the mode, as every Reload does.
6. If the log changes again before Reload applies the filter, Reload deletes the line index it has just saved, and ends like one that could not read the log: the engine shows the log without records, keeps the filter for the next Reload, and Reload throws what the read threw.

The GUI, CLI and PowerShell do step 4, and handle what Reload throws in step 6, in #1028. A read that fails because the log can no longer be opened, with another `IOException` or an `UnauthorizedAccessException`, is thrown as it is by a filter, as in step 3, and ends a Reload as in step 6. Unlike a change, the filter logs it as an error, because nothing has logged it before.

## Testing in Disk Mode

In a Debug build, the environment variable `WEEVIL_TEST_STORAGE=Disk` makes disk mode the default for a builder given no mode. A mode given to the builder, or carried by Reload, wins.

[`Tst/Disk.runsettings`](../../Tst/Disk.runsettings) sets it, and marks the run so a test can check that logs really open on disk. CI runs the Core unit, feature and functional tests a second time with it. Use it for those projects only: a Debug GUI started with the variable set opens logs on disk too.
