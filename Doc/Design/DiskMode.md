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
4. Afterwards, a record's content is read through its chunk and the source: each read opens its own handle, reads the blocks that hold the bytes, checks their checksums and closes the handle. A `ContentBuffer` reads a whole chunk at a time, so a scan such as a filter reads each chunk once.

A read that finds the log shorter, or a block changed, throws `LogFileChangedException`. A log that has only grown logs a warning once, and reads go on.

## Testing in Disk Mode

In a Debug build, the environment variable `WEEVIL_TEST_STORAGE=Disk` makes disk mode the default for a builder given no mode. A mode given to the builder, or carried by Reload, wins.

[`Tst/Disk.runsettings`](../../Tst/Disk.runsettings) sets it, and CI runs the Core unit, feature and functional tests a second time with it. Use it for those projects only: a Debug GUI started with the variable set opens logs on disk too.
