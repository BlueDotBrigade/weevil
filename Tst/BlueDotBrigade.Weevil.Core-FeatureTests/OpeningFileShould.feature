Feature: OpeningFileShould

Ensures that files are opened properly.

# Opening requirements:
# - to support opening empty files
# - default parser creates 1 record per line ?

Scenario: Open empty file
	Given that the "Empty.log" log file name is open 
	Then there will be 0 visible records

Scenario: Default Log
	Given that the default log file is open
	Then there will be 387 visible records