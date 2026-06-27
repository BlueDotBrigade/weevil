Feature: Regular Expressions

@Requirement:411
Scenario: `Regular Expression` filtering
  Given that the default log file is open
  When using the "Regular Expression" filter mode
    And applying the include filter: Voltage=\d{2,3}
  Then there will be 1 matching records
    And all records will include: Voltage=51.9V

@Requirement:411
Scenario: Default filter mode uses regular expressions
  Given that the default log file is open
  When applying the include filter: Voltage=\d{2,3}
  Then there will be 1 matching records
    And all records will include: Voltage=51.9V

@Requirement:394
Scenario: `Case Sensitive` filtering
  Given that the default log file is open
  When using the "Regular Expression" filter mode
    And the case sensitive option is on
    And applying the include filter: DIRECTIVES
  Then there will be 0 matching records

@Requirement:394
Scenario: `Case Insensitive` filtering
  Given that the default log file is open
  When using the "Regular Expression" filter mode
    And the case sensitive option is off
    And applying the include filter: DIRECTIVES
  Then there will be 7 matching records
    And all records will include: Directives