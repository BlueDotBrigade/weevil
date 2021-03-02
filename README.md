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
var filter = Engine
   .UsingPath(@"C:\Temp\application.log")
   .Open()
   .Filter.Apply(FilterType.PlainText, new FilterCriteria("@Comment=Suspect"));

foreach (var record in filter.Results)
{
   Console.WriteLine($"{record.LineNumber} : {record.Metadata.Comment}");
}
```

Search the first 1000 records in the log file looking for serial numbers:

```CSharp
var engine = Engine
   .UsingPath(@"C:\Temp\application.log")
   .UsingLimit(maxRecords: 1000)
   .Open();

var results = engine.Filter
   .Apply(FilterType.RegularExpression, new FilterCriteria("SerialNumber=(?<Value>[a-zA-Z0-9]+)"))
   .Results;

foreach (var record in results)
{
   Console.WriteLine($"{record.CreatedAt} : {record.Content}");
}
```

### Environment Setup

- It is expected that the `%WEEVIL_PLUGINS_PATH%` [environment variable](https://en.wikipedia.org/wiki/Environment_variable#Windows) will point to a directory where additional plugins can be found.
   - For example: `C:\SourceCode\weevil-plugins\MyCompanyName.Weevil.Plugins\bin\x64\Debug`
