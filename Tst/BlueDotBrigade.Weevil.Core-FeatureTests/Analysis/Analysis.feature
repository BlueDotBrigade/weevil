Feature: Analysis

A short summary of the feature

# Test is correct. Weevil is producing the wrong result
Scenario: Detect time gaps greater than threshold
    Given that the default log file is open
    When using elapsed time analysis with a threshold of 14 sec
    Then the flagged record count will be 5

