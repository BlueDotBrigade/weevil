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
- [Build & Compile](#build--compile)

## What is Weevil?

[![GitHub Latest Release](https://img.shields.io/github/release/BlueDotBrigade/Weevil.svg)](https://GitHub.com/dotnet/BlueDotBrigade/releases/)
[![GitHub License](https://img.shields.io/github/license/BlueDotBrigade/Weevil.svg)](https://github.com/BlueDotBrigade/Weevil/blob/master/LICENSE)

*Weevil* is an extensible open-source project which provides a set of tools to facilitate log file analysis, and automatically detect known issues.  In other words, "_boring log files for tasty bytes_".  

For a list of the latest features, please see the [change log][ChangeLog].

### General

1. Supports record-level comments.
2. All operations are non-destructive; the original log file will not be modified.
3. Persisted State
   - Filter history, user comments, and other metadata is automatically loaded when a log file is opened.
   - The application's state is stored as an XML [sidecar][Sidecar] which can be shared with colleagues.

### Filtering

1. Inclusive & exclusive filtering
   - Is used to select or hide log file records.
2. Pinned Records
   - Guarantees that specific records always appear in the filter results.
3. Static Aliases
   - Long and/or complex filters can be added to *Weevil* as static aliases.
4. Monikers
   - Provides support for querying *Weevil*'s metadata.
   - For example: `@Comment` can be used to retrieve records that have a user comment.
5. In-memory
   - The entire log file is loaded into RAM to facilitate fast searches.

### Navigation

1. Find
   - Search filter results for a specific value.
2. Go To
   - Jump to a specific line number.
   - Jump to a specific timestamp.
3. Pinned Records
   - Quickly navigate between records of interest.

### Analysis

[Regular expression][RegEx101] named groups can used to identify key data within the log file.  *Weevil*'s analysis tools can then be used to extract data and/or identify trends.

1. Detect Data
   - Matching values are extracted and appended to the the `Comments` field.
2. Detect Data Transitions
   - `Comments` field is updated when the matching value changes.
   - Example: detecting when
3. Detect Rising Edges
   - `Comments` field is updated when the matching value is higher that the previously detected value.
   - Example: detecting peek CPU usage in a log file
4. Detect Falling Edges: results are only copied when a numerical value decreases
   - `Comments` field is updated when the matching value is lower that the previously detected value.
   - Example: firmware's uptime value has reset to zero

### Plugin Architecture

Realize the greatest value by creating a business-domain specific *Weevil* plug-in which extends the application by providing custom:

1. static aliases
2. log file analysis
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

// The `UniqueId` regular expression named group is used to capture serial hardware serial numbers.
engine.Filter.Apply(
   FilterType.RegularExpression,
   new FilterCriteria(@"Received hardware message. ID=(?<UniqueId>[a-zA-Z0-9]+)"));

// This type of analysis compares the captured serial numbers, 
// and flags the record when a value changes.
engine.Analyzer.Analyze(AnalysisType.DetectDataTransition);

foreach (var record in engine.Filter.Results.Where(r => r.Metadata.IsFlagged == true))
{
   Console.WriteLine($"{record.CreatedAt} {record.Metadata.Comment}");
}
```

## Build & Compile

| Attribute | Description |
| --- | --- |
| [![Last Commit](https://img.shields.io/github/last-commit/BlueDotBrigade/Weevil/main.svg)](https://github.com/BlueDotBrigade/weevil/commits/main) | Indicates when the repository was last updated. |
| [![Lines of code](https://img.shields.io/tokei/lines/github/BlueDotBrigade/Weevil.svg)](https://github.com/BlueDotBrigade/weevil/) | Approximate size of the code base. |
| [![GitHub Repository Size](https://img.shields.io/github/repo-size/BlueDotBrigade/Weevil)](https://github.com/BlueDotBrigade/Weevil) | Total size of the Git repository. |
| [![Latest Code](https://img.shields.io/badge/branch-main-blue)](https://github.com/BlueDotBrigade/weevil) | Source code under development. |
| [![Latest Stable](https://img.shields.io/badge/branch-Releases/2.x-blue)](https://github.com/BlueDotBrigade/weevil/tree/Releases/2.x) | Source code for most recent release. |
| [![GitHub Latest Release](https://img.shields.io/github/release/BlueDotBrigade/Weevil.svg)](https://github.com/BlueDotBrigade/weevil/releases) | Latest version of *Weevil* to be released to production. |

To compile the application...

1. Download the latest [stable release][StableCode] source code.
2. If you have implemented a custom *Weevil* plugin:
   - Prior to starting Visual Studio, create the following Windows [environment variable][EnvironmentVariable]:
      - `%WEEVIL_PLUGINS_PATH%` which refers to the directory where the Weevil plugin assembly (`*.dll`) can be found.
3. Using *Visual Studio*, compile the WPF project: `BlueDotBrigade.Weevil.Gui`.

During development:

- Ensure that changes to the WPF application UI follow the [Style Guide][StyleGuide]

[EnvironmentVariable]: https://en.wikipedia.org/wiki/Environment_variable#Windows

[ChangeLog]: https://github.com/BlueDotBrigade/weevil/blob/Releases/2.x/Doc/Notes/Release/ChangeLog.md

[InstallationGuide]: https://github.com/BlueDotBrigade/weevil/blob/Releases/2.x/Doc/Notes/Release/InstallationGuide.md
[Help]: https://github.com/BlueDotBrigade/weevil/blob/Releases/2.x/Doc/Notes/Release/Help.md
[StyleGuide]: https://github.com/BlueDotBrigade/weevil/blob/main/Doc/Notes/Design/UI/UserInterfaceStyleGuide.md

[RegEx101]: https://regex101.com/
[Sidecar]: https://en.wikipedia.org/wiki/Sidecar_file

[StableCode]: https://github.com/BlueDotBrigade/weevil/tree/Releases/2.x
