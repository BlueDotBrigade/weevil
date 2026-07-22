# Weevil : Help

- [Introduction](#introduction)
- [Getting Started](#getting-started)
- [Filtering Records](#filtering-records)
  - [Filter Concepts](#filter-concepts)
  - [Simple Filter Examples](#simple-filter-examples)
  - [Built-In Expressions](#built-in-expressions)
    - [Monikers](#monikers)
- [Why Regular Expressions Matter](#why-regular-expressions-matter)
  - [Simple Real-World RegEx Examples](#simple-real-world-regex-examples)
  - [Named Groups](#named-groups)
- [Data Analysis](#data-analysis)
  - [Detecting Data](#detecting-data)
  - [Detecting Threshold Crossings](#detecting-threshold-crossings)
  - [Detecting an Unresponsive UI](#detecting-an-unresponsive-ui)
  - [Detecting Gaps in Logging](#detecting-gaps-in-logging)
- [Creating a Line Graph](#creating-a-line-graph)
- [Dashboard](#dashboard)
- [Appendices](#appendices)
  - [Appendix A: Useful RegEx Expressions](#appendix-a-useful-regex-expressions)
  - [Appendix B: Additional Reading](#appendix-b-additional-reading)
  - [Appendix E: Excel](#appendix-e-excel)
  - [Appendix P: Performance Tips](#appendix-p-performance-tips)

---

## Introduction

_Weevil_ helps engineers inspect large log files without changing the original file.

At a high level, Weevil lets you:

- open a log file
- filter records so you only see what matters
- add comments, flags, bookmarks, and regions of interest
- analyze records to detect patterns and anomalies
- graph values extracted from the log

All operations are non-destructive. The source log file is never modified.

If you are new to Weevil, the most important ideas are:

1. **Filter first** to reduce noise.
2. Use **regular expressions** when plain text is not precise enough.
3. Use **named groups** when you want Weevil to extract values for analysis or graphing.

---

## Getting Started

For a new user, a typical workflow looks like this:

1. Open a log file.
2. Apply a filter to keep only the records you care about.
3. Review the filtered results.
4. Add comments, pins, bookmarks, or regions of interest as needed.
5. Run an analyzer if you want Weevil to detect changes, anomalies, or trends.
6. Optionally graph values extracted with a named group.

If the log file is large, start with a simple filter before running deeper analysis.

---

## Filtering Records

Filtering is the fastest way to make a large log file understandable.

### Filter Concepts

A filter is a rule that tells Weevil which records should remain visible.

Weevil supports three main kinds of filter input:

1. **Plain text**
   - Good for quick searches.
   - Example: `error`
2. **Filter aliases**
   - Short names for frequently used or complex filters.
   - Example: `#Fatal`
3. **Monikers**
   - Built-in expressions that query metadata collected by Weevil.
   - Example: `@Comment`

You can combine multiple expressions with `||`, which means logical **OR**.

Example:

`#Fatal||@Comment=suspect`

This means:

- show records that match the `#Fatal` alias, or
- show records whose user comment contains the word `suspect`

### Simple Filter Examples

If a log file contained the following text:

```
A quick brown fox jumps over the lazy dog.
The five boxing wizards jump quickly.
How quickly daft jumping zebras vex!
```

Then these filters would behave as follows:

- Case-sensitive search
  - Filter: `H`
  - Returns: line 3
- Case-insensitive search
  - Filter: `(?i)THE`
  - Returns: line 2
- Search for multiple values
  - Filter: `dog|zebra`
  - Returns: line 1 and line 3
- Search for text that begins with a value
  - Filter: `quick.*`
  - Returns: line 1 and line 3

### Built-In Expressions

#### Monikers

Monikers let you filter on metadata instead of raw text. This is useful after you have pinned records, added comments, flagged results, or defined regions.

The following monikers are supported:

- `@Comment`
  - Shows all records that have a user comment.
  - `@Comment=State` performs a case-insensitive search of the comment text.
  - Note: if you want strictly comment-based results, uncheck **Include Pinned** before using this moniker.
- `@Elapsed`
  - Filters using the measured time between records.
  - `@Elapsed>5000` shows records where the preceding gap exceeds 5 seconds.
- `@Flagged`
  - Shows records that have been flagged.
  - `@Flagged=False` shows records that are not flagged.
- `@Pinned`
  - Shows records that have been pinned.
  - `@Pinned=False` shows records that are not pinned.
- `@IsMultiLine`
  - Shows records that span multiple lines, such as exception call stacks.
- `@ContentLength>128`
  - Shows records whose content is longer than 128 characters.
- `@UiThread`
  - Shows records created by the application's UI thread.
  - `@UiThread=False` shows records not created by the UI thread.
- `@Region`
  - Shows region boundaries for regions of interest.

---

## Why Regular Expressions Matter

Plain text filters are useful, but real log files are rarely consistent enough for plain text alone.

Regular expressions, usually shortened to **RegEx**, let you describe patterns instead of exact text. That makes them useful when values change from line to line.

For example, a log message may contain:

- a session ID that changes every run
- an IP address that changes per machine
- a numeric counter that changes over time
- a hardware serial number that changes when equipment is swapped

With RegEx, you can match the structure of the data instead of one exact value.

This is where Weevil becomes especially powerful:

- you can filter noisy logs more precisely
- you can extract values for analysis
- you can graph numeric values over time
- you can detect changes, first occurrences, rising values, and anomalies

### Simple Real-World RegEx Examples

The following examples show why RegEx is useful in engineering logs:

1. **Find an error code**

   ```
   ErrorCode=\d+
   ```

   Matches text such as:

   - `ErrorCode=12`
   - `ErrorCode=404`

2. **Find an IPv4 address**

   ```
   (?:[0-9]{1,3}\.){3}[0-9]{1,3}
   ```

   Matches text such as:

   - `10.1.25.7`
   - `192.168.0.14`

3. **Find a handle count and capture the number**

   ```
   HandleCount=(?<Handles>\d+)
   ```

   Matches text such as:

   - `HandleCount=245`

   The `Handles` named group captures the numeric value so Weevil can graph or analyze it.

4. **Find a changing key value**

   ```
   Key=(?<Value>[a-zA-Z0-9]+)
   ```

   Matches text such as:

   - `Key=ABC123`
   - `Key=Node7`

   The `Value` named group can then be used by Weevil analyzers.

### Named Groups

Named groups are one of the most important RegEx features in Weevil.

A named group labels the part of a match that you want to extract.

Example:

```
HandleCount=(?<Handles>\d+)
```

Here:

- `HandleCount=` matches the fixed text
- `(?<Handles>\d+)` captures one or more digits and labels that value as `Handles`

Why this matters:

- Weevil can copy captured values into the record comment field
- analyzers can compare captured values between records
- graphs can plot captured numeric values over time

If you remember only one advanced Weevil concept, remember this:

> **Use RegEx to match the record. Use named groups to extract the useful part.**

---

## Data Analysis

After filtering, Weevil can analyze the visible or selected records.

Analysis is most useful when your filter includes a named group, because Weevil can compare the captured values over time.

### Detecting Data

Weevil includes several analyzers that flag relevant records and can copy named-group values into the record comment field.

A record is flagged and a comment is created when:

- `Detect Data`: the regular expression matches the record content
- `Detect First`: the first occurrence of a unique value is found
- `Detect Data Transitions`: the captured value changes from one record to the next
- `Detect Rising Edges`: the captured value increases from one record to the next
- `Detect Falling Edges`: the captured value decreases from one record to the next
- `Threshold Crossings`: the captured numeric value is greater than (`>`), greater than or equal to (`>=`), less than (`<`), or less than or equal to (`<=`) a threshold
- `Detect Temporal Anomalies`: record timestamps appear out of order

Typical workflow:

1. Filter using a regular expression with a named group.
   - Example: `Key=(?<Value>[a-zA-Z0-9]+)`
2. Select an analyzer.
3. Review the results using one of these filters:
   - `@Comment` to show all records with extracted comments
   - `@Flagged` to show records matched by the previous analysis

Example use cases:

- detect when a serial number changes
- detect the first time a user or session appears
- detect when a counter starts increasing rapidly
- detect when records appear out of timestamp order

### Detecting Threshold Crossings

Use this analyzer when you need to answer questions like:

- "When was latency greater than 200?"
- "When was CPU less than or equal to 20?"

Steps:

1. Filter records with a regular expression that captures a numeric named group.
2. Choose **Analyzers => Threshold Crossings**.
3. Enter:
   - a numeric threshold value
   - a comparison operator: `>`, `>=`, `<`, or `<=`
4. Review flagged records with `@Flagged` or review generated comments with `@Comment`.

### Detecting an Unresponsive UI

This analyzer is useful when:

- the application writes many log records from the UI thread, and
- the application does not already measure UI responsiveness directly

Steps:

1. Select the records you want to analyze.
   - If you select a single record, Weevil assumes all records matching the current filter should be analyzed.
2. Choose **Analyzers => Detect Unresponsive UI**.
3. Enter a threshold that defines when the UI should be considered unresponsive.

Suggested thresholds:

- about `250ms` for simple interactions such as button clicks
- about `500ms` for easy tasks
- about `1000ms` when using this indirect log-based analysis method

### Detecting Gaps in Logging

Weevil can also detect when an application stopped writing to the log.

Available analyzers:

- `Measure Elapsed Time`
  - Flags records when the time gap between consecutive records exceeds a threshold.
- `Measure Elapsed Time (UI)`
  - Similar to `Measure Elapsed Time`, but only evaluates records produced by the UI thread (`ThreadId=1`).

This is useful when diagnosing pauses, hangs, startup delays, or blocked background work.

---

## Creating a Line Graph

Use a graph when you want to visualize how a numeric value changes over time.

Steps:

1. Filter the log with a regular expression that includes a named group.
   - Example: `HandleCount=(?<Handles>\d+)`
2. Select the records you want to plot.
   - To select all visible records, press `Ctrl+A`.
3. Select `Graph Data`, or press `Ctrl+Shift+G`.

Good candidates for graphing include:

- memory usage
- CPU usage
- handle count
- queue depth
- request latency

---

## Dashboard

When a log file is opened, Weevil can automatically inspect the data for trends and issues.

If something noteworthy is found, a light-bulb icon appears in the status bar to indicate that an insight may need attention.

---

## Appendices

### Appendix A: Useful RegEx Expressions

The following expressions are practical starting points for common tasks.

#### Basic search examples from this guide

- `H`
  - Simple case-sensitive match
- `(?i)THE`
  - Case-insensitive match
- `dog|zebra`
  - Match either value
- `quick.*`
  - Match text that begins with `quick`

#### Common engineering log patterns

- `ErrorCode=\d+`
  - Match a numeric error code
- `HandleCount=(?<Handles>\d+)`
  - Capture a numeric handle count
- `Key=(?<Value>[a-zA-Z0-9]+)`
  - Capture an alphanumeric value
- `(?:[0-9]{1,3}\.){3}[0-9]{1,3}`
  - Match an IPv4 address
- `ID=(?<UniqueId>[a-zA-Z0-9]+)`
  - Capture a simple hardware, session, or transaction identifier
- `ThreadId=(?<ThreadId>\d+)`
  - Capture a thread ID
- `Duration=(?<Milliseconds>\d+)ms`
  - Capture a duration in milliseconds

#### RegEx reminders

- `\d+`
  - One or more digits
- `[a-zA-Z0-9]+`
  - One or more alphanumeric characters
- `.*`
  - Any characters to the end of the line
- `|`
  - Logical OR
- `(?i)`
  - Case-insensitive mode
- `(?<Name>...)`
  - Named group called `Name`

### Appendix B: Additional Reading

- Regular expression [quick reference][RegExQuickRef]
  - Provides an overview of RegEx syntax.
- [Regular Expression 101][RegEx101]
  - Useful for testing expressions and learning named groups.

### Appendix E: Excel

- The *Microsoft Excel* installer no longer automatically associates the application with *Tab Separated Value* (`*.tsv`) files. This can be resolved by:
  1. executing the following command:
     `reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Clients\Spreadsheet\Microsoft Excel\Capabilities\FileAssociations" /v ".tsv" /t REG_SZ /d "Excel.SLK" /f`
  2. right-clicking a `*.tsv` file and selecting **Open With**
  3. reviewing this reference: [File Association .tsv to excel](https://superuser.com/a/1381871/166002)
- If opening a file in *Excel* still does not work:
  1. create an empty worksheet
  2. select **Data => Get from Text/CSV**

### Appendix P: Performance Tips

- The larger the log file, the longer it will take to apply a new filter.
- The **Clear** commands can improve performance by removing unneeded records from memory.
  - **Clear Before** removes records before the highlighted row in Log Viewer.
  - **Clear After** removes records after the highlighted row in Log Viewer.
- You can force Weevil to release unused RAM back to the operating system by pressing `Ctrl+Alt+Shift+F12`.

[RegExQuickRef]: https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference
[RegEx101]: https://regex101.com/r/EKCf6T/4
