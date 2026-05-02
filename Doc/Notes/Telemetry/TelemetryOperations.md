# Weevil Telemetry Operations

This document describes the current Phase 1 operational setup for Weevil telemetry.

## Runtime switches

Telemetry configuration is read from `HKEY_CURRENT_USER\Software\BlueDotBrigade\Weevil`.

| Registry value | Type | Meaning |
| --- | --- | --- |
| `TelemetryConnectionString` | string | Azure SQL connection string used by the telemetry adapter. Empty or missing disables upload. This value should contain server and database settings only. |
| `TelemetrySource` | string | Non-PII installer/distribution source label included in each telemetry session row. Missing or empty values default to `unknown`. |

Optional process/user environment variables:

| Environment variable | Meaning |
| --- | --- |
| `WEEVIL_TELEMETRY_USERNAME` | Optional SQL username (or API token) applied at runtime. |
| `WEEVIL_TELEMETRY_SECRET` | Optional SQL secret applied at runtime. |

## Installer behavior

- The installer persists telemetry source metadata in the user registry.

## Application behavior

- GUI and CLI configure telemetry at startup using connection string and environment credential settings.
- Telemetry is treated as disabled when both `WEEVIL_TELEMETRY_USERNAME` and `WEEVIL_TELEMETRY_SECRET` are blank.
- If telemetry is disabled, Weevil uses `NullTelemetryClient` and performs no upload work.
- If `TelemetryConnectionString` is empty, the SQL client becomes a no-op and logs a warning.
- On non-Windows platforms, the registry-backed configuration falls back to an empty connection string and source `unknown`.

## Connection string requirements

Provide an Azure SQL connection string in `TelemetryConnectionString`.
If credentials must be provided at runtime (development or CI), set `WEEVIL_TELEMETRY_USERNAME` and `WEEVIL_TELEMETRY_SECRET`.

Operational guidance:

- Use a dedicated low-privilege SQL login.
- Grant only `INSERT` permission on `telemetry.Session`.
- Do not grant read, update, delete, or schema-change permissions.
- Treat credential values as operational secrets and provision them outside source control.
- Prefer storing credentials in environment variables rather than persisting them as part of the connection string.

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
4. Set runtime credentials (`WEEVIL_TELEMETRY_USERNAME` and `WEEVIL_TELEMETRY_SECRET`) in the execution environment.
5. Open and close a log file in Weevil to produce one ended session.

## Validation checklist

Use this checklist after configuration changes.

1. Confirm `TelemetryConnectionString` is present for environments that should upload telemetry.
2. Confirm environment credentials are present when SQL authentication is required.
3. Verify that opening a file starts a session and opening a different file ends the previous session.
4. Verify that closing the application ends the current session once.
5. Verify that a credentials-blank environment produces no upload attempts.
6. Verify that an empty or invalid connection string does not crash the application.
7. Verify that the database receives one row per ended session.
8. Verify that the inserted row contains `Application`, `Source`, `Version`, `IsDebugging`, `SessionStartUtc`, `SessionEndUtc`, `SessionActiveMinutes`, `LogFileSizeBytes`, `InstalledRamMb`, `FilterExecutionCount`, `GraphOpenCount`, `DashboardOpenCount`, and `SchemaVersion`.

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
