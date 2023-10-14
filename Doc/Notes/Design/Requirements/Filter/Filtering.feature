Feature: Filtering

REQUIREMENTS

#406

1. Weevil shall support filtering using 1 or more expressions delimited by a logical OR operator (`||`). #406
2. Weevil shall support filtering using different types of expressions (text, alias, and moniker). #398
3. When an `include` filter is applied, Weevil will display all matching records in the results. #399
4. When an `exclude` filter is applied, Weevil omits all matching records from the results. #400
5. When both `include` and `exclude` filters are applied, Weevil shall prioritize the 'Exclude' filter. #401
6. While a record is `pinned`, Weevil shall always display the pinned record in the results, regardless of any filters that are applied. #371
7. When the filter results are displayed, Weevil shall update the status bar with the number of records in the results. #408
8. When typing of a filter has stopped for 3 seconds (±1), Weevil shall automatically apply the filter.

Filter Options

1. While the `case-sensitivity` option is turned out, text expressions shall be interpreted as case-insensitive.
2. Weevil shall have text expression option of either "plain text" or "regular expression".
3. While the `hide trace` option turned on, when filtering, Weevil shall implicitly apply a `@Severity=Trace` exclude filter.
4. While the `hide debug` option turned on, when filtering, Weevil shall implicitly apply a `@Severity=Debug` exclude filter.
5. While the `show pinned` option turned off, when filtering, Weevil shall ignore the `pinned` status of records.

Feature: Filtering

SRS: Weevil shall support filtering using 1 or more expressions delimited by a logical OR operator (`||`).

@SRS:406, @UserStory:123
Scenario: Mltiple filter expressions seprated by OR
  Given Weevil has opened the file "Default.log"
  When applying the include filter: Directives||Fatal
  Then the results will include 8 records
    And each result will include either
    """
    Directives
    Fatal
    """

@SRS:398, @SRS:406
Scenario: Filtering using a text expression
  Given Weevil has opened the file "Default.log"
  When applying the include filter: Directives
  Then the results will include 7 records
    And each result will include the text "Directives"

@SRS:398, @SRS:406
Scenario: Filtering using an alias expression
  Given Weevil has opened the file "Default.log"
  When applying the include filter: #Fatal
  Then the results will include 1 records
    And each result will include the text "Unrecoverable error has occurred"

@SRS:398, @SRS:406
Scenario: Filtering using a moniker expression
  Given Weevil has opened the file "Default.log"
  When applying the include filter: @Severity=Information
  Then the results will include 36 records
    And each result will include the text "Info"

@SRS:399
Scenario: Include filter identifies important information
  Given Weevil has opened the file "Default.log"
  When applying the include filter: #Information
  Then the results will include 36 records
    And each result will the text "Info"

@SRS:400
Scenario: Exclude filter hides records
  Given Weevil has opened the file "Default.log"
  When applying the exclude filter: Security||Scomp||Core
  Then the results will include 352 records
    And each result will not include either
    """
    Security
    Scomp
    Core
    """

@SRS:401
Scenario: Exclude filter takes precedence over include filter
  Given Weevil has opened the file "Default.log"
  When applying the include filter: #Information
    And applying the exclusive filter: Security||Scomp||Core
  Then the results will include 10 records
    And each result will the text "Info"
    And each result will not include either
    """
    Security
    Scomp
    Core
    """
@SRS:371
Scenario: Pinned records are always displayed
  Given Weevil has opened the file "Default.log"
  When pinning record ID 2
    And pinning record ID 4
    And pinning record ID 8s
    And applying the include filter: NoRecordMatches
  Then the results will include 3 records
    And record ID 2 will be visible
    And record ID 4 will be visible 
    And record ID 8 will be visible 

@SRS:408
Scenario: Status bar displays number of records in results
  Given Weevil has opened the file "Default.log"
  When applying the include filter: #Information
  Then the status bar visible record count will display 36