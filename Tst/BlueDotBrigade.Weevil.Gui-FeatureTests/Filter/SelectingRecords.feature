Feature: Selecting records

As an analyst, I want the grid, the status bar and the navigation commands to agree on which records are selected,
so that I can see what I chose and act on it.

# Droid.log has 387 records. Lines 1 to 20 are one record each: lines 1 and 8 mention Directives, lines 2 to 7 do not.

@Requirement:1064
Scenario: Shift+clicking a line selects the range from the line clicked before, and the status bar counts it
	Given that the default log file is open
	When clicking line 3
	And Shift+clicking line 6
	Then the status bar will show 4 selected records
	And line number 5 will be selected

@Requirement:1064
Scenario: Shift+End selects through the last record, and the grid shows it
	Given that the default log file is open
	When clicking line 3
	And pressing Shift+End
	Then the status bar will show 385 selected records
	And the grid will show and focus the last record

@Bug:806
Scenario: Going to the next comment repeatedly selects only the comment it lands on
	Given that the default log file is open
	And line 10 has the comment "First"
	And line 20 has the comment "Second"
	And lines 2 to 5 are selected
	When going to the next comment 3 times
	Then the status bar will show 1 selected record
	And line number 10 will be selected
	And the grid will show and focus line 10

@Requirement:1015
Scenario: A filter keeps the selected records it still shows, and the status bar counts them
	Given that the default log file is open
	When clicking line 1
	And Ctrl+clicking line 2
	And applying the include filter: Directives
	Then there will be 7 matching records
	And the status bar will show 1 selected record
	And line number 1 will be selected
