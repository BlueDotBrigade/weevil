Feature: Selecting

As an analyst, I want to select records with the mouse and the keyboard
as I do in other Windows lists, so that I can act on the records I chose.

# ElapsedTime.log: lines 1, 3, 5 and 6 mention Alpha; lines 2 and 4 mention Bravo.

@Requirement:1062
Scenario: Selecting from one line to another selects the records the filter shows between them
	Given that the "ElapsedTime.log" log file name is open
	When applying the include filter: Alpha
	And selecting only lines 1 to 5
	Then 3 records will be selected
	And line number 5 will be selected
	And line number 6 will not be selected

@Requirement:1062
Scenario: Selecting a range from the bottom up replaces the selection
	Given that the "ElapsedTime.log" log file name is open
	When selecting only lines 1 to 2
	And selecting only lines 5 to 4
	Then 2 records will be selected
	And line number 4 will be selected
	And line number 1 will not be selected
	And the anchor will be line 5

@Requirement:1062
Scenario: Adding a range keeps the records already selected
	Given that the "ElapsedTime.log" log file name is open
	When selecting only lines 1 to 1
	And adding lines 4 to 5 to the selection
	Then 3 records will be selected
	And line number 1 will be selected
	And the anchor will be line 4

@Requirement:1062
Scenario: Toggling a selected record unselects it and makes it the anchor
	Given that the "ElapsedTime.log" log file name is open
	When selecting only lines 1 to 3
	And toggling the selection of line 2
	Then 2 records will be selected
	And line number 2 will not be selected
	And the anchor will be line 2

@Requirement:1062
Scenario: Toggling an unselected record adds it to the selection and makes it the anchor
	Given that the "ElapsedTime.log" log file name is open
	When selecting only lines 1 to 1
	And toggling the selection of line 3
	Then 2 records will be selected
	And line number 3 will be selected
	And the anchor will be line 3

@Requirement:1062
Scenario: Without an anchor, a range starts from the line selected
	Given that the "ElapsedTime.log" log file name is open
	When selecting only lines 2 to 3
	And applying the include filter: Alpha
	And selecting only from the anchor to line 5
	Then 1 records will be selected
	And line number 5 will be selected
	And the anchor will be line 5

@Requirement:1062
Scenario: Selecting every record keeps the anchor
	Given that the "ElapsedTime.log" log file name is open
	When selecting only lines 2 to 3
	And selecting every record
	Then 6 records will be selected
	And the anchor will be line 2

@Requirement:1062
Scenario: The anchor stays when the filter still shows its record
	Given that the "ElapsedTime.log" log file name is open
	When selecting only lines 3 to 4
	And applying the include filter: Alpha
	Then the anchor will be line 3

@Requirement:1062
Scenario: The anchor is dropped when the filter hides its record, and stays dropped when a later filter shows it
	Given that the "ElapsedTime.log" log file name is open
	When selecting only lines 2 to 3
	And applying the include filter: Alpha
	And applying the include filter: Alpha||Bravo
	Then there will be no anchor

@Requirement:1062
Scenario: Reloading the log keeps the selection and the anchor
	Given that the "ElapsedTime.log" log file name is open
	When applying the include filter: Alpha
	And selecting only lines 3 to 5
	And reloading the log
	Then 2 records will be selected
	And line number 3 will be selected
	And line number 5 will be selected
	And the anchor will be line 3

@Requirement:1062
Scenario: Navigation continues from the line a range was extended to, not from its anchor
	Given that the default log file is open
	When pinning the record on line 10
	And pinning the record on line 20
	And selecting only lines 25 to 15
	And navigating to the next pinned record
	Then the selected record line number will be 20

@Requirement:1062
Scenario: Reloading the log keeps the line navigation continues from
	Given that the default log file is open
	When pinning the record on line 10
	And pinning the record on line 20
	And selecting only lines 25 to 12
	And reloading the log
	And navigating to the next pinned record
	Then the selected record line number will be 20
