Feature: Alias

@Requirement:398, @Requirement:406
Scenario: Filtering using an alias expression
  Given that Weevil has opened the file "Default.log"
  When applying the include filter: #Fatal
  Then the results will include 1 records
    And each result will include the text "Unrecoverable error has occurred"




  Scenario: Applying a filter using an alias
    Given I am viewing a list of log entries
    When I apply the #Summary filter alias
    Then only key high-level log entries should be displayed    