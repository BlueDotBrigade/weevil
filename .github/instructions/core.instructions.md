---
applyTo: "Src/BlueDotBrigade.Weevil.Core/**,Src/BlueDotBrigade.Weevil.Common/**"
---

# Core Library Instructions

## Purpose

These instructions apply to the core log analysis library (`BlueDotBrigade.Weevil.Core`) and the shared infrastructure library (`BlueDotBrigade.Weevil.Common`). The core library must remain stable and performant. Changes here can affect every other component in the repository.

## Stability Rules

- Preserve existing filter semantics. Do not change how inclusive or exclusive filters select records without explicit approval.
- Do not expand method semantics beyond what is strictly required by the issue being addressed.
- Avoid introducing unnecessary abstractions, base classes, or helper layers.
- Prefer minimal, targeted changes to reduce regression risk.

## Naming Conventions

- Follow Microsoft .NET Framework Design Guidelines.
- Methods use verb or verb-phrase names (`Apply`, `CanKeep`, `BuildExpression`).
- Properties use noun or adjective names (`FilterType`, `IsGenuine`, `HasContent`).
- Interfaces are prefixed with `I` and describe a capability or contract (`IFilterStrategy`, `IRecordParser`).
- Internal implementation types do not require the `I` prefix.

## Data Model

- `IRecord` is the immutable public contract for a log record. Do not modify `IRecord` without a compatibility review.
- `Record` is the sealed, thread-safe implementation. Records must never be mutated after creation.
- Mutable per-record state (comments, pinned status, flags) belongs in `Metadata`, which supports `INotifyPropertyChanged`.
- Use `ImmutableArray<IRecord>` for record collections passed across boundaries.
- Use `Record.Dummy` as the null object when a genuine record is absent.
- Use `Record.IsGenuine(record)` or `Record.IsDummyOrNull(record)` to validate record references.

## Filter Architecture

- `IFilterStrategy` defines the filtering contract: `FilterType`, `FilterCriteria`, and `CanKeep(IRecord)`.
- `FilterStrategy.KeepAllRecords` is the singleton that passes all records when no filter is active.
- Inclusive and exclusive filter expressions are composed using `LogicalOrOperation`.
- Pinned and bookmarked records override both include and exclude filters when their respective options are enabled.
- Filter aliases must be registered in both `TsvCoreExtension` and `DefaultCoreExtension`.

## Math Namespace

- Statistics calculators live in `BlueDotBrigade.Weevil.Math` and implement `ICalculator`.
- All calculated values must be rounded to 3 decimal places: `System.Math.Round(value, 3)`.
- Use fully qualified `System.Math.*` calls inside this namespace to avoid collision with the namespace name itself (e.g., `System.Math.Sqrt`, `System.Math.Round`).
- Return `null` from `Calculate()` when the input collection is empty.

## Plugin Coupling

- Do not introduce direct dependencies from `BlueDotBrigade.Weevil.Core` to `BlueDotBrigade.Weevil.Plugins`.
- Extensibility points use `ICoreExtension` and `IPlugin` interfaces.
- Plugin-specific behavior must be injected at construction time via the extension mechanism, not hardcoded.

## Performance

- Log files can contain hundreds of thousands of records. Avoid LINQ operations that enumerate all records multiple times per filter application.
- Use `Parallel.For` or `Parallel.ForEach` for bulk metadata updates.
- Avoid allocating per-record objects in the hot path of filtering or parsing.

## Persistence

- Metadata and application state are persisted in XML sidecar files alongside the log file.
- Do not change the sidecar XML schema without providing a migration path that reads older versions.
- Sidecar versioning namespaces follow the pattern `BlueDotBrigade.Weevil.Configuration.Sidecar.v1`, `.v2`, etc.

## Logging

- Use `Log.Default.Write(LogSeverityType, message, context)` for diagnostics.
- Log timing metrics for expensive operations (file load times, record counts).
- Do not log record content at levels higher than Debug to avoid leaking sensitive data.
