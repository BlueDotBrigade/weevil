Feature: Filtering

USER STORIES

REQUIREMENTS

One or more filter criteria can be used to show or hide log file records.

1. Inclusive and Exclusive Filters
    - Display records matching the inclusive filter while hiding those matching the exclusive filter.
2. Filter Criteria
	 1. Plain Text
	 2. Regular Expressions
	 3. Aliases
		  - Frequently used or complex filters can be assigned a unique key that can be used to speed up the filtering process.
		  - For example, the `#IpAddress` key could be assigned to the following filter criteria  `^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$`.
	 4. Monikers
        - Monikers are built-in keys that can be used to query metadata collected by _Weevil_.
		  - For example, the `@Comment` can be used to identify records that have a user comment.
3. Multiple Criteria
    - Multiple filter criteria can be combined together using a logical "OR" operator (`||`).
4. Pinned Records
	 - Pinned records are guaranteed to be included in the filter results.

SCENARIOS

#Scenario: Displaying records that match an inclusive filter
#
#	Given the analyst wants to search for specific records 
#	And filters are available for the analyst to use 
#	When the analyst applies an inclusive filter to the records
#	Then the system should only display records that matches the filter criteria



