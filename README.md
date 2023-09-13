# Weevil

[![Latest Release](https://img.shields.io/github/release/BlueDotBrigade/Weevil.svg?label=Latest%20Release)](https://github.com/BlueDotBrigade/weevil/releases/)
[![Build Status](https://github.com/BlueDotBrigade/weevil/actions/workflows/dotnet.yml/badge.svg)](https://github.com/BlueDotBrigade/weevil/actions/workflows/dotnet.yml)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=BlueDotBrigade_weevil&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=BlueDotBrigade_weevil)
[![Security Analysis](https://github.com/BlueDotBrigade/weevil/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/BlueDotBrigade/weevil/actions/workflows/codeql-analysis.yml)

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

_Weevil_ is an open-source .NET project that is used by analysts to extract valuable insights from log files.  It's all about "_boring log files for tasty bytes_".  

A complete list of features can be found in the [release notes](https://github.com/BlueDotBrigade/weevil/releases).

## Key Features

1. File and Record Level Notes
    - Capture high-level observations as remarks, or low-level details as record comments.
2. Persisted State
    - Automatically load filter history, record comments, and file level comments when opening a log file.
    - Share the application's state as an XML [sidecar][Sidecar] with colleagues.
3. Non-Destructive Operations
	 - The _Weevil_ application ensures that the original log file is never modified.
4. Simplified Callstacks
    - When a record includes an exception call stack, _Weevil_ simplifies the call stack by only displaying business logic references.
5. Clear Operations
	 - This operation removes records from memory, thus reducing the RAM footprint and speeding up the filtering process.

### Filtering

One or more filter criteria can be used to show or hide log file records.

1. Inclusive and Exclusive Filters
    - Display records matching the inclusive filter while hiding those matching the exclusive filter.
2. Filter Criteria
	 1. Plain Text
	 2. Regular Expressions
	 3. Aliases
		  - Frequently used or complex filters can be assigned a unique key that can be used to speed up the filtering process.
		  - For example, the `#IpAddress` key could be assigned to the following filter criteria  `^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$`.
	 4. Monikers
        - Monikers are built-in keys that can be used to query metadata collected by _Weevil_.
		  - For example, the `@Comment` can be used to identify records that have a user comment.
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

Utilize [Regular expression][RegEx101] named groups to identify key data in log files. Leverage _Weevil_'s analysis tools to then extract data and identify trends.

Each analysis tool updates the `Comments` fields with the values that match the provided named group(s), and the recor's `Flagged` field is set.

1. Detect Data
   - For example: extracting URLs from a log file
2. Detect Data Transitions
   - For example: when a hardware serial number changes
3. Detect Rising Edges
   - For example: detecting peek CPU usage
4. Detect Falling Edges
   - For example: detect when a firmware's uptime has reset
5. Detect Temporal Anomalies
   - For example: detect when records are logged out of order

Furthermore, _Weevil_ includes the ability to generated graphs based on the extracted data.

### Extensible Architecture

Maximize potential by developing domain-specific extensions tailored to your business' needs. _Weevil_ can be enhanced by custom plugins:

1. Log File Parsers
   - Create tailored parsers to accurately interpret log files from various sources and formats, ensuring seamless integration with _Weevil_.
2. Log File Analyzers
   - Design specialized analyzers to process and extract valuable insights from the parsed log data, optimizing the analysis for your specific business domain.
3. Dashboard Insights
   - Develop custom dashboard visualizations and insights that highlight the most relevant information, enabling efficient decision-making and improved understanding of your log data.

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
| [![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=BlueDotBrigade_weevil&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=BlueDotBrigade_weevil) | SonarCube: Number of security issues detected. |
|[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=BlueDotBrigade_weevil&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=BlueDotBrigade_weevil) | SonarCube: Number of security vulnerabilities detected |
| [![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=BlueDotBrigade_weevil&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=BlueDotBrigade_weevil) | SonarCube: Represents the project's SQALE rating. |
|[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=BlueDotBrigade_weevil&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=BlueDotBrigade_weevil)| SonarCube: Characteristics of the code base that suggest the design may have maintenance issues. |

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

### Verification

Software integrity is verified through a number of automated tests which can be found in the [/Weevil/Tst/][AutomatedTests] directory:

- `UnitTests`
- `FunctionalTests`

## Recognition

- [PostSharp](https://www.postsharp.net/)
   - *PostSharp*`s [aspect oriented][AOP] library helps to simplify a code base by reducing [boilerplate][]. Special thanks to the PostSharp team for donating a license.
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

[Boilerplate]: https://en.m.wikipedia.org/wiki/Boilerplate_code
[AOP]: https://en.m.wikipedia.org/wiki/Aspect-oriented_programming

[AutomatedTests]: https://github.com/BlueDotBrigade/weevil/tree/main/Tst
