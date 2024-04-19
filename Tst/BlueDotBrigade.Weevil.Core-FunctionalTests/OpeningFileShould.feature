Feature: OpeningFileShould

Ensures that files are opened properly.

# Opening requirements:
# - to support opening empty files
# - default parser creates 1 record per line ?

Scenario: Open empty file
	Given that the following log file is open `Empty.txt`
	Then the record count will be 0

Scenario: Default Log
	Given that the default log file is open
	Then the record count will be 512