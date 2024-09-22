Feature: Filtering

@Requirement:406, @UserStory:123
Scenario: Display records that match any of the OR separated expressions
	Given that the default log file is open
	When applying the include filter: Directives||Fatal
	Then there will be 8 visible records
	And each record will include either
		"""
		Directives
		Fatal
		"""

@Requirement:399
Scenario: Use include filter to display records containing specific keywords
	Given that the default log file is open
	When applying the include filter: Info
	Then there will be 36 visible records
	And all records will include: Info

@Requirement:400
Scenario: Apply exclude filter to hide records containing specified terms
	Given that the default log file is open
	When applying the exclude filter: Security||Scomp||Core
	Then there will be 352 visible records
	And no record will contain any of the following
		"""
		Security
		Scomp
		Core
		"""

@Requirement:401
Scenario: Exclude filter overrides include filter when both are applied
	Given that the default log file is open
	When entering the include filter: Info
	And entering the exclude filter: Security||Scomp||Core
	And applying the filters
	Then there will be 10 visible records
	And all records will include: Info
	And each record will include all of the following
		"""
		Security
		Scomp
		Core
		"""

@Requirement:371
Scenario: Pinned records remain visible regardless of active filters
	Given that the default log file is open
	When pinning the record on line 2
	And pinning the record on line 4
	And pinning the record on line 8
	And applying the include filter: text not found
	Then there will be 3 visible records
	And line number 2 will be visible
	And line number 4 will be visible
	And line number 8 will be visible

@Requirement:424
Scenario: Use the built-in #Information alias to identify records with this severity-level
	Given that the default log file is open
	When applying the include filter: #Information
	Then there will be 36 visible records
	And all records will include: Info
