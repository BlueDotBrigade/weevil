# Weevil

- [What is Weevil?](#what-is-weevil)
   - [Features](#features)
      - [Filtering](#filtering)
      - [Navigation](#navigation)
      - [Analysis](#analysis)
      - [Other](#other)
- [Documentation](#documentation)
- [Software Development](#software-development)
   - [Environment Setup](#environment-setup)
   - [Sample Code](#sample-code)

## What is Weevil?

[![GitHub Latest Release](https://img.shields.io/github/release/BlueDotBrigade/Weevil.svg)](https://GitHub.com/dotnet/BlueDotBrigade/releases/)
[![GitHub License](https://img.shields.io/github/license/BlueDotBrigade/Weevil.svg)](https://github.com/BlueDotBrigade/Weevil/blob/master/LICENSE)

Weevil is an extensible open-source project that facilitates the analysis of log file data, while automatically detecting known issues. In other words, "_boring log files for tasty bytes_".

### Features

#### Filtering

1. Inclusive & exclusive filtering
   - Is used to select or hide log file records.
2. Static Aliases
   - Long and/or complex filters can be added to Weevil as static aliases.
3. Monikers
   - Provides suppport for querying Weevil's metadata.
   - For example: `@Comment` can be used to retrieve records that have a user comment.
4. Pinned Records
   - Guarantee that a specific record will always appear in the filter results.
5. In-memory
   - The entire log file is loaded into RAM to facilate fast searches.

#### Navigation

1. Find
   - Used to search filter results for a specific value.
2. Previous/Next Pinned Item
   - This feature is used to jump to/from records of interest.

#### Analysis

[Regular expression][RegEx101] named groups can used to identify key data within the log file.  Weevil's analysis tools can then be used to extract data and/or identify trends.

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

#### Other

1. Persisted State
   - Filter history, user comments, and other metadata is automatically loaded when a log file is re-opened.
   - The application's state is stored as XML which can be easily shared with colleagues.

## Documentation

- WPF Application [Help][Help] Manual
- [Change Log][ChangeLog]
- [Installation Guide][InstallationGuide]

## Software Development

[![GitHub Latest Release](https://img.shields.io/github/release/BlueDotBrigade/Weevil.svg)](https://GitHub.com/dotnet/BlueDotBrigade/releases/)
[![GitHub Repository Size](https://img.shields.io/github/repo-size/BlueDotBrigade/Weevil)](https://github.com/BlueDotBrigade/Weevil)

| NuGet Packages | Latest Release | Downloads |
| --- | --- | --- |
| BlueDotBrigade.Weevil.Common | [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Common)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Common) | [![downloads](https://img.shields.io/nuget/dt/BlueDotBrigade.Weevil.Common)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Common) |
| BlueDotBrigade.Weevil.Core | [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Core)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Core) | [![downloads](https://img.shields.io/nuget/dt/BlueDotBrigade.Weevil.Core)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Core) |
| BlueDotBrigade.Weevil.Windows | [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Windows)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Windows) | [![downloads](https://img.shields.io/nuget/dt/BlueDotBrigade.Weevil.Windows)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Windows) |

### Environment Setup

- It is expected that the `%WEEVIL_PLUGINS_PATH%` [environment variable](https://en.wikipedia.org/wiki/Environment_variable#Windows) will point to a directory where additional plugins can be found.
   - For example: `C:\SourceCode\weevil-plugins\MyCompanyName.Weevil.Plugins\bin\x64\Debug`

### Sample Code

Find all log entries that have a user comment that mentions the word `suspect`:

```CSharp

// Monikers like `@Comment` support querying of metadata collected by Weevil.
var filter = Engine
   .UsingPath(@"C:\Temp\RegressionTest.log")
   .Open()
   .Filter.Apply(FilterType.PlainText, new FilterCriteria("@Comment=Suspect"));

foreach (var record in filter.Results)
{
   Console.WriteLine($"{record.LineNumber} : {record.Metadata.Comment}");
}
```

Search the first 1000 records in the log file looking for sub-system initialization failures:

```CSharp
// Weevil's fluent API helps to capture the developer's intent within the code base.
var filter = Engine
   .UsingPath(@"C:\Temp\SubSystem.log")
   .UsingLimit(maxRecords: 1000)
   .Open();
   .Filter.Apply(FilterType.RegularExpression, new FilterCriteria("Initialization failure. Reason=([a-zA-Z]+)"));

foreach (var record in filter.results)
{
   Console.WriteLine($"{record.CreatedAt} : {record.Content}");
}
```

Determine when equipment was changed in production.

```CSharp
var engine = Engine
	.UsingPath(@"C:\Temp\hardware.log")
	.Open();

// The following regular expression includes a named group called: `SerialNumber`.
engine
	.Filter.Apply(
		FilterType.RegularExpression,
		new FilterCriteria(@"Received hardware message. ID=(?<SerialNumber>[a-zA-Z0-9]+)"));

// In this case the analysis uses the named group to:
// 1. identify serial numbers in the log file,
// 2. flag records when the serial number changes, and 
// 3. make note of the new serial number in the user comment field.
engine.Analyzer.Analyze(AnalysisType.DetectDataTransition);

foreach (var record in engine.Filter.Results.Where(x => x.Metadata.IsFlagged == true))
{
	Console.WriteLine($"{record.CreatedAt} {record.Metadata.Comment}");
}
```

[Help]: https://github.com/BlueDotBrigade/weevil/blob/Releases/2.x/Doc/Notes/Release/Help.md
[ChangeLog]: https://github.com/BlueDotBrigade/weevil/blob/Releases/2.x/Doc/Notes/Release/ChangeLog.md
[InstallationGuide]: https://github.com/BlueDotBrigade/weevil/blob/Releases/2.x/Doc/Notes/Release/InstallationGuide.md
[RegEx101]: https://regex101.com/