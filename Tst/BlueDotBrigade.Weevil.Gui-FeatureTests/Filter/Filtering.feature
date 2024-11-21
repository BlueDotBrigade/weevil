Feature: Filtering

# ---------------- VIEW MODEL

@Requirement:410
Scenario: Filter automatically applied when typing pauses
  Given that the default log file is open
  When entering the include filter: #Information
    And waiting 4 sec for: Weevil to automatically apply the filter
  Then the visible record count in the status bar will be 36

@Requirement:410
Scenario: Filter is not automatically applied when typing continues
  Given that the default log file is open
  When entering the include filter: #Error
    And waiting 1 sec
    And entering the include filter: #Error||
    And waiting 1 sec
    And entering the include filter: #Error||#Fatal
    And waiting 1 sec
  Then the visible record count in the status bar will be 387
