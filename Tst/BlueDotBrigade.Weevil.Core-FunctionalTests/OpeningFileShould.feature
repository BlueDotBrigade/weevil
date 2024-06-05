Feature: OpeningFileShould

Ensures that files are opened properly.

# Opening requirements:
# - to support opening empty files
# - default parser creates 1 record per line ?

Scenario: Delete Me
	Given that the 'Empty.txt' log file is open
	Then there will be 0 records

Scenario: Delete Me Too
	Given that the `Empty.txt` log file is open
	Then there will be 0 records

Scenario: Open empty file
	Given that the "Empty.txt" log file is open
	Then there will be 0 records

Scenario: Default Log
	Given that the default log file is open
	Then there will be 512 records