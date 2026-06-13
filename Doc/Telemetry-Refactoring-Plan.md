# Telemetry Refactoring Plan

## Background

The existing telemetry implementation has not been deployed to production, so backwards compatibility is not required. The telemetry schema and code can be simplified without migration support for older telemetry payloads.

The current implementation stores one row per session and uses fixed columns for individual usage counters. This makes telemetry difficult to extend because each new metric requires a database schema change, entity change, DTO change, mapper change, and upload change.

The new design will keep stable session-level data in a session table and move extensible usage counters into a child table.

## Goals

* Keep telemetry extensible and low maintenance.
* Avoid adding a new database column for every new metric.
* Keep metric names consistent across GUI, CLI, Core, and future producers.
* Keep SQL/database/upload logic inside the telemetry library.
* Allow Core, GUI, and CLI to all add metrics to the current telemetry session.
* Ensure database wake-up and outbox upload execute sequentially.
* Do NOT use AOP libraries like Metalama or PostSharp to weave telemetry calls into the codebase.

## Phase 1: Database Schema and Core Telemetry Model

### Database Changes

Rename the existing session table:

```sql
dbo.Session
```

to:

```sql
dbo.telemetry_session
```

Create a new child table for extensible per-session metrics:

```sql
dbo.telemetry_session_metric
```

### Proposed Tables

```sql
CREATE TABLE dbo.telemetry_session
(
    session_id uniqueidentifier NOT NULL PRIMARY KEY,

    application nvarchar(256) NOT NULL,
    source nvarchar(256) NOT NULL,
    version nvarchar(32) NOT NULL,

    is_debugging bit NOT NULL,

    session_start_utc datetime2 NOT NULL,
    session_end_utc datetime2 NOT NULL,

    session_active_minutes float NOT NULL,

    log_file_size_bytes bigint NOT NULL,
    installed_ram_mb bigint NOT NULL,
    installed_cpu nvarchar(256) NOT NULL,

    schema_version nvarchar(16) NOT NULL
);
```

```sql
CREATE TABLE dbo.telemetry_session_metric
(
    telemetry_session_metric_id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,

    session_id uniqueidentifier NOT NULL,

    metric_key nvarchar(128) NOT NULL,
    metric_count int NOT NULL,

    CONSTRAINT fk_telemetry_session_metric_telemetry_session
        FOREIGN KEY (session_id)
        REFERENCES dbo.telemetry_session(session_id),

    CONSTRAINT uq_telemetry_session_metric_session_metric
        UNIQUE (session_id, metric_key)
);
```

```sql
CREATE INDEX ix_telemetry_session_metric_metric_key
ON dbo.telemetry_session_metric(metric_key);
```

```sql
CREATE INDEX ix_telemetry_session_session_start_utc
ON dbo.telemetry_session(session_start_utc);
```

### Code Changes

Update `TelemetryDbContext`:

* Map `TelemetrySession` to `dbo.telemetry_session`.
* Add a new `TelemetrySessionMetric` entity.
* Add a `DbSet<TelemetrySessionMetric>`.
* Configure the one-to-many relationship between session and metrics.
* Remove the fixed counter-column mapping once the new metric model is in place.

Update telemetry session model:

* Remove fixed usage count fields such as:

  * `FilterExecutionCount`
  * `GraphOpenCount`
  * `DashboardOpenCount`
  * `HelpOpenCount`
* Add a collection of session metrics.
* Add an `Increment(metricKey)` method to update the in-memory metric count.

Example model shape:

```csharp
public sealed class TelemetrySession
{
    public Guid SessionId { get; set; }

    public string Application { get; set; } = string.Empty;

    public string Source { get; set; } = "Development";

    public Version Version { get; set; } = new(0, 0);

    public bool IsDebugging { get; set; }

    public DateTime SessionStartUtc { get; set; }

    public DateTime SessionEndUtc { get; set; }

    public double SessionActiveMinutes { get; set; }

    public long LogFileSizeBytes { get; set; }

    public long InstalledRamMb { get; set; }

    public string InstalledCpu { get; set; } = "Unknown";

    public string SchemaVersion { get; set; } = "3.0";

    public IList<TelemetrySessionMetric> Metrics { get; } =
        new List<TelemetrySessionMetric>();

    public void Increment(string metricKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metricKey);

        TelemetrySessionMetric? metric =
            Metrics.SingleOrDefault(x => x.MetricKey == metricKey);

        if (metric is null)
        {
            Metrics.Add(new TelemetrySessionMetric
            {
                SessionId = SessionId,
                MetricKey = metricKey,
                MetricCount = 1
            });
        }
        else
        {
            metric.MetricCount++;
        }
    }
}
```

