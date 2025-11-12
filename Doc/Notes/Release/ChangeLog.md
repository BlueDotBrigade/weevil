# Weevil : Change Log

## Releases

The following summarizes the changes that have been made to Weevil's core engine.  This change log does not include plugin specific features & bug fixes.

### Version 2.11.0

Release on March 2nd, 2025.

#### What's New?

- Improved user experience:
   - Font size is now configurable. #251 
   - Added a `Ctrl+Shift+M` keyboard shortcut for the `@Comment` moniker #376
   - Regions of Interest (ROI) can be used to define a subset of the log file. #375
   - The title bar only displays the name of the open file, thus making it easier to find the desired Weevil application in the Windows task bar.
   - Reorganized menu to follow standard convention (File, Edit, View, etc.) #344
   - Removed _Flagged_ column in order to maximize visual real-estate. #344
   - Visual indicator now shows when a record has been truncated. #491
- Added support for running _Weevil_ headless.
   - _Command-line_ (CLI) filtering.  #156  #295
   - _PowerShell_ filtering. #155 #294
- Addressed technical debt.
   - Improved how the application is tested. #325 #450 #191
      - Requirements have been written in a standardized [EARS][EARS] syntax. #362 #413
      - Functional tests have been written in "plain English" using [Gherkin][Gherkin] syntax.  #191 
   - Replaced the obsolete Microsoft installer with the latest WiX installer. #310
   - Weevil has more of a _fluent_ like API. #377
   - `FluentAssertions` dependency is now pinned to version 7 #475
   - Updated to the latest dependencies (including .NET Core 9). #336 #391 #392 #452

#### Bug Fixes

- Various

### Version 2.10.0

Release on July 27th, 2022.

#### What's New?

- Analyzers:
  - _Detect Temporal Anomaly_ can be used to flag records where the timestamps appear out of order. #307
- Dashboard insight:
  - Notification when log timestamps are out of order. #307
  - Insight can now be copied directly from the dashboard, thus making it easier to share information. #288
- The .NET Framework dependency has been replaced with Microsoft's .NET Core. #293
- The `New Release` notification message is more intuitive. #290
- `Save Raw` now saves the selected records to a file with the same encoding original file. #315
- The name of the open file is now displayed in the Weevil's title bar. #285
- Third-party NuGet packages have been updated to the latest version. #272
- Performance metrics (e.g. RAM available, RAM used, etc.) are now visible in the _About_ dialog. #277

#### Bug Fixes

- `Save Raw` now saves all records when only 1 record has been selected. #315
- `MainWindowViewModel` was accidentally being instantiated twice. #277

### Version 2.9.0

Released on March 6th, 2022.

#### What's New?

- Line graphs can be created using a regular expression _named group_ to extract data from a log file. #159
   - Using the [RegEx101][RegEx] online utility, you can practice capturing data. In [this][RegEx] example, you can use a _named group_ to detect the word `Never` in a paragraph.
      - `(?<NamedGroup>Never)`
 - The _Detect Time Gap_ analysis tool has been renamed to _Measure Elapsed Time_. #256
 - The analysis tools now support detecting rising & falling edges in both ascending/descending order. #244
 - Exception callstacks no longer include file paths, which help to reduce visual noise. #237
    - The user still has the choice to see the original call stack by using the _copy raw_ feature.
- The _Find_ operation now supports case insensitive searches. #235

#### Bug Fixes

- Compressed files (`*.zip`) can be easily opened by dragging them on to the Weevil application window.  This feature had stopped working in earlier releases. #266

### Version 2.8.1

Released on December 1st, 2021

#### What's New?

- `@ContentLength>32` moniker supports identifying records longer than the given length (e.g. 32 characters) #226
- `@IsMultiline` moniker supports identifying records that span multiple lines (e.g. an exception callstack) #227

#### Bug Fixes

- None

### Version 2.8.0

Released on November 22nd, 2021

#### What's New?

