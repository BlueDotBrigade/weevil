Feature: Alias

@Requirement:398, @Requirement:406
Scenario: Filtering using an alias expression
  Given that the default log file is open
  When applying the include filter: #Fatal
  Then there will be 1 visible records
    And each result will include the text "Unrecoverable error has occurred"




  Scenario: Applying a filter using an alias
    Given that the default log file is open
    When applying the include filter: #Summary
    Then only key high-level log entries should be displayed