# Weevil : Help

- [Introduction](#introduction)
- [Examples](#examples)
  - [Sample Input](#sample-input)
  - [Sample Expressions](#sample-expressions)
- [Built-In Expressions](#built-in-expressions)
  - [Monikers](#monikers)
- [Data Analysis](#data-analysis)
  - [Detecting When Data Changes](#detecting-when-data-changes)
  - [Detecting an Unresponsive UI](#detecting-an-unresponsive-ui)
- [Appendices](#appendices)
  - [Appendix A: Additional Reading](#appendix-a-additional-reading)
  - [Appendix E: Excel](#appendix-e-excel)
  - [Appendix P: Performance Tips](#appendix-p-performance-tips)

---

## Introduction

A filter represents one or more expressions - separated by a double pipe (`||`) character - than can be used to identify records of interest within a log file.  

The supported filter types include:

1. *regular expressions* : A sophisticated pattern matching language that can be used to find content. 
2. *expression aliases* : To save time, aliases can be used to reference built-in regular expressions.
3. *Monikers* : Are used to query metadata that has been collected by the *Weevil* application.

For example:

`#Fatal||@Comment=suspect`

| Expression         | Expression Type      | Returns                                                                      |
| ------------------ | -------------------- | ---------------------------------------------------------------------------- |
| `#Fatal`           | *expression alias*   | Identifies records related to application crashes.                           |
| `@Comment=suspect` | *expression monkier* | Records that include the word `suspect` in the user defined comments column. |

When using *Weevil*, it is worth noting that all operations are (include `Clear`) are non-destructive.  In other words, the original log file will remain unchanged.

## Examples

### Sample Input

Assume a log file contained the following:

```Dos
01: A quick brown fox jumps over the lazy dog.
02: The five boxing wizards jump quickly.
03: How quickly daft jumping zebras vex!
```

### Sample Expressions

- Performing a case-sensitive search: 
    - Filter: `H`
    - Returns: line 3
- Performing a case-insensitive search: 
    - Filter: `(?i)THE`
    - Returns: line 2 
- Searching for multiple values: 
    - Filter: `dog|zebra`
    - Returns: line 1 & 3 
- Searching for a pattern that begins & ends with: 
    - Filter: `quick.*jump`
    - Returns: line 1 & 3

## Built-In Expressions

### Monikers

The following expressions can be used to query metadata collected by the Weevil application:

- `@Comment` : identifies all records that have a user comment
  - **Note**: Be sure to uncheck the "Include Pinned" option before using this moniker.
  - `@Comment=State`: performs a case-insensitive search of all user comments for the given value, in this case the word `State`
- `@Elapsed` : is used to measure the time period between records
  - `@Elapsed>5000` : returns a list of records where there was no logging for the preceding 5 seconds
- `@Flagged`: search all records that have been flagged
  - `@Flagged=False`: search all records that have not been flagged 
- `@Pinned` : search for records that have been pinned
  - `@Pinned=False` : search for all records that have not been pinned
- `@UiThread`: identifies all records that were created by the application's UI thread
  - `@UiThread=False`: identifies all records that were not created by the application's UI thread

---

## Data Analysis

### Detecting When Data Changes

To identify records where a value has changed over time:

1. Filter using a regular expression with a named group.
	- For example:  `Key=(?<Value>[a-zA-Z0-9]+)`
2. Use Weevil's `Detect Data Transitions` option.

Result: Weevil will chronologically flag records where the `Value` changes.

### Detecting an Unresponsive UI

This analyzer is useful for applications that:
- perform a lot of logging from the UI thread, and
- do not explicitly measure UI responsiveness.

Begin by:

1. Selecting the records you wish to analyze.
   - Alternatively, select a single record and Weevil will assume that all records that satisfy the current filter criteria should be analyzed.
2. Analyzers => Detect Unresponsive UI
3. Enter the threshold that will determine whe the UI is considered unresponsive.
   - For reference, you would use the following thresholds if an application closely monitored it's UI responsiveness:
     - ~250ms for simple operations (e.g. button clicks)
     - ~500ms for "easy" tasks
   - Given the indirect nature of this analysis, a threshold of 1s (1000ms) is recommended.

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

[RegExQuickRef]: https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference
[RegEx101]: https://regex101.com
