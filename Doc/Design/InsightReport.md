# Insight Report

How Weevil builds an insight report and renders it (#837). The plan and its decisions are in [837-InsightReport.md](../Plans/837-InsightReport.md).

## One Model, Then a Renderer

`InsightReportGenerator.Create` (`BlueDotBrigade.Weevil.Analysis`, in Common) builds an `InsightReport` from an open log and its insights. It reads no record text: an insight keeps only the line numbers of its records (#1139), so building a report never reads the log, and cannot fail in disk mode when the log changes.

A renderer then turns the model into text:

- `MarkdownInsightReportRenderer`, for people: the dashboard's Copy.
- `JsonInsightReportRenderer`, for programs: the CLI's `insight --output-as json`.

| Caller | Output |
|---|---|
| Dashboard's Copy (GUI) | Markdown, for the insights shown |
| `WeevilCli.exe insight` | Its own lines, through the formatter `--output-as` chose: the insights that need attention, or with `--verbose`, the log's metrics and every insight |
| `WeevilCli.exe insight --output-as json` | The whole report as JSON, with or without `--verbose` |
| `Get-Insight` (PowerShell) | The `InsightReport` object |

`InsightReportGenerator.Generate` returns the Markdown directly, for plugins written before #837. It is marked `[Obsolete]`.

The CLI chooses the output format once: `Program` resolves `--output-as` into an `OutputFormat`, and gives it to the commands as `OutputOptions`, through Cocona's services. A document a command renders itself, such as the JSON, is written with `Write.Document`, which the formatter does not wrap.

## Scopes

- `recordCount` and `fileSize` describe the log as it was opened.
- `metrics` are the filter's metrics when the report was built. The CLI and `Get-Insight` apply no filter, so they describe every record.
- `insights`, `from` and `to` describe what the caller analyzed. The CLI and `Get-Insight` analyze the whole log, and estimate the period from its first and last timestamps.

## The JSON Contract

Programs rely on this shape. `JsonInsightReportRendererTests` checks it, property by property.

- Names are camelCase. A context's names and a metric's name are kept as they are.
- Times are ISO 8601. `generatedAt` has the offset of the machine that built the report; `from`, `to` and a metric's time are the log's own times, without an offset, since Weevil's parsers read none.
- A time that is not known is `null`.
- Text is written as it is, such as `Σ` and `'`, not escaped for a web page. Escape it before putting it in HTML. The CLI writes the JSON as UTF-8, without a byte order mark.
- `schemaVersion` changes when a property is removed, renamed or changes its meaning. Adding a property does not change it.

In this example, `Threshold` and `FirstOccurredAt` stand for a plugin's metrics.

```json
{
  "schemaVersion": 1,
  "sourceFileName": "Application.log",
  "context": {
    "Application": "Droid"
  },
  "weevilVersion": "3.0.1.0",
  "generatedAt": "2026-11-12T09:30:00-05:00",
  "from": "2026-05-06T10:00:00",
  "to": null,
  "recordCount": 13,
  "fileSize": 2048,
  "metrics": [
    { "name": "Errors", "value": 1 },
    { "name": "Threshold", "value": "00:00:05" },
    { "name": "FirstOccurredAt", "value": null }
  ],
  "insights": [
    {
      "title": "Critical Errors",
      "metricValue": "3",
      "metricUnit": "Σ",
      "details": "Critical errors found",
      "isAttentionRequired": true,
      "relatedLineNumbers": [ 42, 7 ]
    }
  ]
}
```

| Property | Type | Meaning |
|---|---|---|
| `schemaVersion` | number | The version of this shape: 1 |
| `sourceFileName` | string | The log's file name, without its folder |
| `context` | object | The log's context, such as the version of the application that wrote it, as names and string values. `{}` when it has none |
| `weevilVersion` | string | The version of Weevil that built the report: a .NET version in four parts |
| `generatedAt` | string | When the report was built |
| `from`, `to` | string or `null` | The period analyzed |
| `recordCount` | number | The number of records in the log |
| `fileSize` | number | The log's size, in bytes |
| `metrics` | array | The filter's metrics, in the order the filter gives them. Weevil's own are counts per severity: `Information`, `Warnings`, `Errors` and `Fatals`. A plugin can add others |
| `metrics[].value` | number, string or `null` | Weevil's own metrics are numbers. A plugin's value is written as System.Text.Json writes its type: a duration as `[-][d.]hh:mm:ss[.fffffff]`, such as `"00:00:05"`, and a time as above, `null` when it is not known |
| `insights` | array | Every insight, in the order Weevil shows them |
| `insights[].metricValue` | string | The insight's key figure, as Weevil shows it |
| `insights[].isAttentionRequired` | boolean | Whether the insight needs attention. Filter by it to get only the problems |
| `insights[].relatedLineNumbers` | array of numbers | The line numbers of the records the insight refers to, with the primary one first. Never their text |
