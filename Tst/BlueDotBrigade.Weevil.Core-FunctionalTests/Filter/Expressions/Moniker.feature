Feature: Moniker


@Requirement:398, @Requirement:406
Scenario: Filtering using a moniker expression
  Given that the default log file is open
  When applying the include filter: @Severity=Information
  Then the results will include 36 records
    And each result will include the text "Info"




  Scenario: Filtering using a moniker for metadata query
    Given that the default log file is open
    When I apply the @Pinned moniker
    Then only records flagged as pinned should be displayed

  Scenario: Filtering based on severity using a moniker
    Given that the default log file is open
    When I apply the @Severity=warning moniker
    Then only log entries with a warning severity level should be displayed    