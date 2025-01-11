Feature: StatusBarShall

A short summary of the feature

@Requirement:408
Scenario: Display record count when loading log file
Given that the default log file is open
Then the status bar visible record count will be 387

@Requirement:408
Scenario: Display record count of zero when loading an empty file
Given that the "Empty.txt" log file name is open
Then the status bar visible record count will be 0