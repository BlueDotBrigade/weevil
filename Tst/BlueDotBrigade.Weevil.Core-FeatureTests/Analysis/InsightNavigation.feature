Feature: Insight Navigation

As an analyst, I want to navigate to records related to an insight,
so that I can quickly troubleshoot problems detected by the insight.

Scenario: Navigate to first critical error from insight
	Given that the default log file is open
	When the critical errors insight is refreshed
	Then the insight should have related records
		And the first related record line number should be greater than 0

Scenario: Navigate to temporal anomaly from insight
	Given that the default log file is open
	When the temporal anomaly insight is refreshed
	Then the insight should have related records
		And the first related record line number should be greater than 0

Scenario: Insight with no problems has no related records
	Given that the default log file is open
	When applying the include filter: Information
		And the critical errors insight is refreshed
	Then the insight should not have related records

Scenario: Related records are flagged for navigation
	Given that the default log file is open
	When the critical errors insight is refreshed
	Then the insight should have related records
		And all related records should be flaggable