- Navigate between filter results...
  - Find: Search for text in filter results. (#118)
  - GoTo: Jump to a specific timestamp. (#119)
  - GoTo: Jump to a specific line number. (#150)
  - Flagged Records: Quickly navigate between records of interest. (#211)
  - Record Comments: Navigate between records that have a user comment. (#212)
- Clear content between selected records. (#209)
- Includes a *Regular Expression* tool that can be used to check syntax. (#186)
- Keyboard focus is now set to the `TextBox` when an input dialog is displayed. (#166)
- Better handle long log entries (e.g. 2,000,000 characters) (#200)

#### Bug Fixes

- Focus was not properly set after clicking include/exclude ComboBox. (#182)
- Improved performance by using the default `MaxDegreeOfParallelism`. (#161)

### Version 2.7.0

Released on May 22th, 2021

#### What's New?

- A status bar light bulb, and accompanying dashboard has been added to provide insight about the open file. (#20, #117)
- Updated NuGet package dependencies to the latest version. (#111)
- A "Detect Rising Edge" `Analyzer` has been added. (#113)
- A new XML notification message is now used to notify users about new Weevil releases. (#117)
- The status bar tool tips are now more intuitive (#117)
- `Copy+Shift+C` operation will result in the following placeholder `--:--:--` being used when no timestamp is available. (#117)
- Weevil now captures basic hardware information (e.g. CPU & RAM) to facilitate troubleshooting production issues (#125)
- CPU & RAM details are now fully visible in About dialog. (#117)
- `@Elapsed` moniker now has a default value of 3 seconds (#135)
- The existing `@Elapsed` moniker has now been exposed as an `Analyzer` and dashboard `Insight` (#135)
- A full "Garbage Collection" can now be triggered using `Ctrl+Alt+Shift+F12` to help release RAM back to the OS. (#22)
- User has the option to copy either the raw record, or a "friendly" version that has a simplified .NET call stack. (#134)
- Weevil now monitors the responsiveness of it's own user interface. (#110)

#### Bug Fixes

- The status bar now reflects the number of flagged `Record`s post-analysis (#116) (#121)
- The menu no longer remains disabled when: you view the context menu before loading a file (#139)
- Fixed: filter aliases do not resolve properly when multiple aliases start with the same text. (#98)

### Version 2.6.1

Released on March 27th, 2021

#### What's New?

- The "New Release" notification now includes a "Download" hyperlink. (#92)
- The "Include Debug" & "Include Trace" menu options have been moved to the filter options. (#101, #104)

#### Bug Fixes

- Filter aliases do not resolve properly when multiple aliases start with the same text. (#98)

### Version 2.6.0

Released on March 15th, 2021

#### What's New?

- Case-insensitive filtering is now supported. (#57)
- "Filtered" results are now displayed after a `Clear()` operation (#78)
- Can now see the progress bar during `Reload()`, so that I know the application is responding to my request. (#77)
- Simplified Weevil's context menu. (#89, #91)
- If Record content includes a .NET callstack, the callstack will be automatically simplified to a more readable form. (#88)
- Exceptionally long content (e.g. a base64 encoded image) is now automatically truncated and a midline eclipse will be added to the end of the line. (#87)

#### Bug Fixes

- Filter history stopped working after log file `Reload()`. (#76)
- Previously an exception thrown when switching between different types of log files. (#85)

### Version 2.5.2

Released on March 7th, 2021

#### What's New?

- No changes.

#### Bug Fixes

- Fixed a record selection bug that resulted in unexpected results during `Clear Before & After` operation. (#66)
- The `TotalRecordCount` in the status bar now shows the correct value. (#62)
- Added the missing NLog configuration that was preventing Weevil from writing to it's own log file. (#64)

### Version 2.5.1

Released on March 4th, 2021

#### What's New?

- No new features.

#### Bug Fixes

- Fixed an unexpected validation error that prevented a record from being unpinned. (#58)
  - > Value could not be converted.

### Version 2.5.0

Released on March 4th, 2021

#### What's New?

- Weevil now has a "Dark Theme". (#11)
- The `Content` column now automatically wraps to fit within the window.
- Added an `About` dialog box (#6)
- `Alt+I` and `Alt+E` keyboard shortcuts will move cursor to the inclusive/exclusive filter `ComboBoxes`. (#36)

#### Bug Fixes

- Icon in the status bar reminds the user when a `Clear` operation has been performed (#29)
- The "new release available" notification works again.

### Version 2.4.0

This version was not officially released.

#### What's New?

- `Open As` dialog box can be used to load only a portion of a log file.
  - This feature is useful when using Weevil to open large log files.
- Weevil can now open any plain text file.
- Resource usage report now includes a `WorkingSet64` column.

#### Bug Fixes

- Weevil would crash when using "Detect Data" and the same key/value pair was detected more than once.
- Files locked by another process can now be opened.
- Weevil was unable to find the `Plugins` directory, if the application was moved to a different directory.
- The log's sidecar (e.g. user comments) were not saved if you clicked "Close" (`X`) in the application's title bar.
- Record `Comment` was not saved, if the record was removed from memory as the result of a `Clear` operation.
