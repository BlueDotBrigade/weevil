Feature: Moniker


@Requirement:398, @Requirement:406
Scenario: Filtering using a moniker expression
  Given that the default log file is open
  When applying the include filter: @Severity=Information
  Then the results will include 36 records
    And each result will include the text: "Info"


  Scenario: Filtering using a moniker for metadata query
    Given that the default log file is open
    When applying the include filter: @Pinned
    Then only pinned records will be displayed

  Scenario: Filtering based on severity using a moniker
    Given that the default log file is open
    When applying the include filter: @Severity=warning
    Then only records with a warning severity level will be displayed