Feature: Regular Expressions

@Requirement:411
Scenario: `Regular Expression` filtering
  Given that the default log file is open
  When the case sensitive option is on
    And applying the include filter: Voltage=\d{2}
  Then the results will include 1 records
    And each result will include the text "Voltage=51"

@Requirement:394
Scenario: `Case Sensitive` regular expression filtering
  Given that the default log file is open
  When selecting the regular expression filter mode
    And using case sensitive filtering
    And applying the include filter: directives
  Then the results will include 0 records

@Requirement:394
Scenario: `Case Insensitive` regular expression filtering
  Given that the default log file is open
  When selecting the regular expression filter mode
    And using case insensitive filtering
    And applying the include filter: directives
  Then the results will include 7 records
    And each result will include the text "Directives"