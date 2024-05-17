Feature: StatusBarShall

A short summary of the feature

Scenario: Display 0 for empty files.
Given that the log file is open at `Empty.txt`
Then the record count will be 0