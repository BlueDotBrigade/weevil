# An Open During a Log Roll Saves an Index for the Wrong File

> Notes from #1247, 2026-11-18.

## Symptom

A large log is renamed while Weevil opens it, as a logging library does when it rolls the log, for example `App.log` to `App.log.1`. The open finishes, but the next open of `App.log` parses it in full, and `App.log.1` has no `.idx`.

## Cause

Weevil lets other applications rename a log while it reads it (#1247), and its handle follows the file. The open reads `App.log.1`'s bytes, then saves their index under the name it was given: `App.log.idx`. The next open of the new `App.log` rejects that index, because the first and last 4 KB and the length no longer match, and parses again.

## Fix

Nothing is wrong: no stale index is ever used. On a log big enough for disk mode the saved index is a large wasted write, which is the cost of never blocking the application that writes the log.
