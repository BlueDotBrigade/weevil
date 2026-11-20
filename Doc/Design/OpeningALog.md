# Opening a Log

How Weevil opens a log: the kinds of open, what each reads and writes beside the log, and when it trusts what it saved.

## The Files

- **The log** is only ever read, through a handle that lets other applications write, rename or delete it meanwhile (`FileHelper.Open`, #1247).
- **`<log>.idx`** (`SavedIndex`) is a cache: the line index and, after a full open, what parsed the log and the record table it made. Its layout is in [RecordTable.md](RecordTable.md#saved-with-the-index). Deleting it is always safe. `Engine.EstimateMemory` reads only its header's line count, unchecked ([DiskMode.md](DiskMode.md#storage-modes)).
- **`<log>.xml`**, the sidecar (`SidecarManager`), keeps the user's work: comments, pins, bookmarks, regions, the table of contents, remarks, filter history and the plugin's context. Nothing checks it against the log: its entries apply by line number.
- Neither file is saved for a log in the temporary folder (`FileHelper.IsInTemporaryDirectory`).
- **`<log>.lock`** (`LogLock`) is held while the log is open, so another instance of Weevil does not open it and overwrite the `.xml` and `.idx` ([below](#locking-the-log), #1254). It is deleted when the log closes, and is safe to delete when no Weevil has the log open.
- **A log opened from a zip** is extracted to a folder of its own, kept between opens: `%LOCALAPPDATA%\BlueDotBrigade\Weevil\Zips\<key>\<log's name>` (#1253). The key is 16 hexadecimal characters of a SHA-256 of the zip's full path, each container's entry name, and the log's CRC-32 and size as the zip records them (`IContainerReader.GetCopyKey`), so the same file of the same zip finds its folder without being extracted first, and paths stay short.
  - **Two paths** (#1312). The engine names the log as the analyst knows it, with the zip read like a folder, as Explorer and the Open As dialog show it (`Location.ToString()`): for example `C:\Logs\Archive.zip\2026\App.log`. The GUI gives it with `IEngineBuilder.UsingDisplayPath`, and Reload keeps it. `ICoreEngine.SourceFilePath`, the status bar, the dashboard, the graph and `LogFileSource.Path`, which `LogFileChangedException` and the read warnings name, all give that path. The copy is read: Core keeps its path internal (`CoreEngine.FilePath`), and `ICoreEngine.SourceDirectory` is the copy's folder, where exports, reports and splits are saved. The title shows the log's name, as for any log. The GUI keeps the copy's path itself, for what reads the file: a plugin's check, the About box's file size, and whether the open log is the copy. Telemetry takes the size from the engine, since the readable path is not a file.
  - Its lock is kept there too, beside the copy, so it protects what two instances of one user would share: the copy, its sidecar and its `.idx`. Each user has a copy of their own, so other users are not refused.
  - Its sidecar and `.idx` are saved there like any log's, so its comments, pins and bookmarks are back, and it reopens without being parsed, when it is opened from the zip again.
  - A sidecar or index the zip holds beside the log is copied out only when the folder has none: the kept one holds the analyst's later work.
  - A copy already there, of the entry's size, is used as it is: it may be the log that is open, so it is never rewritten or deleted by a second open of the same file, whether that open is cancelled or fails. A shorter one, left by a copy that stopped part way, is copied over.
  - When another log opens or Weevil closes, only the extracted log is deleted (`ExtractedLog`), and not while another Weevil has it open. The folder, its sidecar and index, and exports saved beside the log stay. A crash leaves the log's copy too, which the next open of the same file uses.
  - **Known limits.** Nothing cleans the folder up, and nothing shows it: each zipped log opened keeps its sidecar and `.idx`, which holds the record table, about 22 bytes per record. The key includes the zip's path, so a zip moved or renamed starts again without its annotations.

## The Steps

`EngineBuilder.Open` first locks the log ([below](#locking-the-log)), then `CoreEngine.CoreEngineBuilder.Build` opens it in this order:

1. **Plugin:** `PluginFactory` picks the plugin from the log's path.
2. **Line index:** `SavedIndex.Load` reads the `.idx` and checks it against the log ([below](#trusting-a-saved-index)). When it matches, only the bytes added since are scanned (`LineIndex.Update`); otherwise the whole log is (`LineIndex.Build`). The `.idx` is saved when the line index changed, unless it holds a table: saving would drop it, so a full open saves both.
3. **Range:** a region or section is looked up in the sidecar (`SidecarManager.LoadRegions`, `LoadSections`), or, on Reload, in the session kept. The engine keeps what it was asked to load (`LoadCriteria`), and `IEngine.Part` says which part is open (`LogPart`, #1219).
4. **Records:** a full open reads them through `SavedIndex.ReadAllRecords`: from the saved table, extended when the log grew, or else by parsing the log (`SerializedRecordRepository`, `LogLineReader`), whose parse and table it then saves with the line index. A partial open parses what it needs.
5. **Sidecar:** `SidecarManager.Load` reads the `.xml`.
6. **Context:** one given to the builder wins (`UsingContext`, which the GUI always passes from Open As), then the sidecar's, then the plugin's `DetermineContext`.

A full open starts at line 1, with no range, region, section or limit. Only a full open reuses or saves a table.

The builder decides only whether an open is full. `SavedIndex` decides the rest (#1256): what an open reads from the `.idx`, whether a full open reuses, extends or replaces its table, and what is written. What the `.idx` held when it was loaded is an immutable `IndexFile`, so no flag carries a decision from one step to the next.

Both line scanners, the line index's (`LineIndex`) and the parser's (`LogLineReader`), detect the encoding and find line breaks through `LogEncoding`, so they count the same lines, as `StreamReader.ReadLine` does. A start-line open and the sidecar's byte offsets rely on it.

## Locking the Log

While a log is open, its `Engine` holds `<log>.lock` beside it (`LogLock`, #1254). It holds two lines of UTF-8 text, `User=` and `Computer=`, so another Weevil can say who has the log.

- **Taken** by `EngineBuilder.Open`, before the log is read, so a refused open reads and writes nothing. It is released if the open fails. Clear and Reload keep the same `Engine`, and so its lock ([EngineAndSession.md](EngineAndSession.md#clear-and-reload)). `Engine.Surrogate` holds none: every instance shows the same empty file.
- **Refused:** when another handle holds it, in another process or in this one, the open throws `LogInUseException`, an `IOException` that names the user and computer: *"app.log is already open in Weevil by jsmith on PC-42."*, or *"... in another Weevil."* when the file can't be read. Core writes the message to the application log as a warning. `Engine.ThrowIfInUse` checks without opening, as the GUI does before it extracts a log from a zip.
- **Reopened by the same window:** `IEngineBuilder.Replacing(engine)` names the engine an open replaces. When both read the same file, the new engine takes over the lock once it has opened; if the open fails, the lock stays where it was.
- **Released** by `IEngine.Dispose`, which deletes the file and closes the handle, and does nothing else: the engine still reads its records. Save first. The GUI disposes the engine an open replaces, and the open log's engine when it closes; the CLI and PowerShell when the command ends.
- **After a crash** of the process, the operating system closes the handle, so the lock is released. Windows deletes the file; elsewhere it stays, and the next open takes it over. An existing file is never a refusal by itself.
- **On a share,** a computer that crashes, sleeps or drops off the network keeps its handle on the server until the SMB session or durable handle times out, which takes minutes: others are refused, naming that computer, until then. After a long network drop the lock can be lost without Weevil knowing. A synced folder, such as OneDrive or Dropbox, doesn't carry the lock between computers.
- **On Windows, while a log is open, its folder can't be renamed, moved or deleted,** because the lock is open in it. The log itself still can be.
- **Another user's lock:** where a user may create files but not change another user's, Weevil can't open the other's lock for writing, and opens the log unlocked. The same permissions keep it from overwriting their `.xml` and `.idx`.
- **Can't be created,** for example in a folder Weevil can't write to: the log opens without one, and the application log says why, as Clear clears there without saving (#1246).
- **How it is held.** Windows enforces share modes on every handle, between processes, within one, and over SMB: the file is held with write access, sharing only reading, so another Weevil can read who holds it, but not take it or delete it, and `DeleteOnClose` deletes it. Elsewhere Weevil takes an advisory `flock` on the handle itself, `LOCK_EX | LOCK_NB`, so no one else holds it, then makes it shared with `LOCK_SH | LOCK_NB`, so another Weevil's exclusive lock still fails while a read, which .NET locks shared, succeeds. It doesn't rely on .NET's own lock for `FileShare.None`, which `DOTNET_SYSTEM_IO_DISABLEFILELOCKING` turns off; a C library without `flock` leaves the log unlocked. There the file is deleted while still held, then closed: a Weevil that opened it in the instant before could still lock the deleted file, while a third locks a new one, a race of microseconds that is not handled. Only Weevil honours the lock there, and on a network file system it depends on the client. Byte-range locks were rejected: on Linux, .NET takes process-wide POSIX locks, which neither conflict within one process nor survive another handle to the file closing. The experiment is in [LogLockPoC](../Research/Weevil3/LogLockPoC/README.md).

## Trusting a Saved Index

**Every open** (`IndexFile.TryRead`, then `LineIndex.Matches`) rejects the whole `.idx` unless:

- its signature and format version match;
- each section's XxHash3 checksum matches, except the table's (`IndexSection`), the header agrees with the checkpoints, and the end marker ends the file;
- the log's byte order mark gives the same encoding;
- the log is at least as long as the length indexed and the end of the saved parse;
- the SHA-256 hashes of the first and last 4 KB of the length indexed match.

The table's sections are skipped here, so a damaged table costs only the table.

**A full open** (`SavedIndex.ReadAllRecords`, `SavedTableReader`) then reuses the table only when:

- the same plugin name, and the same builds (MVIDs) of the plugin, Core and Common, made it (`SavedParse.IsSameParser`). So did the same storage mode, unless the log is UTF-8, or UTF-16 in the computer's byte order, whose table is the same in both modes (#1296);
- the `.idx`, read again through a handle kept open, still holds that parse: another instance of Weevil may have replaced it;
- the table's sections pass their checksums and their own consistency checks;
- every 64 KB block of the log that was parsed matches its saved checksum. This finds a change in the middle that keeps the log's length, which the line index cannot see.

## The Kinds of Open

Every open reads the sidecar once, twice for a region or section (once on Reload, which looks in the session), and checks the `.idx` as [above](#trusting-a-saved-index).

| Open | Reads | Checks | Writes |
|---|---|---|---|
| First, with no usable `.idx` | The log twice: a scan for the line index, then the parse. | Nothing more: there is no table. | The `.idx` twice: the line index before the parse, then with the parse and table. |
| Reopen of an unchanged log | The `.idx`'s line index twice and its table once. The log's first and last 4 KB twice, and every block parsed once. | The parser and builds, then every block parsed against its checksum. | Nothing. |
| Reopen of a grown log | As a reopen, then the new bytes twice: scanned for the line index, then parsed from the last chunk's first record. | As a reopen, over the old part. | The `.idx`, with the updated line index and the longer table. |
| Table rejected: damaged, or a block changed | As a reopen until the table is rejected, then the whole log, parsed. | Whether the bytes parsed are those of the saved parse: if not, the log changed in the middle. | The `.idx`, with the parse and table, after rebuilding the line index when the log changed in the middle. |
| Another plugin or build, or the other mode for a log RAM keeps as strings | The `.idx`'s line index, then the whole log, parsed. | Only the parser, builds and mode: the table is not read. | The `.idx`, with the new parse and table. |
| The log fails to be read while the table is checked | As a reopen until the read fails, then the whole log, parsed, which usually fails too. | As a reopen. | Nothing, when the parse makes the same table: the saved one is kept. |
| Reload | As the open it repeats: full or partial. | As the open it repeats. | As the open it repeats, after deleting the `.idx` when a read on disk found the log changed. |
| Partial | The `.idx`'s line index. The log up to the end of the range or the limit. | Only the `.idx`, as every open does. | The `.idx`, only when its line index changed and it holds no table. |
| Any, in the temporary folder | As the same open elsewhere. | As the same open elsewhere. | Nothing. |

- **Reopen** (#1023): in RAM, the blocks checked fill the chunks (`SavedTableReader`). On disk, they are only checked, and the chunks read back later through a `LogFileSource` made from the saved checksums (`LogFileSource.Create`).
- **A grown log:** `GrownLogParser` keeps the rows before the last chunk, parses from its first record to the end, and the whole longer table is saved again. A table with no chunk, whose rows all keep a string, cannot say where to resume, so its log is parsed in full.
- **Reload** (`Engine.Reload`) asks for what the open it replaces asked for: its start line, range, region, section and limit (#1219). So it reloads a full open in full, and a partial open as the same part of the log as it is now:
  - a region or section is found again by name in the session kept, whose regions and table of contents may have changed since the log opened. When the name is gone, Reload throws a `KeyNotFoundException` before it lets go of the records, so they stay;
  - lines from a start line run to the new end of the log, so appended lines appear;
  - a range of lines gives the same lines.
  - In disk mode, when a read found the log changed (`LogFileSource.HasChanged`), it first deletes the `.idx` (`SavedIndex.Delete`), so the log is indexed and parsed again ([DiskMode.md](DiskMode.md#a-log-that-changed)).
  - It keeps the session in memory (#1107). Its context is read from the sidecar again, or worked out by the plugin.
  - Core does not save the sidecar first; the GUI does.
- **A partial open** always parses, and never saves a parse or a table.
  - A start line (`Engine.UsingPath(path, line)`) starts reading at the line's byte offset, from the line index (#977).
  - Without a start line, a range (`UsingRange`), region (`UsingRegion`, #1213) or section (`UsingSection`, #1212) reads from the top. Each stops after its last line. A limit (`UsingLimit`) stops after that many records.
  - It trusts the line index on its head and tail hashes alone, so it cannot see a change in the middle of the log.
  - `Engine.ReadParts` lists, from the sidecar alone, the parts that can be opened by name: the level-1 sections, each with its lines, then the named regions. A region without a name, such as one marked with Mark Start and Mark End, can't be asked for, so it isn't listed.
  - Weevil's Open As dialog offers the whole log, one of those sections or regions, or a range of lines typed by the analyst, one at a time (#1214). For a log in a zip, it lists the parts from the sidecar kept beside the copy of an earlier open, or else from the one in the zip. The part chosen wins over a range from a plugin's own Open As window. A section or region opens by name; lines start reading at the first line's byte offset.
- **RAM and disk** (#1025): both modes compute each block's checksum while parsing, and save `.idx` files of the same format. For UTF-8, and UTF-16 in the computer's byte order, both modes make the same table: its rows keep the log's bytes, and a string only for a record a plugin made itself or whose text it changed. So a reopen in the other mode reuses it, through its own mode's path (#1296). For UTF-16 in the other byte order and UTF-32, RAM keeps a string for each record, so switching modes parses again, and replaces the table. Both modes keep the log's `LogFileSource` once open: its path and the checksum of each 64 KB block, about 8 bytes per block, and no file handle. Only disk mode reads through it, and it checks every block it reads back ([DiskMode.md](DiskMode.md#reading-a-log-on-disk)). Core tells the modes apart by `StorageMode`, never by whether a source is set; only the engine without records that Reload uses has none, as would a log read from a stream that is not a file (#1255).

## Stale or Broken Files

- **An `.idx` that `IndexFile.TryRead` or `LineIndex.Matches` rejects,** or that cannot be read, is rebuilt from a scan of the log, and saved without a table.
- **A table from another parser or build,** or from the other mode for a log RAM keeps as strings, is ignored. The log is parsed, and its table replaces the saved one.
- **A damaged table, or a block that changed,** rejects the table, and the log is parsed. When the bytes parsed differ from those of the saved parse, the log changed in the middle, so `SavedIndex` rebuilds the line index before saving it. An `.idx` saved only by partial opens has no parse to compare with, so there such a change goes unseen.
- **A read that fails while the `.idx` is checked** names the file that failed, in a warning (#1256, #1323). When the `.idx` failed, it is treated as damaged: the line index is rebuilt and saved, or the parse saves its own table. When the log failed (`LogReadException`), the `.idx` may still be the log's, so it is kept. A failure while the line index is checked rebuilds the line index for this open, without saving it; a full open then checks the table again, if the `.idx` was read. A failure while the table is checked parses the log, and keeps the table unless the parse makes a different one.
- **An `.idx` that cannot be saved** costs only a warning. It is written to `.idx.tmp`, then moved over the `.idx`, so it is never left half-written.
- **A log that changes once open:** on disk, the next read throws `LogFileChangedException` ([DiskMode.md](DiskMode.md#a-log-that-changed)). In RAM, nothing reads the log once it is open: the next open or Reload reads it as it is then. The CLI and PowerShell check after each command whether it is gone or shorter (`EngineExtensions.RunOnce`).
- **A sidecar** that neither the v2 nor the v1 loader can read stops the open: `SidecarManager.Load` throws `NotSupportedException`. The v1 loader, tried when v2 cannot read the file, first rewrites its old XML namespaces in place (`LogMetadataLoader`): the only time an open writes the `.xml`.
- **The application log** says which path an open took: whether the line index was rebuilt (`SavedIndex`), whether the records came from the saved index, and whether only a grown log's new part was parsed (`GrownLogParser`).

## Writing the Sidecar

- `Engine.Save` writes it. Core saves before Clear. The GUI saves before it opens another log or reloads, on Save and on exit. The CLI and PowerShell never save it.
- A save copies the previous sidecar to `.xml~`, then merges: it keeps the saved entries of lines not in memory, such as those a partial open or Clear left out (`v2.SidecarLoader`). Each entry keeps its line number and, from the line index, its byte offset.
