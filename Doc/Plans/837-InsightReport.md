# 837: Build the Insight Report as a Model, Then Render It

> **Status:** Done
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Issue #837, under #835 ("Review how output is generated"). It follows the research comment on #837, with the decisions the owner approved on 2026-11-12. It replaces draft pull request #843, which was six months behind `main` and built five renderers.

## Goal

- The insight report is built once, as an `InsightReport` object, and then rendered: as Markdown for people, and as JSON for programs.
- The CLI's `Insight` command, the GUI dashboard's Copy and PowerShell's `Get-Insight` use the same model.
- JSON comes from the model, never from headings and lines run through a formatter.

## Where Things Stand

- `InsightReportGenerator.Generate` (Common) builds a Markdown string directly. Only the dashboard's Copy uses it.
- The CLI's `Insight` command writes its own headings and tab-separated lines through `Write` and the formatter `--output-as` chose. `--verbose` adds the log's metrics and every insight.
- `--output-as` offers Markdown, plain text, HTML, JSON and XML formatters. Their JSON and XML wrap each heading and line, so a program gets formatting fragments, not data.
- `Get-Insight` writes each `IInsight` to the pipeline.

## Decisions

Approved by the owner on 2026-11-12.

1. **One model, which is the contract.** `InsightReport` holds what a program needs, not what the Markdown happens to show:
   - about the report: the source file, the context as names and values, Weevil's version, when it was generated (with its offset), the period analyzed (`null` when it is not known), and a schema version. No display text, such as a title: renderers write their own;
   - the log's metrics: the record count, the file size, and the filter's metrics, such as the count per severity;
   - every insight, each with its attention flag. A program filters by the flag; the human output shows what it shows today.
2. **An insight's related records are their line numbers, never their text.** `IInsight` already keeps only line numbers (#1139), so building a report never reads the log, and cannot fail in disk mode when the log changes. The output stays small, and record text stays out of a report that may be pasted or sent elsewhere. A timestamp would need the log's records, so it is left out.
3. **Two renderers: Markdown and JSON.** XML and HTML are dropped until someone needs them.
4. **The JSON is a contract:** camelCase names, ISO 8601 times, `null` for an unknown period, the version as a string, readable Unicode, and a schema version. `Doc/Design` describes it.
5. **The format is chosen once.** `Program` parses `--output-as` into an output format that the commands receive. An unsupported value is an error naming the supported ones.
6. **`Get-Insight` returns the model.** PowerShell users get the same data as the CLI's JSON, as objects. This changes the type of its output.
7. **Plugins keep working.** `InsightReportGenerator.Create` returns the model. `Generate` keeps returning the same Markdown string, marked `[Obsolete]`.

## Steps

One pull request each, in order.

1. **The model and the Markdown renderer. Nothing visible changes.**
   - `InsightReport`, `InsightReportItem` and `InsightReportMetric` in Common, with `InsightReportGenerator.Create`.
   - `MarkdownInsightReportRenderer` produces exactly what `Generate` produced; `Generate` calls it, and is marked `[Obsolete]`. The dashboard's Copy uses `Create` and the renderer.
   - The CLI's `Insight` command builds the model, and writes its usual output from it, the verbose metrics included.
   - Tests: the Markdown is unchanged (a golden test); the model splits insights by their flag, carries no record text, and keeps the metrics in order.
2. **JSON.** `JsonInsightReportRenderer`, with the contract above, described in [InsightReport.md](../Design/InsightReport.md); the CLI's `Insight` writes the whole report as JSON for `--output-as json`, and its usual lines otherwise; `Get-Insight` returns the model. Tests: a JSON contract test, and a CLI functional test of `insight --output-as json`.
   - The format is resolved once, into an `OutputFormat` that `Program` gives the commands. Rejecting an unsupported value waits for step 3.
   - A metric's time that is not known (`Record.CreationTimeUnknown`) is `null`, as the period's is (decision 4). Weevil's own filter metrics are counts and hold no time; a plugin's metric collector may return one. The CLI's `--verbose` would show it as "unknown", rather than 12/31/9999 23:59:59.
   - The CLI writes JSON as UTF-8, so a program reads its text, such as "Σ", even on Windows.
3. **Remove the formatter-based XML, HTML and JSON output.** `--output-as` accepts what the commands support, and rejects anything else with a message. This step changes behaviour, so it goes last.
   - `OutputAs.ResolveFormatter` has no caller after step 2: remove it, and move its tests of `--output-as=json` and `--OutputAs` to `ResolveFormat`.
   - Done: `--output-as` accepts `markdown`, `plaintext` and `json`; any other value, or none, is an error naming them, with exit code 1. `encrypt` refuses `json`, since it writes text. `HtmlFormatter`, `XmlFormatter` and `JsonFormatter` are removed. [InsightReport.md](../Design/InsightReport.md) describes the result.

## Not in This Plan

- Warnings and errors in the report (`Diagnostics`, step 8 of the research): add them when a report has one to carry.
- Other commands' output, such as `Filter`'s results file, which holds records, not a report.
