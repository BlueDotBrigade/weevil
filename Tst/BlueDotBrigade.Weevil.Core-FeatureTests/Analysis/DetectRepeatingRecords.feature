Feature: Detect Repeating Records

A short summary of the feature

@Requirement:515
Scenario: No Repeating Records Found
	 Given that the default log file is open
	 When applying the include filter: text that does not exist
		  And detecting both edges using the regular expression: Key performance metrics
	 Then the flagged record count will be 0

@Requirement:515
Scenario: Block found at start of results
	 Given that the default log file is open
	 When applying the include filter: Directives||Peripheral detected
		  And detecting both edges using the regular expression: Directives
	 Then the flagged record count will be 2

@Requirement:515
Scenario: Block found at end of results
	 Given that the default log file is open
	 When using the include filter: Directives||Peripheral detected
		  When applying the exclude filter: mission
		  And detecting both edges using the regular expression: Directives
	 Then the flagged record count will be 2

@Requirement:515
Scenario: Blocks found at start, middle and end of results
	 Given that the default log file is open
	 When applying the include filter: Key performance metrics||Security
		  And detecting both edges using the regular expression: Key performance metrics
	 Then the flagged record count will be 6
