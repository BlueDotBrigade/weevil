Feature: Filtering

# ---------------- VIEW MODEL

@Requirement:410
Scenario: Filter automatically applied when typing pauses
  Given that the default log file is open
  When entering the include filter: #Information
    And waiting 4 seconds
  Then there will be 36 results

@Requirement:410
Scenario: Filter is not automatically applied when typing continues
  Given that the default log file is open
  When entering the include filter: #Error
    And waiting 1 seconds
    And entering the include filter: #Error||
    And waiting 1 seconds
    And entering the include filter: #Error||#Fatal
    And waiting 1 seconds
  Then there will be 387 results
