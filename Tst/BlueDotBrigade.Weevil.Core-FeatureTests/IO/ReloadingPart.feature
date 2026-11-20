Feature: Reloading part of a log

Reload opens again the part of the log that was opened, in the log as it is now:
a region or section is found again by its name, lines from a start line run to the new end of the log, and a range of lines stays the same.

@Requirement:1157
Scenario: Reloading a region opens it again as the log's regions now define it
	Given that the default log file has been saved beside the tests with these regions in its sidecar:
		| Region  | First Line | Last Line |
		| Startup | 10         | 40        |
	And its "Startup" region has been opened in RAM
	And the "Startup" region has been changed to run from line 20 to line 30
	When the log is reloaded
	Then it will show the records of lines 20 to 30 of the whole log, and no others
	And the part open will be the "Startup" region

@Requirement:1157
Scenario: Reloading a region that is gone keeps the records shown, and names the regions there are
	Given that the default log file has been saved beside the tests with these regions in its sidecar:
		| Region   | First Line | Last Line |
		| Startup  | 10         | 40        |
		| Shutdown | 350        | 399       |
	And its "Startup" region has been opened in Disk
	And the "Startup" region has been removed
	When the log is reloaded
	Then the log will not reload, and the reason will name: Startup, Shutdown
	And it will show the records of lines 10 to 40 of the whole log, and no others

@Requirement:1157
Scenario: Reloading the last section shows the lines appended to the log
	Given that the default log file has been saved beside the tests with these sections in its sidecar:
		| Section  | Level | Line |
		| Startup  | 1     | 1    |
		| Shutdown | 1     | 300  |
	And its "Shutdown" section has been opened in Disk
	And records have been appended to the log
	When the log is reloaded
	Then it will show the records from line 300 to the end of the whole log, and no others

@Requirement:1157
Scenario: Reloading a log opened from a line shows the lines appended to the log
	Given that the default log file has been saved beside the tests
	And it has been opened from line 300 in RAM
	And records have been appended to the log
	When the log is reloaded
	Then it will show the records from line 300 to the end of the whole log, and no others

@Requirement:1157
Scenario: Reloading a range of lines shows the same lines
	Given that the default log file has been saved beside the tests
	And its lines 10 to 40 have been opened in Disk
	And records have been appended to the log
	When the log is reloaded
	Then it will show the records of lines 10 to 40 of the whole log, and no others
