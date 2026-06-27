# Weevil Telemetry Phase 2 Plan

## Background

Weevil telemetry is session-based. One opened log file should represent one telemetry session:

- Opening a log file starts a telemetry session.
- Opening another log file ends the previous session and starts a new one.
- Closing Weevil ends the current session.

The most important Phase 2 metric is `SessionActiveMinutes`, which should be a reasonable estimate of how long an analyst actively used Weevil during a session. The estimate does not need to prove every second of reading time. It should be simple, explainable, testable, and realistic for log-analysis workflows.

A previous approach saved session telemetry directly to MS SQL. That creates user-experience risk because Azure SQL can be slow, idle, offline, unreachable, or misconfigured. Phase 2 must remove SQL from the UI close path and use local persistence so session capture remains fast and reliable while upload becomes background, best-effort, retryable work.

The recommended Phase 2 pattern is a local XML telemetry outbox:

```text
TelemetrySession DTO
  -> save to local XML file
  -> background worker uploads pending XML sessions to MS SQL
  -> delete XML only after successful SQL save
```

XML is preferred because pending session files can be opened directly in a browser for inspection and troubleshooting.

## Goals

### Goal 1: Reasonably estimate active Weevil usage

Use a simple 15-minute activity lease model that supports:

- GUI usage today.
- Run-once CLI usage today.
- Future interactive CLI usage.

The model should estimate product usage well enough for telemetry without attempting perfect activity auditing.

### Goal 2: Save session metrics to MS SQL without hurting UX

Session metrics should eventually be saved to MS SQL, but SQL must never be on the close path.

- Closing Weevil should remain fast.
- Opening a new log should remain responsive.
- SQL upload should run in the background.
- Failed upload should preserve local XML for later retry.

## Key Architectural Principles

Use this ownership model:

```text
Telemetry library
  Owns most implementation.

Core library
  Reports domain-level Weevil activity to Telemetry.

GUI / CLI projects
  Add only thin adapters if Core cannot observe the interaction.
```

Important constraints:

- Most code belongs in the Telemetry library.
- Telemetry should expose only the small API surface needed by Core, GUI, and CLI.
- Prefer high cohesion and low coupling.
- Hook Telemetry into Core first.
- Add CLI or GUI hooks only when Core cannot observe the activity.
- Do not integrate with WPF commands.
- The solution must be testable without MS SQL.
- Tests should cover realistic active-usage scenarios.

The Telemetry library should not know about WPF, `ScrollViewer`, `DataGrid`, window focus, visible rows, or CLI prompt mechanics. GUI-specific observations should be translated into generic telemetry signals.

## Terminology

Use `RenewActivityLease()` to describe activity signals that say “the user is still here.” Product code should not expose watchdog-inspired names such as `KickWatchdog()`, `PetWatchdog()`, or `FeedWatchdog()`.

`RecordActivity(...)` may be the public API used by callers, while `RenewActivityLease()` can be used internally or in tests to describe the active-usage behavior.

## Active Usage Design

Use a 15-minute activity lease:

```csharp
private static readonly TimeSpan ActivityLeaseDuration = TimeSpan.FromMinutes(15);
```

`SessionActiveMinutes` should be calculated as the sum of capped intervals between observed activity signals:

```text
counted active time = min(elapsed time since last activity, activity lease duration)
```

Why 15 minutes:

- One minute is too short for log analysis.
- Analysts may spend 10 to 15 minutes scrolling, reading, comparing records, and thinking before another command.
- Fifteen minutes bounds lunch and overnight overcounting while preserving realistic reading time.

Session start initializes the active usage accumulator. Session end renews once using the end time, rounds `SessionActiveMinutes`, and returns a DTO.

## Proposed Telemetry API

The final API can be adjusted during implementation, but should stay small:

```csharp
public interface ITelemetrySessionTracker
{
    void StartSession(TelemetrySessionStartRequest request);

    void RecordActivity(TelemetryActivityKind activityKind);

    void RecordFilterExecution();

    void RecordGraphOpen();

    void RecordDashboardOpen();

    TelemetrySessionDto EndSession();
}
```

`RecordFilterExecution()`, `RecordGraphOpen()`, and `RecordDashboardOpen()` may wrap `RecordActivity(...)` when they also increment counters.

Example activity kinds:

