# Testing

## General

Sample log file: /weevil/Tst/BlueDotBrigade.Weevil.Core-UnitTests/.Daten/.Global/Default.log

## Unit Testing

These tests ensure that a class' methods & properties are working as expected.

## Functional Testing

The Weevil core engine can be accessed via:
1. WPF Application
2. Console Application / Command-Line Interface (CLI)
3. PowerShell

For this reason, it is best to perform the majority of testing at the `BlueDotBrigade.Weevil.Core` level.



This project uses two approaches to functional testing:
1. Gherkin scenarios
2. Integration tests (i.e. non-Gherkin)
