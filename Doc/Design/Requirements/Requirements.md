Feature: Filtering

REQUIREMENTS

1. The software shall support filtering using 1 or more expressions delimited by a logical OR operator (`||`). #406
2. The software shall support filtering using different types of expressions (text, alias, and moniker). #398
3. When an `include` filter is applied, the software shall display all matching records. #399
4. When an `exclude` filter is applied, the software shall omit all matching records. #400
5. When both `include` and `exclude` filters are applied, the software shall prioritize the 'Exclude' filter. #401
6. While the `case sensitive` option is on, when filtering, the software shall differentiate between upper and lower case characters. #420
7. The software shall allow the user to mark a record as pinned. #416
8. While the `persistent pins` option is `on` , when filtering, the software shall always display pinned records. #371
9. When the filtered records are displayed, the software shall show the number of visible records in the status bar. #408
10. While the `show debug` option is off, when filtering, the software will hide records with a `debug` severity. #418
11. While the `show trace` option is off, when filtering, the software will hide records with a `trace` severity. #419

1. The software shall have an option to use either `plain text` (default) or `regular expression` text expressions. #411
2. The software shall have an option to turn `case sensitive` filtering on (default) or off. #394
3. The software shall have an option to turn `show debug` records on (default) or off. #414
4. The software shall have an option to turn `show trace` records on (default) or off. #415
5. The software shall have an option to turn `persistent pins` on (default) or off. #417

## Performance

1. When a 20 GB reference log that has no saved index is opened, the software shall finish opening it within 120 seconds. #1084
2. When a log whose saved index matches the log is opened, the software shall open it without parsing the whole log again. #1085
3. While a reference log is open in RAM mode, the software shall use no more than 1.2 GB of working set per GB of log. #1086
4. While a reference log is open in disk mode, the software shall use no more than 137 MB of working set per GB of log. Provisional until the disk-mode proof of concept for #986. #1087
5. While a 20 GB reference log is open in disk mode on an NVMe drive, when a filter is applied, the software shall finish filtering within 10 seconds. #1088
6. While a reference log is open in RAM mode, when a filter is applied, the software shall finish filtering within 110% of the time Weevil 2.12.1 takes for the same log and filter. #1089

### Performance Reference Conditions

The performance requirements are measured under these conditions, through the engine API:

| | |
|---|---|
| Machine | Intel Core i9-9900K, 32 GB of RAM, NVMe SSD |
| Operating system | Windows 11 |
| Runtime | .NET 10, Release build |
| Reference log | A synthetic Weevil TSV log whose records average 131 bytes, as the [memory harness](../../Research/Weevil3/MemoryHarness/README.md) writes them (#978) |
| First open | The log has no saved index (`.idx` file) |
| RAM footprint | The working set after opening finishes |
| Filter duration | From applying the filter until its results are ready, for a plain-text and a regular-expression filter; in disk mode, with the operating system's file cache cold |

`Tst/BlueDotBrigade.Weevil.Core-PerformanceTests` checks them on smaller logs in CI. Each test names its requirement with `[WorkItem(N)]`.

For the most up-to-date documentation see:
https://github.com/BlueDotBrigade/weevil/issues/1090
Related design note: [VisualStudioTargetLocation.md](../../Research/VisualStudioTargetLocation/VisualStudioTargetLocation.md)
