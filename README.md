# Weevil

- [What is Weevil?](#what-is-weevil)
   - [General](#general)
   - [Filtering](#filtering)
   - [Navigation](#navigation)
   - [Analysis](#analysis)
   - [Plugin Architecture](#plugin-architecture)
- [How to use Weevil?](#how-to-use-weevil)
   - [WPF Application](#wpf-application)
   - [NuGet Packages](#nuget-packages)
- [Source Code](#source-code)
   - [Compiling](#compiling)
   - [Development](#development)

## What is Weevil?

[![GitHub Latest Release](https://img.shields.io/github/release/BlueDotBrigade/Weevil.svg)](https://github.com/BlueDotBrigade/weevil/releases/)
[![GitHub License](https://img.shields.io/github/license/BlueDotBrigade/Weevil.svg)](https://github.com/BlueDotBrigade/Weevil/blob/master/LICENSE.md)

*Weevil* is an extensible .NET open-source project that makes it easier for analysts to review log files. In other words, "_boring log files for tasty bytes_".  

For a list of the latest features, please refer to the [change log](https://github.com/BlueDotBrigade/weevil/releases).

### General

1. Supports record-level comments.
2. Persisted State
   - Filter history, user comments, and other metadata is automatically loaded when a log file is opened.
   - The application's state is stored as an XML [sidecar][Sidecar] which can be shared with colleagues.
3. All operations are non-destructive; the original log file will not be modified.

### Filtering

1. Inclusive & exclusive filtering
   - Is used to select or hide log file records.
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

### Plugin Architecture

Realize the greatest value by creating a business-domain specific *Weevil* plug-in which extends the application by creating custom:

1. log file parsers
2. log file analyzers
3. dashboard insight

## How to use Weevil?

### WPF Application

For more information, please refer to the following:

- [Installation Guide][InstallationGuide]
- [Help Manual][Help]

### NuGet Packages

| Latest Release | NuGet Package |
| --- | --- |
| [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Common)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Common) | BlueDotBrigade.Weevil.Common.nupkg |
| [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Core)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Core) | BlueDotBrigade.Weevil.Core.nupkg |
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

## Source Code

| Attribute | Description |
| --- | --- |
| [![GitHub Latest Release](https://img.shields.io/github/release/BlueDotBrigade/Weevil.svg)](https://github.com/BlueDotBrigade/weevil/releases) | Latest version of *Weevil* to be released to production. |
| [![Latest Stable](https://img.shields.io/badge/branch-Releases/2.x-blue)](https://github.com/BlueDotBrigade/weevil/tree/Releases/2.x) | Release branch for the most stable version of *Weevil*. |
| [![Latest Code](https://img.shields.io/badge/branch-main-blue)](https://github.com/BlueDotBrigade/weevil/tree/main) | Development branch for the *Weevil* source code. |
| [![GitHub Repository Size](https://img.shields.io/github/repo-size/BlueDotBrigade/Weevil)](https://github.com/BlueDotBrigade/Weevil) | Total size of *Weevil*'s Git repository. |
| [![Lines of code](https://img.shields.io/tokei/lines/github/BlueDotBrigade/Weevil.svg)](https://github.com/BlueDotBrigade/weevil/) | Total number of lines of code. |
| [![Last Commit](https://img.shields.io/github/last-commit/BlueDotBrigade/Weevil/main.svg)](https://github.com/BlueDotBrigade/weevil/commits/main) | Indicates when the repository was last updated. |

### Compiling

The following steps outline how to build Weevil's WPF application:

1. Download the latest [stable release][StableCode] source code.
2. If you have implemented a custom *Weevil* plugin:
   - Prior to starting Visual Studio, create the following Windows [environment variable][EnvironmentVariable]:
      - `%WEEVIL_PLUGINS_PATH%` which refers to the directory where the Weevil plugin assembly (`*.dll`) can be found.
3. Using *Visual Studio*, compile the WPF project: `BlueDotBrigade.Weevil.Gui`.

### Development

- When working on the WPF application, please be sure to follow the [Style Guide][StyleGuide] for the user interface.

[EnvironmentVariable]: https://en.wikipedia.org/wiki/Environment_variable#Windows

[InstallationGuide]: https://github.com/BlueDotBrigade/weevil/blob/Releases/2.x/Doc/Notes/Release/InstallationGuide.md
[Help]: https://github.com/BlueDotBrigade/weevil/blob/Releases/2.x/Doc/Notes/Release/Help.md
[StyleGuide]: https://github.com/BlueDotBrigade/weevil/blob/main/Doc/Notes/Design/UI/UserInterfaceStyleGuide.md

[RegEx101]: https://regex101.com/
[Sidecar]: https://en.wikipedia.org/wiki/Sidecar_file

[StableCode]: https://github.com/BlueDotBrigade/weevil/tree/Releases/2.x
