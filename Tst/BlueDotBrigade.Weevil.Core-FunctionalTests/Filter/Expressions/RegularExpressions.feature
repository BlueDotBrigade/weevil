Feature: Regular Expressions

@Requirement:411
Scenario: `Regular Expression` filtering
  Given that the default log file is open
  When the case sensitive option is on
    And applying the include filter: Voltage=\d{2}
  Then there will be 1 results
    And each result will include: Voltage=51

@Requirement:394
Scenario: `Case Sensitive` regular expression filtering
  Given that the default log file is open
  When selecting the regular expression filter mode
    And the case sensitive option is on
    And applying the include filter: directives
  Then there will be 0 results

@Requirement:394
Scenario: `Case Insensitive` regular expression filtering
  Given that the default log file is open
  When selecting the regular expression filter mode
    And the case sensitive option is on
    And applying the include filter: directives
  Then there will be 7 results
    And each result will include: "Directives"