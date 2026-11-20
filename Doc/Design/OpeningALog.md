# Opening a Log

How Weevil opens a log: the kinds of open, what each reads and writes beside the log, and when it trusts what it saved.

## The Files

- **The log** is only ever read, through a handle that lets other applications write, rename or delete it meanwhile (`FileHelper.Open`, #1247).
- **`<log>.idx`** (`LineIndexFile`) is a cache: the line index and, after a full open, what parsed the log and the record table it made. Its layout is in [RecordTable.md](RecordTable.md#saved-with-the-index). Deleting it is always safe. `Engine.EstimateMemory` reads only its header's line count, unchecked ([DiskMode.md](DiskMode.md#storage-modes)).
- **`<log>.xml`**, the sidecar (`SidecarManager`), keeps the user's work: comments, pins, bookmarks, regions, the table of contents, remarks, filter history and the plugin's context. Nothing checks it against the log: its entries apply by line number.
- Neither file is saved for a log in the temporary folder (`FileHelper.IsInTemporaryDirectory`).
- **A log opened from a zip** is extracted to a folder of its own, kept between opens: `%LOCALAPPDATA%\BlueDotBrigade\Weevil\Zips\<key>\<log's name>` (#1253). The key is 16 hexadecimal characters of a SHA-256 of the zip's full path, each container's entry name, and the log's CRC-32 and size as the zip records them (`IContainerReader.GetCopyKey`), so the same file of the same zip finds its folder without being extracted first, and paths stay short.
  - **Two paths** (#1312). The engine names the log as the analyst knows it, with the zip read like a folder, as Explorer and the Open As dialog show it (`Location.ToString()`): for example `C:\Logs\Archive.zip\2026\App.log`. The GUI gives it with `IEngineBuilder.UsingDisplayPath`, and Reload keeps it. `ICoreEngine.SourceFilePath`, the status bar, the dashboard, the graph and `LogFileSource.Path`, which `LogFileChangedException` and the read warnings name, all give that path. The copy is read: Core keeps its path internal (`CoreEngine.FilePath`), and `ICoreEngine.SourceDirectory` is the copy's folder, where exports, reports and splits are saved. The title shows the log's name, as for any log. The GUI keeps the copy's path itself, for what reads the file: a plugin's check, the About box's file size, and whether the open log is the copy. Telemetry takes the size from the engine, since the readable path is not a file.
  - Its sidecar and `.idx` are saved there like any log's, so its comments, pins and bookmarks are back, and it reopens without being parsed, when it is opened from the zip again.
  - A sidecar or index the zip holds beside the log is copied out only when the folder has none: the kept one holds the analyst's later work.
  - A copy already there, of the entry's size, is used as it is: it may be the log that is open, so it is never rewritten or deleted by a second open of the same file, whether that open is cancelled or fails. A shorter one, left by a copy that stopped part way, is copied over.
  - When another log opens or Weevil closes, only the extracted log is deleted (`ExtractedLog`). The folder, its sidecar and index, and exports saved beside the log stay. A crash leaves the log's copy too, which the next open of the same file uses.
  - **Known limits.** Nothing cleans the folder up, and nothing shows it: each zipped log opened keeps its sidecar and `.idx`, which holds the record table, about 22 bytes per record. The key includes the zip's path, so a zip moved or renamed starts again without its annotations. Two instances of Weevil that open the same file of the same zip share one copy: the one that closes first deletes the other's log (#1254 is to refuse the second open).

## The Steps

`CoreEngine.CoreEngineBuilder.Build` opens a log in this order:

1. **Plugin:** `PluginFactory` picks the plugin from the log's path.
2. **Line index:** `LineIndexFile.Load` reads the `.idx` and checks it against the log ([below](#trusting-a-saved-index)). When it matches, only the bytes added since are scanned (`LineIndex.Update`); otherwise the whole log is (`LineIndex.Build`). The `.idx` is saved when the line index changed, unless it holds a table: saving would drop it, so a full open saves both.
3. **Range:** a region or section is looked up in the sidecar (`SidecarManager.LoadRegions`, `LoadSections`).
4. **Records:** a full open tries the saved table first (`TryReopen`). Otherwise the log is parsed (`SerializedRecordRepository`, `LogLineReader`), and after a full parse `LineIndexFile.SaveParse` saves the parse and its table with the line index.
5. **Sidecar:** `SidecarManager.Load` reads the `.xml`.
6. **Context:** one given to the builder wins (`UsingContext`, which the GUI always passes from Open As), then the sidecar's, then the plugin's `DetermineContext`.

A full open starts at line 1, with no range, region, section or limit. Only a full open reuses or saves a table.

## Trusting a Saved Index

**Every open** (`LineIndex.TryRead`) rejects the whole `.idx` unless:

- its signature and format version match;
- each section's XxHash3 checksum matches, except the table's (`IndexSection`), the header agrees with the checkpoints, and the end marker ends the file;
- the log's byte order mark gives the same encoding;
- the log is at least as long as the length indexed and the end of the saved parse;
- the SHA-256 hashes of the first and last 4 KB of the length indexed match.

The table's sections are skipped here, so a damaged table costs only the table.

**A full open** (`LineIndexFile.TryLoadTable`, `SavedTableReader`) then reuses the table only when:

- the same plugin name, and the same builds (MVIDs) of the plugin, Core and Common, made it (`SavedParse.IsSameParser`). So did the same storage mode, unless the log is UTF-8, or UTF-16 in the computer's byte order, whose table is the same in both modes (#1296);
- the `.idx`, read again through a handle kept open, still holds that parse: another instance of Weevil may have replaced it;
- the table's sections pass their checksums and their own consistency checks;
- every 64 KB block of the log that was parsed matches its saved checksum. This finds a change in the middle that keeps the log's length, which the line index cannot see.

## The Kinds of Open

| Open | Reads | Writes |
|---|---|---|
| First, with no usable `.idx` | The log twice: a scan for the line index, then the parse. The sidecar. | The `.idx` twice: the line index before the parse, then with the parse and table. |
| Reopen of an unchanged log | The `.idx`'s line index twice and its table once. The log's first and last 4 KB twice, and every block parsed once. The sidecar. | Nothing. |
| Reopen of a grown log | As a reopen, then the new bytes twice: scanned for the line index, then parsed from the last chunk's first record. | The `.idx`, with the updated line index and the longer table. |
| Reload | As a full open. | As a full open, after deleting the `.idx` when a read on disk found the log changed. |
| Partial | The `.idx`'s line index. The log up to the end of the range or the limit. The sidecar, twice for a region or section. | The `.idx`, only when its line index changed and it holds no table. |

- **Reopen** (#1023): in RAM, the blocks checked fill the chunks (`SavedTableReader`). On disk, they are only checked, and the chunks read back later through a `LogFileSource` made from the saved checksums (`LogFileSource.Create`).
- **A grown log:** `GrownLogParser` keeps the rows before the last chunk, parses from its first record to the end, and the whole longer table is saved again. A table with no chunk, whose rows all keep a string, cannot say where to resume, so its log is parsed in full.
- **Reload** (`Engine.Reload`) is always a full open: it keeps no start line, range, region, section or limit of the open it replaces.
  - In disk mode, when a read found the log changed (`LogFileSource.HasChanged`), it first deletes the `.idx` (`LineIndexFile.Delete`), so the log is indexed and parsed again ([DiskMode.md](DiskMode.md#a-log-that-changed)).
  - It keeps the session in memory (#1107). Its context is read from the sidecar again, or worked out by the plugin.
  - Core does not save the sidecar first; the GUI does.
- **A partial open** always parses, and never saves a parse or a table.
  - A start line (`Engine.UsingPath(path, line)`) starts reading at the line's byte offset, from the line index (#977).
  - Without a start line, a range (`UsingRange`), region (`UsingRegion`, #1213) or section (`UsingSection`, #1212) reads from the top. Each stops after its last line. A limit (`UsingLimit`) stops after that many records.
  - It trusts the line index on its head and tail hashes alone, so it cannot see a change in the middle of the log.
- **RAM and disk** (#1025): both modes compute each block's checksum while parsing, and save `.idx` files of the same format. For UTF-8, and UTF-16 in the computer's byte order, both modes make the same table: its rows keep the log's bytes, and a string only for a record a plugin made itself or whose text it changed. So a reopen in the other mode reuses it, through its own mode's path (#1296). For UTF-16 in the other byte order and UTF-32, RAM keeps a string for each record, so switching modes parses again, and replaces the table. Both modes keep the log's `LogFileSource` once open: its path and the checksum of each 64 KB block, about 8 bytes per block, and no file handle. Only disk mode reads through it, and it checks every block it reads back ([DiskMode.md](DiskMode.md#reading-a-log-on-disk)). Core tells the modes apart by `StorageMode`, never by whether a source is set; only the engine without records that Reload uses has none, as would a log read from a stream that is not a file (#1255).

## Stale or Broken Files

- **An `.idx` that `LineIndex.TryRead` rejects,** or cannot read, is rebuilt from a scan of the log, and saved without a table.
- **A table from another parser or build,** or from the other mode for a log RAM keeps as strings, is ignored. The log is parsed, and its table replaces the saved one.
- **A damaged table, or a block that changed,** rejects the table, and the log is parsed. When the bytes parsed differ from those of the saved parse, the log changed in the middle, so `SaveParse` rebuilds the line index before saving it. An `.idx` saved only by partial opens has no parse to compare with, so there such a change goes unseen.
- **An `.idx` that cannot be saved** costs only a warning. It is written to `.idx.tmp`, then moved over the `.idx`, so it is never left half-written.
- **A log that changes once open:** on disk, the next read throws `LogFileChangedException` ([DiskMode.md](DiskMode.md#a-log-that-changed)). In RAM, nothing reads the log once it is open: the next open or Reload reads it as it is then. The CLI and PowerShell check after each command whether it is gone or shorter (`EngineExtensions.RunOnce`).
- **A sidecar** that neither the v2 nor the v1 loader can read stops the open: `SidecarManager.Load` throws `NotSupportedException`. The v1 loader, tried when v2 cannot read the file, first rewrites its old XML namespaces in place (`LogMetadataLoader`): the only time an open writes the `.xml`.
- **The application log** says which path an open took: whether the line index was rebuilt (`LineIndexFile`), whether the records came from the saved index, and whether only a grown log's new part was parsed (`GrownLogParser`).

## Writing the Sidecar

- `Engine.Save` writes it. Core saves before Clear. The GUI saves before it opens another log or reloads, on Save and on exit. The CLI and PowerShell never save it.
- A save copies the previous sidecar to `.xml~`, then merges: it keeps the saved entries of lines not in memory, such as those a partial open or Clear left out (`v2.SidecarLoader`). Each entry keeps its line number and, from the line index, its byte offset.