```csharp
public enum TelemetryActivityKind
{
    FileOpened,
    FilterApplied,
    AnalysisExecuted,
    RecordsCleared,
    RecordSelectionChanged,
    RecordAnnotationChanged,
    ViewportChanged,
    CommandEntered,
    CommandCompleted,
    Unknown
}
```

Collapse older concepts such as `RecordSessionHeartbeat()`, `RecordNavigationAction()`, and `RecordRecordAction()` into `RecordActivity(...)` unless separate metrics are still required.

Core should depend on a narrow activity abstraction:

```csharp
public interface ITelemetryActivitySink
{
    void RecordActivity(TelemetryActivityKind activityKind);
}
```

## Implementation Design

### Telemetry library

Prefer this layout for most Phase 2 implementation:

```text
TelemetrySessionLifecycle
TelemetryActiveUsageAccumulator
TelemetrySessionDto
TelemetrySessionXmlStore
TelemetryUploadWorker
MsSqlTelemetryClient
ITelemetrySessionTracker
ITelemetryActivitySink
```

`TelemetryActiveUsageAccumulator` should be a small, highly testable class that:

- Stores the configured lease duration.
- Tracks the last activity timestamp.
- Adds positive elapsed time capped to the lease duration.
- Ignores zero or negative elapsed time for active-minute accumulation.
- Can be reset between sessions.

### Local XML outbox

Use local XML files as a pending telemetry outbox.

Recommended location:

```text
%ProgramData%\BlueDotBrigade\Weevil\Temp\Telemetry\Pending
```

Recommended filename:

```text
{SessionId}.xml
```

Use an XML-friendly DTO and avoid runtime-only serializer friction such as `System.Version` properties.

```csharp
public sealed class TelemetrySessionDto
{
    public Guid SessionId { get; set; }

    public string Application { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Version { get; set; } = "0.0";

    public bool IsDebugging { get; set; }

    public DateTime SessionStartUtc { get; set; }

    public DateTime SessionEndUtc { get; set; }

    public double SessionActiveMinutes { get; set; }

    public long LogFileSizeBytes { get; set; }

    public long InstalledRamMb { get; set; }

    public string InstalledCpu { get; set; } = string.Empty;

    public int FilterExecutionCount { get; set; }

    public int GraphOpenCount { get; set; }

    public int DashboardOpenCount { get; set; }

    public string SchemaVersion { get; set; } = "2.0";
}
```

Implement a simple file store:

```csharp
public interface ITelemetrySessionStore
{
    void Save(TelemetrySessionDto session);

    IReadOnlyList<PendingTelemetrySession> GetPendingSessions(int maxCount);

    void Delete(PendingTelemetrySession session);
}

public sealed class PendingTelemetrySession
{
    public string FilePath { get; init; } = string.Empty;

    public TelemetrySessionDto Session { get; init; } = new();
}
```

Use atomic-ish writes:

1. Ensure the pending directory exists.
2. Write `{SessionId}.xml.tmp`.
3. Close the file.
4. Move or rename to `{SessionId}.xml`.

No archive folder is required for the first implementation.

### Background saving and upload

Telemetry work must not harm UI responsiveness.

Close behavior should be:

1. Stop GUI viewport/activity timer first.
2. End the telemetry session.
3. Queue or perform local XML persistence safely.
4. Do not upload to SQL during close.
5. Exit quickly.

The local XML write is small, but implementation should still avoid UI responsiveness problems. If a background writer is used, close-time behavior must not lose the ended session. A bounded local-only flush is acceptable if necessary; SQL is not.

Create a background upload abstraction:

```csharp
public interface ITelemetryUploadWorker
{
    void TriggerUpload();
}
```

`TriggerUpload()` should start background upload if one is not already running, return immediately, and never block startup, file-open, filtering, or close.

Upload flow:

1. Read pending XML files.
2. Upload each session to MS SQL.
3. If upload succeeds, delete the XML file.
4. If a duplicate `SessionId` already exists, treat that as success and delete the XML file.
5. If upload fails due to a transient error, retry according to policy.
6. If upload still fails, leave the XML file in place.

Trigger background upload only at safe times:

- Application startup.
- When a new log file is opened.
- Optionally after a session XML file is saved, except during close.

Do not upload on close.

## Retry and Credential Behavior

Retry policy:

