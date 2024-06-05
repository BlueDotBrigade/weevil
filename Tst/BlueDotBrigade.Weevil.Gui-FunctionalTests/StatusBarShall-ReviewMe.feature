Feature: StatusBarShall

A short summary of the feature

Scenario: Display 0 for empty files.
Given that the "Empty.txt" log file is open
Then the visible record count in the status bar will be 0