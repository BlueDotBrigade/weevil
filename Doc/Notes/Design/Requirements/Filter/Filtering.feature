Feature: Filtering

REQUIREMENTS

#406

1. The system shall support filtering using 1 or more expressions delimited by a logical OR operator (`||`). #406
2. The system shall support filtering using different types of expressions (text, alias, and moniker). #398
3. When an `include` filter is applied, the system displays all matching records in the results. #399
4. When an `exclude` filter is applied, the system omits all matching records from the results. #400
5. When both `include` and `exclude` filters are applied, the system shall prioritize the 'Exclude' filter. #401
6. The system shall enable an exclude filter for `Trace` records by default.
7. The system shall enable an exclude filter for `Debug` records by default.
8. While the `case-sensitivity` option is turned out, text expressions shall be interpreted as case-insensitive.
9. While an analyst is creating a filter, the system shall automatically activate the filter 3 seconds after the analyst ceases typing.
10. While a record is `pinned`, the system shall always display pinned records in the results, regardless of any filters that are applied.
11. The system shall display the number of records in the filter results in the status bar.

Feature: Filtering

@SRS:406
Scenario: Filtering using multiple expressions seprated by OR
  Given Weevil has opened the file "Default.log"
  When the inclusive filter is: Directives||Fatal
  Then there will be 8 records in the filter results
    And each result will include either
    """
    Directives
    Fatal
    """

@SRS:398, @SRS:406
Scenario: Filtering using a text expression
  Given Weevil has opened the file "Default.log"
  When the inclusive filter is: Directives
  Then there will be 7 records in the filter results
    And each result will include the text "Directives"

@SRS:398, @SRS:406
Scenario: Filtering using an alias expression
  Given Weevil has opened the file "Default.log"
  When the inclusive filter is: #Fatal
  Then there will be 1 record in the filter results
    And each result will include the text "Unrecoverable error has occurred"

@SRS:398, @SRS:406
Scenario: Filtering using a moniker expression
  Given Weevil has opened the file "Default.log"
  When the inclusive filter is: @Severity=Information
  Then there will be 36 records in the filter results
    And each result will include the text "Info"

@SRS:399
Scenario: Include filter identifies important information
  Given Weevil has opened the file "Default.log"
  When the include filter is: #Information
  Then there will be 36 records in the filter results
    And each result will the text "Info"

@SRS:400
Scenario: Exclude filter hides records
  Given Weevil has opened the file "Default.log"
  When the exclude filter is: Security||Scomp||Core
  Then there will be 352 records in the filter results
    And each result will not include either
    """
    Security
    Scomp
    Core
    """

@SRS:401
Scenario: Exclude filter takes precedence over include filter
  Given Weevil has opened the file "Default.log"
  When the include filter is: #Information
    And the exclude filter is: Security||Scomp||Core
  Then there will be 10 records in the filter results
    And each result will the text "Info"
    And each result will not include either
    """
    Security
    Scomp
    Core
    """