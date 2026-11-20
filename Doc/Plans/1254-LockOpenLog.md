# 1254: Refuse a Log Another Weevil Has Open

> **Status:** In progress
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

User story #1254, Weevil 3.0. Branch `Feature/1254-LockOpenLog`.

> As an analyst, I want Weevil to refuse to open a log that another Weevil already has open, in order not to lose comments and pins to the other window's saves.

Each open log has a sidecar (`.xml`) and a saved index (`.idx`). Today two instances of Weevil can open the same log, on one computer or through a share, and each overwrites the other's files: the last save wins. Word keeps `~$file.docx` beside a document for the same reason, and LibreOffice `.~lock.file#`.

## Assumption

When the lock can't be created, for example in a folder Weevil can't write to, the log still opens, and the application log says why it isn't locked. This matches Clear in a read-only folder (#1246): nothing Weevil writes there could be lost to another instance either.

## Decisions

1. **Core owns the lock.** The CLI, PowerShell and tests need the rule too, and the GUI is a thin layer over Core.
   - `Engine`, the facade that lives as long as its open log, holds it. `EngineBuilder.Open` takes it before the log is read, so a refused open reads and writes nothing, and releases it if the open fails.
   - `IEngine` becomes `IDisposable`. `Dispose` releases the lock, and nothing else: the engine still reads its records, so a row or a dashboard still on screen keeps working. Save first: once disposed, nothing keeps another Weevil from writing the sidecar.
   - Clear, Reload and F5 keep the same `Engine`, so they keep its lock and never refuse themselves.
   - `Engine.Surrogate`, the empty log every instance of Weevil shows until a log opens, holds no lock: every instance opens the same file.
