Feature: Moniker


@Requirement:398, @Requirement:406
Scenario: Filtering using a moniker expression
	Given that the default log file is open
	When applying the include filter: @Severity=Information
	Then there will be 37 matching records
	And all records will include: Info