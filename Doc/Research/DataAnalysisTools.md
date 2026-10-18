# Data Analysis Tools

This document captures possible Weevil analysis operations for identifying patterns in application log files.

## Terminology

Recommended terms:

- **User-facing term:** Analysis Operation
- **Implementation term:** Analyzer
- **Configuration term:** Detection Rule or Analysis Rule
- **Result term:** Finding, Flagged Record, or Insight

For Weevil, **Analysis Operation** is the recommended product/documentation term. It is clearer than "tool," less academic than "technique," and still maps well to the existing analyzer concept.

For roadmap planning, this document uses **Roadmap Fit** with these values:

1. **Implemented**
2. **Next**
3. **Later**
4. **Someday**

## Menu Taxonomy

The user-facing menu should use a small number of categories because several analyzers naturally overlap across industry terms such as change detection, event detection, state analysis, signal analysis, and sequence analysis.

Recommended menu structure:

```text
Analysis
├── Extraction
├── Pattern
└── Temporal
```

This structure maps to the analyst's workflow:

1. **Extraction** — get structured values out of log records.
2. **Pattern** — identify repeated, changing, stable, first, last, rising, or falling values.
3. **Temporal** — reason about timestamp order and time-related behavior.

## Current Analyzer Mapping

| Category | Current Menu Name | Recommended UI Name | Industry / Documentation Term | Notes |
| --- | --- | --- | --- | --- |
| **Extraction** | Annotate Records | Extract Data | Structured Data Extraction | Extracts structured values from matching log records. |
| **Pattern** | First Occurrence | First Occurrence | First Occurrence Detection / Novelty Detection | Flags the first time each unique captured value appears. |
| **Pattern** | Repeating Records | Matching Record Runs | Run Boundary Detection / Contiguous Match Detection | Flags the first and last record in each consecutive block of matching records. |
| **Pattern** | Transitions | State Transitions | State Transition Detection | Flags records where a captured value changes. |
| **Pattern** | Rising Edges | Rising Edges | Rising Edge Detection | Flags upward changes in numeric or ordered captured values. |
| **Pattern** | Falling Edges | Falling Edges | Falling Edge Detection | Flags downward changes in numeric or ordered captured values. |
| **Pattern** | Detect Stability | Stable Value Runs | Stable Run Detection / Constant-Value Run Detection | Flags the start and stop of consecutive records where a captured value remains unchanged. |
| **Temporal** | Temporal Anomalies | Out-of-Order Timestamps | Temporal Ordering Anomaly Detection / Out-of-Order Event Detection | Flags records whose timestamps appear out of sequence. |

## Important Distinction: Stable Value Runs vs. Matching Record Runs

`Detect Stability` and `Repeating Records` should not be treated as the same analysis operation. They both identify **runs**, but they define a run differently.

| Current Menu Name | Recommended UI Name | What Defines the Run? | What Gets Flagged? | Best Industry Term |
| --- | --- | --- | --- | --- |
| Detect Stability | Stable Value Runs | Consecutive records with the same extracted named-group value. | The first and last record for each stable value run. | Stable Run Detection / Constant-Value Run Detection |
| Repeating Records | Matching Record Runs | Consecutive records that match the expression, regardless of captured value. | The first and last record in each matching block of two or more records. | Run Boundary Detection / Contiguous Match Detection |

### Example

Given these log records:

```text
1  State=Idle
2  State=Idle
3  State=Busy
4  State=Busy
5  Heartbeat
```

And this expression:

```regex
State=(?<State>\w+)
```

### Stable Value Runs

**Stable Value Runs** cares about the captured value of `State`.

It sees two stable value runs:

```text
Lines 1-2: State=Idle
Lines 3-4: State=Busy
```

Expected flagged boundaries:

```text
1  Start State: Idle
2  Stop State: Idle
3  Start State: Busy
4  Stop State: Busy
```

This operation answers:

