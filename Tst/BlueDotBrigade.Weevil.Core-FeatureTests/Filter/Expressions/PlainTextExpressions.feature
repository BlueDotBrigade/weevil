Feature: Plain Text Expressions

@Requirement:398, @Requirement:406, @Requirement:411
Scenario: Filtering using a plain text expression
	Given that the default log file is open
	When using the "Plain Text" filter mode
	 And applying the include filter: Directives
	Then there will be 7 visible records
	 And all records will include: Directives

@Requirement:394
Scenario: `Case Sensitive` filtering
	Given that the default log file is open
	When using the "Plain Text" filter mode
	 And the case sensitive option is on
	And applying the include filter: DIRECTIVES
	Then there will be 0 visible records

@Requirement:394
Scenario: `Case Insensitive` filtering
	Given that the default log file is open
	When using the "Plain Text" filter mode
	 And the case sensitive option is off
	And applying the include filter: DIRECTIVES
	Then there will be 7 visible records