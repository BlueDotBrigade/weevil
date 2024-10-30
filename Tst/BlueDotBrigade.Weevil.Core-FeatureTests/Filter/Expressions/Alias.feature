Feature: Alias

@Requirement:398, @Requirement:406
Scenario: Filtering using an alias expression
  Given that the default log file is open
  When applying the include filter: #Critical
  Then there will be 1 visible records
    And all records will include: Unrecoverable error has occurred