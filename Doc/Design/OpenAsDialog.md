# Open As Dialog

One window to find a log and choose how to open it (#1157): a file browser, beside an options area that the host fills, and Open and Cancel. It replaced the Windows file dialog, the Select File window for zips and the separate Open As window of #1028. It is plain WPF, written from scratch, with no third-party code. The [#1157 plan](../Plans/1157-OpenAsDialog.md#decisions) records how it was decided. How Core then opens the log, or part of it, is in [OpeningALog.md](OpeningALog.md).

## The Parts

| Part | Where | What |
|---|---|---|
| The generic control | `BlueDotBrigade.Weevil.Windows.Dialogs` | `FileBrowser` and its model, `OpenAsDialog`, and in `Storage` what reads drives, folders and zips, such as `ContainerReader` and `Location`. Knows nothing about Weevil. |
| The plugin interface | `BlueDotBrigade.Weevil.Windows` (`IO`) | `IOpenAsOptions` and `IOpenAsOptionsProvider`, the NuGet package plugins build against. |
| Weevil's host | `BlueDotBrigade.Weevil.Gui` (`IO`) | `DialogBoxService.TryShowOpenAs` sets the dialog up. `OpenAsOptionsViewModel` and `OpenAsOptionsView` are Weevil's options, `OpenAsAdvice` suggests RAM or Disk, `OpenAsLookup` reads a log's parts and finds its plugin, and `OpenAsOptionsHost` chooses whose options are shown. |

## The Generic Control

Other Blue Dot Brigade applications will share it, so its project references nothing but WPF: no Weevil project, MaterialDesign or Metalama (#1333). It has no theme of its own: standard WPF controls, so the host's application-wide styles restyle them. Its change notification is written by hand. It moves to the shared library after 3.0 (#1341).

### Browsing

- **`FileBrowser`** shows places on the left (This PC, which lists the drives, then Desktop, Documents, Downloads and the recent folders), Back, Forward, Up and Refresh, a path box, a name filter, the file types, and a list of folders, zips and files with their size, type and date, sorted by any column. Folders and drives come first, then zips and files together, as in Explorer. Names and types sort as Explorer sorts them, with Windows' `StrCmpLogicalW`: case is ignored and digits compare as numbers, so `app.log.2` comes before `app.log.10` (#1344).
- **The keyboard alone works,** with the keys `FileBrowser`'s remarks list, and screen readers name each control (`AutomationProperties.Name`).
- **Off the UI thread:** `FileBrowserModel` lists each folder on a background thread. A newer navigation discards an older one's result, because a listing blocked on a share that doesn't answer can't be cancelled. The window stays responsive.
- **Where it starts:** the location the host gives. Without one, the most recent folder that can be found, or else Documents. A location that can't be found falls back to This PC.
- **What a folder holds:** hidden folders such as ProgramData and AppData are listed, since many logs live there; those both hidden and system are not, as in Explorer. A folder that can't be read shows why, instead of appearing empty. A file that OneDrive keeps online only shows a cloud, and nothing reads it until the analyst opens it, since reading it downloads it.
- **Zips are folders.** `ContainerReader` lists a zip, and zips inside it to any depth, without extracting it. A zip inside a zip is copied to a temporary folder the first time it is read, since a zip can be read only from a stream that can seek. `Location` names a place inside zips: `C:\Logs.zip\2026\Old.zip\App.log` is `C:\Logs.zip`, then `2026/Old.zip`, then `App.log`. A damaged zip shows why instead of an empty list. So does a file protected by a password, ZipCrypto or AES (#1211): .NET lists such a zip's files but can't read them, so opening one, or entering a zip inside it, says that a password protects it and to extract it with the program that made the zip. A zip whose list of files is itself encrypted can't be listed, so it is shown as damaged. An entry whose name a file can't have, such as `C:other.log`, is listed, but opening it says the zip names a file outside the folder, and Weevil reads neither its sidecar nor its plugin.
- **The path box** (`Location.Parse`) enters each zip in the path typed or pasted. `\\server\share` works, though the network's servers aren't browsed. A drive's root is written `C:\`, and a drive's letter alone, such as `C:`, is read as the root, as Explorer reads it (#1328): to Windows, `C:` alone is the current folder on drive C.
- **Left out on purpose** (#1157): Windows' indexed search, the right-click menu, Quick Access pins, browsing a network's servers, thumbnails and the preview pane.

### Opening

- **`OpenAsDialog`** reports the item selected (`SelectionChanged`), so the host updates its options, and enables Open while the host's `CanOpen` is true. Double-clicking a file, or pressing Enter on it, is Open.
- **On Open,** a file on disk is given as it is. A file in a zip is extracted with its *companions*, the files the host names (`CompanionSuffixes`) that travel with it, such as `App.log.xml` beside `App.log`. With `CopiesFolder`, it is extracted to a folder of its own named by `GetCopyKey`, so the same file opened again finds the companions kept beside it (#1253). Without it, to a new temporary folder.
- **`CheckBeforeOpening`** runs off the UI thread before anything is extracted or the window closes. An `IOException` it throws keeps the window open and shows its message.
- **Cancel,** or closing the window, opens nothing and changes no file. It stops an extraction that is running, and deletes what it had copied so far (#1251). An extraction that finishes just as the window closes is deleted from a temporary folder, but a copy in `CopiesFolder` stays: it may be the log the host already has open, and the next open of the same file reuses it (#1253).
- **What the host gives it:** where to start (`InitialLocation`), the recent folders (`RecentFolders`, read back after the window closes), the file types (`FileTypes`) and companions (`CompanionSuffixes`), the options (`OptionsContent`), a notice above Open (`Notice`), and where to log (`Log`), since it has no log of its own (#1333).

## Weevil's Options

`OpenAsOptionsViewModel` fills the options area unless the log's plugin supplies its own. It follows the selection: a folder, a zip or nothing shows no details.

- **The log's details:** its name, size and date, the folder or zip it is in, and whether OneDrive keeps it online only.
- **The file types:** "Log files" shows `*.log`, rolled logs such as `app.log.1`, `*.txt`, `*.tsv` and `*.csv`, and hides Weevil's `.xml` and `.idx`; "All files" shows everything. A log's companions are its sidecar (`.xml`) and saved index (`.idx`).
- **Which part to open** (#1212, #1213, #1214): the whole log, one level-1 section or one named region from its sidecar, or a range of lines typed by the analyst. One at a time. `OpenAsLookup.ReadParts` lists the parts off the UI thread, and the options say what the sidecar holds: none, how many sections and regions, or why it can't be read. For a log in a zip, the sidecar is the one kept beside the copy of an earlier open, which holds the analyst's regions, or else the one in the zip. A file stored online only has its sidecar left unread. Selecting another log goes back to the whole log.
- **RAM or Disk** (#1215): `OpenAsAdvice` suggests RAM when the log's estimated memory, plus a 1 GB reserve, fits in the memory available, and Disk otherwise. It warns about RAM for a log that won't fit, and about Disk for a log on a network drive; a warning never stops the open. A file in a zip, or stored online only, isn't read for the estimate: it is estimated from its size alone. Once the analyst chooses a mode, selecting another log keeps it.
- **The notice:** opening a log accepts Weevil's license, as in 2.x, with a link to the license file (`OpenAsNoticeView`).

## Plugins' Options

A plugin may show its own options in place of Weevil's while one of its logs is selected (#1216). Most plugins don't, and use Weevil's.

- **`IOpenAsOptionsProvider`,** exported from an assembly of its own in `Plugins\Wpf`, names the plugin it is for, and creates its options (`IOpenAsOptions`) and their view. Only Weevil's window loads `Plugins\Wpf`, so a plugin's `IPlugin` class, which Core, the CLI and PowerShell load, needs no WPF.
- **Options say how to open, not which log:** the part, RAM or Disk, and a context for `IEngineBuilder.UsingContext`. The log opened is always the one the analyst selected. `IPlugin` no longer has `CanOpenAs` or `ShowOpenAs`, which showed a plugin's own window after Weevil's in 2.x.
- **Finding the plugin** (`OpenAsLookup.FindOptionsProvider`) happens off the UI thread, and only when some plugin supplies options. For a log in a zip, the plugin checks only its first 64 KB, read from the zip into a temporary file: the log is not extracted. A log stored online only uses Weevil's options, since reading any of it would download it.
- **`OpenAsOptionsHost`** creates a plugin's options the first time one of its logs is selected, keeps them while the window is open, and disposes them after. Open stays disabled until the options shown have caught up with the log selected. A plugin whose provider or options fail is replaced by Weevil's options, since browsing must never stop Weevil.
- **Loading:** `PluginFactory` loads each plugin assembly, and creates each plugin, on its own, so one that fails to load, or whose constructor throws, is skipped with a warning instead of stopping every open.
- The interfaces' remarks say what a plugin must and mustn't do, such as which thread each member is called on.

## Every Way In

Every way to open a log shows the dialog (#1217), and `FilterViewModel.ShowOpenAsAsync` opens what it returns.

- **Open, Ctrl+O and the menu's Open…** start in the open log's folder. For a log from a zip, that is the zip, or the folder in it. With no log open, they start in the most recent folder visited that can be found.
- **A log dropped on the window, or given by Explorer or on the command line,** is selected in its folder. A zip is opened, like a folder. A drop is accepted for `.log`, `.txt`, `.tsv`, `.csv` and `.zip` files, so a rolled log such as `app.log.1` isn't, and is ignored while a filter, an analysis or an open runs (#996). The command line takes an existing file, not a folder.
- **Recent folders** (#1218): the last 8 folders and zips visited, newest first, kept between runs in Weevil's settings (`Settings.Default.RecentFolders`), even when the analyst cancels. They are looked for off the UI thread when the window opens, since a folder on a share that doesn't answer blocks the call; for a folder in a zip, only the zip is looked for. One that can't be found is skipped without an error, and kept after the others, so it returns once it can be found, until newer folders push it out.
- **After Open,** Weevil opens the log with the part, mode and context chosen. A log from a zip is named by the zip and the log (#1312), and its copy is kept as [OpeningALog.md](OpeningALog.md#the-files) describes.

## Tests

- **The generic control:** `BlueDotBrigade.Weevil.Windows-UnitTests` (`Dialogs`) tests the browser's model and `Storage` without a window. It references only the generic control's project, so a test can't use a Weevil type either. It runs on Windows only.
- **Weevil's options and plugins:** `BlueDotBrigade.Weevil.Gui-FeatureTests` (`IO`) tests `OpenAsAdvice`, `OpenAsLookup`, `OpenAsOptionsHost` and `OpenAsOptionsViewModel`. `FilterViewModelTests` tests the ways in.
- **By hand:** what automated tests can't check, such as look and theme, the keyboard and Narrator, OneDrive and network paths, is in #1342.
