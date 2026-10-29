# A Debug Build Keeps a Method's Locals Alive

> Notes from #1126, 2026-10-29.

## Symptom

Reload let go of the old engine before it read the log again, and nothing referred to the old records any more. Yet the old records were still in memory while the new log was read. A test that watched one record through a `WeakReference` kept failing.

## Cause

In a Debug build, the JIT keeps every local variable and temporary of a method alive until the method returns, so the debugger can show them. A Release build lets the garbage collector free an object after its last use.

In `Engine.Reload`, a temporary holding the old filter kept the old engine's filter alive until Reload returned. Through it, all 10 million old records stayed alive, so the forced collection during the read still found two copies of the log.

CI builds and tests in Debug, so the tests see this behaviour too.

## Fix

Move the code that reads the old object into a method of its own, marked `[MethodImpl(MethodImplOptions.NoInlining)]`, and return only what must outlive it. Its locals die when it returns. See `Engine.Release`.

## A Finalizer Has the Same Effect

An object with a finalizer, and everything it refers to, survives one more full collection. `~CoreEngine` only wrote a log line, yet it kept each discarded engine and its records alive for a second collection, so it was removed.

## Testing That an Object Was Freed

- Create the `WeakReference` in a `NoInlining` helper method, or the test method's own locals keep the object alive. See `EngineTests.TrackFirstRecord`.
- Pass `trackResurrection: true`. A short weak reference is cleared before finalizers run, so it reports an object as freed while a finalizer still keeps it.
