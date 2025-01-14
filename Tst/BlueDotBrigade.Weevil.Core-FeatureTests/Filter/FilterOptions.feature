Feature: Filter Options

@Requirement:411, @Requirement:365
Scenario: Identify records of interest using plain-text filtering
	Given that the default log file is open
	When using the "Plain Text" filter mode
	And the case sensitive option is on
	And applying the include filter: S/N
	Then all records will include: S/N

@Requirement:411, @Requirement:422 
Scenario: Identify records of interest using regular expressions
	Given that the default log file is open
	When using the "Regular Expression" filter mode
	And the case sensitive option is on
	And applying the include filter: Directives.*mission
	Then each record will include all of the following
	"""
	Directives
	mission
	"""

@Requirement:414, @Requirement:418
Scenario: Hide debug-level records when "Show Debug" option is disabled
	Given that the default log file is open
	When the "Show Debug" filter option is off
	And the "Show Trace" filter option is off
	And applying the filters
	Then there will be 43 matching records

@Requirement:414, @Requirement:418
Scenario: Display debug-level records when "Show Debug" option is enabled
	Given that the default log file is open
	When the "Show Debug" filter option is on
	And the "Show Trace" filter option is off
	And applying the filters
	Then there will be 49 matching records

@Requirement:415, @Requirement:419
Scenario: Hide trace-level records when "Show Trace" option is disabled
	Given that the default log file is open
	When the "Show Debug" filter option is off
	And the "Show Trace" filter option is off
	And applying the filters
	Then there will be 43 matching records

@Requirement:415, @Requirement:419
Scenario: Display trace-level records when "Show Trace" option is enabled
	Given that the default log file is open
	When the "Show Debug" filter option is off
	And the "Show Trace" filter option is on
	And applying the filters
	Then there will be 381 matching records

@Requirement:394, @Requirement:420
Scenario: `Case Sensitive` filtering
	Given that the default log file is open
	When the case sensitive option is on
	And applying the include filter: DIRECTIVES
	Then there will be 0 matching records

@Requirement:394, @Requirement:420
Scenario: `Case Insensitive` filtering
	Given that the default log file is open
	When the case sensitive option is off
	And applying the include filter: DIRECTIVES
	Then there will be 7 matching records