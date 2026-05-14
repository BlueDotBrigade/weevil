Recommended terminology:

* User-facing term: Analysis Operation
* Implementation term: Analyzer
* Configuration term: Detection Rule or Analysis Rule
* Result term: Finding, Flagged Record, or Insight

For Weevil, consider using Analysis Operation in documentation and UI planning. It is clearer than “tool,” less academic than “technique,” and still maps well to the existing Analyzer implementation concept. Weevil’s docs already describe these as “analyzers” that flag records, copy named-group values into comments, and compare extracted values over time.  ￼

For the roadmap column, I would use Roadmap Fit with these values:

1. Implemented
2. Next
3. Later
4. Someday

That keeps the column short, sortable, and product-friendly.

⸻

1. Data Extraction & Enrichment

Purpose: turn raw log text into structured values that other analysis operations can use. This is foundational because Weevil’s analysis model depends heavily on RegEx named groups, extracted values, comments, flags, and graphable values.  ￼

Roadmap Fit	Analysis Operation	Aliases / Related Terms	Purpose / Value
Implemented	Structured Data Extraction	Detect Data, field extraction, entity extraction, pattern extraction, RegEx extraction, information extraction	Finds records matching a pattern and extracts named-group values into comments or analysis output.
Implemented	Graphable Value Extraction	metric extraction, numeric extraction, series extraction	Extracts numeric values such as memory, CPU, handle count, queue depth, or latency so they can be graphed over time. Weevil already supports graphing named-group values.  ￼
Next	Template Extraction	log template extraction, message template detection, structural parsing	Converts repeated log shapes into reusable templates, such as User {UserId} connected from {IpAddress}. Useful for reducing noise before deeper analysis.
Next	Multi-Field Extraction	compound extraction, record enrichment, feature extraction	Extracts multiple named groups from the same record, such as UserId, SessionId, Duration, and ErrorCode, so later operations can compare relationships.
Later	Type-Aware Extraction	typed parsing, semantic field extraction, normalized extraction	Interprets extracted values as numbers, timestamps, durations, IDs, versions, IPs, GUIDs, etc., instead of treating all captures as strings.
Later	Derived Field Extraction	computed fields, calculated fields, feature derivation	Creates calculated values from extracted fields, such as latency buckets, normalized paths, or major/minor version numbers.
Someday	Automatic Field Discovery	schema inference, field discovery, pattern discovery	Suggests likely fields in a log file without the user writing the RegEx first. Non-ML heuristics are possible, but ML/AI versions belong in vNext.

⸻

2. Occurrence & Frequency Analysis

Purpose: identify what appears, appears for the first time, repeats, disappears, or changes in frequency.

Roadmap Fit	Analysis Operation	Aliases / Related Terms	Purpose / Value
Implemented	Novelty Detection	Detect First, first occurrence detection, new value detection, unique value detection, distinct value detection	Flags the first time each unique captured value appears. Useful for users, sessions, devices, error codes, or transaction IDs.
Implemented	Stability Detection	Detect Stable Values, steady-state detection, persistent value detection, plateau detection, convergence detection	Highlights when a captured value stops changing. Useful for repeated states, stuck counters, repeated messages, or values that have settled.
Next	Frequency Counting	occurrence counting, value frequency analysis, cardinality analysis	Counts how often each captured value appears. Useful for ranking top errors, top users, most common states, or repeated warnings.
Next	Burst Detection	spike detection, frequency surge detection, event storm detection, rate spike detection	Detects sudden increases in matching records, such as error storms, retry floods, or unusually noisy components.
Next	Silence Detection	gap detection, missing event detection, heartbeat loss detection, no-log detection	Detects when expected records stop appearing. Weevil already has elapsed-time operations for gaps in logging, so this would be a natural extension/generalization.  ￼
Later	Rare Event Detection	low-frequency detection, rarity detection, uncommon value detection	Finds values that appear unusually rarely. Useful for one-off errors, rare state transitions, or unexpected device IDs.
Later	Top-N Analysis	ranking, frequency ranking, heavy hitters	Shows the most common values for a named group. Useful for quick triage.
Someday	Distribution Shift Detection	population drift, frequency drift, behavioral drift	Detects when the distribution of values changes between two regions or time windows. Useful, but likely more advanced than the current analyzer model.

