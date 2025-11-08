Feature: Detect Stable Values

A short summary of the feature

@Requirement:540
Scenario: No stable values found when no named groups are captured
        Given that the StableValueAnalyzer.log log file name is open
        When applying the include filter: Temperature=Cold
                And detecting stable values using the include filter expressions
        Then the flagged record count will be 0

@Requirement:540
Scenario: Plateau boundaries are flagged and annotated
        Given that the StableValueAnalyzer.log log file name is open
        When applying the include filter: Temperature=(?<State>[A-Za-z]+)
                And detecting stable values using the include filter expressions
        Then the flagged record count will be 5
                And the record on line 1 will have the comment: Start State: Cold
                And the record on line 3 will have the comment: Stop State: Cold
                And the record on line 4 will have the comment: Start State: Warm
                And the record on line 5 will have the comment: Stop State: Warm
                And the record on line 6 will have the comment: Start State: Hot, Stop State: Hot
