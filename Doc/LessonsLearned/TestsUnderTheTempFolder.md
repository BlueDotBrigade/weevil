# Tests Fail When the Repository Is Under the Temporary Folder

> Notes from the Weevil 3.0 audit and #1269, 2026-11-18.

## Symptom

Run from a clone or git worktree under `/tmp` (Linux) or `%TEMP%` (Windows), dozens of tests that save a sidecar or a `.idx` fail: for example a reopen "without being parsed", or a comment kept after Clear. The same tests pass from a clone elsewhere.

## Cause

Weevil saves neither a sidecar nor a `.idx` for a log in the temporary folder, because a log there is usually a copy extracted from an archive (`FileHelper.IsInTemporaryDirectory`). A test's log, copied beside the test, is then in that folder too.

## Fix

Run the tests from a clone outside the temporary folder. A scratch worktree belongs, for example, under your home folder.
