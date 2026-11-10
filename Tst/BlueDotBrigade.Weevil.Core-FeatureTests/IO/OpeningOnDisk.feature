Feature: Opening a log on disk

A log opened on disk keeps only its record table in memory,
and reads each record's content back from the log when it is needed.

@Requirement:421
Scenario Outline: A log opened on disk shows the same records and filter results as in RAM
	Given that the default log file has been saved as <Encoding> with <Line Endings> line endings
	And that the saved log file is open on disk
	When applying the include filter: Directives|Fatal|…
	Then the log on disk will show the same records and filter results as in RAM

Examples:
	| Encoding       | Line Endings |
	| UTF-8          | CRLF         |
	| UTF-8          | LF           |
	| UTF-8          | CR           |
	| UTF-8 with BOM | CRLF         |
	| UTF-16 LE      | LF           |
	| UTF-16 BE      | CR           |
	| UTF-32 LE      | CRLF         |
	| UTF-32 BE      | LF           |

@Requirement:421
Scenario: A filter on pinned records reads nothing from a log opened on disk
	Given that the default log file has been saved as UTF-8 with LF line endings
	And that the saved log file is open on disk
	And line 5 is pinned
	And that the saved log file has been deleted
	When applying the include filter: @Pinned
	Then there will be 1 matching records
	And line number 5 will be visible
