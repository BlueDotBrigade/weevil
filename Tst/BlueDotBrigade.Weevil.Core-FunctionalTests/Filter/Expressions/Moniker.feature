Feature: Moniker


@Requirement:398, @Requirement:406
Scenario: Filtering using a moniker expression
  Given that the default log file is open
  When applying the include filter: @Severity=Information
  Then there will be 36 results
    And each result will include: Info


  Scenario: Filtering using a moniker for metadata query
    Given that the default log file is open
    When applying the include filter: @Pinned
    Then all results will be pinned

  Scenario: Filtering based on severity using a moniker
    Given that the default log file is open
    When applying the include filter: @Severity=warning
    Then all results will have a severity of warning
  