# Weevil Telemetry Operations

This document describes the current Phase 1 operational setup for Weevil telemetry.

## Runtime switches

Telemetry configuration is read from `HKEY_CURRENT_USER\Software\BlueDotBrigade\Weevil`.

| Registry value | Type | Meaning |
| --- | --- | --- |
| `TelemetryEnabled` | integer or boolean-like value | Enables or disables telemetry. Missing or invalid values are treated as enabled. |
| `TelemetryConnectionString` | string | Azure SQL connection string used by the telemetry adapter. Empty or missing disables upload. |

## Installer behavior

- The installer exposes a telemetry checkbox.
- Default value: enabled
- Persisted registry value: `TelemetryEnabled`
- Installer message: telemetry improves the application and does not collect personal identifying information.

## Application behavior

- GUI and CLI both respect `TelemetryEnabled`.
- If telemetry is disabled, Weevil uses `NullTelemetryClient` and performs no upload work.
- If telemetry is enabled but `TelemetryConnectionString` is empty, the SQL client becomes a no-op and logs a warning.
- On non-Windows platforms, the registry-backed configuration falls back to enabled with no connection string.

## Connection string requirements

Provide an Azure SQL connection string in `TelemetryConnectionString`.

Operational guidance:

- Use a dedicated low-privilege SQL login.
- Grant only `INSERT` permission on `telemetry.Session`.
- Do not grant read, update, delete, or schema-change permissions.
- Treat the connection string as an operational secret and provision it outside source control.

The runtime provider enforces these settings even if the supplied connection string says otherwise:

- `Encrypt=True`
- `TrustServerCertificate=False`
- `Connect Timeout=5`

Current provider defaults:

- Async command timeout: 30 seconds
- Sync best-effort command timeout: 5 seconds
- Connection timeout: 5 seconds

## Recommended setup flow

1. Deploy the `telemetry.Session` table using the schema documented in `TelemetrySchemaAndPrivacy.md`.
2. Create a SQL login that can insert into `telemetry.Session` only.
3. Set `TelemetryConnectionString` in `HKEY_CURRENT_USER\Software\BlueDotBrigade\Weevil`.
4. Install Weevil with telemetry enabled, or set `TelemetryEnabled` to `1`.
5. Open and close a log file in Weevil to produce one ended session.

## Validation checklist

Use this checklist after configuration changes.

1. Confirm `TelemetryEnabled` is present and set to the expected value.
2. Confirm `TelemetryConnectionString` is present for environments that should upload telemetry.
3. Verify that opening a file starts a session and opening a different file ends the previous session.
4. Verify that closing the application ends the current session once.
5. Verify that a disabled install produces no upload attempts.
6. Verify that an empty or invalid connection string does not crash the application.
7. Verify that the database receives one row per ended session.
8. Verify that the inserted row contains `Application`, `Version`, `SessionStartUtc`, `SessionEndUtc`, `SessionActiveMinutes`, `LogFileSizeBytes`, `InstalledRamMb`, `FilterExecutionCount`, `GraphOpenCount`, `DashboardOpenCount`, and `SchemaVersion`.

## Regression checks

The existing telemetry unit tests cover the main operational guarantees:

- disabled/no-op behavior
- lifecycle start/end behavior
- idle-time exclusion
- async rollover upload
- sync shutdown upload
- exactly-once upload behavior
- connection-string hardening and timeout enforcement

Run from the repository root:

`dotnet test Weevil-v2.sln --configuration Debug -p:Platform=x64 --no-build`
