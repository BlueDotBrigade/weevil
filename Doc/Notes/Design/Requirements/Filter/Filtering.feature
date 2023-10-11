Feature: Filtering

REQUIREMENTS

1. The system shall support filtering using 1 or more expressions delimited by a logical OR operator (`||`).
2. The system shall support filtering using different types of expressions (text, alias, and moniker).
3. When an `include` filter is applied, the system displays all matching records in the results.
4. When an exclude filter is applied, the system omits all matching records from the results.
5. When both `include` and `exclude` filters are applied, the system shall prioritize the 'Exclude' filter.
6. The system shall enable an exclude" filter for `Trace` records by default.
7. The system shall enable an exclude filter for `Debug` records by default.
8. While the `case-sensitivity` option is turned out, text expressions shall be interpreted as case-insensitive.
9. While an analyst is creating a filter, the system shall automatically activate the filter 3 seconds after the analyst ceases typing.
10. While a record is `pinned`, the system shall always display pinned records in the results, regardless of any filters that are applied.
11. The system shall display the number of records in the filter results in the status bar.

Feature: Filtering

Scenario: 1 inclusive filter displays matching records
	Given Weevil has opened the file "Default.log"
	When the inclusive filter is "Directives"
  Then all records will include the text "Directives"
        And there will be 7 filter results

  
Scenario: Filter using one expression
    Given Weevil has opened the file "Sample.log"
    When the inclusive filter is "sample log message"
        And the filters are applied
    Then all records will include the text "sample log message"
        And there will be 123 filter results

Scenario: Filter using multiple expressions delimited by ||
    Given Weevil has opened the file "Sample.log"
    When the inclusive filter is "sample log message"
        And the filters are applied
    Then all records will include the text "sample log message"
        And there will be 123 filter results

Scenario: Filter using different types of expressions


Scenario: Opening file
    Given the default log is open
    Then the record count will be 123

  Scenario: Displaying records that match an inclusive filter
    Given the analyst wants to search for specific records
    And filters are available for the analyst to use
    When the analyst applies an inclusive filter to the records
    Then the system should only display records that matches the filter criteria

  Scenario: Applying a filter using a domain-specific alias
    Given I am viewing a list of log entries
    When I apply the #Summary filter alias
    Then only key high-level log entries should be displayed

  Scenario: Filtering using a moniker for metadata query
    Given I am viewing a list of log entries
    When I apply the @Pinned moniker
    Then only records flagged as pinned should be displayed

  Scenario: Filtering based on severity using a moniker
    Given I am viewing a list of log entries
    When I apply the @Severity=warning moniker
    Then only log entries with a warning severity level should be displayed

@Requirement:365
Scenario: Filter log entries by hardware serial number
	Given that the default log file is open
	When the plain text filtering option is selected
	And case sensitive filtering has been enabled
	And the inclusive filter is applied `S/N`
	Then all records will include `S/N`