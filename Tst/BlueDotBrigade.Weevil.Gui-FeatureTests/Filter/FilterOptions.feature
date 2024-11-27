Feature: Filter Options

A short summary of the feature

@Requirement:XXX
Scenario: Filter is automatically applied when option is on
	Given that the default log file is open
	When the automatic filtering option is on
		 And entering the include filter: #Information
		 And waiting 5 sec for: filter to be automatically applied
	Then there will be 37 matching records

@Requirement:XXX
Scenario: Filter is not automatically applied when typing continues
	Given that the default log file is open
	When the automatic filtering option is off
		 And entering the include filter: #Information
		 And waiting 5 sec for: filter to be automatically applied
	Then there will be 387 matching records

# NOTE: automatic filtering is somewhat unique in that it is applied, in part, by code within XAML
# 
# REQUIREMENT
# https://github.com/BlueDotBrigade/weevil/issues/394
# https://github.com/BlueDotBrigade/weevil/issues/420
# The software shall have an option to turn automatic filtering on (default) or off
# While the automatic filtering option is on, when the user stops typing for more than 3 seconds, the software shall automatically apply the filter. #410
# 
# MANUAL TEST
# https://github.com/BlueDotBrigade/weevil/issues/405
#
# Filters will be automatically applied after 3 seconds
# Filters will not be applied if typing continues (less than 3 second gap)