- Make at most 3 attempts to save a session to MS SQL.
- Wait 1 minute between attempts.
- Do not block UI while waiting.

Credential behavior:

- If no telemetry username and no telemetry password are configured, SQL upload is disabled, but session metrics are still saved to XML.
- If the initial SQL connect fails due to invalid credentials, disable SQL upload for the current process, leave XML files pending, and log the failure.
- If SQL or the network is transiently unavailable, retry up to 3 attempts with 1 minute between attempts.

## GUI Activity Monitoring

Do not integrate with WPF commands.

Start with Core-level activity reporting. Only add GUI-specific monitoring if Core activity does not provide a sufficient active-usage estimate.

If GUI monitoring is added, use a simple viewport snapshot:

```csharp
internal readonly record struct ViewportSnapshot(
    double VerticalOffset,
    double HorizontalOffset,
    int DisplayedRecordCount);
```

Do not capture first visible record, last visible record, realized row objects, or `DataGrid` items.

GUI polling rule:

```text
Window active:
  run one-minute viewport polling timer

Window inactive:
  stop viewport polling

Closing:
  stop viewport polling first
```

On each timer tick:

1. Capture the current viewport snapshot.
2. Compare it to the previous snapshot.
3. If the snapshot changed, call `Telemetry.RecordActivity(TelemetryActivityKind.ViewportChanged)`.

Use offset tolerance:

```csharp
private static bool HasChanged(ViewportSnapshot previous, ViewportSnapshot current)
{
    return
        Math.Abs(previous.VerticalOffset - current.VerticalOffset) > 0.01 ||
        Math.Abs(previous.HorizontalOffset - current.HorizontalOffset) > 0.01 ||
        previous.DisplayedRecordCount != current.DisplayedRecordCount;
}
```

## CLI Behavior

Current CLI is run-once:

- Start the session when the command starts.
- Record command activity.
- End the session when the command ends.
- Save XML locally.
- Trigger upload on startup or command start, but never block command completion.

Future interactive CLI should use the same activity lease model:

- Start one interactive session.
- Renew the activity lease when the user enters a command, command starts, command completes, or the prompt receives meaningful input.
- End the session when the interactive shell exits.

## Scenarios

### Active usage scenarios

```gherkin
Scenario: File open was counted as active usage
  Given a telemetry session was started at 10:00 AM
  When the session ends at 10:05 AM
  Then five active minutes will be recorded
```

```gherkin
Scenario: Long reading time was capped
  Given a telemetry session was started at 10:00 AM
  When the session ends at 10:30 AM
  Then fifteen active minutes will be recorded
```

```gherkin
Scenario: Repeated activity extended active usage
  Given a telemetry session was started at 10:00 AM
  And filter activity was recorded at 10:10 AM
  When the session ends at 10:20 AM
  Then twenty active minutes will be recorded
```

```gherkin
Scenario: User spent time scrolling before filtering
  Given a telemetry session was started at 10:00 AM
  And viewport activity was recorded at 10:05 AM
  And viewport activity was recorded at 10:12 AM
  When filter activity is recorded at 10:20 AM
  Then twenty active minutes will be recorded
```

```gherkin
Scenario: Lunch time was capped
  Given a telemetry session was started at 10:00 AM
  And viewport activity was recorded at 10:02 AM
  When viewport activity is recorded at 1:00 PM
  Then the lunch gap will contribute no more than fifteen active minutes
```

```gherkin
Scenario: Overnight usage was capped
  Given a telemetry session was started on Thursday at 4:55 PM
  And filter activity was recorded on Thursday at 5:05 PM
  When navigation activity is recorded on Friday at 9:00 AM
  Then overnight hours will not be included in active minutes
  And the overnight gap will contribute no more than fifteen active minutes
```

```gherkin
Scenario: Inactive window allowed the activity lease to expire
  Given viewport activity was recorded at 10:00 AM
  And the Weevil window was inactive at 10:01 AM
  When no further activity is recorded before 10:30 AM
  Then no more than fifteen active minutes will be recorded after the last activity
```

```gherkin
Scenario: Second monitor reading was partially counted
  Given viewport activity was recorded at 10:00 AM
  And the Weevil window was inactive at 10:01 AM
  When the user reads Weevil on a second monitor
  Then the existing activity lease will continue for at most fifteen minutes
```

### Core-first scenarios

