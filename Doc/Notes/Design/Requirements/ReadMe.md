# About Writing Documentation
  
## Appendices

### VS Code Shortcuts

- `Ctrl+Space` : to see available snippets
- `Ctrl+Alt+I` : to start CoPilot GPT chat

### Additional Reading

- https://alistairmavin.com/ears
- https://www.jamasoftware.com/requirements-management-guide/writing-requirements/adopting-the-ears-notation-to-improve-requirements-engineering

### DSL Terminology

- Filter Expressions:
	- text : This filter type is used to identify records that include specific content.
	- alias : A keyword that refers to a built-in regular expression.
	- moniker : Can be used to query metadata collected by the system.
- record : Refers to a log entry that typically takes up 1 line of a log file. typically refers to 1 line of a log file, but can represent multiple lines when the source application writes a log etnry that ina callstack

### Templates

#### Git Template

```T
# Input
- Chronologically sorted records.
- Regular expression that includes a named group.

# Result
- Record is flagged when the named group detects a value that is different from the last time a match was made.

# Example


Line Time Record
0001 9:01 Variable=100
0002 9:02 Hello World
0003 9:03 Variable=100
0004 9:04 Variable=400

The following named group results in record on line 4 being flagged.

Variable=(?<>\d\d\d)
```

#### Unknown Format

With this format represents use cases (UC) and their associated functional (F) and non-functional (NF) requirements. Each requirement is also tagged with a priority level (in this case, "MUST").

This kind of hierarchical structuring is commonly used in software engineering for requirements specification, but there isn't a single, universally-accepted name or standard for this specific format. It resembles elements of various requirements engineering methodologies and notations, such as Use Case Modeling, IEEE 830 (for Software Requirements Specifications), and the Volere Requirements Specification Template, among others.

The format is designed to provide a structured way to capture both what the system is supposed to do (functional requirements like "F1.1") and how well it should do it (non-functional requirements like "NF1.1"), all organized under broader use cases ("UC1," "UC2," etc.) that describe a system's interactions with external elements (often users).

```T
- UC1: Filter by plain text
  - F1.1: Display only records that contain the specified text. (MUST)
  - NF1.1: Response time for filtering operation is less than 3 seconds. (MUST)
  - ...
- UC2: Filter by regular expressions
  - F2.1: ...
  ...
```

#### EARS Syntax

UBIQUITOUS

`The <system name> shall <system response>`
These requirements are always active (so there is no EARS keyword)
Example: The mobile phone shall have a mass of less than XX grams.

STATE DRIVEN

`While <precondition(s)>, the <system name> shall <system response>`
These requirements are active as long as the specified state remains true and are denoted by the keyword While.
Example: While there is no card in the ATM, the ATM shall display "insert card to begin".

EVENT DRIVEN

`When <trigger>, the <system name> shall <system response>`
These requirements specify how a system must respond when a triggering event occurs and are denoted by the keyword When.
Example: When “mute” is selected, the laptop shall suppress all audio output.

OPTIONAL FEATURE

`Where <feature is included>, the <system name> shall <system response>`
These requirements apply in products or systems that include the specified feature and are denoted by the keyword Where.
Example: Where the car has a sunroof, the car shall have a sunroof control panel on the driver door.

UNWANTED BEHAVIOR

`If <trigger>, then the <system name> shall <system response>`
These requirements are used to specify the required system response to undesired situations and are denoted by the keywords If and Then.
Example: If an invalid credit card number is entered, then the website shall display "please re-enter credit card details".

COMPLEX

`While <precondition(s)>, When <trigger>, the <system name> shall <system response>`
These requirements can be used to express richer system behavior by using more than one EARS keyword.
Example: While the aircraft is on ground, when reverse thrust is commanded, the engine control system shall enable reverse thrust.
Complex requirements for unwanted behavior also include the If-Then keywords.

#### EARS Best Practices

1. Use clear and concise language to express the requirement.
2. Use active voice to describe the system behavior.
3. Use specific and measurable criteria to define the requirement.
4. Use consistent terminology throughout the requirements document.
5. Use the appropriate EARS type to express the system behavior.
6. Use examples to illustrate the requirement and clarify any ambiguity.
7. Use traceability to link requirements to design, implementation, and testing.
8. Use version control to manage changes to the requirements document.
9. Use reviews and inspections to ensure the quality of the requirements.
10. Use a requirements management tool to organize, track, and report on the requirements.