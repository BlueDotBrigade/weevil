Feature: OpeningFileShould

Ensures that files are opened properly.

# Opening requirements:
# - to support opening empty files
# - default parser creates 1 record per line ?

Scenario: Open empty file
	Given the user has opened the log file `<FileName>`
	Then the record count shall be <RecordCount>

Examples:
	| FileName                | RecordCount |
	| Empty.txt               | 0           |

Scenario: Default Log
	 Given the user has opened the default log file
	 Then the record count shall be 512

Scenario: Specific Log
	 Given the user has opened the log file `Empty.txt`
	 Then the record count shall be 0
