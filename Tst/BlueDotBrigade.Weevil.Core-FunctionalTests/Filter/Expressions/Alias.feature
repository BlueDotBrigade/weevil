Feature: Alias

@Requirement:398, @Requirement:406
Scenario: Filtering using an alias expression
  Given that the default log file is open
  When applying the include filter: #Fatal
  Then there will be 1 results
    And each result will include: Unrecoverable error has occurred




  Scenario: Applying a filter using an alias
    Given that the default log file is open
    When applying the include filter: #Summary
    Then the results will be
    """
    """