⸻

3. State & Change Detection

Purpose: understand how values move from one state to another.

Roadmap Fit	Analysis Operation	Aliases / Related Terms	Purpose / Value
Implemented	State Transition Detection	Detect Data Transitions, change detection, value transition detection, state change detection	Flags records where a captured value changes from the previous captured value. Weevil’s README uses this pattern for detecting hardware serial number changes.  ￼
Implemented	Steady-State Detection	Detect Stable Values, stable value detection, plateau detection	Identifies when a changing value becomes stable or repetitive.
Next	Transition Summary	transition matrix, state transition table, change summary	Summarizes observed transitions, for example Idle → Busy, Busy → Failed, Failed → Recovering. Excellent fit for logs with state machines.
Next	Invalid Transition Detection	forbidden transition detection, state machine validation, transition rule checking	Flags transitions that should not happen, such as Closed → Running or Authenticated → Anonymous.
Later	Oscillation Detection	flapping detection, toggle detection, state instability detection	Detects values switching back and forth repeatedly, such as Connected/Disconnected or Healthy/Unhealthy.
Later	Regression Detection	rollback detection, version regression, state regression	Detects when a value moves backward, such as firmware version decreasing, sequence number resetting, or lifecycle state reverting.
Later	Transition Duration Analysis	dwell time analysis, state duration analysis, time-in-state analysis	Measures how long the system remains in each state before transitioning.
Someday	State Model Inference	finite-state model discovery, workflow mining, process mining	Infers likely states and transitions from observed logs. Powerful, but more complex and likely a future advanced feature.

⸻

4. Numeric Signal & Threshold Analysis

Purpose: analyze numeric values extracted from logs, especially counters, durations, percentages, resource usage, and queue depths.

Roadmap Fit	Analysis Operation	Aliases / Related Terms	Purpose / Value
Implemented	Rising Edge Detection	Detect Rising Edges, upward transition detection, positive edge detection, increasing value detection	Flags when a captured numeric value increases from one record to the next.
Implemented	Falling Edge Detection	Detect Falling Edges, downward transition detection, negative edge detection, decreasing value detection, reset detection	Flags when a captured numeric value decreases from one record to the next.
Implemented	Elapsed Time Threshold Detection	Measure Elapsed Time, gap threshold detection, pause detection	Flags records where elapsed time between records exceeds a threshold. Weevil documents this for pauses, hangs, startup delays, and blocked background work.  ￼
Implemented	UI Responsiveness Detection	Detect Unresponsive UI, UI hang detection, responsiveness threshold detection	Detects likely UI responsiveness problems by measuring time gaps in UI-thread logging.  ￼
Next	Threshold Crossing Detection	limit detection, boundary crossing, high/low watermark detection	Flags when a value crosses a user-defined threshold, such as CPU > 90, QueueDepth > 1000, or Latency > 500ms.
Next	Peak Detection	local maximum detection, spike detection, crest detection	Finds local maxima, such as peak CPU, peak memory, peak queue depth, or longest duration.
Next	Reset Detection	counter reset detection, wraparound detection, falling edge specialization	Detects when a counter unexpectedly drops to zero or near zero, such as uptime, sequence number, or retry count.
Later	Trend Detection	slope detection, monotonic trend detection, increasing/decreasing trend analysis	Detects values that consistently rise or fall over a region, such as memory leaks or slowly increasing latency.
Later	Rate-of-Change Detection	delta analysis, derivative analysis, velocity analysis	Flags when the change between values is unusually large, not merely increasing or decreasing.
Later	Outlier Detection	statistical anomaly detection, extreme value detection, z-score detection	Finds numeric values that are unusually high or low compared with the surrounding data.
Someday	Seasonal / Cyclic Signal Detection	periodicity detection, recurrence detection, frequency analysis	Detects repeating numeric patterns, such as spikes every 5 minutes or periodic queue growth.