> Where did a captured value remain constant, and where did that stable run start and stop?

### Matching Record Runs

**Matching Record Runs** only cares whether each record matches the expression.

It sees one consecutive block of matching records:

```text
Lines 1-4: records matched the expression
Line 5: record did not match the expression
```

Expected flagged boundaries:

```text
1  01-Begins
4  01-Ends
```

It does **not** care that the captured value changed from `Idle` to `Busy`.

This operation answers:

> Where did a consecutive block of matching records begin and end?

### Naming Notes

- Avoid using **Plateaus** as the primary name for `Detect Stability`. Plateau detection is appropriate for numeric signals, but this analyzer works with any captured value: states, IDs, versions, error codes, strings, or other tokens.
- Avoid using **Repeating Records** as the primary name if the analyzer does not require identical records. The implementation identifies consecutive matching records, so **Matching Record Runs** is more accurate.
- Both operations belong in **Pattern** because they identify structural patterns in filtered or extracted log records. They are not primarily temporal operations, even though they operate over record order.

## 1. Extraction

Purpose: turn raw log text into structured values that other analysis operations can use.

| Roadmap Fit | Analysis Operation | Aliases / Related Terms | Purpose / Value |
| --- | --- | --- | --- |
| **Implemented** | **Structured Data Extraction** | Extract Data, Detect Data, field extraction, entity extraction, pattern extraction, RegEx extraction, information extraction | Finds records matching a pattern and extracts named-group values into comments or analysis output. |
| **Implemented** | **Graphable Value Extraction** | Metric extraction, numeric extraction, series extraction | Extracts numeric values such as memory, CPU, handle count, queue depth, or latency so they can be graphed over time. |
| **Next** | **Template Extraction** | Log template extraction, message template detection, structural parsing | Converts repeated log shapes into reusable templates, such as `User {UserId} connected from {IpAddress}`. Useful for reducing noise before deeper analysis. |
| **Next** | **Multi-Field Extraction** | Compound extraction, record enrichment, feature extraction | Extracts multiple named groups from the same record, such as `UserId`, `SessionId`, `Duration`, and `ErrorCode`, so later operations can compare relationships. |
| **Later** | **Type-Aware Extraction** | Typed parsing, semantic field extraction, normalized extraction | Interprets extracted values as numbers, timestamps, durations, IDs, versions, IPs, GUIDs, etc., instead of treating all captures as strings. |
| **Later** | **Derived Field Extraction** | Computed fields, calculated fields, feature derivation | Creates calculated values from extracted fields, such as latency buckets, normalized paths, or major/minor version numbers. |
| **Someday** | **Automatic Field Discovery** | Schema inference, field discovery, pattern discovery | Suggests likely fields in a log file without the user writing the RegEx first. Non-ML heuristics are possible, but ML/AI versions belong in vNext. |

## 2. Pattern

Purpose: identify repeated, changing, stable, first, last, rising, or falling values and matching record runs.

