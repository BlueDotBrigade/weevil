Feature: Opening one section

An analyst can open only one level-1 section of a log's table of contents, rather than the whole log.
A section runs from its line to the line before the next level-1 section, or to the end of the log, so it holds the sections nested in it.

Background:
	Given that the default log file has been saved beside the tests with these sections in its sidecar:
		| Section  | Level | Line |
		| Startup  | 1     | 1    |
		| Patrol   | 1     | 120  |
		| Contact  | 2     | 150  |
		| Shutdown | 1     | 300  |

@Requirement:1157
Scenario Outline: Opening a section shows only its records, with the sections nested in it
	When its "<Section>" section is opened in <Mode>
	Then it will show the records of lines <First Line> to <Last Line> of the whole log, and no others

Examples:
	| Section  | Mode | First Line | Last Line |
	| startup  | RAM  | 1          | 119       |
	| Patrol   | Disk | 120        | 299       |

@Requirement:1157
Scenario: Opening the last section shows its records to the end of the log
	When its "Shutdown" section is opened in RAM
	Then it will show the records from line 300 to the end of the whole log, and no others

@Requirement:1157
Scenario: A section that is not level 1 cannot be opened, and the reason names those that can
	When its "Contact" section is opened in RAM
	Then the log will not open, and the reason will name: Startup, Patrol, Shutdown

@Requirement:1157
Scenario: A region and a section cannot both be opened
	Given that the default log file has been saved beside the tests with these regions in its sidecar:
		| Region | First Line | Last Line |
		| Patrol | 120        | 200       |
	When its "Patrol" region and its "Patrol" section are both opened
	Then the log will not open, and the reason will name: Patrol
