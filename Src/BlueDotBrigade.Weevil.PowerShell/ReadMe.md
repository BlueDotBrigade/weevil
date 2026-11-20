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

With `-Storage Disk`, the records `Get-Records` returns read their text from the log when the pipeline uses them. If the log is truncated, replaced or deleted before then, those reads fail. If the log is renamed, deleted or replaced while it is filtered, the cmdlet stops with an error that says to run it again. A filter that could finish, for example in RAM, keeps its results and writes a warning.

## A Log Another Weevil Has Open

`Get-Records` and `Get-Insight` keep the log locked while they run, as Weevil's window does while a log is open. A log another Weevil has open stops the cmdlet with a terminating error, of category `ResourceBusy`, that names who has it: *app.log is already open in Weevil by jsmith on PC-42.*

## One Region

`-Region` opens only one region, one of those the log's sidecar lists, so a large log opens faster:

```PowerShell
Get-Records -FilePath .\Sample.log -Include Error -Region "Startup"
```

## One Section

`-Section` opens only one level-1 section of the log's table of contents, with the sections nested in it. It runs to the line before the next level-1 section, or to the end of the log:

```PowerShell
Get-Records -FilePath .\Sample.log -Include Error -Section "Startup"
```