| Roadmap Fit | Analysis Operation | Aliases / Related Terms | Purpose / Value |
| --- | --- | --- | --- |
| **Implemented** | **First Occurrence** | First occurrence detection, novelty detection, new value detection, unique value detection, distinct value detection | Flags the first time each unique captured value appears. Useful for users, sessions, devices, error codes, or transaction IDs. |
| **Implemented** | **Matching Record Runs** | Run boundary detection, contiguous match detection, matching block detection | Flags the first and last record in each consecutive block of records matching the expression. Does not compare captured values. |
| **Implemented** | **Stable Value Runs** | Stable run detection, constant-value run detection, stable value detection | Flags the start and stop of consecutive records where a captured named-group value remains unchanged. |
| **Implemented** | **State Transitions** | State transition detection, change detection, value transition detection, state change detection | Flags records where a captured value changes from the previous captured value. |
| **Implemented** | **Rising Edges** | Rising edge detection, upward transition detection, positive edge detection, increasing value detection | Flags when a captured numeric or ordered value increases from one record to the next. |
| **Implemented** | **Falling Edges** | Falling edge detection, downward transition detection, negative edge detection, decreasing value detection, reset detection | Flags when a captured numeric or ordered value decreases from one record to the next. |
| **Next** | **Last Occurrence** | Last occurrence detection, final occurrence detection | Flags the last time each unique captured value appears. Useful as a counterpart to First Occurrence. |
| **Next** | **Frequency Counting** | Occurrence counting, value frequency analysis, cardinality analysis | Counts how often each captured value appears. Useful for ranking top errors, top users, most common states, or repeated warnings. |
| **Next** | **Threshold Crossings** | Threshold crossing detection, limit detection, boundary crossing, high/low watermark detection | Flags when a value crosses a user-defined threshold, such as `CPU > 90`, `QueueDepth > 1000`, or `Latency > 500ms`. |
| **Next** | **Transition Summary** | Transition matrix, state transition table, change summary | Summarizes observed transitions, for example `Idle -> Busy`, `Busy -> Failed`, and `Failed -> Recovering`. Excellent fit for logs with state machines. |
| **Later** | **Burst Detection** | Spike detection, frequency surge detection, event storm detection, rate spike detection | Detects sudden increases in matching records, such as error storms, retry floods, or unusually noisy components. |
| **Later** | **Peak Detection** | Local maximum detection, spike detection, crest detection | Finds local maxima, such as peak CPU, peak memory, peak queue depth, or longest duration. |
| **Later** | **Reset Detection** | Counter reset detection, wraparound detection, falling edge specialization | Detects when a counter unexpectedly drops to zero or near zero, such as uptime, sequence number, or retry count. |
| **Later** | **Oscillation Detection** | Flapping detection, toggle detection, state instability detection | Detects values switching back and forth repeatedly, such as `Connected` / `Disconnected` or `Healthy` / `Unhealthy`. |
| **Later** | **Trend Detection** | Slope detection, monotonic trend detection, increasing/decreasing trend analysis | Detects values that consistently rise or fall over a region, such as memory leaks or slowly increasing latency. |
| **Later** | **Rate-of-Change Detection** | Delta analysis, derivative analysis, velocity analysis | Flags when the change between values is unusually large, not merely increasing or decreasing. |
| **Later** | **Outlier Detection** | Statistical anomaly detection, extreme value detection, z-score detection | Finds numeric values that are unusually high or low compared with the surrounding data. |
| **Someday** | **Distribution Shift Detection** | Population drift, frequency drift, behavioral drift | Detects when the distribution of values changes between two regions or time windows. Useful, but likely more advanced than the current analyzer model. |
| **Someday** | **Seasonal / Cyclic Signal Detection** | Periodicity detection, recurrence detection, frequency analysis | Detects repeating numeric patterns, such as spikes every 5 minutes or periodic queue growth. |
| **Someday** | **State Model Inference** | Finite-state model discovery, workflow mining, process mining | Infers likely states and transitions from observed logs. Powerful, but more complex and likely a future advanced feature. |

## 3. Temporal

Purpose: understand ordering, timing, causality-like relationships, repeated event sequences, and missing steps.

