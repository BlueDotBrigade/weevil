Feature: Reopening a log

A log opened before is reopened from the record table saved with its index, without being parsed again,
as long as the log has not changed and the same build of Weevil and of the plugin would parse it.

@Requirement:1085
Scenario Outline: A log opened before is reopened without being parsed, with the same records
	Given that the default log file has been saved beside the tests as <Encoding> with <Line Endings> line endings
	And that the saved log file has been opened in <Mode>
	When the saved log file is opened again in <Mode>
	Then it will have been opened without being parsed
	And it will show the same records as a parse of the log

Examples:
	| Encoding       | Line Endings | Mode |
	| UTF-8          | CRLF         | RAM  |
	| UTF-8          | LF           | Disk |
	| UTF-8          | CR           | RAM  |
	| UTF-8 with BOM | CRLF         | Disk |
	| UTF-16 LE      | LF           | RAM  |
	| UTF-16 BE      | CR           | RAM  |
	| UTF-16 BE      | LF           | Disk |
	| UTF-32 LE      | CRLF         | RAM  |
	| UTF-32 BE      | LF           | Disk |

Scenario: A log changed in the middle since it was opened is parsed again
	Given that the default log file has been saved beside the tests as UTF-8 with LF line endings
	And that the saved log file has been opened in RAM
	And one character in the middle of the saved log file has been changed
	When the saved log file is opened again in RAM
	Then it will have been parsed, because its saved record table does not match it
	And it will show the same records as a parse of the log

Scenario: A log opened in RAM is parsed again when it is reopened on disk
	Given that the default log file has been saved beside the tests as UTF-8 with LF line endings
	And that the saved log file has been opened in RAM
	When the saved log file is opened again in Disk
	Then it will have been parsed
	And it will show the same records as a parse of the log

Scenario: A log whose saved record table is damaged is parsed again
	Given that the default log file has been saved beside the tests as UTF-8 with LF line endings
	And that the saved log file has been opened in RAM
	And one byte of the saved record table has been changed
	When the saved log file is opened again in RAM
	Then it will have been parsed, because its saved record table does not match it
	And it will show the same records as a parse of the log

Scenario: A damaged record table is saved again by the parse that replaces it
	Given that the default log file has been saved beside the tests as UTF-8 with LF line endings
	And that the saved log file has been opened in RAM
	And one byte of the saved record table has been changed
	And that the saved log file has been opened in RAM
	When the saved log file is opened again in RAM
	Then it will have been opened without being parsed
	And it will show the same records as a parse of the log

Scenario Outline: A log that grew since it was opened is parsed only in its new part, with the same records
	Given that the default log file has been saved beside the tests as <Encoding> with <Line Endings> line endings
	And that the saved log file has been opened in <Mode>
	And records have been appended to the saved log file
	When the saved log file is opened again in <Mode>
	Then it will have been parsed only in its new part
	And it will show the same records as a parse of the log

Examples:
	| Encoding       | Line Endings | Mode |
	| UTF-8          | LF           | RAM  |
	| UTF-8 with BOM | CRLF         | Disk |
	| UTF-16 LE      | CR           | Disk |
	| UTF-16 BE      | LF           | RAM  |
	| UTF-32 LE      | CRLF         | RAM  |

Scenario: A log whose last record gained lines since it was opened shows them in that record
	Given that the default log file has been saved beside the tests as UTF-8 with LF line endings
	And that the saved log file has been opened in RAM
	And lines that continue the last record have been appended to the saved log file
	When the saved log file is opened again in RAM
	Then it will have been parsed only in its new part
	And its last record will span several lines
	And it will show the same records as a parse of the log

Scenario: A log that grew twice is parsed only in its new part each time
	Given that the default log file has been saved beside the tests as UTF-8 with CRLF line endings
	And that the saved log file has been opened in Disk
	And records have been appended to the saved log file
	And that the saved log file has been opened in Disk
	And records have been appended to the saved log file
	When the saved log file is opened again in Disk
	Then it will have been parsed only in its new part
	And it will show the same records as a parse of the log

Scenario: A log that grew and also changed in its old part is parsed in full
	Given that the default log file has been saved beside the tests as UTF-8 with LF line endings
	And that the saved log file has been opened in RAM
	And one character in the middle of the saved log file has been changed
	And records have been appended to the saved log file
	When the saved log file is opened again in RAM
	Then it will have been parsed, because its saved record table does not match it
	And it will show the same records as a parse of the log

Scenario: A log given a line break in the middle since it was opened is indexed again
	Given that the default log file has been saved beside the tests as UTF-8 with LF line endings
	And that the saved log file has been opened in RAM
	And a line break has replaced one character in the middle of the saved log file
	And that the saved log file has been opened in RAM
	When the saved log file is opened again in RAM from line 350
	Then it will show the same records as a parse of the log