```gherkin
Scenario: Filter operation renewed activity
  Given a telemetry session was active
  When Core applies a filter
  Then the activity lease will be renewed
  And the filter execution count will increase
```

```gherkin
Scenario: Filter execution was tracked when result count did not change
  Given a telemetry session was active
  And the displayed record count was unchanged
  When Core applies a filter
  Then the filter execution count will increase
  And the activity lease will be renewed
```

```gherkin
Scenario: Analysis operation renewed activity
  Given a telemetry session was active
  When Core executes analysis
  Then the activity lease will be renewed
```

```gherkin
Scenario: Clear operation renewed activity
  Given a telemetry session was active
  When Core clears records
  Then the activity lease will be renewed
```

### GUI adapter scenarios

```gherkin
Scenario: Viewport vertical offset change renewed activity
  Given a viewport snapshot was captured
  And the vertical offset changed
  When the viewport monitor checks activity
  Then viewport activity will be recorded
```

```gherkin
Scenario: Viewport horizontal offset change renewed activity
  Given a viewport snapshot was captured
  And the horizontal offset changed
  When the viewport monitor checks activity
  Then viewport activity will be recorded
```

```gherkin
Scenario: Displayed record count change renewed activity
  Given a viewport snapshot was captured
  And the displayed record count changed
  When the viewport monitor checks activity
  Then viewport activity will be recorded
```

```gherkin
Scenario: Unchanged viewport did not renew activity
  Given a viewport snapshot was captured
  And the viewport snapshot was unchanged
  When the viewport monitor checks activity
  Then viewport activity will not be recorded
```

```gherkin
Scenario: Closing stopped viewport polling first
  Given viewport polling was running
  When Weevil closes
  Then viewport polling will stop before the telemetry session ends
```

### Session rollover scenarios

```gherkin
Scenario: Opening a new log ended the previous session
  Given a telemetry session was active for LogA
  When LogB is opened
  Then the LogA session will be ended
  And the LogA session will be saved to XML
  And a new session will be started for LogB
```

```gherkin
Scenario: Opening a new log triggered background upload
  Given a pending telemetry XML file existed
  When a new log is opened
  Then telemetry upload will be triggered in the background
  And the file-open workflow will not wait for SQL
```

### Close scenarios

```gherkin
Scenario: Closing Weevil saved telemetry locally
  Given a telemetry session was active
  When Weevil closes
  Then the viewport timer will be stopped first
  And the ended session will be saved to XML
  And SQL upload will not be awaited
```

```gherkin
Scenario: Closing Weevil did not wait for offline SQL
  Given a telemetry session was active
  And the SQL database was unavailable
  When Weevil closes
  Then the ended session will be saved to XML
  And Weevil will not wait for SQL upload
```

### XML persistence scenarios

```gherkin
Scenario: Ended session was saved to XML
  Given a telemetry session was active
  When the session ends
  Then a telemetry XML file will be saved
```

```gherkin
Scenario: XML save was testable without SQL
  Given a telemetry session DTO was created
  When the DTO is saved to XML
  Then the DTO will be loaded from XML without accessing SQL
```

```gherkin
Scenario: Failed XML save was logged
  Given a telemetry session DTO was created
  And the XML store was unavailable
  When the DTO is saved to XML
  Then the failure will be logged
  And the user workflow will not crash
```

### Upload scenarios

```gherkin
Scenario: Successful SQL upload deleted pending XML
  Given a pending telemetry XML file existed
  And SQL upload succeeded
  When pending telemetry is uploaded
  Then the pending XML file will be deleted
```

```gherkin
Scenario: Failed SQL upload preserved pending XML
  Given a pending telemetry XML file existed
  And SQL upload failed
  When pending telemetry is uploaded
  Then the pending XML file will remain
```

```gherkin
Scenario: Duplicate SQL session was treated as uploaded
  Given a pending telemetry XML file existed
  And SQL reported that the SessionId already existed
  When pending telemetry is uploaded
  Then the pending XML file will be deleted
```

```gherkin
Scenario: Telemetry credentials were missing
  Given no telemetry username was configured
  And no telemetry password was configured
  When a telemetry session ends
  Then the session metrics will be saved to XML
  And SQL upload will not be attempted
```

