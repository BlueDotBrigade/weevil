# Weevil

- [What is Weevil?](#what-is-weevil)
   - [General](#general)
   - [Filtering](#filtering)
   - [Navigation](#navigation)
   - [Analysis](#analysis)
   - [Plugin Architecture](#plugin-architecture)
- [Getting Started](#getting-started)
   - [WPF Application](#wpf-application)
   - [NuGet Packages](#nuget-packages)
- [Development](#development)
   - [Guidelines](#guidelines)
   - [Compiling](#compiling)
- [Recognition](#recognition)
   - [Open Source Projects](#open-source-projects)
   - [Contributors](#contributors)

## What is Weevil?

[![Release Notes](https://img.shields.io/github/release/BlueDotBrigade/Weevil.svg?label=Release%20Notes)](https://github.com/BlueDotBrigade/weevil/releases/)
[![NuGet Package](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Core?label=NuGet%20Package)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Core)
[![Build Status](https://github.com/BlueDotBrigade/weevil/actions/workflows/dotnet.yml/badge.svg)](https://github.com/BlueDotBrigade/weevil/actions/workflows/dotnet.yml)

![(Weevil demo: your app does not support *.Gif)](Doc/Notes/Release/v10_0_0/Weevil-Demo.gif)

*Weevil* is an extensible .NET open-source project that makes it easier for analysts to review log files. In other words, "_boring log files for tasty bytes_".  

For a list of the latest features, please refer to the [release notes](https://github.com/BlueDotBrigade/weevil/releases).

### General

1. File Level Remarks
   - Useful for making note of high level observations.
2. Record Level Comments
   - Useful when trying to understand individual log entries.
3. Persisted State
   - Filter history, user comments, and other metadata are automatically loaded when a log file is opened.
   - The application's state is stored as an XML [sidecar][Sidecar] which can be shared with colleagues.
4. Non-Destructive operations
   - The original log file is never modified.
5. Pinned Records
   - Ensures that certain records are always included in the filter results. 
6. Simplified Exception Callstack
   - Calls to .NET library methods are automatically removed from call stacks, thus making it easier to focus on the business domain logic. 
   - Full callstack is still available. 
7. Clear Operations
   - Can be used to reduce RAM footprint, speed up filtering, and reduce visual noise. 


### Filtering

1. Inclusive & exclusive filtering
   - Can be used to quickly select or hide records.
2. Pinned Records
   - Guarantees that specific records always appear in the filter results.
3. Filter Aliases
   -  An alias can be added to _Weevil_ to make it easier to reference complex and/or frequently used filters.
4. Monikers
   - Provides support for querying *Weevil*'s metadata.
   - For example: `@Comment` can be used to retrieve records that have a user comment.
5. In-memory
   - The entire log file is loaded into RAM to facilitate fast searches.

### Navigation

1. Find
   - Search for text in filter results.
2. Go To
   - Jump to a specific line number.
   - Jump to a specific timestamp.
3. Pinned Records
   - Quickly navigate between records of interest.
4. Flagged Records
   - Navigate between records flagged during previous analysis. 
5. Record Comments
   - Navigate between records that have a user comment. 

### Analysis

[Regular expression][RegEx101] named groups can used to identify key data within the log file.  *Weevil*'s analysis tools can then be used to extract data and/or identify trends.

1. Detect Data
   - `Comments` field is updated with values that match the provided named group(s).
   - `Flagged` field is set to `True` for matching records. 
   - Example: extracting URLs from a log file.
2. Detect Data Transitions
   - `Comments` field is updated when the matching value changes.
   - `Flagged` field is set to `True` for matching records.
   - Example: detecting when hardware serial numbers change. 
3. Detect Rising Edges
   - `Comments` field is updated when the matching value is higher that the previously detected value.
   - `Flagged` field is set to `True` for matching records.
   - Example: detecting peek CPU usage in a log file
4. Detect Falling Edges:
   - `Comments` field is updated when the matching value is lower that the previously detected value.
   - `Flagged` field is set to `True` for matching records.
   - Example: firmware's uptime value has reset to zero
5. Detect Temporal Anomaly:
   - `Comments` field is updated when record timestamps appear out of order.
   - `Flagged` field is set to `True` for matching records.
   - Example: 7th record is logged at `10:30 AM`, and the 8th record is logged at `10:15 AM`
6. Charts:
   - Regular expression _named groups_ can be used to extract values and generate a line graph.

### Plugin Architecture

Realize the greatest value by creating a business-domain specific *Weevil* plugin which extends the application by creating custom:

1. log file parsers
2. log file analyzers
3. dashboard insight

## Getting Started

### WPF Application

For more information, please refer to the following:

- [Installation Guide][InstallationGuide]
- [Help Manual][Help]

### NuGet Packages

| Latest Release                                                                                                                                  | NuGet Package                       |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------- |
| [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Common)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Common)   | BlueDotBrigade.Weevil.Common.nupkg  |
| [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Core)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Core)       | BlueDotBrigade.Weevil.Core.nupkg    |
| [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Windows)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Windows) | BlueDotBrigade.Weevil.Windows.nupkg |

A .NET application can use *Weevil*'s feature set by directly referencing the `BlueDotBrigade.Weevil.Core` *NuGet* package.

For example, one could determine when equipment was changed using the following sample code:

```CSharp
var engine = Engine
   .UsingPath(@"C:\Temp\hardware.log")
   .Open();

// The `UniqueId` regular expression named group is used to
// capture serial hardware serial numbers.
engine.Filter.Apply(
   FilterType.RegularExpression,
   new FilterCriteria(@"Received hardware message. ID=(?<UniqueId>[a-zA-Z0-9]+)"));

// This type of analysis compares the captured serial numbers,
// and flags the record when a value changes.
engine.Analyzer.Analyze(AnalysisType.DetectDataTransition);

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
| [![Build Status](https://github.com/BlueDotBrigade/weevil/actions/workflows/dotnet.yml/badge.svg)](https://github.com/BlueDotBrigade/weevil/actions/workflows/dotnet.yml)                        | A value of `passing` indicates that the `main` branch is compiling & that the automated tests have passed. |
| [![Security Analysis](https://github.com/BlueDotBrigade/weevil/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/BlueDotBrigade/weevil/actions/workflows/codeql-analysis.yml) | The result of GitHub's most recent security analysis of the Weevil code base.                              |
| [![GitHub Repository Size](https://img.shields.io/github/repo-size/BlueDotBrigade/Weevil)](https://github.com/BlueDotBrigade/Weevil)                                                             | Total size of *Weevil*'s Git repository.                                                                   |
| [![Lines of code](https://img.shields.io/tokei/lines/github/BlueDotBrigade/Weevil.svg)](https://github.com/BlueDotBrigade/weevil/)                                                               | Total number of lines of code in the Git repository.                                                       |
| [![Last Commit](https://img.shields.io/github/last-commit/BlueDotBrigade/Weevil/main.svg)](https://github.com/BlueDotBrigade/weevil/commits/main)                                                | Indicates when the Git repository was last updated.                                                        |

### Guidelines

- When working on the WPF application, please be sure to follow the [Style Guide][StyleGuide] for the user interface.

### Compiling

The following steps outline how to build Weevil's WPF application:

1. Download the latest [stable release][StableCode] source code.
2. If you have implemented a custom *Weevil* plugin:
   - Prior to starting Visual Studio, create the following Windows [environment variable][EnvironmentVariable]:
      - `%WEEVIL_PLUGINS_PATH%` which refers to the directory where the Weevil plugin assembly (`*.dll`) can be found.
3. Using *Visual Studio*, compile the WPF project: `BlueDotBrigade.Weevil.Gui`
[EnvironmentVariable]: https://en.wikipedia.org/wiki/Environment_variable#Windows

## Recognition

- [PostSharp](https://www.postsharp.net/)
   - *PostSharp*`s aspect oriented library helps to simplify a code base by reducing boiler plate code. A special thanks for the donated license.
- [GitHub](https://www.GitHub.com)
   - Free Git repository hosting platform for this project & many others like it.

### Open Source Projects

- [Live Charts](https://github.com/beto-rodriguez/LiveCharts2)
  - Beto Rodriguez et al. have developed an impressive WPF charting library. Am looking forward to future releases. 
- [Material Design in XAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
   - An excellent WPF library that helps to standardize themes & improve the overall quality of an application's user interface.  

### Contributors

A special thanks to all of those who have contributed to this project. 

[InstallationGuide]: https://github.com/BlueDotBrigade/weevil/blob/Releases/2.x/Doc/Notes/Release/InstallationGuide.md
[Help]: https://github.com/BlueDotBrigade/weevil/blob/Releases/2.x/Doc/Notes/Release/Help.md
[StyleGuide]: https://github.com/BlueDotBrigade/weevil/blob/main/Doc/Notes/Design/UI/UserInterfaceStyleGuide.md

[RegEx101]: https://regex101.com/
[Sidecar]: https://en.wikipedia.org/wiki/Sidecar_file

[StableCode]: https://github.com/BlueDotBrigade/weevil/tree/Releases/2.x
