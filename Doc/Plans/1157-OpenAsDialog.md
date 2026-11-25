# 1157: One Open As Dialog

> **Status:** In progress
>
> **AI agents:** Do not start, resume or re-implement this plan unless the user asks you to in the current conversation.

Step 6 of the [#986 plan](986-DiskMode.md#steps). User story #1157. It replaces today's two steps: the Windows file dialog, then the Open As window of [#1028](1028-OpenAs.md).

## Goal

Open is the first thing an analyst does, so it should feel like Weevil from the first click:

- **One dialog,** Weevil's own, where the analyst finds a log and chooses how to open it.
- **The details follow the selection:** each log selected shows its size, its sections and regions when it has a sidecar, and the RAM or Disk advice.
- **Load less than the whole log:** one section, one region, or one range of lines.
- **A generic control:** the file browser knows nothing about Weevil, so other Blue Dot Brigade applications can use it, and it later moves to a shared library.

## Decisions

Decided by the owner on 2026-11-14:

1. **Built in WPF, from scratch,** rather than hosting Windows' Explorer browser through COM, which would bring focus, DPI and layering problems. No third-party code is copied and no dependency is added: on 2026-11-15, a search of 15 open-source projects found nothing worth copying. Windows' own functions, such as `StrCmpLogicalW` for sorting `app.log.2` before `app.log.10`, may be called.
2. **The browser offers what an analyst uses in the Windows dialog,** and leaves out five things that are not worth rebuilding: Windows' indexed search (a filter by name in the current folder instead), the right-click menu, Quick Access pins (recent and favourite folders instead), browsing a network's servers (typing `\\server\share` still works), and thumbnails or the preview pane (the details panel replaces them). Anything else that proves complicated, such as OneDrive beyond its status icon, is raised before it is built.
3. **A zip is a container, like a folder.** It may hold folders, logs, sidecars and other zips, to any depth. A log in a zip has a sidecar when one is beside it in the archive, named after the log as everywhere else: `Application.log`, `Application.log.xml`, `Application.log.idx`. Opening extracts the log with its sidecar and index.
4. **Changes to a log opened from a zip are not saved,** as today.
5. **One way to narrow the log at a time:** the whole log, one section, one region, or one range of lines.
6. **One log at a time.**
7. **RAM or Disk:** Weevil suggests a mode for each log selected. Once the analyst picks one, selecting another log does not change it.
8. **Every way in shows the dialog:** Open, Ctrl+O and the menu, and also drag-and-drop, Explorer and the command line, with the path filled in.
9. **"Log files"** also shows rolled logs, such as `app.log.1`, and hides Weevil's `.xml` and `.idx` files.
10. **Plugins:** Weevil detects the selected log's plugin, which either uses Weevil's options or supplies its own, through a new optional interface, so existing plugins keep working.
11. **The control lives in `BlueDotBrigade.Weevil.Windows` first,** and later moves to a shared library. It does not depend on MaterialDesign or on any other Weevil project. (2026-11-25: the project split of step 7 was brought forward in #1333, after audit finding U7-2: the control is in `BlueDotBrigade.Weevil.Windows.Dialogs`, which references nothing but WPF. Only the move to a shared library remains.)
12. **It ships with Weevil 3.0.**
13. **A proof of concept comes first,** standalone, with no Weevil code, checked by the owner on Windows.

## Design

### The generic control

- **`FileBrowser`**, a user control: a navigation pane (Desktop, Documents, Downloads, the drives, recent folders), Back, Forward and Up, a path box, a name filter, file-type filters, and a list of folders, containers and files with their size, type and date, sorted by any column.
  - Folders are listed off the UI thread. A newer navigation discards an older one's results, because a call blocked on an unreachable share cannot be cancelled.
  - A folder it cannot read shows why, instead of appearing empty. Hidden folders, such as ProgramData and AppData, are listed; folders both hidden and system are not, as in Explorer.
  - An online-only OneDrive file shows a cloud, and is not read until the analyst opens it, since reading it downloads it.
  - The keyboard alone works, and screen readers name every control.
- **`OpenAsDialog`**, a window that holds the browser, an options area the host fills, and Open and Cancel. It reports the selection, so the host updates its options. On Open, a file in a zip is extracted with its *companions*, the files the host says travel with it (for Weevil, `.xml` and `.idx`).
- **No theme of its own:** standard WPF controls, without MaterialDesign style names, so the host's application-wide styles restyle them. Change notification is written by hand, not with Metalama, so other applications need neither.

### Weevil

- **Its options panel:** the log's details, then Whole log, Section, Region or Lines, then RAM or Disk with its advice and warnings.
- **Core loads one section, region or range.** Core already loads a range of lines. Only level-1 sections are offered, and one runs from its line to the line before the next level-1 section, or to the end of the log, so it holds the sections nested in it (decided by the owner on 2026-11-15). A region runs from its first line to its last.
- **The CLI and PowerShell** gain the same choices.
- **Plugins:** a new optional interface supplies a plugin's options panel. It replaces `IPlugin.ShowOpenAs`, which shows a plugin's own window after Weevil's today.

## Steps

One pull request per step, in order.

1. **Proof of concept** (`Doc/Research/Weevil3/OpenAsDialogPoC/`): the generic control as a library of its own, and a sample application with Weevil-like options read from sidecars with plain XML, without Weevil. A self-test checks the parts that need no window, such as folders, zips and companions, on any operating system. The owner checks the rest on Windows.
2. **The control in `BlueDotBrigade.Weevil.Windows`,** from what the proof of concept settles, with its tests.
3. **Core and the CLI:** load one section or region, by name, and read a log's sections and regions without opening it.
4. **Weevil's options panel and the plugin interface.**
5. **Every way in, recent folders, zips through the dialog, and what the status bar and Reload show after a partial open.** The Windows file dialog and the Select File window go.
6. **A `manual_test` issue** for the dialog, and the design in `Doc/Design/`.
7. **After 3.0:** the control moves to the shared library.

## Not in This Plan

- Saving changes to a log opened from a zip.
- Several regions or sections at once.
- Opening several logs at once.
