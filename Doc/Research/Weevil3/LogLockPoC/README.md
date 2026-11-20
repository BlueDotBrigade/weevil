# Log Lock Proof of Concept

Question for #1254 ([plan](../../../Plans/1254-LockOpenLog.md)): how can Weevil hold a file beside an open log so that another Weevil, in another process or in this one, can't take it, but can read who holds it, on Windows and on Linux?

## What It Does

`Program.cs` opens `App.log.lock` in a folder (by default under the temporary folder) in several ways, and reports whether a second handle opens it:

```
dotnet run -- table [folder]          every case, in one process
dotnet run -- hold [folder] [ms]      holds the lock as Weevil does
dotnet run -- probe [folder]          in another process, tries to take it, then reads who holds it
```

## Results on Linux

.NET 10, 2026-11-20, a local file system. `table` in one process, and `hold` with `probe` between two, gave the same answers:

| Holder | Second handle | Result |
|---|---|---|
| Write access, `FileShare.Read` | The same | Opens: both take a shared `flock` |
| Write access, `FileShare.Read` | `FileShare.None` | Sharing violation (`EAGAIN`, `HResult` 11) |
| `FileShare.None` | Read, `FileShare.ReadWrite` | Sharing violation: who holds it can't be read |
| `FileStream.Lock` on a range | `Lock` on the same range, same process | Opens: the lock belongs to the process |
| `FileShare.None`, then `flock(LOCK_SH \| LOCK_NB)` | `FileShare.None` | Sharing violation |
| The same | Read, `FileShare.ReadWrite \| Delete` | Reads who holds it |
| The same, with `DeleteOnClose`, disposed | | The file is deleted |
| The same, the holder killed with `SIGKILL` | `FileShare.None` | Opens: the file was left, unheld |

A second handle's failed open with `DeleteOnClose` does not delete the file someone else holds.

## Why

- **Linux:** .NET emulates `FileShare` with an advisory `flock` on each handle: `LOCK_EX` for `FileShare.None`, `LOCK_SH` for any other share. So `FileShare.Read` on both sides lets both hold the file, and `FileShare.None` keeps others from reading it. Converting the exclusive lock to a shared one with `LOCK_NB` keeps out every other exclusive open while letting reads through; Linux converts it under one spinlock, so nothing gets in between. `flock` is per open file description, so two handles in one process conflict too.
- **`FileStream.Lock`** uses `fcntl` record locks on Linux, which belong to the process: they don't conflict within one process, and closing any handle to the file releases them all.
- **Windows** enforces share modes on every handle, within a process too, and SMB servers enforce them for every client. Write access sharing only reading refuses another writer and lets a reader read. `DeleteOnClose` deletes the file once the last handle closes, also when the process dies. Not yet run here on Windows; the CI tests check the behaviour Weevil relies on.

## Decision

Adopted in `LogLock` (#1254): on Windows, write access with `FileShare.Read` and `DeleteOnClose`; elsewhere `FileShare.None` with `DeleteOnClose`, then `flock(LOCK_SH | LOCK_NB)`. A reader shares reading, writing and deleting. Described in [OpeningALog.md](../../../Design/OpeningALog.md#locking-the-log).
