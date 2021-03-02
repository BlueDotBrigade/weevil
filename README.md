# Weevil

| Package | Latest Release | Downloads |
| --- | --- | --- |
| BlueDotBrigade.Weevil.Common | [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Common)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Common) | [![downloads](https://img.shields.io/nuget/dt/BlueDotBrigade.Weevil.Common)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Common) |
| BlueDotBrigade.Weevil.Core | [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Core)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Core) | [![downloads](https://img.shields.io/nuget/dt/BlueDotBrigade.Weevil.Core)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Core) |
| BlueDotBrigade.Weevil.Windows | [![latest version](https://img.shields.io/nuget/v/BlueDotBrigade.Weevil.Windows)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Windows) | [![downloads](https://img.shields.io/nuget/dt/BlueDotBrigade.Weevil.Windows)](https://www.nuget.org/packages/BlueDotBrigade.Weevil.Windows) |

## For Developers

The following prints log entries that include a serial number (up to a maximum of 128 records):

```CSharp
var engine = Engine
   .UsingPath("C:\Temp\application.log")
   .UsingLimit(maxRecords: 128)
   .Open();

var results = engine
   .Filter.Apply(FilterType.RegularExpression, new FilterCriteria("SerialNumber=(?<Value>[a-zA-Z0-9]+)"))
   .Results;

foreach (var record in results)
{
   Console.WriteLine($"{record.LineNumber}: {record.Content}");
}
```

### Environment Setup

- It is expected that the `%WEEVIL_PLUGINS_PATH%` [environment variable](https://en.wikipedia.org/wiki/Environment_variable#Windows) will point to a directory where additional plugins can be found.
   - For example: `C:\SourceCode\weevil-plugins\MyCompanyName.Weevil.Plugins\bin\x64\Debug`
