# Weevil Telemetry Operations

This document describes the current Phase 1 operational setup for Weevil telemetry.

## Runtime switches

Telemetry configuration is read from `HKEY_CURRENT_USER\Software\BlueDotBrigade\Weevil`.

| Registry value | Type | Meaning |
| --- | --- | --- |
| `TelemetryEnabled` | integer or boolean-like value | Enables or disables telemetry. Missing or invalid values are intentionally treated as enabled so the default-enabled, opt-out installer behavior remains effective unless the user explicitly turns telemetry off. |
| `TelemetryConnectionString` | string | Azure SQL connection string used by the telemetry adapter. Empty or missing disables upload. This value should contain server and database settings only. |

Optional process/user environment variables:

| Environment variable | Meaning |
| --- | --- |
| `WEEVIL_TELEMETRY_SQL_USERNAME` | Optional SQL username applied at runtime. |
| `WEEVIL_TELEMETRY_SQL_PASSWORD_OR_API_TOKEN` | Optional SQL password/API token applied at runtime. |

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
If credentials must be provided at runtime (development or CI), set `WEEVIL_TELEMETRY_SQL_USERNAME` and `WEEVIL_TELEMETRY_SQL_PASSWORD_OR_API_TOKEN`.

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
4. Install Weevil with telemetry enabled, or set `TelemetryEnabled` to `1`.
5. Open and close a log file in Weevil to produce one ended session.

## Validation checklist

Use this checklist after configuration changes.

1. Confirm `TelemetryEnabled` is present and set to the expected value.
2. Confirm `TelemetryConnectionString` is present for environments that should upload telemetry.
3. Confirm environment credentials are present when SQL authentication is required.
4. Verify that opening a file starts a session and opening a different file ends the previous session.
5. Verify that closing the application ends the current session once.
6. Verify that a disabled install produces no upload attempts.
7. Verify that an empty or invalid connection string does not crash the application.
8. Verify that the database receives one row per ended session.
9. Verify that the inserted row contains `Application`, `Version`, `SessionStartUtc`, `SessionEndUtc`, `SessionActiveMinutes`, `LogFileSizeBytes`, `InstalledRamMb`, `FilterExecutionCount`, `GraphOpenCount`, `DashboardOpenCount`, and `SchemaVersion`.

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
