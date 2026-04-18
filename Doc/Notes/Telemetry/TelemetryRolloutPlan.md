# Weevil Telemetry Plan (Peer-Reviewed, AI-Executable)

## Why this version
This plan has been peer-reviewed against .NET telemetry best practices with one guiding question:
**What is the simplest non-brittle approach that preserves privacy and future flexibility?**

Outcome:
- Keep **session telemetry only** in Phase 1.
- Keep **one small abstraction** (not a framework).
- Keep telemetry **best-effort and non-blocking**.
- Keep vendor coupling low, but do not over-engineer.

---

## 1) Scope and constraints
### Goals
- Understand real usage patterns (session counts/duration/basic context).
- Improve product quality with minimal code and minimal operating cost.
- Preserve user trust with transparent opt-out.

### Hard constraints
- No PII.
- Installer opt-out, default enabled.
- Small data volume.
- Low maintenance and low brittleness.

### Explicit non-goals (Phase 1)
- No event firehose.
- No local disk queue/retry engine.
- No complex schema/version migration system.

---

## 2) Recommended architecture (simple + durable)
### 2.1 Minimal contract
Use a thin interface only:

```csharp
public interface ITelemetryClient
{
    Task SendAsync(TelemetrySession session, CancellationToken ct);
    void SendSync(TelemetrySession session);
}
```

Implementations:
- `NullTelemetryClient` (telemetry disabled/failsafe).
- `AppInsightsTelemetryClient` (or selected provider).

### 2.2 Phase 1 session payload (subset of Appendix A schema)
Capture only:
- `SessionId` (GUID)
- `Application` (`WeevilGui.exe` / `WeevilCli.exe`)
- `Version` (Weevil version)
- `SessionStartUtc`, `SessionEndUtc`
- `SessionActiveMinutes`
- `LogFileSizeBytes`
- `InstalledRamMb`
- `FilterExecutionCount`
- `GraphOpenCount`
- `DashboardOpenCount`
- `SchemaVersion`

### 2.3 Lifecycle rules
- Session starts when a log file opens.
- Session ends when:
  - another file opens,
  - app shuts down,
  - unhandled exception occurs.
- Exactly one upload attempt per ended session.

### 2.4 Upload behavior
- **Normal flow** (new file open): asynchronous send.
- **Shutdown/crash**: synchronous best-effort send with short timeout.
- Never block normal UX for telemetry.
- Never crash app because telemetry failed.

---

## 3) Vendor strategy (avoid brittle coupling)
### Preferred Phase 1 default
Use Azure Application Insights if it is fastest to ship.

Why this is still non-brittle:
- Provider is behind `ITelemetryClient`.
- Session/lifecycle logic remains provider-agnostic.
- Switching provider later only changes adapter + DI config.

### Stability rules
- No provider types outside adapter project/folder.
- Telemetry exceptions are swallowed + internal log only.
- Add a short network timeout and cancellation support.

---

## 4) Two-phase rollout
## Phase 1 (ship)
1. Installer opt-out (WiX), default enabled, explicit wording.
2. Runtime setting loaded in CLI + GUI.
3. Session manager + idle detection timer (60s cadence).
4. Lifecycle hooks for start/end triggers.
5. Async send on rollover, sync send on shutdown/crash.
6. Null client + provider adapter.
7. Capture the Phase 1 telemetry subset defined in Appendix A.
8. Unit/functional coverage for lifecycle and triggers.

## Phase 2 (later)
1. Extend telemetry using the additive fields/entities defined in Appendix A.
2. Add exception/performance metrics.
3. Add bounded local persistence for unsent payloads.
4. Add retry/backoff/batching.
5. Add simple dashboards and weekly review KPIs.

---

## 5) AI execution plan (Claude / GitHub Copilot)
Use these PR slices exactly.

## PR-1: Contracts + null path
- Add `ITelemetryClient`, `TelemetrySession`, `NullTelemetryClient`.
- Add tests for disabled/no-op behavior.
- No runtime behavior changes yet.

## PR-2: Installer + config plumbing
- Add WiX checkbox (default enabled).
- Persist and load setting in CLI/GUI startup.
- Required text:
  - “We collect anonymous usage data to improve the application. No personal identifying information will be collected.”

## PR-3: Session lifecycle + idle accounting
- Start on file open.
- End previous on new file open.
- Record meaningful activity:
  - CLI command execution,
  - GUI filter/navigation/record actions.
- Exclude idle time.

## PR-4: Upload triggers
- Async on rollover.
- Sync best-effort on shutdown/crash.
- Enforce exactly-once send per session.

## PR-5: Provider adapter
- Implement App Insights adapter (or chosen provider) behind interface.
- Add timeout, cancellation, and failure isolation.

## PR-6: Hardening + docs
- Final schema/privacy docs.
- Ops setup notes (keys/config/validation).
- Final regression checks.

---

## 6) Test gates (must pass per PR)
### Functional
- Opt-out disables all telemetry behavior.
- Session start/end triggers are correct.
- Application attribution is correct (`WeevilGui.exe` / `WeevilCli.exe`).

### Idle logic
- Idle threshold excludes inactive time.
- Timer has negligible overhead.

### Upload semantics
- Async path does not block normal file-open flow.
- Sync path executes on shutdown/crash handler.
- Only one upload attempt per ended session.

### Resilience
- Provider timeout/failure does not crash app.
- User workflows remain unaffected.

---

## 7) Copy/paste prompt template for AI agents
Use this prompt for each PR:

> Implement **PR-X** from `Doc/Notes/Telemetry/TelemetryRolloutPlan.md` only.
> Constraints:
> - Keep changes within PR-X scope.
> - Preserve existing behavior outside scope.
> - Add/adjust tests for new behavior.
> - Ensure all automated tests pass.
> - Keep provider details inside telemetry adapter only.
> Deliverables:
> - code,
> - tests,
> - brief change summary,
> - risk notes.

---

## 8) Definition of done
Phase 1 is done when:
- Users can opt out at install time, and runtime fully respects that choice.
- Session telemetry is captured accurately with idle exclusion.
- Upload behavior matches lifecycle rules and is non-disruptive.
- Failures in telemetry never affect core app functionality.

---

## Appendix A) Final/future telemetry schema (authoritative)
Design goal: keep schema additive so Phase 1 writes a strict subset and Phase 2 extends without breaking compatibility.

### A.1 `Session` entity
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

### A.2 `SessionFilterExecution` entity
Purpose: support future filter analytics without widening the session entity excessively.

Required in Phase 1:
- `SessionId`
- `ExecutedAtUtc`
- `ExecutedPeriodMinutes`

Planned additive fields (Phase 2+):
- `HasIncludeFilter`
- `HasExcludeFilter`
- `FilterOptions`

### A.3 `SessionEvent` entity
Introduced in Phase 2:
- `SessionId`
- `EventType` (e.g., `FilterApplied`, `AnalyzerRun`, `GraphOpened`, `DashboardOpened`)
- `TimestampUtc`
- `ExecutionDurationMs` (when applicable)
- event-specific safe metadata (no PII)

### A.4 Guardrails
- CPU model/CPU telemetry is excluded from all phases.
- No PII in any entity.
