# Weevil: Power Shell

Requires PowerShell 7.6 or later, the first release on .NET 10.

```PowerShell
# Script creates an instance of the Weevil engine.
.\Select-Records.ps1 -logFilePath .\Sample.log -Include Info

# Example of how to include special characters in predetermined PowerShell string.
.\Select-HttpErrors.ps1 -logFilePath .\Sample.log
```

## Insights

`Get-Insight` returns one report: the log's metrics, and every insight with whether it needs attention. It holds the same data as `WeevilCli.exe insight --output-as json`.

```PowerShell
$report = Get-Insight -FilePath .\Sample.log
$report.Metrics | Format-Table Name, Value
$report.Insights | Where-Object IsAttentionRequired | Format-List Title, MetricValue, MetricUnit, Details
```

An insight's `RelatedLineNumbers` are the line numbers of its records, never their text.

## A Log Larger Than Memory

`Get-Records` and `Get-Insight` open a log in RAM. `-Storage Disk` keeps only the record table in memory, and reads each record's text back from the log when it is needed:

```PowerShell
Get-Records -FilePath .\Sample.log -Include Error -Storage Disk
```

With `-Storage Disk`, the records `Get-Records` returns read their text from the log when the pipeline uses them. If the log is truncated, replaced or deleted before then, those reads fail. If the log changes while it is filtered, the cmdlet reloads it once and filters it again.