```csharp
public sealed class TelemetrySessionMetric
{
    public long TelemetrySessionMetricId { get; set; }

    public Guid SessionId { get; set; }

    public string MetricKey { get; set; } = string.Empty;

    public int MetricCount { get; set; }

    public TelemetrySession? Session { get; set; }
}
```

Update the XML DTO and mapper:

* Replace fixed count properties with a collection of metric DTOs.
* Update `TelemetrySessionMapper` to map metrics in both directions.
* Set `SchemaVersion` to `3.0`.

Example metric DTO:

```csharp
public sealed class TelemetrySessionMetricDto
{
    public string MetricKey { get; set; } = string.Empty;

    public int MetricCount { get; set; }
}
```

### Shared Metric Recorder

Introduce a shared metric recorder abstraction that can be used by Core, GUI, CLI, and future producers.

```csharp
public interface ITelemetryMetricRecorder
{
    void Increment(string metricKey);
}
```

Add shared metric keys:

```csharp
public static class TelemetryMetrics
{
    public const string LogOpened = "Log.Opened";

    public const string FilterApplied = "Filter.Applied";

    public const string NavigationGoToLine = "Navigation.GoToLine";
    public const string NavigationGoToTimestamp = "Navigation.GoToTimestamp";
    public const string NavigationFindNextContent = "Navigation.FindNextContent";
    public const string NavigationFindPreviousContent = "Navigation.FindPreviousContent";

    public const string AnalysisRun = "Analysis.Run";
    public const string InsightOpened = "Insight.Opened";

    public const string HelpOpened = "Help.Opened";
    public const string DashboardOpened = "Dashboard.Opened";
    public const string GraphOpened = "Graph.Opened";

    public const string CliFilterCommand = "Cli.Command.Filter";
    public const string CliInsightCommand = "Cli.Command.Insight";
}
```

`TelemetrySessionLifecycle` should implement `ITelemetryMetricRecorder` and update the current session’s metric collection.

### Phase 1 Acceptance Criteria

* The database uses `dbo.telemetry_session` instead of `dbo.Session`.
* The database includes `dbo.telemetry_session_metric`.
* Session-level metadata remains in `telemetry_session`.
* Usage counters are stored in `telemetry_session_metric`.
* Adding a new metric does not require a database schema change.
* XML telemetry files can store multiple metric key/count pairs.
* Upload saves one session row and zero or more metric rows.
* Existing fixed telemetry count columns are removed from the new model.

## Phase 2: Sequential Database Wake-Up and Outbox Upload

### Problem

The current implementation wakes up the database and triggers outbox upload independently. These operations should be executed consecutively.

The intended sequence is:

```text
Application starts or log file opens
    Attempt to wake up database
    If wake-up succeeds
        Upload pending telemetry session files
    If wake-up fails
        Keep pending telemetry files on disk
        Try again later
```

### Required Behavior

The telemetry library should provide one coordinated operation, for example:

```csharp
WakeupThenUploadOutbox()
```

This operation should:

1. Attempt to open a database connection
	- A successful connection could take up to 1 minute if the MS SQL database instance is asleep.
2. Treat a successful connection as confirmation that the database is awake.
	- As a sanity check, run a simple query: `SELECT SYSUTCDATETIME() AS UtcNow;`
3. Only after successful wake-up, upload pending telemetry XML files.  Delete each file after successful upload.
4. Leave pending files on disk if wake-up fails.
5. Log failures as telemetry diagnostics, not user-facing errors.

### Implementation Notes

- Do NOT block or break the user’s workflow - UI should always remain responsive (including when the user attempts to close the application.)
- Do NOT add TPL (async/await) code to the `BlueDotBrigade.Weevil.Core` project.
- The current fire-and-forget warm-up method should be changed or wrapped so that upload depends on successful wake-up.
- Only one attempt should be made to wake up the database, then upload the outbox if wake-up succeeds.

### Phase 2 Acceptance Criteria

* Database warm-up and outbox upload are no longer independent parallel operations.
* Outbox upload only runs after a successful database wake-up.
* Failed wake-up does not delete local telemetry files.
* Failed wake-up does not show an error to the user.
* Pending telemetry files are uploaded successfully after a later successful wake-up.
* The application remains responsive during warm-up and upload.
* The application can be closed at any time without:
	- Blocking on warm-up or upload.
	- Leaving the application in a bad state.
	- Leaving telemetry files on disk that have already been uploaded to the database. (avoid duplicate uploads)

## Phase 3: Collect Shared Usage Metrics from Core

