Feature: Alias

@Requirement:398, @Requirement:406
Scenario: Filtering using an alias expression
  Given that the default log file is open
  When applying the include filter: #Critical
  Then there will be 1 matching records
    And all records will include: Unrecoverable error has occurred

@Bug:1294
Scenario: Filtering a TSV log with the IPv4 alias keeps the records that hold an address
  # The TSV extension's alias escaped its digits twice, so it matched no address.
  Given that the "IpAddresses.log" log file name is open
  When applying the include filter: #IPv4
  Then there will be 2 matching records
    And all records will include: [Network]
