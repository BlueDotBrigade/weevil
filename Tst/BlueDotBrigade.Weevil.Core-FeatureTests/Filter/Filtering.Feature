Feature: Filtering

@Requirement:406, @UserStory:123
Scenario: Logical OR operator used to define multiple expressions
	Given that the default log file is open
	When applying the include filter: Directives||Fatal
	Then there will be 8 matching records
	And each record will include either
		"""
		Directives
		Fatal
		"""

@Requirement:399
Scenario: Include filter displays records with specific keywords
	Given that the default log file is open
	When applying the include filter: Info
	Then all records will include: Info

@Requirement:400
Scenario: Exclude filter hides records with specified terms
	Given that the default log file is open
	When applying the exclude filter: Trace||Debug
	Then no record will contain any of the following
		"""
		Trace
		Debug
		"""

@Requirement:401
Scenario: Exclude filter overrides include filter when both are applied
	Given that the default log file is open
	When using the include filter: Core
	And using the exclude filter: Debug
	And applying the filters
	Then all records will include: Core
	And all records will exclude: Debug

@Requirement:371, @Requirement:416, @Requirement:417
Scenario: Pinned records remain visible regardless of active filters
	Given that the default log file is open
	When pinning the record on line 2
		 And pinning the record on line 4
		 And pinning the record on line 8
		 And applying the include filter: text not found
	Then there will be 3 matching records
		 And line number 2 will be visible
		 And line number 4 will be visible
		 And line number 8 will be visible

@Requirement:424
Scenario: Built-in filter alias #IPv6 identifies records with IP addresses
	Given that the default log file is open
	When using the "Regular Expression" filter mode
		  And applying the include filter: #IPV6
	Then there will be 1 matching records
		  And all records will include: S/N=IA-1073R037

Scenario: Bookmarked records remain visible with include filter when bookmarks always visible is enabled
	Given that the default log file is open
	When bookmarking the record on line 2
		 And bookmarking the record on line 4
		 And bookmarking the record on line 8
		 And unpinning the record on line 2
		 And unpinning the record on line 4
		 And unpinning the record on line 8
		 And the "Show Pinned" filter option is on
		 And the "Show Bookmarks" filter option is on
		 And applying the include filter: text not found
	Then there will be 3 matching records
		 And line number 2 will be visible
		 And line number 4 will be visible
		 And line number 8 will be visible

Scenario: Bookmarked records remain visible with exclude filter when bookmarks always visible is enabled
	Given that the default log file is open
	When bookmarking the record on line 2
		 And bookmarking the record on line 4
		 And bookmarking the record on line 8
		 And unpinning the record on line 2
		 And unpinning the record on line 4
		 And unpinning the record on line 8
		 And the "Show Pinned" filter option is on
		 And the "Show Bookmarks" filter option is on
		 And applying the exclude filter: Info
	Then there will be 3 matching records
		 And line number 2 will be visible
		 And line number 4 will be visible
		 And line number 8 will be visible
