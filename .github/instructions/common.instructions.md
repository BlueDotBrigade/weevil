---
applyTo: "Src/BlueDotBrigade.Weevil.Common/**"
---

# Common Library Instructions

## Purpose

These instructions apply to the shared infrastructure library (`BlueDotBrigade.Weevil.Common`). This library defines the public contracts, data types, and cross-cutting utilities consumed by every other component in the repository. Changes here have the widest impact.

## Design Principles

Apply the project-wide design principles defined in `copilot-instructions.md`. Because `Common` is consumed by every component in the codebase, design decisions here have the widest ripple effect.

## Stability Rules

- Do not modify the `IRecord` interface without a full compatibility review; it is a public contract consumed across the entire codebase.
- Do not expand method or property semantics beyond what is strictly required by the issue being addressed.
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

## Filter Contracts

- `IFilterStrategy` defines the filtering contract: `FilterType`, `FilterCriteria`, and `CanKeep(IRecord)`.
- `FilterCriteria` is constructed directly: `new FilterCriteria(includeExpression)` or use `FilterCriteria.None` when no filter should be applied.
- `FilterType` specifies how the expression is interpreted (e.g., `PlainText`, `RegularExpression`, `Temporal`).
- Do not change `IFilterCriteria` or `FilterCriteria` in ways that break existing serialized filter history.

## Logging

- Use `Log.Default.Write(LogSeverityType, message, context)` for diagnostics.
- Do not log record content at levels higher than Debug to avoid leaking sensitive data.
