# Scratch Pad

## To Do

1. [X] Review each scenario 1 last time to ensure the standardize syntax is being used.
2. [X] Standardize Gherkin... 
	 - should statements include colon?
		  - probably best to **ot** use double quotes around text, because filters can contain any character				-
3. [ ] When term should requirements & scenarios refer to: `records` or `results` ?
	 - Consider requirements: https://github.com/BlueDotBrigade/weevil/issues/413
	 - And Gherkin `Then` statements
2. [ ] Write the code-behind for the Gherkin steps
3. [ ] Status bar
	- move "flagged records" to the section with "selected records"
4. [ ] Update the context Menu
	- Add to the menu: show records with comments "Ctrl+Shift+M"
	- Rename: Anaylze -> Analytics
	- Move "Show Graph" to : Analytics 
	- Move "Remove Flags" to: Analytics 
	- Rename: Analyze More -> Plugin Analytics
5. [ ] Fix Toggle Filtering <<< does this work properly?
	- Ctrl+Shift+T to turn off all filtering
	- Ctrl+Shift+T to re-apply previous filter

## Gherkin Syntax

### Given

Given that the default log file is open
Given that the "Empty.txt" log file is open
Given that the log file is open at "c:\Temp\Empty.txt"

### When

When selecting the plain text filter mode
When selecting the regular expression filter mode

When the case sensitive option is on/off
When the debug records option is on/off
When the trace records option is on/off
When the persistent pins option is on/off

When applying the include filter: 
When entering the include filter: 
When applying the exclude filter: 
When entering the exclude filter: 
When applying the filters
When canceling the filtering

When pinning record ID 2 <<<<<<<<<<<<<<<<<<<<<<

### Then

Then there will be 36 results <<< DO NOT USE
Then there will be 36 visible records
Then each result will include: abc
Then each result will include either
"""
enter
list
here
"""
Then each result will exclude: abc
Then each result will exclude
"""
enter
list
here
"""

Then each result will be pinned
Then the 4th result will be pinned
Then the results will be
"""
"""


Then the visible record count in the status bar will be 123,456
Then the selected record count in the status bar will be 123,456
Then the total record count in the status bar will be 123,456
Then the flagged record count in the status bar will be 123
Then the elapsed time in the status bar will be 4:00
Then the context in the status bar will be: 1.2.3.4 SomeKey=SomeValue
Then the message in the status bar will include: hello world

