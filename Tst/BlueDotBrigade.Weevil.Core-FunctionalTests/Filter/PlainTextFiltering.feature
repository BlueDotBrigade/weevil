Feature: Plain Text Filtering

@Requirement:365
Scenario: Filter log entries by hardware serial number
	Given that the default log file is open
	When the plain text filtering option is selected
	And case sensitive filtering has been enabled
	And the inclusive filter is applied `S/N`
	Then all records will include `S/N`