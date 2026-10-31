Feature: Pinning

As an analyst, I want to pin or unpin all the records I selected in one step,
so that I can keep them in view while I filter.

Scenario: Toggling the pins of the selected records pins them all when the first is not pinned
	Given that the "ElapsedTime.log" log file name is open
		And line 2 is pinned
	When selecting only lines 1 to 3
		And toggling the pins of the selected records
	Then the record on line 1 will be pinned
		And the record on line 2 will be pinned
		And the record on line 3 will be pinned

Scenario: Toggling the pins of the selected records unpins them all when the first is pinned
	Given that the "ElapsedTime.log" log file name is open
		And line 1 is pinned
		And line 3 is pinned
	When selecting only lines 1 to 3
		And toggling the pins of the selected records
	Then the record on line 1 will not be pinned
		And the record on line 2 will not be pinned
		And the record on line 3 will not be pinned
