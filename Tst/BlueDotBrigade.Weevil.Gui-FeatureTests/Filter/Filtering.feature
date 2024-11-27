Feature: Filtering

@Requirement:410
Scenario: Filter automatically applied when typing pauses
	Given that the default log file is open
	When using the include filter: #Information
		  And waiting 4 sec for: filter to be automatically applied
	Then there will be 37 matching records

@Requirement:410
Scenario: Filter is not automatically applied when typing continues
	Given that the default log file is open
	When using the include filter: #Error
		 And waiting 1 sec
		 And using the include filter: #Error||
		 And waiting 1 sec
		 And using the include filter: #Error||#Fatal
		 And waiting 1 sec
	Then there will be 387 matching records
