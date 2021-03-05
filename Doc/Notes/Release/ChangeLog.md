# Weevil : Change Log

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