### Goal

Usage metrics should be collected as close as possible to the operation being performed. This keeps GUI and CLI code DRY and ensures reporting uses consistent metric names.

Core should emit semantic metrics for operations that are shared by GUI and CLI.

Examples:

```text
Filter.Applied
Analysis.Run
Navigation.GoToLine
Navigation.GoToTimestamp
Navigation.FindNextContent
Navigation.FindPreviousContent
```

GUI and CLI should only record host-specific metrics.

Examples:

```text
Help.Opened
Dashboard.Opened
Graph.Opened
Cli.Command.Filter
Cli.Command.Insight
```

### Ownership Rules

`BlueDotBrigade.Weevil.Core` records metrics for Core operations.

`BlueDotBrigade.Weevil.Gui` records metrics for GUI-only behavior.

`BlueDotBrigade.Weevil.Cli` records metrics for CLI-only behavior.

Telemetry records, stores, and uploads metrics.

Core should not know about SQL, XML outbox storage, Azure SQL, or upload behavior.

### Suggested Shared Context

Add a shared telemetry context that defaults to a no-op recorder:

```csharp
public static class TelemetryContext
{
    public static ITelemetryMetricRecorder Recorder { get; set; } =
        NullTelemetryMetricRecorder.Instance;

    public static void Increment(string metricKey)
    {
        Recorder.Increment(metricKey);
    }
}
```

At application startup, GUI and CLI can configure:

```csharp
TelemetryContext.Recorder = TelemetrySessionLifecycle.Shared;
```

Core can then record metrics without referencing the telemetry project.

Example:

```csharp
TelemetryContext.Increment(TelemetryMetrics.FilterApplied);
```

### Suppression Support

Some Core operations are internal setup operations and should not be counted as user activity. For example, an initial default filter may be applied during engine construction.

Add suppression support:

```csharp
using (TelemetryContext.Suppress())
{
    // Internal setup work that should not count as user activity.
}
```

### Phase 3 Acceptance Criteria

* Core records shared semantic metrics.
* GUI and CLI no longer need duplicate metric calls for Core operations.
* GUI and CLI can still record host-specific metrics.
* Metric names are consistent across GUI and CLI.
* Core does not reference the telemetry project.
* Telemetry storage and upload remain in the telemetry library.
* Internal setup operations are not counted as user activity.

## Reporting Examples

### Count Total Help Opens

```sql
SELECT
    SUM(m.metric_count) AS help_open_count
FROM dbo.telemetry_session_metric m
WHERE m.metric_key = 'Help.Opened';
```

### Count Sessions Where Help Was Opened

```sql
SELECT
    COUNT(*) AS session_count
FROM dbo.telemetry_session_metric m
WHERE
    m.metric_key = 'Help.Opened'
    AND m.metric_count > 0;
```

### Select Sessions Where Help Was Opened

```sql
SELECT
    s.session_id,
    s.application,
    s.source,
    s.version,
    s.is_debugging,
    s.session_start_utc,
    s.session_end_utc,
    s.session_active_minutes,
    s.log_file_size_bytes,
    s.installed_ram_mb,
    s.installed_cpu,
    s.schema_version,
    m.metric_count AS help_open_count
FROM dbo.telemetry_session s
JOIN dbo.telemetry_session_metric m
    ON m.session_id = s.session_id
WHERE
    m.metric_key = 'Help.Opened'
    AND m.metric_count > 0
ORDER BY
    s.session_start_utc DESC;
```

### Count Help Opens by Application

```sql
SELECT
    s.application,
    SUM(m.metric_count) AS help_open_count
FROM dbo.telemetry_session s
JOIN dbo.telemetry_session_metric m
    ON m.session_id = s.session_id
WHERE
    m.metric_key = 'Help.Opened'
GROUP BY
    s.application
ORDER BY
    help_open_count DESC;
```

## Final Target Architecture

```text
Core
    Emits shared semantic usage metrics.

GUI
    Starts and ends telemetry sessions.
    Emits GUI-only metrics (e.g. Help.Opened, Dashboard.Opened, Graph.Opened).

CLI
    Starts and ends telemetry sessions.
    Emits CLI-only metrics.

Telemetry
    Owns session lifecycle.
    Owns XML outbox.
    Owns database wake-up.
    Owns upload.
    Owns EF/SQL schema.
    Stores extensible session metrics.

Database
    telemetry_session
    telemetry_session_metric
```

This design keeps the telemetry system flexible, minimizes schema churn, prevents GUI/CLI metric drift, and keeps the database/upload implementation isolated inside the telemetry library.
