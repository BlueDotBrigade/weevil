# Weevil : Change Log

## Releases

The following summarizes the changes that have been made to Weevil's core engine.  This change log does not include plugin specific features & bug fixes.

### Version 2.6.1

Released on March 27th, 2021

#### What's New?

- The "New Release" notification now includes a "Download" hyperlink. (#92)
- The "Include Debug" & "Include Trace" menu options have been moved to the filter options. (#101, #104)

#### Bug Fixes

- Static aliases do not resolve properly when multiple aliases start with the same text. (#98)

### Version 2.6.0

Released on March 15th, 2021

#### What's New?

- "Filtered" results are now visible after a `Clear()` operation (#78)
- Can now see the progress bar during `Reload()`, so that I know the application is responding to my request. (#77)
- Simplified Weevil's context menu. (#89, #91)
- If Record content includes a .NET callstack, the callstack will be automatically simplified to a more readable form. (#88)
- Case-insensitive filtering is now supported. (#57)

#### Bug Fixes

- Filter history stopped working after log file `Reload()`. (#76)
- Exceptionally long content (e.g. a base64 encoded image) is now automatically truncated and a midline eclipse will be added to the end of the line. (#87)
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