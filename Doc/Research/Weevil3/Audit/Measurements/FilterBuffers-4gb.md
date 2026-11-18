# Disk Mode: What a Filter Allocates, and a Prototype Fix

2026-11-17, `main` e3dee822, Linux (see [README.md](README.md)). Supports `Findings/U3-DiskMode.md` (U3-2) and `Findings/Performance.md`.

**Not a change to Weevil.** The counts and the prototype come from throwaway builds of Core in a scratch worktree. [Prototypes/FilterBufferPool.patch](Prototypes/FilterBufferPool.patch) is the prototype's diff, kept so the owner can repeat the measurement. It's an experiment, not a proposed implementation.

## Cause, Confirmed

One plain-text filter (`timeout`) on a log opened on disk, after a warm-up filter. The number of thread-local states comes from a counter added to `FilterMapReduceState`'s constructor. The allocations by type come from [AllocDiag](AllocDiag/).

| Build | Log | States created (4 workers) | Allocated | Large `byte[]` | Full (gen 2) collections |
|---|---|---|---|---|---|
| `main` | 1 GB (60 chunks) | 18 | 881 MB | 497 MB | 2 |
| `main` | 4 GB (239 chunks) | 47 | 2,877 MB | 1,349 MB | 2 |
| Pool of buffers per filter | 1 GB | 16 | 703 MB | 319 MB | 1 |
| Pool of buffers per filter | 4 GB | 58 | 1,997 MB | 470 MB | 0 |
| Pool, buffers rounded up to a power of two | 1 GB | 15 | 586 MB | 201 MB | 0 |
| Pool, buffers rounded up to a power of two | 4 GB | 49 | 1,729 MB | 201 MB | 0 |

- **`Parallel.ForEach` replaces its local state repeatedly.** It created 18 states for 4 workers on the 1 GB log and 47 on the 4 GB log. Each new state holds a new `ContentBuffer`, whose first whole-chunk read allocates a new array of about 16 MB (`Common/Data/ContentBuffer.cs:120-123`).
- **A pool alone isn't enough.** Chunks differ in length by a few bytes, so a reused buffer still grows whenever a slightly longer chunk arrives.
- **With buffers that don't need to grow,** the large `byte[]` allocated per filter is constant (201 MB) instead of growing with the log, and the full collections stop.
- **`Record`** (48 bytes per row per filter) is the same in every build, and in RAM. It's the documented cost of records created on demand (`.github/instructions/core.instructions.md`, Performance).

## Filtering the 4 GB Log on Disk With Little Memory

`DOTNET_GCHeapHardLimit` caps the managed heap. The table alone holds 0.67 GB. Full results: [Ops-4gb-disk-heap-capped.txt](Ops-4gb-disk-heap-capped.txt), and `results` below.

| Build | Heap cap | Operations that finished (of 9) | Notes |
|---|---|---|---|
| `main` | 1.5 GB | 9 | 20–50% slower than without a cap; peak working set 1.61 GB |
| `main` | 1 GB | 7 | `timeout` (11.6 million results) and `Warning||Error` threw `OutOfMemoryException` |
| Pool, power of two | 1 GB | 1 | Buffers of 32 MB instead of 16 MB: more memory live at once |

**Conclusion.** Disk mode on `main` needs about the record table plus 0.5–0.8 GB to filter. Pooling the buffers removes the large-object churn and the full collections, but the buffers must be sized to the chunk, not rounded up, and capped at the number of workers. Otherwise the live footprint at the peak grows instead of shrinking. Prototype results under a tight cap are in `Ops-4gb-disk-limit-1gb-prototype.txt`.
