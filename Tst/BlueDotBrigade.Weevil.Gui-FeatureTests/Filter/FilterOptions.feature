Feature: Filter Options

A short summary of the feature

@Requirement:467
Scenario: Filter is automatically applied when option is on
	Given that the default log file is open
	When the automatic filtering option is on
		 And entering the include filter: #Information
		 And waiting 5 sec for: filter to be automatically applied
	Then there will be 37 matching records

@Requirement:467
Scenario: Filter is not automatically applied when option is off
	Given that the default log file is open
	When the automatic filtering option is off
		 And entering the include filter: #Information
		 And waiting 5 sec for: filter to be automatically applied
	Then there will be 387 matching records