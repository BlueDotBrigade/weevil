# Weevil

| Package | Latest Release | Downloads |
| --- | --- | --- |
| BlueDotBrigade.Weevil.Common | [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Common)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Common) | [![downloads](https://img.shields.io/nuget/dt/BlueDotBrigade.Weevil.Common)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Common) |
| BlueDotBrigade.Weevil.Core | [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Core)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Core) | [![downloads](https://img.shields.io/nuget/dt/BlueDotBrigade.Weevil.Core)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Core) |
| BlueDotBrigade.Weevil.Windows | [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Windows)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Windows) | [![downloads](https://img.shields.io/nuget/dt/BlueDotBrigade.Weevil.Windows)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Windows) |

## For Developers


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
	.UsingPath(InputData.GetFilePath(@"C:\Temp\hardware.log"))
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

### Environment Setup

- It is expected that the `%WEEVIL_PLUGINS_PATH%` [environment variable](https://en.wikipedia.org/wiki/Environment_variable#Windows) will point to a directory where additional plugins can be found.
   - For example: `C:\SourceCode\weevil-plugins\MyCompanyName.Weevil.Plugins\bin\x64\Debug`
