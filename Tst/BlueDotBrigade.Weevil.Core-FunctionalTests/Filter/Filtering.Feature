Feature: Filtering

@Requirement:365
Scenario: Filter log entries by hardware serial number
	Given that the default log file is open
	When the plain text filtering option is selected
	And the case sensitive option is on
	And the inclusive filter is applied `S/N`
	Then all records will include `S/N`



  Scenario: Displaying records that match an inclusive filter
    Given that the default log file is open
    And filters are available for the analyst to use
    When the analyst applies an inclusive filter to the records
    Then the system should only display records that matches the filter criteria







@Requirement:406, @UserStory:123
Scenario: Two include filter expressions separated by OR
  Given that Weevil has opened the file "Default.log"
  When applying the include filter: Directives||Fatal
  Then the results will include 8 records
    And each result will include either
    """
    Directives
    Fatal
    """

@Requirement:399
Scenario: Include filter identifies important information
  Given that Weevil has opened the file "Default.log"
  When applying the include filter: #Information
  Then the results will include 36 records
    And each result will the text "Info"

@Requirement:400
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

@Requirement:401
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
@Requirement:371
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

@Requirement:414, @Requirement:418
Scenario: `Show Debug` option hides trace records
  Given that Weevil has opened the file "Default.log"
  When the `Show Debug` filter option is off
    And applying the include filter: Diagnostics
  Then the results will include 19 records

@Requirement:414, @Requirement:418
Scenario: `Show Debug` option shows trace records 
  Given that Weevil has opened the file "Default.log"
  When the `Show Debug` filter option is on
    And applying the include filter: Core
  Then the results will include 22 records

@Requirement:415, @Requirement:419
Scenario: `Show Trace` option hides trace records
  Given that Weevil has opened the file "Default.log"
  When the `Show Trace` filter option is off
    And applying the include filter: Diagnostics
  Then the results will include 3 records

@Requirement:415, @Requirement:419
Scenario: `Show Trace` option displays trace records 
  Given that Weevil has opened the file "Default.log"
  When the `Show Trace` filter option is on
    And applying the include filter: Diagnostics
  Then the results will include 102 records