```gherkin
Scenario: Invalid credentials stopped upload retries
  Given pending telemetry XML files existed
  And SQL rejected the credentials during the initial connection
  When pending telemetry is uploaded
  Then no further SQL upload attempts will be made during this process
  And the pending XML files will remain
```

```gherkin
Scenario: Transient SQL failure was retried
  Given a pending telemetry XML file existed
  And SQL was temporarily unavailable
  When pending telemetry is uploaded
  Then SQL upload will be attempted at most three times
  And one minute will separate each attempt
```

```gherkin
Scenario: Upload retries did not block the UI
  Given a pending telemetry XML file existed
  And SQL was temporarily unavailable
  When pending telemetry upload is retrying
  Then the UI will remain responsive
```

### CLI scenarios

```gherkin
Scenario: Run-once CLI command recorded active duration
  Given a CLI telemetry session was started
  When the CLI command completes
  Then command execution time will be recorded as active usage
  And the session will be saved to XML
```

```gherkin
Scenario: Future interactive CLI command renewed activity
  Given an interactive CLI telemetry session was active
  When the user enters a command
  Then the activity lease will be renewed
```

## Test Strategy

Automated tests should avoid requiring MS SQL except for isolated SQL client tests.

Recommended test groups:

- Unit tests for `TelemetryActiveUsageAccumulator` covering file open, long reading, repeated activity, scrolling before filtering, lunch, overnight, inactive window, and second-monitor reading scenarios.
- Unit tests for telemetry session lifecycle rollover and end-session DTO finalization.
- Unit tests for Core activity reporting that verify filters, analysis, clear operations, and relevant annotation/bookmark actions renew the activity lease.
- Unit tests for optional GUI viewport monitoring that verify vertical offset, horizontal offset, and `DisplayedRecordCount` changes record `ViewportChanged` activity, unchanged snapshots do not, and close stops polling before session end.
- Unit tests for XML save, load, atomic-ish rename behavior, delete-after-success behavior, and failure logging.
- Unit tests for upload worker concurrency so `TriggerUpload()` starts at most one active upload and returns immediately.
- Unit tests for retry policy, missing credentials, invalid credentials, duplicate `SessionId`, transient failures, and preserving XML on failure.
- Integration tests for `MsSqlTelemetryClient` can be isolated and skipped unless configured credentials and database access are available.

## GitHub Issue Breakdown

### Issue 1: Telemetry Phase 2: Estimate active usage with activity lease

Reference this plan: `Doc/Telemetry-Phase2-Plan.md`.

Scope:

- Implement the 15-minute activity lease.
- Add `TelemetryActiveUsageAccumulator`.
- Wire Core-first activity reporting.
- Add optional GUI viewport snapshot polling only if necessary.
- Avoid WPF command hooks.
- Account for run-once CLI and future interactive CLI scenarios.
- Add unit tests for realistic active-usage scenarios.

Acceptance criteria:

- `SessionActiveMinutes` uses capped activity intervals.
- Long gaps are capped to 15 minutes.
- Core domain activity renews the activity lease.
- Tests cover reading, scrolling, lunch, overnight, inactive window, and repeated activity scenarios.
- GUI viewport polling, if added, uses only `VerticalOffset`, `HorizontalOffset`, and `DisplayedRecordCount`.
- Viewport timer stops before session end during close.

### Issue 2: Telemetry Phase 2: Save session metrics to XML and upload to MS SQL in background

Reference this plan: `Doc/Telemetry-Phase2-Plan.md`.

Scope:

- Implement the XML outbox.
- Add `TelemetrySessionDto`.
- Add a local pending session store.
- Add a background upload worker.
- Ensure no SQL upload occurs during close.
- Implement upload retry policy.
- Implement missing credentials and invalid credentials behavior.
- Delete XML only after successful SQL upload or duplicate `SessionId` confirmation.

Acceptance criteria:

- Ended sessions are saved to XML.
- SQL upload runs in the background.
- Closing Weevil does not wait for SQL.
- Opening a new log saves the previous session locally and triggers background upload.
- Missing username and password disables upload but still saves XML.
- Invalid credentials stop further SQL attempts for the current process.
- Transient SQL failures are retried at most 3 times with 1 minute between attempts.
- Successful SQL upload deletes the XML file.
- Duplicate `SessionId` is treated as successful upload and deletes the XML file.
- Failed upload leaves XML pending.
- Tests do not require access to MS SQL except isolated SQL client tests.
