Feature: Filtering

*** USER STORIES***

As an analyst, I want to be able to filter log entries by plain text, so that I can find the information I need. #364

As an analyst, I want to be able to filter log entries using regular expressions, so that I have a more powerful search capability. #367

As an analyst, I want to be able to 'pin' a record, so that the record always appears in the filter results. #370

As an analyst, there should be a shortcut for @Comments filter, to make it easier to use this frequently used tool. #376

1.	Text Pattern Matching:

As an analyst,
I want to enter a text pattern to filter log entries,
So that I can view records matching the pattern based on the selected mode (RegEx or plain text).

2.	Filter Alias:

As an analyst,
I want to use filter aliases prefixed with a hashtag,
So that I can quickly apply domain-specific filters to the log entries.


3.	Moniker for Metadata Query:

As an analyst,
I want to use monikers prefixed with the at symbol,
So that I can query specific metadata collected by Weevil and filter log entries accordingly.


4.	Pinned Moniker:

As an analyst,
I want to use the @Pinned moniker,
So that I can easily filter and view all records that I've flagged as pinned.


	5.	Severity Moniker:

As an analyst,
I want to use the @Severity=[level] moniker,
So that I can filter log entries based on their severity level (info, warning, error, critical).

***EARS SYNTAX***

Generic EARS syntax
The clauses of a requirement written in EARS always appear in the same order. The basic structure of an EARS requirement is:
While <optional pre-condition>, when <optional trigger>, the <system name> shall <system response>
The EARS ruleset states that a requirement must have: Zero or many preconditions; Zero or one trigger; One system name; One or many system responses.
The application of the EARS notation produces requirements in a small number of patterns, depending on the clauses that are used. The patterns are illustrated below.

Ubiquitous requirements are always active (so there is no EARS keyword)
The <system name> shall <system response>
Example: The mobile phone shall have a mass of less than XX grams.

State driven requirements are active as long as the specified state remains true and are denoted by the keyword While.
While <precondition(s)>, the <system name> shall <system response>
Example: While there is no card in the ATM, the ATM shall display “insert card to begin”.

Event driven requirements specify how a system must respond when a triggering event occurs and are denoted by the keyword When.
When <trigger>, the <system name> shall <system response>
Example: When “mute” is selected, the laptop shall suppress all audio output.

Optional feature requirements apply in products or systems that include the specified feature and are denoted by the keyword Where.
Where <feature is included>, the <system name> shall <system response>
Example: Where the car has a sunroof, the car shall have a sunroof control panel on the driver door.

Unwanted behaviour requirements are used to specify the required system response to undesired situations and are denoted by the keywords If and Then.
If <trigger>, then the <system name> shall <system response>
Example: If an invalid credit card number is entered, then the website shall display “please re-enter credit card details”.

Complex requirements
The simple building blocks of the EARS patterns described above can be combined to specify requirements for richer system behaviour. Requirements that include more than one EARS keyword are called Complex requirements.
While <precondition(s)>, When <trigger>, the <system name> shall <system response>
Example: While the aircraft is on ground, when reverse thrust is commanded, the engine control system shall enable reverse thrust.
Complex requirements for unwanted behaviour also include the If-Then keywords.

***REQUIREMENTS***

One or more filter criteria can be used to show or hide log file records.

1. Inclusive and Exclusive Filters
    - Display records matching the inclusive filter while hiding those matching the exclusive filter.
2. Filter Criteria
	 1. Plain Text
	 2. Regular Expressions
	 3. Aliases
		  - Frequently used or complex filters can be assigned a unique key that can be used to speed up the filtering process.
		  - For example, the `#IpAddress` key could be assigned to the following filter criteria  `^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$`.
	 4. Monikers
        - Monikers are built-in keys that can be used to query metadata collected by _Weevil_.
		  - For example, the `@Comment` can be used to identify records that have a user comment.
3. Multiple Criteria
    - Multiple filter criteria can be combined together using a logical "OR" operator (`||`).
4. Pinned Records
	 - Pinned records are guaranteed to be included in the filter results.

Filtering

0. When creating a filter, the system shall support a combination of filter types delimited by an OR (`||`) operator.
1. The system shall allow analysts to select a filter mode: plain text or regular expression.
2. The system shall allow analysts to enter a text pattern to filter log entries based on the selected mode (RegEx or plain text).
3. The system shall support filter aliases prefixed with a hashtag for domain-specific filtering.
4. The system shall support monikers prefixed with the at symbol to query specific metadata collected by Weevil.
5. When an analyst uses the @Pinned moniker, Then all records flagged as pinned shall be displayed.
6. When an analyst uses the @Severity=[level] moniker, Then log entries based on their severity level (info, warning, error, critical) shall be displayed.
7. When an analyst pins a log entry, Then that entry shall always be displayed, regardless of any EXCLUDE filters applied.
8. The system shall provide a case-sensitive option for the INCLUDE filter.
9. The system shall have an EXCLUDE filter for `Trace` records enabled by default.
10. The system shall have an EXCLUDE filter for `Debug` records enabled by default.
11. While an analyst is setting a filter, Then the system shall automatically apply the filter 3 seconds after the analyst stops typing.

Filter type: Text
Filter type: Moniker
Filter type: Alias
1. The system shall allow analysts to select between 'plain text' and 'regular expression' filter modes.
2. The system shall enable analysts to input a text pattern for filtering log entries based on the selected mode.
3. The system shall recognize filter aliases prefixed with a hashtag for domain-specific filtering.
4. The system shall interpret monikers prefixed with the at symbol to query specific metadata collected by Weevil.


1. When an analyst uses the @Pinned moniker, the system shall display all records flagged as pinned.
2. When an analyst uses the @Severity=[level] moniker, the system shall display log entries corresponding to the specified severity level.
3. When an analyst pins a log entry, the system shall persistently display that entry, irrespective of any exclude filters applied.
4. The system shall offer a case-sensitive setting for the include filter.

Filter Critieria
1. The system shall enable an exclude filter for `Trace` records by default.
2. The system shall enable an exclude filter for `Debug` records by default.

11. While an analyst is inputting a filter, the system shall automatically activate the filter 3 seconds after the analyst ceases typing.


SCENARIOS

  Scenario: Opening file
    Given the default log is open
    Then the record count will be 123

  Scenario: Displaying records that match an inclusive filter
    Given the analyst wants to search for specific records
    And filters are available for the analyst to use
    When the analyst applies an inclusive filter to the records
    Then the system should only display records that matches the filter criteria

  Scenario: Applying a filter using a domain-specific alias
    Given I am viewing a list of log entries
    When I apply the #Summary filter alias
    Then only key high-level log entries should be displayed

  Scenario: Filtering using a moniker for metadata query
    Given I am viewing a list of log entries
    When I apply the @Pinned moniker
    Then only records flagged as pinned should be displayed

  Scenario: Filtering based on severity using a moniker
    Given I am viewing a list of log entries
    When I apply the @Severity=warning moniker
    Then only log entries with a warning severity level should be displayed
