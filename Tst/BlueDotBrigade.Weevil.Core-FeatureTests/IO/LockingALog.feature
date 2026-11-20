Feature: Locking a log

While a log is open, Weevil keeps a lock beside it, so another Weevil, on this computer or another, does not open it:
the two would overwrite each other's comments, pins and saved index.

@Requirement:1254
Scenario: A log another Weevil has open does not open, and the reason names who has it
	Given that a copy of the default log file is open in another Weevil
	When the copy is opened
	Then the log will not open, and the reason will name this user and this computer

@Requirement:1254
Scenario: Closing a log removes its lock
	Given that a copy of the default log file is open in another Weevil
	When the other Weevil closes it
	Then the copy will have no lock beside it

@Requirement:1254
Scenario: A log opens once the other Weevil has closed it
	Given that a copy of the default log file is open in another Weevil
	And the other Weevil has closed it
	When the copy is opened
	Then it will show the records of the copy

@Requirement:1254
Scenario: A lock left by a Weevil that crashed does not stop the log from opening
	Given that a copy of the default log file has a lock beside it that no Weevil holds
	When the copy is opened
	Then it will show the records of the copy
	And its lock will name this user and this computer

@Requirement:1254
Scenario Outline: The open log stays locked when it is reloaded or cleared
	Given that a copy of the default log file is open
	When it is <Changed>
	Then another Weevil will not open the copy

Examples:
	| Changed  |
	| reloaded |
	| cleared  |

@Requirement:1254
Scenario: A log whose lock can't be created still opens
	Given that a copy of the default log file has a folder beside it with its lock's name
	When the copy is opened
	Then it will show the records of the copy
