Feature: Filtering

REQUIREMENTS

When <trigger>, the <system name> shall <system response>.

1. The software shall support filtering using 1 or more expressions delimited by a logical OR operator (`||`). #406
2. The software shall support filtering using different types of expressions (text, alias, and moniker). #398
3. When an `include` filter is applied, the software shall display all matching records in the results. #399
4. When an `exclude` filter is applied, the software shall omit all matching records from the results. #400
5. When both `include` and `exclude` filters are applied, the software shall prioritize the 'Exclude' filter. #401
6. While the `case sensitive` option is on, when filtering, the software shall differentiate between upper and lower case characters. #420
6. The software shall allow the user to mark a record as pinned. #416
7. While the `persistent pins` option is `on` , when filtering, the software shall ALWAYS display pinned records in the results. #371
8. When the filter results are displayed, the software shall update the status bar with the number of records in the results. #408
9. When typing of a filter has stopped for 3 seconds (±1), the software shall automatically apply the filter. #410
10. While the `show debug` option is off, when filtering, the software will hide records with a `debug` severity. #418
11. While the `show trace` option is off, when filtering, the software will hide records with a `trace` severity. #419

Filter Options

1. The software shall have an option to use either `plain text` (default) or `regular expression` text expressions. #411
2. The software shall have an option to turn `case sensitive` filtering on (default) or off. #394
3. The software shall have an option to turn `show debug` records on (default) or off. #414
4. The software shall have an option to turn `show trace` records on (default) or off. #415
5. The software shall have an option to turn `persistent pins` on (default) or off. #417





Feature: Filtering

SRS: The software shall support filtering using 1 or more expressions delimited by a logical OR operator (`||`).

@SRS:406, @UserStory:123
Scenario: Two include filter expressions separated by OR
  Given that Weevil has opened the file "Default.log"
  When applying the include filter: Directives||Fatal
  Then the results will include 8 records
    And each result will include either
    """
    Directives
    Fatal
    """

@SRS:406, @UserStory:123
Scenario: Two exclude filter expressions separated by OR
  Given that Weevil has opened the file "Default.log"
  When applying the exclude filter: #Debug||#Trace
  Then the results will include 1234 records
    And each result will not include either
    """
    Directives
    Fatal
    """

@SRS:398, @SRS:406
Scenario: Filtering using a text expression
  Given that Weevil has opened the file "Default.log"
  When applying the include filter: Directives
  Then the results will include 7 records
    And each result will include the text "Directives"

@SRS:398, @SRS:406
Scenario: Filtering using an alias expression
  Given that Weevil has opened the file "Default.log"
  When applying the include filter: #Fatal
  Then the results will include 1 records
    And each result will include the text "Unrecoverable error has occurred"

@SRS:398, @SRS:406
Scenario: Filtering using a moniker expression
  Given that Weevil has opened the file "Default.log"
  When applying the include filter: @Severity=Information
  Then the results will include 36 records
    And each result will include the text "Info"

@SRS:399
Scenario: Include filter identifies important information
  Given that Weevil has opened the file "Default.log"
  When applying the include filter: #Information
  Then the results will include 36 records
    And each result will the text "Info"

@SRS:400
Scenario: Exclude filter hides records
  Given that Weevil has opened the file "Default.log"
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
  Given that Weevil has opened the file "Default.log"
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
  Given that Weevil has opened the file "Default.log"
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
  Given that Weevil has opened the file "Default.log"
  When applying the include filter: #Information
  Then the status bar visible record count will display 36

@SRS:410
Scenario: Filter automatically applied when typing pauses
  Given that Weevil has opened the file "Default.log"
  When entering the include filter: #Information
    And waiting 4 seconds
  Then the results will include 36 records

# 387 = all records
@SRS:410
Scenario: Filter is not automatically applied when typing continues
  Given that Weevil has opened the file "Default.log"
  When entering the include filter: #Error
    And waiting 1 seconds
    And entering the include filter: #Error||
    And waiting 1 seconds
    And entering the include filter: #Error||#Fatal
    And waiting 1 seconds
  Then the results will include all records

@SRS:411
Scenario: `Plain Text` filter mode selected by default
  Given that Weevil has opened the file "Default.log"
  Then the filter mode will be `Plain Text`

@SRS:411
Scenario: `Plain Text` filtering
  Given that Weevil has opened the file "Default.log"
  When selecting the plain text filter mode
    applying the include filter: Directives 
  Then the results will include 7 records
    And each result will include the text "Directives"

@SRS:411
Scenario: `Regular Expression` filtering
  Given that Weevil has opened the file "Default.log"
  When selecting the plain text filter mode
    applying the include filter: Voltage=\d{2}
  Then the results will include 1 records
    And each result will include the text "Voltage=51"

@SRS:394
Scenario: `Case Sensitive` plain text filtering
  Given that Weevil has opened the file "Default.log"
  When selecting the plain text filter mode
    And using case sensitive filtering
    And applying the include filter: directives
  Then the results will include 0 records

@SRS:394
Scenario: `Case Insensitive` plain text filtering
  Given that Weevil has opened the file "Default.log"
  When selecting the plain text filter mode
    And using case insensitive filtering
    And applying the include filter: directives
  Then the results will include 7 records
    And each result will include the text "Directives"

@SRS:394
Scenario: `Case Sensitive` regular expression filtering
  Given that Weevil has opened the file "Default.log"
  When selecting the regular expression filter mode
    And using case sensitive filtering
    And applying the include filter: directives
  Then the results will include 0 records

@SRS:394
Scenario: `Case Insensitive` regular expression filtering
  Given that Weevil has opened the file "Default.log"
  When selecting the regular expression filter mode
    And using case insensitive filtering
    And applying the include filter: directives
  Then the results will include 7 records
    And each result will include the text "Directives"

@SRS:414,418
Scenario `Show Debug` option hides trace records
  Given that Weevil has opened the file "Default.log"
  When the `Show Debug` filter option is off
    And applying the include filter: Diagnostics
  Then the results will include 19 records

@SRS:414,418
Scenario `Show Debug` option shows trace records 
  Given that Weevil has opened the file "Default.log"
  When the `Show Debug` filter option is on
    And applying the include filter: Core
  Then the results will include 22 records

@SRS:415,419
Scenario `Show Trace` option hides trace records
  Given that Weevil has opened the file "Default.log"
  When the `Show Trace` filter option is off
    And applying the include filter: Diagnostics
  Then the results will include 3 records

@SRS:415,419
Scenario `Show Trace` option displays trace records 
  Given that Weevil has opened the file "Default.log"
  When the `Show Trace` filter option is on
    And applying the include filter: Diagnostics
  Then the results will include 102 records