Feature: Filtering

REQUIREMENTS

1. The software shall support filtering using 1 or more expressions delimited by a logical OR operator (`||`). #406
2. The software shall support filtering using different types of expressions (text, alias, and moniker). #398
3. When an `include` filter is applied, the software shall display all matching records in the results. #399
4. When an `exclude` filter is applied, the software shall omit all matching records from the results. #400
5. When both `include` and `exclude` filters are applied, the software shall prioritize the 'Exclude' filter. #401
6. While the `case sensitive` option is on, when filtering, the software shall differentiate between upper and lower case characters. #420
6. The software shall allow the user to mark a record as pinned. #416
7. While the `persistent pins` option is `on` , when filtering, the software shall always include pinned records in the results. #371
8. When the filter results are displayed, the software shall update the status bar with the number of records in the results. #408
9. When typing of a filter has stopped for 3 seconds (±1), the software shall automatically apply the filter. #410
10. While the `show debug` option is off, when filtering, the software will hide records with a `debug` severity. #418
11. While the `show trace` option is off, when filtering, the software will hide records with a `trace` severity. #419


1. The software shall have an option to use either `plain text` (default) or `regular expression` text expressions. #411
2. The software shall have an option to turn `case sensitive` filtering on (default) or off. #394
3. The software shall have an option to turn `show debug` records on (default) or off. #414
4. The software shall have an option to turn `show trace` records on (default) or off. #415
5. The software shall have an option to turn `persistent pins` on (default) or off. #417


For the most up-to-date documentation see:
https://github.com/BlueDotBrigade/weevil/issues/413