| Roadmap Fit | Analysis Operation | Aliases / Related Terms | Purpose / Value |
| --- | --- | --- | --- |
| **Implemented** | **Out-of-Order Timestamps** | Temporal ordering anomaly detection, out-of-order event detection, chronological anomaly detection, timestamp ordering analysis | Flags records whose timestamps appear out of sequence, such as `10:12` appearing before `9:45`. |
| **Implemented** | **Gap Detection** | Elapsed time analysis, logging gap detection, pause detection, silence detection | Detects unusually long gaps between records. Already present through elapsed-time analysis. |
| **Implemented** | **UI Responsiveness Detection** | UI hang detection, responsiveness threshold detection | Detects likely UI responsiveness problems by measuring time gaps in UI-thread logging. |
| **Next** | **Silence Detection** | Missing event detection, heartbeat loss detection, no-log detection | Detects when expected records stop appearing. Natural extension of elapsed-time analysis. |
| **Next** | **Sequence Pattern Detection** | Sequential pattern mining, event sequence analysis, repeated sequence detection | Finds repeated chains such as `Start -> Retry -> Timeout -> Failure`. Very valuable for log analysis and still feasible without ML. |
| **Next** | **Missing Step Detection** | Sequence gap detection, expected event detection, incomplete workflow detection | Flags when an expected event in a known sequence is missing, such as `RequestStarted` without `RequestCompleted`. |
| **Next** | **Session Reconstruction** | Sessionization, trace reconstruction, transaction reconstruction, flow reconstruction | Groups records by a named group such as `SessionId`, `CorrelationId`, `RequestId`, or `ThreadId`. |
| **Later** | **Latency Between Events** | Duration analysis, step timing analysis, inter-event timing | Measures time between two related events, such as request start/end, connection open/close, or retry intervals. |
| **Later** | **Repeated Failure Sequence Detection** | Retry loop detection, failure loop detection, cycle detection | Detects recurring event loops, such as repeated retries or reconnect attempts. |
| **Later** | **Event Correlation Detection** | Co-occurrence analysis, correlation analysis, related event detection | Finds events that often occur near each other, such as a warning that frequently precedes an exception. |
| **Someday** | **Causal Chain Analysis** | Dependency chain detection, propagation analysis, root-cause path analysis | Attempts to infer chains of related events. Useful but risky without explicit correlation IDs or domain rules. |
| **Someday** | **Process Mining** | Workflow discovery, trace mining, behavioral model discovery | Discovers workflows from logs automatically. Powerful, but probably belongs after session reconstruction and sequence detection exist. |

## Highest-Value Next Candidates

If the goal is practical log-pattern discovery with minimal architectural risk, these are the strongest next candidates.

| Rank | Analysis Operation | Category | Why |
| ---: | --- | --- | --- |
| 1 | **Last Occurrence** | Pattern | Simple counterpart to First Occurrence and useful for identifying when a value disappears or stops recurring. |
| 2 | **Threshold Crossings** | Pattern | Simple mental model, easy UI, useful for CPU, memory, latency, queue depth, duration, count, etc. |
| 3 | **Frequency Counting** | Pattern | Immediately useful for ranking common errors, states, users, sessions, and repeated messages. |
| 4 | **Sequence Pattern Detection** | Temporal | High diagnostic value; helps users see repeated operational flows and failure paths. |
| 5 | **Session Reconstruction** | Temporal | Unlocks richer analysis by grouping records by `SessionId`, `RequestId`, `ThreadId`, etc. |
| 6 | **Transition Summary** | Pattern | Natural extension of State Transitions; especially useful for state-machine-style logs. |
| 7 | **Burst Detection** | Pattern | Great for detecting error storms, retry floods, noisy components, and sudden behavior changes. |
| 8 | **Missing Step Detection** | Temporal | Useful once users can define expected event sequences. |
| 9 | **Outlier Detection** | Pattern | Valuable, but needs careful UX to avoid "statistical magic." |

## Naming Recommendation

Keep UI names approachable, but use the industry term as a subtitle or documentation phrase.

| UI Name | Technical Subtitle |
| --- | --- |
| Extract Data | Structured Data Extraction |
| First Occurrence | First Occurrence Detection |
| Matching Record Runs | Run Boundary Detection |
| Stable Value Runs | Stable Run Detection |
| State Transitions | State Transition Detection |
| Rising Edges | Rising Edge Detection |
| Falling Edges | Falling Edge Detection |
| Out-of-Order Timestamps | Temporal Ordering Anomaly Detection |
