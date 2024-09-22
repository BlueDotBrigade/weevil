# Gherkin Guidance

1. Clear and Concise Titles:
	 - Scenario titles should focus on the functionality or behavior being tested rather than on specific examples or data. The titles should succinctly describe what is being tested in a general sense, making them clear and understandable to all stakeholders.	 
	 - Titles should succinctly describe the functionality or behavior being tested without referencing specific data or examples.
	 - Focus on what the system does, not on the specific data used in the test.
	 - Avoid mentioning specific examples or data in the titles.
	 - Use a consistent naming convention across all scenarios.
Structured Steps (Given-When-Then):
2. Consistent Terminology:
	 - Use the same terms to describe similar concepts (e.g., "records," "entries," "lines").
	 - Enclose specific values or options in quotation marks for clarity.
3. Avoid Ambiguity:
	 - Ensure that each step is unambiguous and precisely describes the action or expectation.
	 - Avoid contradictory statements within the same scenario.
4. Maintain Readability:
	 - Use plain English that can be understood by all stakeholders.
	 - Avoid technical jargon unless it's commonly accepted by the audience.

## Given

- DO write in present perfect tense. [#A]

## When

- DO write in present tense.

## Then

- DO write in future tense

## Appendices

### Appendix E: Endnotes

#### [#A] Present Perfect Tense

In Gherkin syntax, "Given" statements describe the context or preconditions before the main actions occur. While they introduce a state or condition that has been established before the scenario's actions, they are typically written in the present tense to reflect the current state of the system.

For example:

"Given the user is logged in"
"Given the account balance is $100"
"Given the default log file is open"
Using the present tense in "Given" statements is standard practice because it clearly describes the current state resulting from past actions, providing a clear and immediate context for the scenario.