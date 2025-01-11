Feature: Plain Text Expressions

@Requirement:398, @Requirement:406, @Requirement:411
Scenario: Filtering using a plain text expression
	Given that the default log file is open
	When using the "Plain Text" filter mode
	 And applying the include filter: Directives
	Then there will be 7 matching records
	 And all records will include: Directives