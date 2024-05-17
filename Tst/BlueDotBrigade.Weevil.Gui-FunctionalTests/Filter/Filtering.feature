Feature: Filtering

@Requirement:408
Scenario: Status bar displays number of records in results
  Given that the default log file is open
  When applying the include filter: #Information
  Then the status bar visible record count will display 36

@Requirement:410
Scenario: Filter automatically applied when typing pauses
  Given that the default log file is open
  When entering the include filter: #Information
    And waiting 4 seconds
  Then the results will include 36 records

# 387 = all records
@Requirement:410
Scenario: Filter is not automatically applied when typing continues
  Given that the default log file is open
  When entering the include filter: #Error
    And waiting 1 seconds
    And entering the include filter: #Error||
    And waiting 1 seconds
    And entering the include filter: #Error||#Fatal
    And waiting 1 seconds
  Then the results will include all records

@Requirement:411
Scenario: `Regular Expression` filter mode selected by default
  Given that the default log file is open
  Then the filter mode will be `Regular Expression`


