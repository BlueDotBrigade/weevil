# Weevil Telemetry Schema and Privacy

This document is the Phase 1 reference for the telemetry emitted by Weevil.

## Scope

Phase 1 collects session telemetry only.

- One payload is created for each ended session.
- A session starts when a log file is opened.
- A session ends when another file is opened, the application shuts down, or an unhandled exception ends the session.
- Upload is best-effort and must never block normal use of the application.

## Privacy commitments

Weevil telemetry is intentionally minimal.

- No personal identifying information (PII) is collected.
- No record content is uploaded.
- No filter text is uploaded.
- No CPU model or detailed hardware fingerprinting is collected.
- Telemetry can be disabled by the user through the installer option and the persisted registry setting.

## Phase 1 payload

The runtime payload is `BlueDotBrigade.Weevil.Diagnostics.TelemetrySession`.

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `SessionId` | `Guid` | Yes | Unique identifier for the ended session. |
| `Application` | `string` | Yes | Executable name, currently `WeevilGui.exe` or `WeevilCli.exe`. |
| `Version` | `Version` | Yes | Weevil application version. Persisted as a string. |
| `SessionStartUtc` | `DateTime` | Yes | UTC timestamp when the session started. |
| `SessionEndUtc` | `DateTime` | Yes | UTC timestamp when the session ended. |
| `SessionActiveMinutes` | `double` | Yes | Active duration only, rounded to 3 decimal places. Idle periods over 1 minute are excluded. |
| `LogFileSizeBytes` | `long` | Yes | Size of the opened log file. |
| `InstalledRamMb` | `long` | Yes | Installed system memory in megabytes. |
| `FilterExecutionCount` | `int` | Yes | Number of filter executions recorded in the session. |
| `GraphOpenCount` | `int` | Yes | Number of graph opens recorded in the session. |
| `DashboardOpenCount` | `int` | Yes | Number of dashboard opens recorded in the session. |
| `SchemaVersion` | `string` | Yes | Current runtime value is `1.0`. |

## Activity accounting

`SessionActiveMinutes` increases only when meaningful activity is recorded.

- GUI filter execution
- GUI navigation and selection actions
- GUI record actions such as bookmark, pin, and comment
- GUI dashboard open
- GUI graph open
- CLI command execution

If elapsed inactivity is greater than 1 minute, that interval is treated as idle time and is not added to the active duration.

## Storage model

The current provider stores session rows in Azure SQL through Entity Framework.

- Schema: `telemetry`
- Table: `Session`
- Primary key: `SessionId`
- Insert-only behavior; Weevil does not update or delete telemetry rows

### Current column constraints

- `Application`: required, max length 256
- `Version`: required, max length 32
- `SchemaVersion`: required, max length 16

## Transport and failure guarantees

- Upload on file rollover is asynchronous.
- Upload on shutdown or crash is synchronous best-effort.
- Telemetry failures are swallowed and logged internally.
- SQL transport is always encrypted.
- Server certificate validation is always required.
- The provider enforces a short connection timeout to avoid disrupting the user workflow.

## Explicit non-goals

The following are not part of Phase 1:

- Event-level telemetry
- Local disk queue or retry persistence
- Complex schema migration support
- Upload of filter expressions, comments, or log record payloads

## Future extension

Appendix A of `TelemetryRolloutPlan.md` remains the additive roadmap for future entities such as `SessionFilterExecution`, `SessionEvent`, and `SessionAnalyzerExecution`.