⸻

5. Temporal & Sequence Analysis

Purpose: understand ordering, timing, causality-like relationships, repeated event sequences, and missing steps.

Roadmap Fit	Analysis Operation	Aliases / Related Terms	Purpose / Value
Implemented	Temporal Anomaly Detection	Detect Temporal Anomalies, chronological anomaly detection, timestamp ordering analysis, out-of-order detection	Flags records whose timestamps appear out of order.
Implemented	Gap Detection	elapsed time analysis, logging gap detection, pause detection, silence detection	Detects unusually long gaps between records. Already present through Measure Elapsed Time and Measure Elapsed Time (UI).  ￼
Next	Sequence Pattern Detection	sequential pattern mining, event sequence analysis, repeated sequence detection	Finds repeated chains such as Start → Retry → Timeout → Failure. Very valuable for log analysis and still feasible without ML.
Next	Missing Step Detection	sequence gap detection, expected event detection, incomplete workflow detection	Flags when an expected event in a known sequence is missing, such as RequestStarted without RequestCompleted.
Next	Session Reconstruction	sessionization, trace reconstruction, transaction reconstruction, flow reconstruction	Groups records by a named group such as SessionId, CorrelationId, RequestId, or ThreadId.
Later	Latency Between Events	duration analysis, step timing analysis, inter-event timing	Measures time between two related events, such as request start/end, connection open/close, or retry intervals.
Later	Repeated Failure Sequence Detection	retry loop detection, failure loop detection, cycle detection	Detects recurring event loops, such as repeated retries or reconnect attempts.
Later	Event Correlation Detection	co-occurrence analysis, correlation analysis, related event detection	Finds events that often occur near each other, such as a warning that frequently precedes an exception.
Someday	Causal Chain Analysis	dependency chain detection, propagation analysis, root-cause path analysis	Attempts to infer chains of related events. Useful but risky without explicit correlation IDs or domain rules.
Someday	Process Mining	workflow discovery, trace mining, behavioral model discovery	Discovers workflows from logs automatically. Powerful, but probably belongs after session reconstruction and sequence detection exist.

⸻

Highest-Value “Next” Candidates

If the goal is practical log-pattern discovery with minimal architectural risk, I would rank the next features this way:

Rank	Analysis Operation	Category	Why
1	Threshold Crossing Detection	Numeric Signal & Threshold Analysis	Simple mental model, easy UI, useful for CPU, memory, latency, queue depth, duration, count, etc.
2	Frequency Counting	Occurrence & Frequency Analysis	Immediately useful for ranking common errors, states, users, sessions, and repeated messages.
3	Sequence Pattern Detection	Temporal & Sequence Analysis	High diagnostic value; helps users see repeated operational flows and failure paths.
4	Session Reconstruction	Temporal & Sequence Analysis	Unlocks richer analysis by grouping records by SessionId, RequestId, ThreadId, etc.
5	Transition Summary	State & Change Detection	Natural extension of Detect Data Transitions; especially useful for state-machine-style logs.
6	Burst Detection	Occurrence & Frequency Analysis	Great for detecting error storms, retry floods, noisy components, and sudden behavior changes.
7	Missing Step Detection	Temporal & Sequence Analysis	Useful once users can define expected event sequences.
8	Outlier Detection	Numeric Signal & Threshold Analysis	Valuable, but needs careful UX to avoid “statistical magic.”

Strongest recommendation: keep the UI names approachable, but use the industry term as a subtitle or documentation phrase.

Example:

UI Name	Technical Subtitle
Detect Threshold Crossings	Threshold Crossing Detection
Count Occurrences	Frequency Analysis
Detect Event Sequences	Sequential Pattern Detection
Group Related Records	Session Reconstruction
Summarize State Changes	Transition Summary