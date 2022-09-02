Feature: OpeningFileShould

Ensures that files are opened properly.

#340

Scenario: Open empty file
	Given that Weevil has started
	When the user opens the `<FileName>` file
	Then the record count shall be <RecordCount>

Examples:
	| FileName                | RecordCount |
	| Empty.txt               | 0           |