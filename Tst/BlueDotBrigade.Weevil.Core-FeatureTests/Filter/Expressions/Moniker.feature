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

# Refreshing measured the records the elapsed-time filter showed, rather than those before it, so the results changed each time.
@Bug:1224
Scenario: Refreshing a filter by elapsed time keeps the same records
	Given that the "ElapsedTime.log" log file name is open
	When applying the include filter: Alpha
	And applying the include filter: @Elapsed=1500
	And refreshing the filter
	And refreshing the filter
	Then there will be 2 matching records
	And line number 3 will be visible
	And line number 5 will be visible

# Reloading measured every record of the log, rather than those the earlier filter showed.
@Bug:1224
Scenario: Reloading a log filtered by elapsed time keeps the same records
	Given that the "ElapsedTime.log" log file name is open
	When applying the include filter: Alpha
	And applying the include filter: @Elapsed=1500
	And reloading the log
	And refreshing the filter
	Then there will be 2 matching records
	And line number 3 will be visible
	And line number 5 will be visible

# A log that grew has no results shown before the filter for its new records, so the filter measures as it did before, and finds them.
@Bug:1224
Scenario: Reloading a log that grew since it was filtered by elapsed time shows the new records that match
	Given that a copy of the "ElapsedTime.log" log file is open
	When applying the include filter: Alpha
	And applying the include filter: @Elapsed=1500
	And this record is appended to the log:
		"""
		3050-09-30 11:26:20.00000	1977	2	Info	Alpha: 9 seconds after the previous Alpha
		"""
	And reloading the log
	Then line number 7 will be visible

# Applying a filter again, as new options or a Clear do, measures the records shown now (#1231): line 3 has no visible record before it.
# Only refreshing (F5) and reloading keep the elapsed times the filter compared (#1224).
Scenario: Applying a filter by elapsed time again measures the records it shows
	Given that the "ElapsedTime.log" log file name is open
	When applying the include filter: Alpha
	And applying the include filter: @Elapsed=1500
	And applying the include filter: @Elapsed=1500
	Then there will be 1 matching records
	And line number 5 will be visible
