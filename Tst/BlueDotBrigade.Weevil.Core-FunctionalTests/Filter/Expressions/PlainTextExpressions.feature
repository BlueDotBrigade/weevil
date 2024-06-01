Feature: Plain Text Expressions

@Requirement:398, @Requirement:406
Scenario: Filtering using a plain text expression
  Given that the default log file is open
  When applying the include filter: Directives
  Then the results will include 7 records
    And the results will include: Directives

@Requirement:411
Scenario: `Plain Text` filtering
  Given that the default log file is open
  When selecting the plain text filter mode
    And applying the include filter: Directives 
  Then the results will include 7 records
    And each result will include the text "Directives"

@Requirement:394
Scenario: `Case Sensitive` plain text filtering
  Given that the default log file is open
  When selecting the plain text filter mode
    And the case sensitive option is on
    And applying the include filter: directives
  Then the results will include 0 records

@Requirement:394
Scenario: `Case Insensitive` plain text filtering
  Given that the default log file is open
  When selecting the plain text filter mode
    And the case sensitive option is off
    And applying the include filter: directives
  Then the results will include 7 records
    And each result will include the text "Directives"
