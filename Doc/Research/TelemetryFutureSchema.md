# Telemetry: Future Schema

> Not implemented. Ideas for extending telemetry after phase 1, kept from Appendix A of the telemetry rollout plan, which was deleted once phase 1 was implemented (#983).

Design goal: keep schema additive so Phase 1 writes a strict subset and Phase 2 extends without breaking compatibility.

## `Session` entity
Required in Phase 1:
- `SessionId`
- `Application` (`WeevilGui.exe` / `WeevilCli.exe`)
- `Version`
- `SessionStartUtc`
- `SessionEndUtc`
- `SessionActiveMinutes`
- `LogFileSizeBytes`
- `InstalledRamMb`
- `FilterExecutionCount`
- `GraphOpenCount`
- `DashboardOpenCount`
- `SchemaVersion`

Planned additive fields (Phase 2+):
- Exception and performance summary fields.
- Additional high-value usage counters.

## `SessionFilterExecution` entity
Purpose: support future filter analytics without widening the session entity excessively.

Introduced in Phase 2:
- `SessionId`
- `ExecutedAtUtc`
- `ExecutedPeriodMinutes`
- `HasIncludeFilter`
- `HasExcludeFilter`
- `FilterOptions`
- `ExecutionDurationMs`

## `SessionEvent` entity
Introduced in Phase 2:
- `EventId`
- `SessionId`
- `EventType` (e.g., `FilterApplied`, `AnalyzerRun`, `GraphOpened`, `DashboardOpened`, `RecordCommentAdded`)
- `EventCategory` (`Filter`, `Analyzer`, `Navigation`, `UI`)
- `TimestampUtc`
- `ExecutionDurationMs` (when applicable)
- event-specific safe metadata (no PII)

## `SessionAnalyzerExecution` entity
Introduced in Phase 2:
- `SessionId`
- `AnalyzerType`
- `StartedAtUtc`
- `CompletedAtUtc`
- `ExecutionDurationMs`
- `InputRecordCount`
- `OutputMetricCount`

## Guardrails
- CPU model/CPU telemetry is excluded from all phases.
- No PII in any entity.
