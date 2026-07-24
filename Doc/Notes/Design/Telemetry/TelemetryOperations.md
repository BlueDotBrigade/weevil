# Weevil Telemetry Operations

This document describes the current Phase 1 operational setup for Weevil telemetry.

## Runtime switches

Telemetry connection settings are embedded in application source code.

Optional process/user environment variables:

| Environment variable | Meaning |
| --- | --- |
| `WEEVIL_TELEMETRY_ENABLED` | Enables telemetry only when set to `1` or `true` (case-insensitive for `true`). Any other value disables telemetry. |
| `WEEVIL_TELEMETRY_USERNAME` | Optional SQL username (or API token) applied at runtime. |
| `WEEVIL_TELEMETRY_SECRET` | Optional SQL secret applied at runtime. Supports encrypted values produced by `WeevilCli.exe protect-secret`. |

## Application behavior

- GUI and CLI configure telemetry at startup using the `WEEVIL_TELEMETRY_ENABLED` consent switch plus optional environment credentials.
- Telemetry is treated as disabled unless `WEEVIL_TELEMETRY_ENABLED` is set to `1` or `true` (case-insensitive for `true`).
- If telemetry is disabled, Weevil uses `NullTelemetryClient` and performs no upload work.
- The telemetry session source is fixed to `unknown`.

## Connection string requirements

The Azure SQL connection string is embedded in source code.
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
- `Connect Timeout=60`

Current provider defaults:

- Connection timeout: 60 seconds (allows Azure SQL serverless to resume from a paused state)
- Async command timeout: 15 seconds
- Sync best-effort command timeout: 15 seconds

## Recommended setup flow

1. Deploy the `telemetry.Session` table using the schema documented in `TelemetrySchemaAndPrivacy.md`.
2. Create a SQL login that can insert into `telemetry.Session` only.
3. Set `WEEVIL_TELEMETRY_ENABLED` in the execution environment to `1` or `true`.
4. Set runtime credentials (`WEEVIL_TELEMETRY_USERNAME` and `WEEVIL_TELEMETRY_SECRET`) in the execution environment when SQL authentication is required.
   - To avoid storing the secret in plaintext, encrypt it first: `WeevilCli.exe protect-secret --secret "YourPassword"`
   - Copy the resulting `ENC:…` value into the `WEEVIL_TELEMETRY_SECRET` environment variable.
   - Weevil decrypts the value automatically at startup using the application’s AES-256-GCM secret protection scheme.
5. Open and close a log file in Weevil to produce one ended session.

## Validation checklist

Use this checklist after configuration changes.

1. Confirm `WEEVIL_TELEMETRY_ENABLED` is set to `1` or `true` when telemetry consent has been granted.
2. Confirm environment credentials are present when SQL authentication is required.
3. Verify that opening a file starts a session and opening a different file ends the previous session.
4. Verify that closing the application ends the current session once.
5. Verify that a disabled telemetry environment produces no upload attempts and no telemetry XML output.
6. Verify that an invalid connection string does not crash the application.
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
