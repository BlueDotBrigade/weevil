# Filtering

Sample log file: /weevil/Tst/BlueDotBrigade.Weevil.Core-UnitTests/.Daten/.Global/Default.log

## User Stories

As an analyst, I want to be able to filter log entries using plain text, so that I can easily find relevant information.

As an analyst, I want to be able to filter log entries using regular expressions, so that I can perform complex searches.

As an analyst, I want to be able to pin important log entries, so that they are always included in the filter results.

As an analyst, I want a case-sensitive filter option, so that I can distinguish between text with different capitalization (e.g. `ID` vs `Grid`).

As an analyst, I want a hide `Trace` records filter option (default: on), so that useless log entries are hidden thus reducing visual noise.

As an analyst, I want a hide `Debug` records filter option (default: on), so that useless log entries are hidden thus reducing visual noise.

As an analyst, I want Weevil to automatically apply the current filter 3 seconds after I stop typing, in order to speed up the filtering process.

As an analyst, I want an `include pinned` filter option (default: on), so I can decide if pinned records appear in the search results.

## Requirements

Sample log file entry:

```
Info 2018-11-23 11:20:35 CompanyName.ApplicationName.ClassName Application is starting... Version=1.2.3
```


Requirement:

The system shall display the number of records in the filter results in the status bar, using clear and concise language, in active voice, and with specific and measurable criteria. This requirement is ubiquitous, as it is always active, and is expressed using the EARS syntax.
Requirement:

The system shall display the number of records in the filter results in the status bar, using clear and concise language, in active voice, and with specific and measurable criteria. This requirement is ubiquitous, as it is always active, and is expressed using the EARS syntax.

Requirement:

The system shall display the number of records in the filter results in the status bar, using clear and concise language, in active voice, and with specific and measurable criteria. This requirement is ubiquitous, as it is always active, and is expressed using the EARS syntax.

### Filter Options

1. The system shall have text expression option of either "plain text" or "regular expression".
2. While the `hide trace` option turned on, when filtering, the system shall implicitly apply a `@Severity=Trace` exclude filter.
3. While the `hide debug` option turned on, when filtering, the system shall implicitly apply a `@Severity=Debug` exclude filter.
4. While the `show pinned` option turned off, when filtering, the system shall ignore the `pinned` status of records.

### Expressions

Text:

1. While "plain text" mode is selected, the system shall interpret text expressions as a "plain text" filter.
2. While "regular expression" mode is selected, the system shall interpret text expressions as "regular expressions".

Aliases:

1. The system, shall interpret expressions prefixed with the `#` symbool as a filter alias.
2. When using the `#IpAddress` alias, the system shall use a regular expression to identify records that include a computer's IP address.

Monikers:

1. The system, shall interpret expressions prefixed with the `@` symbool as a filter monkier.
2. When using the `@Comment` monkier, the system shall identify log file records that include an analyst's annotation.
3. When using the `@Pinned` monkier, the system shall identify records that have been "pinned" by the user.
4. When using the `@Severity=[level]` monkier, the system shall identify records that match the given severity (trace, debug, information, warning, error, critical).

## Scenarios

## Appendices

### Domain Specific Language (DSL)

- record: Represents a log entry which typically corresponds with 1 line from a log file, but may wrap to additional lines when "newline" characters are embedded in the log message (e.g. a callstack).
- filter: Are used to identify matching records.
- alias: Is a pre-defined filter which is represented by a key/value pair prefixed by a hashtag (e.g. #Severity=Warning)
- moniker: Are used to query metadata collected by Weevil. This key/value pair is prefixed by an "at" symbol (e.g. @HasComment=True)
- analyst: In the context of a user story, an "analyst" is the user who is using the Weevil application.

| Preferred Term | Do not refer To                           |
| -------------- | ----------------------------------------- |
| analyst        | user                                      |
| as an analyst  | As an analyst using the Weevil log viewer |
| record         | log entry                                 |

