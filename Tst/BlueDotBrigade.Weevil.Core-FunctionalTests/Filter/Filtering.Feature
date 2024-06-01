Feature: Filtering

@Requirement:365
Scenario: Filter log entries by hardware serial number
	Given that the default log file is open
	When selecting the plain text filter mode
	And the case sensitive option is on
	And applying the include filter: S/N
	Then all records will include: S/N

@Requirement:406, @UserStory:123
Scenario: Two include filter expressions separated by OR
	Given that the default log file is open
	When applying the include filter: Directives||Fatal
	Then there will be 8 visible records
	And the records will include either
		"""
		Directives
		Fatal
		"""

@Requirement:399
Scenario: Include filter identifies important information
	Given that the default log file is open
	When applying the include filter: #Information
	Then there will be 36 visible records
	And all records will include: Info

@Requirement:400
Scenario: Exclude filter hides records
	Given that the default log file is open
	When applying the exclude filter: Security||Scomp||Core
	Then there will be 352 visible records
	And the records will exclude either
		"""
		Security
		Scomp
		Core
		"""

@Requirement:401
Scenario: Exclude filter takes precedence over include filter
	Given that the default log file is open
	When entering the include filter: #Information
	And entering the exclusive filter: Security||Scomp||Core
	And applying the filters
	Then there will be 10 visible records
	And all records will include: Info
	And the records will exclude
		"""
		Security
		Scomp
		Core
		"""
@Requirement:371
Scenario: Pinned records are always displayed
	Given that the default log file is open
	When pinning record ID 2
	And pinning record ID 4
	And pinning record ID 8s
	And applying the include filter: NoRecordMatches
	Then the results will include 3 records
	And record ID 2 will be visible
	And record ID 4 will be visible
	And record ID 8 will be visible

@Requirement:414, @Requirement:418
Scenario: `Show Debug` option hides trace records
	Given that the default log file is open
	When the `Show Debug` filter option is off
	And applying the include filter: Diagnostics
	Then the results will include 19 records

@Requirement:414, @Requirement:418
Scenario: `Show Debug` option shows trace records
	Given that the default log file is open
	When the `Show Debug` filter option is on
	And applying the include filter: Core
	Then the results will include 22 records

@Requirement:415, @Requirement:419
Scenario: `Show Trace` option hides trace records
	Given that the default log file is open
	When the `Show Trace` filter option is off
	And applying the include filter: Diagnostics
	Then the results will include 3 records

@Requirement:415, @Requirement:419
Scenario: `Show Trace` option displays trace records
	Given that the default log file is open
	When the `Show Trace` filter option is on
	And applying the include filter: Diagnostics
	Then the results will include 102 records