# Weevil

[![Latest Release](https://img.shields.io/github/release/BlueDotBrigade/Weevil.svg?label=Latest%20Release)](https://github.com/BlueDotBrigade/weevil/releases/)
[![Master branch](https://github.com/BlueDotBrigade/weevil/actions/workflows/dotnet.yml/badge.svg)](https://github.com/BlueDotBrigade/weevil/actions/workflows/dotnet.yml)

- [What is Weevil?](#what-is-weevil)
- [Key Features](#key-features)
   - [Filtering](#filtering)
   - [Navigation](#navigation)
   - [Analysis](#analysis)
   - [Extensible Architecture](#extensible-architecture)
- [Software Development](#software-development)
   - [WPF Application](#wpf-application)
   - [NuGet Packages](#nuget-packages)
- [Development](#development)
   - [Guidelines](#guidelines)
   - [Compiling](#compiling)
   - [Verification](#verification)
- [Recognition](#recognition)
   - [Open Source Projects](#open-source-projects)
   - [Contributors](#contributors)

## What is Weevil?

![WeevilDemo](Doc/Notes/Release/v10_0_0/Weevil-Demo.gif)

_Weevil_ is an open-source .NET project that helps analysts extract valuable insights from log files. It's all about _boring log files for tasty bytes_.

A complete list of features can be found in the [release notes](https://github.com/BlueDotBrigade/weevil/releases).

## Key Features

1. File and Record Notes
     - Capture high-level observations as remarks, or low-level details as record comments.
2. Persisted State
     - Automatically load filter history, record comments, and file-level comments when opening a log file.
     - Share the application's state as an XML [sidecar][Sidecar] with colleagues.
3. Non-Destructive Operations
     - The _Weevil_ application ensures that the original log file is never modified.
4. Simplified Call Stacks
     - When a record includes an exception call stack, _Weevil_ simplifies it by displaying only business logic references.
5. Clear Operations
     - This operation removes records from memory, reducing RAM usage and speeding up filtering.

### Filtering

One or more filter criteria can be used to show or hide log file records.

1. Inclusive and Exclusive Filters
    - Display records matching the inclusive filter while hiding those matching the exclusive filter.
2. Filter Criteria
     1. Plain Text
     2. Regular Expressions
     3. Saved Keywords
          - Frequently used or complex filters can be saved as reusable keywords to speed up filtering.
          - For example, `#IpAddress` could be used as a shortcut for this filter: `^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$`.
     4. Built-in Keywords
         - Built-in keywords can be used to search metadata collected by _Weevil_.
         - For example, `@Comment` identifies records that have a user comment.
3. Multiple Criteria
     - Multiple filter criteria can be combined together using a logical "OR" operator (`||`).
4. Pinned Records
     - Pinned records are guaranteed to be included in the filter results.

### Navigation

1. Find
    - Search for text within filtered results.
2. Go To
    - Jump to specific line numbers or timestamps.
3. Pinned Records
    - Effortlessly navigate between important records.
4. Flagged Records
     - Move between records flagged during prior analysis.
5. Record Comments
    - Navigate between records containing user comments.

### Analysis

Use [regular expression][RegEx101] named groups to identify key data in log files. Then use _Weevil_'s analysis tools to extract data and identify trends.

Each analysis tool updates the `Comments` field with values that match the provided named group(s) and sets the record's `Flagged` field.

1. Annotate Records
   - Extract matched named-group values into record comments.
2. First Occurrence
   - Flag the first record for each unique captured value.
3. Last Occurrence
   - Flag the last record for each unique captured value.
4. Stable Value Runs
   - Flag the start and end of repeated value runs.
5. State Transitions
   - Flag when a captured value first appears or changes.
6. Rising Edges
   - Flag when a numeric value increases.
7. Falling Edges
   - Flag when a numeric value decreases.
8. Matching Record Runs
   - Flag runs of consecutive records that match a pattern.
9. Out-of-Order Timestamps
   - Flag records whose timestamps move backward unexpectedly.
10. Measure UI Thread Time
    - Flag records after unusually long UI thread delays.
11. Measure Elapsed Time
    - Calculate the time between consecutive records.
12. Calculate Statistics
    - Calculate summary statistics for selected records.

Furthermore, _Weevil_ supports:
- defining Regions of Interest (ROI)
- creating graphs using named groups

### Extensible Architecture

Maximize value by developing domain-specific extensions tailored to your business needs. _Weevil_ can be enhanced with custom plugins:

1. Log File Parsers
   - Create tailored parsers to accurately interpret log files from various sources and formats for seamless integration with _Weevil_.
2. Log File Analyzers
   - Design specialized analyzers to process and extract valuable insights from the parsed log data, optimizing the analysis for your specific business domain.
3. Dashboard Insights
   - Develop custom dashboard visualizations and insights that highlight the most relevant information, enabling efficient decision-making and a better understanding of your log data.

## Software Development

### WPF Application

- [Installation Guide][InstallationGuide]
- [Help Manual][Help]

### NuGet Packages

| Latest Release                                                                                                                                  | NuGet Package                       |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------- |
| [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Common)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Common)   | BlueDotBrigade.Weevil.Common.nupkg  |
| [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Core)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Core)       | BlueDotBrigade.Weevil.Core.nupkg    |
| [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Windows)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Windows) | BlueDotBrigade.Weevil.Windows.nupkg |

A .NET application can use *Weevil*'s feature set by directly referencing the `BlueDotBrigade.Weevil.Core` *NuGet* package.

For example, you can determine when equipment changed by using the following sample code:

```CSharp
var engine = Engine
   .UsingPath(@"C:\Temp\hardware.log")
   .Open();

// The `UniqueId` regular expression named group is used to
// capture hardware serial numbers.
engine.Filter.Apply(
   FilterType.RegularExpression,
   new FilterCriteria(@"Received hardware message. ID=(?<UniqueId>[a-zA-Z0-9]+)"));

// This type of analysis compares the captured serial numbers
// and flags the record when a value changes.
engine.Analyzer.Analyze(AnalysisType.StateTransitions);

foreach (var record in engine.Filter.Results.Where(r => r.Metadata.IsFlagged == true))
{
   Console.WriteLine(
   $"{record.CreatedAt} {record.Metadata.Comment}");
}
```

## Development

| Attribute                                                                                                                                                                                        | Description                                                                                                |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------- |
| [![GitHub Latest Release](https://img.shields.io/github/release/BlueDotBrigade/Weevil.svg?label=Latest%20Release)](https://github.com/BlueDotBrigade/weevil/releases)                                                   | The list of features & bug fixes for the latest *Weevil* release.                                          |
| [![Latest Stable](https://img.shields.io/badge/branch-Releases/2.x-blue?label=Release%20Branch)](https://github.com/BlueDotBrigade/weevil/tree/Releases/2.x)                                                            | Source code for the most stable version of *Weevil*.                                                       |
| [![Latest Code](https://img.shields.io/badge/branch-main-blue?label=Development%20Branch)](https://github.com/BlueDotBrigade/weevil/tree/main)                                                                              | The most up-to-date source code. This branch includes features that are still under development.           |
| [![Main branch](https://github.com/BlueDotBrigade/weevil/actions/workflows/dotnet.yml/badge.svg)](https://github.com/BlueDotBrigade/weevil/actions/workflows/dotnet.yml)                          | A value of `passing` indicates that the `main` branch is compiling and that the automated tests have passed. |
| [![GitHub Repository Size](https://img.shields.io/github/repo-size/BlueDotBrigade/Weevil)](https://github.com/BlueDotBrigade/Weevil)                                                             | Total size of *Weevil*'s Git repository.                                                                   |
| [![Last Commit](https://img.shields.io/github/last-commit/BlueDotBrigade/Weevil/main.svg)](https://github.com/BlueDotBrigade/weevil/commits/main)                                                | Indicates when the Git repository was last updated.                                                        |

### Guidelines

- When working on the WPF application, please follow the [Style Guide][StyleGuide] for the user interface.

### Compiling

The following steps outline how to build Weevil's WPF application:

1. Download the latest [stable release][StableCode] source code.
2. If you have implemented a custom *Weevil* plugin:
   - Prior to starting Visual Studio, create the following Windows [environment variable][EnvironmentVariable]:
      - `%WEEVIL_PLUGINS_PATH%` which refers to the directory where the Weevil plugin assembly (`*.dll`) can be found.
3. Using *Visual Studio*, compile the WPF project: `BlueDotBrigade.Weevil.Gui`.
[EnvironmentVariable]: https://en.wikipedia.org/wiki/Environment_variable#Windows

### Verification

Software integrity is verified through a number of automated tests that can be found in the [/Weevil/Tst/][AutomatedTests] directory:

- `UnitTests`
- `FunctionalTests`

## Recognition

- [Metalama](https://www.postsharp.net/metalama)
   - *Metalama*'s [aspect-oriented][AOP] library helps simplify the codebase by reducing [boilerplate][]. Special thanks to the PostSharp Technologies team for creating this successor to PostSharp.
- [GitHub](https://www.GitHub.com)
   - Free Git repository hosting for this project and many others.

### Open Source Projects

- [Live Charts](https://github.com/beto-rodriguez/LiveCharts2)
    - Beto Rodriguez et al. have developed an impressive WPF charting library. We look forward to future releases.
- [Material Design in XAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
    - An excellent WPF library that helps standardize themes and improve the overall quality of an application's user interface.
- [Cocona](https://github.com/mayuki/Cocona)
    - Mayuki Sawatari et al. have created an excellent library for building .NET command-line applications.

### Contributors

A special thanks to everyone who has contributed to this project.

[InstallationGuide]: https://github.com/BlueDotBrigade/weevil/blob/Releases/2.x/Doc/Notes/Release/InstallationGuide.md
[Help]: https://github.com/BlueDotBrigade/weevil/blob/Releases/2.x/Doc/Notes/Release/Help.md
[StyleGuide]: https://github.com/BlueDotBrigade/weevil/blob/main/Doc/Notes/Design/UI/UserInterfaceStyleGuide.md

[RegEx101]: https://regex101.com/
[Sidecar]: https://en.wikipedia.org/wiki/Sidecar_file

[StableCode]: https://github.com/BlueDotBrigade/weevil/tree/Releases/2.x

[Boilerplate]: https://en.m.wikipedia.org/wiki/Boilerplate_code
[AOP]: https://en.m.wikipedia.org/wiki/Aspect-oriented_programming

[AutomatedTests]: https://github.com/BlueDotBrigade/weevil/tree/main/Tst
