Feature: Filtering

@Requirement:410
Scenario: Filter automatically applied when typing pauses
	Given that the default log file is open
	When entering the include filter: #Information
		  And waiting 4 sec for: filter to be automatically applied
	Then there will be 37 matching records

@Requirement:410
Scenario: Filter is not automatically applied when typing continues
	Given that the default log file is open
	When entering the include filter: #Information
		  And waiting 1 sec for: too short pause for automatic filtering
	Then there will be 387 matching records
