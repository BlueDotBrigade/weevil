Feature: Sandbox

A short summary of the feature

Scenario: Can reqnroll access WPF ViewModel property?
Given that the default log file is open
Then try to read ViewModel property


Scenario: Exception thrown when reading ViewModel property
Given that the default log file is open
Then exception thrown when reading ViewModel property via the token