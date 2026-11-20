# A Sidecar Save Keeps the Lines Not in Memory

> Notes from #1268, 2026-11-18.

## Symptom

After Clear, or after a Reload that could not read the log, Save keeps the saved comments and pins of records no longer in memory, rather than erasing them. But a change to such a record that was never saved is not written.

## Cause

A save writes the comments and pins of the records in memory, and keeps what the sidecar already held for every other line (`v2.SidecarLoader`). A record that Clear removed is not in memory, so its unsaved change is not written.

## Fix

Clear saves the sidecar first (#1004). Code that removes records from memory, or replaces them, must save before it does, as Clear does. Where the sidecar can never be written, as in a read-only folder, Clear goes ahead without saving, since no save could keep those changes; a failure that may pass, such as a lock, stops it (#1246). Reload, opening another log and closing Weevil follow the same rule; the last two ask first when the log has unsaved changes (#1307). A Reload that fails leaves an engine without records, so a save then keeps only what was already saved.
