Feature: Opening one region

An analyst can open only one of the regions a log's sidecar lists, rather than the whole log.

@Requirement:1157
Scenario Outline: Opening a region shows only its records
	Given that the default log file has been saved beside the tests with these regions in its sidecar:
		| Region   | First Line | Last Line |
		| Startup  | 10         | 40        |
		| Shutdown | 350        | 399       |
	When its "startup" region is opened in <Mode>
	Then it will show the records of lines 10 to 40 of the whole log, and no others

Examples:
	| Mode |
	| RAM  |
	| Disk |

@Requirement:1157
Scenario: Opening a region the sidecar does not list names the regions it does list
	Given that the default log file has been saved beside the tests with these regions in its sidecar:
		| Region   | First Line | Last Line |
		| Startup  | 10         | 40        |
		| Shutdown | 350        | 399       |
	When its "Teardown" region is opened in RAM
	Then the log will not open, and the reason will name the regions Startup and Shutdown
