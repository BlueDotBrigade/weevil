# Weevil : Help

- [Filtering](#filtering)
  - [Examples](#examples)
  - [Built-In Expressions](#built-in-expressions)
    - [Monikers](#monikers)
- [Data Analysis](#data-analysis)
  - [Creating a Line Graph](#creating-a-line-graph)
  - [Detecting Data](#detecting-data)
    - [Detecting an Unresponsive UI](#detecting-an-unresponsive-ui)
    - [Detecting Gaps In Logging](#detecting-gaps-in-logging)
- [Dashboard](#dashboard)
- [Appendices](#appendices)
  - [Appendix A: Additional Reading](#appendix-a-additional-reading)
  - [Appendix E: Excel](#appendix-e-excel)
  - [Appendix P: Performance Tips](#appendix-p-performance-tips)

---

## Filtering

A filter represents one or more expressions - separated by a double pipe (`||`) character - than can be used to identify records of interest within a log file.  

The supported filter types include:

1. *regular expressions* : A sophisticated pattern matching language that can be used to find content. 
2. *filter aliases* : An alias can be added to _Weevil_ to make it easier to reference complex and/or frequently used filters.
3. *Monikers* : Are used to query metadata that has been collected by the *Weevil* application.

For example:

`#Fatal||@Comment=suspect`

| Expression         | Expression Type      | Returns                                                                      |
| ------------------ | -------------------- | ---------------------------------------------------------------------------- |
| `#Fatal`           | *filter alias*   | Identifies records related to application crashes.                           |
| `@Comment=suspect` | *expression monkier* | Records that include the word `suspect` in the user defined comments column. |

When using *Weevil*, it is worth noting that all operations are (include `Clear`) are non-destructive.  In other words, the original log file will remain unchanged.

### Examples

If a log file contained the following:

```
A quick brown fox jumps over the lazy dog.
The five boxing wizards jump quickly.
How quickly daft jumping zebras vex!
```

Then a user could applies these inclusive filters:

- Case-sensitive search:
    - Filter: `H`
    - Returns: line 3
- Case-insensitive search:
    - Filter: `(?i)THE`
    - Returns: line 2 
- Searching for multiple values:
    - Filter: `dog|zebra`
    - Returns: line 1 & 3
- Searching for text that begins with:
    - Filter: `quick.*`
    - Returns: line 1 & 3

### Built-In Expressions

#### Monikers

The following expressions can be used to query metadata collected by the Weevil application:

- `@Comment` : identifies all records that have a user comment
  - **Note**: Be sure to uncheck the "Include Pinned" option before using this moniker.
  - `@Comment=State`: performs a case-insensitive search of all user comments for the given value, in this case the word `State`
- `@Elapsed` : is used to measure the time period between records
  - `@Elapsed>5000` : identify for records with an elapsed time greater than the given value
  - returns a list of records where there was no logging for the preceding 5 seconds
- `@Flagged`: identify all records that have been flagged
  - `@Flagged=False`: identify all records that have not been flagged 
- `@Pinned` : identify records that have been pinned
  - `@Pinned=False` : identify all records that have not been pinned
- `@IsMultiLine` : identify records that span multiple lines (e.g. an exception  callstack)
- `@ContentLength>128` : identify records longer than the given value
- `@UiThread`: identifies all records that were created by the application's UI thread
  - `@UiThread=False`: identifies all records that were not created by the application's UI thread
- `@Region`: identifies the beginning & end of each region of interest.

---

## Data Analysis

### Creating a Line Graph

A line graph can be created using the following steps:

1. Using regular expression with a _named group_, filter the log file so that only the records of interest are visible.
  - For example in this regular expression `HandleCount=(?<Handles>\d+)` the number of Windows handles is being detected by the `Handles` named group.
  - [Regex101.com][RegEx101] is a useful tool for learning about regular expressions & named groups.
2. Select the records you wish to plot. To select all records, press `Ctrl+A`.
3. Select `Graph Data` from the menu, or press `Ctrl+Shift+G`.

### Detecting Data

Weevil has several analyzers that can be used to extract data from a log file, flag relevant records, and copy regular expression "named group" values into the record's comment field:

A record is flagged & a comment is created when...

- `Detect Data`: the regular expression matches record content
- `Detect First`: the first occurrence of a unique value is found
- `Detect Data Transitions`: the matching regular expression value changes from one record to the next
- `Detect Rising Edges`: the matching regular expression value increases from one record to the next
- `Detect Falling Edges`: the matching regular expression value increases from one record to the next
- `Detect Temporal Anomalies`: record timestamps appear out of order

Steps:

1. Filter using a regular expression with a named group.

	- For example:  `Key=(?<Value>[a-zA-Z0-9]+)`

2. Select an appropriate analyzer.
3. Post-analysis you can view the records of interest by using the following inclusive filter:

   - `@Comment` to show all records with a comment, or
   - `@Flagged` to show the records that matched the previous analysis 

Result: Weevil will chronologically flag records where the `Value` changes.

#### Detecting an Unresponsive UI

This analyzer is useful for applications that:

- perform a lot of logging from the UI thread, and
- do not explicitly measure UI responsiveness.

Steps:

1. Selecting the records you wish to analyze.
   - Alternatively, select a single record and Weevil will assume that all records that satisfy the current filter criteria should be analyzed.
2. Analyzers => Detect Unresponsive UI
3. Enter the threshold that will determine whe the UI is considered unresponsive.
   - For reference, you would use the following thresholds if an application closely monitored it's UI responsiveness:
     - ~250ms for simple operations (e.g. button clicks)
     - ~500ms for "easy" tasks
   - Given the indirect nature of this analysis, a threshold of 1s (1000ms) is recommended.

#### Detecting Gaps In Logging

Weevil includes analyzers that can be used to detect when an application stopped writing to the log file:

- `Measure Elapsed Time` : flags records when the time period between records exceeds the given threshold
- `Measure Elapsed Time (UI)` : unlike `Measure Elapsed Time` this analyzer only measures the time period between records generated by the UI (`ThreadId=1`)

## Dashboard

When a log file is opened, Weevil will silently begin analyzing the data looking for trends.  A light-bulb icon will appear in the status bar post-analysis informing the user of any insight that may be of interest.

---

## Appendices

### Appendix A: Additional Reading

- Regular expression [quick reference][RegExQuickRef]
	- Provides an overview of the RegEx syntax.
- [Regular Expression 101][RegEx101] utility
	- Useful for trying different regular expressions.

### Appendix E: Excel

- The *Microsoft Excel* installer no longer associates the application with *Tab Separated Value* (`*.tsv`) files.  This can be resolved by:
   1. executing the following command: `reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Clients\Spreadsheet\Microsoft Excel\Capabilities\FileAssociations" /v ".tsv" /t REG_SZ /d "Excel.SLK" /f`
   2. Right click a `*.tsv` file and select "Open With"
   3. For more information, see: [File Association .tsv to excel](https://superuser.com/a/1381871/166002)
- If opening a file in *Excel* does not work, then try the following:
   1. create an empty worksheet
   2. Excel => Data tab => Get from Text/CSV

### Appendix P: Performance Tips

- The bigger the log file, the longer it will take to apply a new filter.
- The *Clear* commands improve performance be removing portions of the log file that are not needed. 
  - *Clear Before* removes all log file records from memory before the highlighted row in Log Viewer.
  - *Clear After* removes all log file records from memory before the highlighted row in Log Viewer.
- You can force Weevil to release unused RAM back to the operating system by pressing: `Ctrl+Alt+Shift+F12`

[RegExQuickRef]: https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference
[RegEx101]: https://regex101.com/r/EKCf6T/4
