Feature: Pin Navigation

As an analyst, I want to navigate between pinned records and then
continue navigating sequentially, so that I can review adjacent records
after finding a point of interest.

@Requirement:699
Scenario: Navigate to next record after navigating to last pinned record
	Given that the default log file is open
	When pinning the record on line 10
		And pinning the record on line 20
		And selecting the record on line 5
		And navigating to the next pinned record
		And navigating to the next pinned record
		And selecting the next record
	Then the selected record line number will be 21

@Requirement:699
Scenario: Navigate to previous record after navigating to a pinned record
	Given that the default log file is open
	When pinning the record on line 10
		And pinning the record on line 20
		And selecting the record on line 5
		And navigating to the next pinned record
		And selecting the previous record
	Then the selected record line number will be 9
