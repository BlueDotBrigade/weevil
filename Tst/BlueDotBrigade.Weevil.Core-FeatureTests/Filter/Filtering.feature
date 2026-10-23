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
		 And the "Show Pinned" filter option is on
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

Scenario: Bookmarked records remain visible when using include filter
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

Scenario: Bookmarked records remain visible with using exclude filter
	Given that the default log file is open
	When bookmarking the record on line 2
		 And bookmarking the record on line 4
		 And bookmarking the record on line 8
		 And unpinning the record on line 2
		 And unpinning the record on line 4
		 And unpinning the record on line 8
		 And the "Show Pinned" filter option is on
		 And the "Show Bookmarks" filter option is on
		 And applying the exclude filter: #AllRecords
	Then there will be 3 matching records
		 And line number 2 will be visible
		 And line number 4 will be visible
		 And line number 8 will be visible

# Core Filtering Behavior - No Special Options

Scenario: No filters applied displays all records
	Given that the default log file is open
	When the "Show Pinned" filter option is off
		 And the "Show Bookmarks" filter option is off
		 And applying the filters
	Then there will be 387 matching records

Scenario: Only include filter displays matching records
	Given that the default log file is open
	When the "Show Pinned" filter option is off
		 And the "Show Bookmarks" filter option is off
		 And applying the include filter: Info
	Then there will be 37 matching records
		 And all records will include: Info

Scenario: Only exclude filter hides matching records from all records
	Given that the default log file is open
	When the "Show Pinned" filter option is off
		 And the "Show Bookmarks" filter option is off
		 And applying the exclude filter: Info
	Then there will be 350 matching records
		 And no record will contain any of the following
		"""
		Info
		"""

# Show Pinned/Bookmarks with INCLUDE Filters

Scenario: Show Pinned disabled with include filter means pinned records not shown unless they match
	Given that the default log file is open
	When pinning the record on line 2
		 And pinning the record on line 5
		 And the "Show Pinned" filter option is off
		 And the "Show Bookmarks" filter option is off
		 And applying the include filter: Diagnostics
	Then there will be 102 matching records
		 And all records will include: Diagnostics

Scenario: Show Bookmarks disabled with include filter means bookmarked records not shown unless they match
	Given that the default log file is open
	When bookmarking the record on line 2
		 And bookmarking the record on line 5
		 And unpinning the record on line 2
		 And unpinning the record on line 5
		 And the "Show Pinned" filter option is off
		 And the "Show Bookmarks" filter option is off
		 And applying the include filter: Diagnostics
	Then there will be 102 matching records
		 And all records will include: Diagnostics

# Show Pinned/Bookmarks with EXCLUDE Filters

Scenario: Show Pinned disabled with exclude filter means pinned records are excluded if they match
	Given that the default log file is open
	When pinning the record on line 2
		 And pinning the record on line 5
		 And the "Show Pinned" filter option is off
		 And the "Show Bookmarks" filter option is off
		 And applying the exclude filter: Trace
	Then there will be 49 matching records
		 And no record will contain any of the following
		"""
		Trace
		"""

Scenario: Show Bookmarks disabled with exclude filter means bookmarked records are excluded if they match
	Given that the default log file is open
	When bookmarking the record on line 2
		 And bookmarking the record on line 5
		 And unpinning the record on line 2
		 And unpinning the record on line 5
		 And the "Show Pinned" filter option is off
		 And the "Show Bookmarks" filter option is off
		 And applying the exclude filter: Trace
	Then there will be 49 matching records
		 And no record will contain any of the following
		"""
		Trace
		"""

Scenario: Show Pinned enabled with exclude filter means pinned records always visible even if excluded
	Given that the default log file is open
	When pinning the record on line 2
		 And pinning the record on line 3
		 And the "Show Pinned" filter option is on
		 And the "Show Bookmarks" filter option is off
		 And applying the exclude filter: Trace
	Then there will be 51 matching records
		 And line number 2 will be visible
		 And line number 3 will be visible

Scenario: Show Bookmarks enabled with exclude filter means bookmarked records always visible even if excluded
	Given that the default log file is open
	When bookmarking the record on line 2
		 And bookmarking the record on line 3
		 And unpinning the record on line 2
		 And unpinning the record on line 3
		 And the "Show Pinned" filter option is off
		 And the "Show Bookmarks" filter option is on
		 And applying the exclude filter: Trace
	Then there will be 51 matching records
		 And line number 2 will be visible
		 And line number 3 will be visible

# Combined INCLUDE & EXCLUDE Filters

Scenario: Include and Exclude with Show Pinned off means pinned records excluded if they match exclude
	Given that the default log file is open
	When pinning the record on line 2
		 And pinning the record on line 3
		 And the "Show Pinned" filter option is off
		 And the "Show Bookmarks" filter option is off
		 And using the include filter: Trace||Info
		 And using the exclude filter: Diagnostics
		 And applying the filters
	Then there will be 275 matching records
		 And no record will contain any of the following
		"""
		Diagnostics
		"""

Scenario: Include and Exclude with Show Bookmarks off means bookmarked records excluded if they match exclude
	Given that the default log file is open
	When bookmarking the record on line 2
		 And bookmarking the record on line 3
		 And unpinning the record on line 2
		 And unpinning the record on line 3
		 And the "Show Pinned" filter option is off
		 And the "Show Bookmarks" filter option is off
		 And using the include filter: Trace||Info
		 And using the exclude filter: Diagnostics
		 And applying the filters
	Then there will be 275 matching records
		 And no record will contain any of the following
		"""
		Diagnostics
		"""

Scenario: Include and Exclude with Show Pinned on means pinned records always visible
	Given that the default log file is open
	When pinning the record on line 3
		 And pinning the record on line 7
		 And the "Show Pinned" filter option is on
		 And the "Show Bookmarks" filter option is off
		 And using the include filter: Info
		 And using the exclude filter: Directives
		 And applying the filters
	Then there will be 32 matching records
		 And line number 3 will be visible
		 And line number 7 will be visible

Scenario: Include and Exclude with Show Bookmarks on means bookmarked records always visible
	Given that the default log file is open
	When bookmarking the record on line 3
		 And bookmarking the record on line 7
		 And unpinning the record on line 3
		 And unpinning the record on line 7
		 And the "Show Pinned" filter option is off
		 And the "Show Bookmarks" filter option is on
		 And using the include filter: Info
		 And using the exclude filter: Directives
		 And applying the filters
	Then there will be 32 matching records
		 And line number 3 will be visible
		 And line number 7 will be visible

# A selected record that a filter hides is no longer selected (#1015).
Scenario: A filter keeps only the selected records that it shows
	Given that the "ElapsedTime.log" log file name is open
	When selecting the records on lines 1 to 4
	And applying the include filter: Alpha
	Then 2 records will be selected
	And line number 1 will be selected
	And line number 3 will be selected

Scenario: A selected record hidden by a filter stays unselected when a later filter shows it
	Given that the "ElapsedTime.log" log file name is open
	When selecting the records on lines 1 to 4
	And applying the include filter: Alpha
	And applying the include filter: Alpha||Bravo
	Then 2 records will be selected
	And line number 2 will not be selected

Scenario: The selection period spans only the selected records that the filter shows
	Given that the "ElapsedTime.log" log file name is open
	When selecting the records on lines 1 to 6
	And applying the include filter: Bravo
	Then the selection period will be 8 seconds
