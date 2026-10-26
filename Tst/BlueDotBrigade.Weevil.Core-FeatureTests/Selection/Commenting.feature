Feature: Commenting

As an analyst, I want to put one comment on all the records I selected,
so that I can mark them in one step, for example by pasting text onto them.

Scenario: Commenting the selected records keeps the comments they already have
	Given that the "ElapsedTime.log" log file name is open
		And line 2 has the comment: Retry
	When selecting only lines 1 to 3
		And commenting the selected records: Checked
	Then the record on line 1 will have the comment: Checked
		And the record on line 2 will have the comment: Retry
		And the record on line 3 will have the comment: Checked

Scenario: Commenting the selected records can replace the comments they already have
	Given that the "ElapsedTime.log" log file name is open
		And line 2 has the comment: Retry
	When selecting only lines 1 to 3
		And commenting the selected records, replacing their comments: Checked
	Then the record on line 2 will have the comment: Checked

Scenario: Commenting the selected records with only white space stores nothing
	Given that the "ElapsedTime.log" log file name is open
		And line 2 has the comment: Retry
	When selecting only lines 1 to 3
		And commenting the selected records with only white space
	Then the record on line 1 will have no comment
		And the record on line 2 will have the comment: Retry