2. **Reopening the log it has open.** Opening a log replaces the engine the GUI holds only once the new one has opened, so the old one still holds the lock while the new one asks for it. `IEngineBuilder.Replacing(engine)` names the engine replaced: when both read the same file, the new engine takes over its lock once it has opened. If the open fails, the lock stays with the engine replaced. The GUI then disposes the engine replaced. No process-wide registry of locks: two engines of the same log in one process are refused like two instances of Weevil, which keeps the rule testable in one process and the lock's lifetime explicit.
3. **The file.** `<log>.lock` beside the log, kept open while the log is, holds two lines of UTF-8 text: `User=` and `Computer=` (`Environment.UserName`, `Environment.MachineName`). `LogLock`, internal to Core, takes and releases it.
4. **How it is held, per operating system.** Verified on Linux with an experiment, in one process and between two (the experiment's results are below). Windows' behaviour is documented, and checked by CI.
   - **Windows** enforces share modes on every handle, between processes, within one process, and over SMB, where the server enforces them for every client. Held with write access, `FileShare.Read` and `FileOptions.DeleteOnClose`: another Weevil's attempt to take it, which asks for write access, fails with a sharing violation, while a read of who holds it succeeds (it shares read, write and delete). Not sharing delete keeps the file from being deleted while it is held.
   - **Linux and macOS:** .NET takes an advisory `flock` per handle, `LOCK_EX` for `FileShare.None` and `LOCK_SH` for any other share, so `FileShare.Read` on both sides would let two instances hold it. Weevil opens it with `FileShare.None`, so no other Weevil holds it, then converts the lock to `LOCK_SH | LOCK_NB` through `flock` itself: another Weevil's `LOCK_EX` still fails, and a read, which takes `LOCK_SH`, succeeds. With `LOCK_NB`, Linux converts the lock under one spinlock, so no other lock gets in between. Only Weevil honours the lock: other applications may still read or write the file. On network file systems, `flock` depends on the client (NFS emulates it with byte-range locks; CIFS may keep it on one computer).
   - Byte-range locks (`FileStream.Lock`) were rejected: on Linux, .NET takes process-wide POSIX locks, which neither conflict within a process nor survive another handle to the file being closed.
5. **Refused.** `LogInUseException`, an `IOException` in `BlueDotBrigade.Weevil.IO` (Common, beside `LogFileChangedException`), carries the holder's `UserName` and `ComputerName`. Its message: *"app.log is already open in Weevil by jsmith on PC-42."*, or *"app.log is already open in another Weevil."* when the file can't be read, for example while the other is still writing it. Core writes it to the application log as a warning, so every front end logs it once.
6. **Closed.** Disposing the engine closes the handle, and `DeleteOnClose` deletes the file: on Linux .NET deletes it before closing the handle, so no other Weevil takes the lock on a file no longer in the folder.
7. **After a crash** the operating system closes the dead process's handle, so its lock is released. Windows then deletes the file, because of `DeleteOnClose`. On Linux the file stays, and the next Weevil opens it, takes the lock and rewrites it: an existing file is never a refusal by itself.
8. **A log opened from a zip** keeps its lock beside its copy: `%LOCALAPPDATA%\BlueDotBrigade\Weevil\Zips\<key>\<log>.lock`.
   - The copy, its sidecar and its index are per user, so two users opening the same zip on a share have their own and lose nothing to each other; two instances of one user share them, and the lock beside them protects exactly what they share. A lock beside the zip would refuse other users for nothing, and could not be written on a read-only share.
   - The key is worked out from the zip's directory alone (#1253), so the lock's path is known before anything is extracted. `Engine.ThrowIfInUse(path)` checks it then: Open As calls it, through a new `OpenAsDialog.CheckBeforeOpening`, before it extracts (or, for a log not in a zip, before it closes), and shows the refusal in the dialog, which stays open; dropping a zip on Weevil checks before extracting too. The GUI does not check the log it already has open: reopening it is allowed (decision 2).
   - A check, then an open, leaves a moment for another Weevil to open the log in between. The open then refuses, and nothing is lost: an extraction never rewrites a complete copy (#1253), and `ExtractedLog.Delete` never deletes a copy another Weevil has open.
9. **GUI.** A refused open shows its message the way an open that fails already does (`IDialogBoxService.ShowMessage`, from the open's background task), titled *Log Already Open*, and the open log stays. The open's operation in the single `BusyState` (#1257) ends as for any failed open. The engine replaced is disposed after a successful open. Closing Weevil saves, then releases the lock, then deletes the zipped log's copy; closing while an operation runs releases the lock before deleting the copies. `FilterViewModel.DeleteExtractedLogs` becomes `Close`, which does both.
10. **CLI and PowerShell** dispose the engine when the command ends. A refused open stops the CLI with the message and exit code 1, as a log that changed does (#1247), and a cmdlet with a terminating error of category `ResourceBusy`.
11. **Plugins.** A plugin whose `ShowOpenAs` opens an engine must dispose it, or Weevil's own open of the same log is refused until the engine is collected. No plugin in this repository opens one, and `ShowOpenAs` is being replaced (#1157).
12. **Unchanged:** the `.xml` and `.idx` are written as today.

## Tests

- **Core feature tests** (Reqnroll, `IO/LockingALog.feature`): a second open is refused, naming who has the log; closing deletes the lock and lets another open; a leftover lock no one holds doesn't block; Reload and Clear of the open log work; a lock that can't be created, because a folder has its name, still lets the log open, on any system and as an administrator.
- **Core unit tests:** `Replacing` hands the lock over, and keeps it with the engine replaced when the open fails; `Engine.ThrowIfInUse`.
- **CLI and PowerShell:** a held log stops the command with the warning.
- **GUI** (Windows only): reopening the log Weevil has open; a refused open keeps the open log; a zipped log refused before extraction leaves the other instance's copy.
- **Existing tests** open the same test log many times in one process, and now dispose each engine (`using`), so the next open is not refused. The feature tests' `Token` disposes its engine when the scenario ends, and when a step opens another. No test is disabled or skipped.
- `*.lock` is ignored by git, as `*.idx` is.

## Steps

1. This plan.
2. Core: `LogLock`, `LogInUseException`, `IEngine : IDisposable`, `Replacing`, `ThrowIfInUse`, with their tests, and the existing tests disposing their engines.
3. CLI and PowerShell.
4. GUI: dispose, reopen, the refusal's message, Open As's check before extraction, `ExtractedLog`.
5. `Doc/Design` (OpeningALog, EngineAndSession), the help and the PowerShell readme.

## Experiment

On Linux (.NET 10, ext4-like local file system, 2026-11-20), in one process and between two:

| Holder | Second | Result |
|---|---|---|
| `FileShare.Read`, write access | Same | Opens: both take `LOCK_SH` |
| `FileShare.Read`, write access | `FileShare.None` | Sharing violation (`EAGAIN`, `HResult` 11) |
| `FileShare.None` | Read, `FileShare.ReadWrite` | Sharing violation: the owner can't be read |
| `FileStream.Lock` on a range | `Lock` on the same range, same process | Succeeds: POSIX locks are per process |
| `FileShare.None`, then `flock(LOCK_SH \| LOCK_NB)` | `FileShare.None` | Sharing violation, in one process and between two |
| Same | Read, `FileShare.ReadWrite \| Delete` | Reads the owner |
| Same, holder killed with `SIGKILL` | `FileShare.None` | Takes it: the file was left, unheld |
| Same, disposed | | The file is deleted (`DeleteOnClose`) |

A failed open with `DeleteOnClose` does not delete the file someone else holds.
