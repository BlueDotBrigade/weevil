Feature: OpeningFileShould

Ensures that files are opened properly.

Scenario: Open empty file
	Given that Weevil has started
	When the user opens the `<FileName>` file
	Then the record count shall be <ExpectedCount>

Examples:
	| FileName                   | ExpectedCount |
	| EmptyFile.txt              | 0             |
	| FileWithOnlyWhitespace.txt | 0             |
	