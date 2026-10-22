Feature: Moniker


@Requirement:398, @Requirement:406
Scenario: Filtering using a moniker expression
	Given that the default log file is open
	When applying the include filter: @Severity=Information
	Then there will be 37 matching records
	And all records will include: Info

Scenario: Filtering using the @Line moniker
	Given that the default log file is open
	When applying the include filter: @Line=100
	Then there will be 1 matching records

# An elapsed time is the time since the previous record that the earlier filter showed (#1016).
Scenario: Filtering by elapsed time after another filter
	Given that the "ElapsedTime.log" log file name is open
	When applying the include filter: Alpha
	And applying the include filter: @Elapsed=1500
	Then there will be 2 matching records
	And line number 3 will be visible
	And line number 5 will be visible
