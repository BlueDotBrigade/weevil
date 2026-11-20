Feature: Listing the parts of a log

Before a log opens, Weevil lists the parts of it that can be opened on their own, from its sidecar:
its level-1 sections, each with the sections nested in it, then its named regions.

@Requirement:1157
Scenario: The parts of a log are listed from its sidecar alone
	Given that the default log file has been saved beside the tests with these sections in its sidecar:
		| Section  | Level | Line |
		| Startup  | 1     | 1    |
		| Patrol   | 1     | 120  |
		| Contact  | 2     | 150  |
		| Shutdown | 1     | 300  |
	And that the default log file has been saved beside the tests with these regions in its sidecar:
		| Region   | First Line | Last Line |
		| Docking  | 10         | 40        |
		|          | 350        | 399       |
	And the log itself has been deleted
	When the parts of the log are listed
	Then these parts will be listed:
		| Part    | Name     | First Line | Last Line |
		| Section | Startup  | 1          | 119       |
		| Section | Patrol   | 120        | 299       |
		| Section | Shutdown | 300        | end       |
		| Region  | Docking  | 10         | 40        |
