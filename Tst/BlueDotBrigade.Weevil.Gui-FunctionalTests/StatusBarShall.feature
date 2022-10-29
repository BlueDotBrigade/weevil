Feature: StatusBarShall

A short summary of the feature

Scenario: Display 0 for empty files.
	Given Weevil has started
	When the user has opened the log file `C:\Temp\EmptyFile.log`
	Then the record count will be 0