Feature: Calculator

@Requirement:327
Scenario: Add two numbers
	Given the first number is 50
		And the second number is 70
	When the two numbers are added
	Then the result should be 120

@Requirement:323
Scenario: Subtract two numbers
	Given the first number is <First>
		And the second number is <Second>
	When the two numbers are subtracted
	Then the result should be <Answer>

Examples:
	| First | Second | Answer |
	| 50    | 10     | 40     |
	| 50    | 20     | 30     |

@Requirement:100
Scenario: Multiply two numbers
	Given the first number is 2
		And the second number is 10
	When the two numbers are multiplied
	Then the result should be 20