Feature: Analysis

A short summary of the feature

Scenario: Detect records in wrong chronological order
	Given that the default log file is open
	When using temporal anomaly analysis with a threshold of 1 ms
	Then the flagged record count will be 1

Scenario: Detect time gaps greater than threshold
    Given that the default log file is open
    When using elapsed time analysis with a threshold of 14 sec
    Then the flagged record count will be 5
