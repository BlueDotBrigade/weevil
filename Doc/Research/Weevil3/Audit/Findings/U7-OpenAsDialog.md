# U7: Open As Dialog and File Browser (#1157, In Progress)

Part of the [Weevil 3.0 audit](../Progress.md). This code is a generic WPF file browser and Open dialog: it browses drives, folders and zips to any depth without extracting them, and gives the host a path on disk for the chosen file, extracting a file from a zip along with its companions.

> **Status:** Final. Reviewed on 2026-11-17 at `main` e3dee822, by one reviewer covering every lens.

## Summary

Only the first production piece of #1157 is merged (#1210), and nothing in Weevil uses it yet. So this is a light review of a foundation, not of a feature. **That foundation is sound.** The classes are small and each has one job. Comments explain why, not what. The code keeps the plan's main rules: no Weevil, MaterialDesign or Metalama reference, hand-written change notification, listings on a background thread, and late results discarded. A test pins the rule on late results. `Location` is an immutable value type that names a place inside nested zips, and `ContainerReader` lists a zip without extracting it. The host's sections and regions need only `Exists` and `OpenRead` from the storage layer, so the abstraction is a reasonable base for the remaining pieces.

Nothing has been replaced yet, so there is no simpler-or-more-complex verdict against 2.x. Today, 2.x and 3.0 open a zip by extracting all of it and asking for a file in a second window. The new design extracts only the chosen log and its companions, which is less work and fewer temporary files.

The concerns are two plan rules that hold only by convention. First, **disk I/O still happens on the UI thread**: `Location.Parse` checks the disk, skips `IContainerReader`, and runs on the UI thread when the host hands over recent folders (U7-1). Second, **nothing guards the "knows nothing about Weevil" boundary**. The assembly references MaterialDesign and Weevil.Common, and the repository's agent instructions say to do the opposite of what this control must do (U7-2). Both cost little to fix now and more after the 11 remaining pieces build on them. There is no High finding.

## What Was Examined

- `Src/BlueDotBrigade.Weevil.Windows/Dialogs/**`, all 16 files (2,111 lines). History: two commits, 87658c66 and 4dfa1758 (#1210).
- `Src/BlueDotBrigade.Weevil.Windows/BlueDotBrigade.Weevil.Windows.csproj`.
- Tests: `Tst/BlueDotBrigade.Weevil.Windows-UnitTests/**`, 36 tests in 5 classes, and how CI runs them (`.github/workflows/dotnet.yml:55`, `:102`).
- The proof of concept: `Doc/Research/Weevil3/OpenAsDialogPoC/README.md`, its `Dialogs` project file, and a diff of its `Storage` and `Wpf` code against `Src`. Its self-test, `SelfTest/Program.cs`.
- Today's open path, which the dialog will replace: `Gui/Filter/FilterViewModel.cs:544-627`, `Gui/IO/ExtractedLog.cs`, `Gui/IO/OpenAsDialog.xaml.cs`, `Gui/IO/DialogBoxService.cs:175-185`. 2.x's open path: `origin/Releases/2.x:Src/BlueDotBrigade.Weevil.Gui/Filter/FilterViewModel.cs:486`.
- `Src/BlueDotBrigade.Weevil.Common/IPlugin.cs` and `IO/OpenAsResult.cs`, for steps 4 and 5.
- Documents: `Doc/Plans/1157-OpenAsDialog.md`, `Doc/Design/DiskMode.md:12`, `.github/copilot-instructions.md`, `.github/instructions/*.instructions.md`.

## Strengths

- **The plan's constraints hold in the code.**
  - The `Dialogs` code imports only `System.*` and its own namespaces. Nothing names Weevil, MaterialDesign or Metalama.
  - Change notification is written by hand with one `Set<T>` helper (`Dialogs/FileBrowserModel.cs:24`, `:355-367`).
  - The XAML uses standard controls with no colours, and a comment says why (`Dialogs/FileBrowser.xaml:6`, `Dialogs/OpenAsDialog.xaml:8`).
- **Late results are handled by design, and specified.**
  - A navigation counter discards a listing that a newer navigation overtook (`FileBrowserModel.cs:257-267`). The remark explains why the listing is discarded rather than cancelled: "a listing blocked on an unreachable network share cannot be cancelled" (`:25-26`).
  - A test holds a slow listing open and asserts that its late result is ignored (`Tst/BlueDotBrigade.Weevil.Windows-UnitTests/Dialogs/FileBrowserModelTests.cs:93-112`).
- **`Location` captures a hard idea simply.** It is an immutable, case-insensitive value with equality. Its remark documents how a path through nested zips is encoded, with an example (`Dialogs/Storage/Location.cs:8-16`). Climbing out of two zips is a one-line test (`LocationTests.cs:15-30`).
- **The storage layer has no WPF.**
  - `Storage/*` uses only `System.IO` and `System.IO.Compression`, so it could serve the CLI or a headless test. The proof of concept's self-test ran it on Linux.
  - `IContainerReader` lets the model be specified with a substitute (`FileBrowserModelTests.cs:17-32`).
- **Zips are read the way users make them.**
  - A folder known only from its files' paths is found (`Storage/ContainerReader.cs:321-379`).
  - Backslash entries and any letter case are matched (`:436-444`).
  - A nested zip's copy is keyed by the outer zip's write time, so a replaced zip is read again (`:392-393`).
- **The rules a log reader needs.**
  - Files are opened with `FileShare.ReadWrite | FileShare.Delete`, so the application writing a log is never blocked (`ContainerReader.cs:176-178`, `:446-449`).
  - A folder that cannot be read shows why instead of looking empty, and hidden folders are listed unless they are also system folders, as in Explorer. The remark gives the reason: ProgramData and AppData (`:286-309`).
  - An online-only OneDrive file is marked from its attributes and is not read (`:47-50`, `:314`).
- **Built for the keyboard and screen readers.** Every control has an `AutomationProperties.Name` (`FileBrowser.xaml:29-41`, `:55`, `:67`, `:84`, `:88`, `:129`). The shortcuts are listed in the class remarks (`FileBrowser.xaml.cs:17-20`).
- **Small decisions are explained.** Two examples: "Nothing else is selected, so entering a folder does not make the host read the first file" (`FileBrowserModel.cs:281`), and Enter in the path box is marked handled "so the host window's default button does not open the file selected before" (`FileBrowser.xaml.cs:180`).
- **A safer matcher.** File-type patterns use `FileSystemName.MatchesSimpleExpression` instead of a generated regular expression, so no pattern can make a match slow (`Storage/FileTypeFilter.cs:12-13`, `:42-45`; commit 4dfa1758).
- **The process the plan set out was followed.** First a standalone proof of concept with a checklist for the owner on Windows (`OpenAsDialogPoC/README.md`), then production code with tests. The tests are named as behaviour (`GivenASlowListing_WhenANewerNavigationFinishesFirst_ThenTheSlowListingIsDiscarded`), and Windows CI runs them (`.github/workflows/dotnet.yml:55`).
- **Neutral for #991.** The browser holds no engine or log state, and "one log at a time" (decision 6) is the host's rule, not the control's.

## Findings

### U7-1. `Location.Parse` checks the disk on the UI thread, outside `IContainerReader`

- **Priority:** Medium
- **Kind:** Architecture
- **New in 3.0?** New design
- **Evidence:**
  - **The rule.** "Folders are listed off the UI thread … because a call blocked on an unreachable share cannot be cancelled" (`Doc/Plans/1157-OpenAsDialog.md:41`).
  - **`Parse` checks the disk.** It calls `Directory.Exists` and `File.Exists` on the whole text, then `File.Exists` on each shorter prefix (`Src/BlueDotBrigade.Weevil.Windows/Dialogs/Storage/Location.cs:163`, `:171-183`). For a missing `\\server\share\Logs\2026`, that is two network checks for the whole path, then one for each shorter prefix: `\\server\share\Logs`, `\\server\share` and `\\server`.
  - **One caller is right.** `GoToPathTextAsync` runs `Parse` inside `Task.Run` (`Dialogs/FileBrowserModel.cs:215-221`).
  - **One is not.** The `RecentFolders` setter calls `Parse` for each path on the UI thread (`Dialogs/FileBrowser.xaml.cs:69-86`, `:78`). It has to: it fills an `ObservableCollection` bound to a list box. The host sets it "when it opens the browser" (`:67`), through `OpenAsDialog.RecentFolders` (`Dialogs/OpenAsDialog.xaml.cs:105-109`). Step 5 of the plan, "recent folders", is the piece that will call it.
  - **The round trip throws away what `Location` knew.** The getter returns each recent folder's `ToString()` (`FileBrowser.xaml.cs:71`). The setter then has to check the disk to find which parts were zips, a fact the `Location` already held.
  - **A smaller case.** The model's constructor checks that Desktop, Documents and Downloads exist on the UI thread (`FileBrowserModel.cs:52-54`, `:347-353`). In managed networks those folders are often redirected to a server.
  - **The substitute cannot see it.** Because `Parse` skips the reader, the model test for a typed path must create a real folder (`Tst/BlueDotBrigade.Weevil.Windows-UnitTests/Dialogs/FileBrowserModelTests.cs:162-170`), although the class summary says "with a substitute reader, so no folder needs to exist" (`:5-6`).
- **Impact:** Suppose a recent folder is on a share that is no longer reachable, say an evidence share used over VPN yesterday. Then opening the dialog freezes Weevil's window for as long as Windows takes to give up on each path check, often tens of seconds (not measured). That is the case the plan designed background listing for. Nobody hits it yet, since recent folders are not stored. Step 5 would build on this API as it stands.
- **Recommendation:** Make `Location.Parse` purely syntactic. Split at parts with a container extension, as it already does inside a zip (`Location.cs:193-202`). Leave existence to `IContainerReader.GetKind`, which already runs off the UI thread. Recent folders then cost no I/O to restore. A missing one fails when the analyst clicks it, as any listing does. Optionally, check the three standard places in the first background listing. This also lets the typed-path test use the substitute alone. **Complexity:** S.
- **Sceptic:** Not challenged (Medium)

### U7-2. Nothing guards the boundary of the generic control

- **Priority:** Medium
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **The decision.** "It does not depend on MaterialDesign or on any other Weevil project" (`Doc/Plans/1157-OpenAsDialog.md:32`). "Change notification is written by hand, not with Metalama, so other applications need neither" (`:46`).
  - **It holds today, by care alone.** The `Dialogs` code imports nothing from Weevil or MaterialDesign. But its assembly references both: `MaterialDesignThemes` 4.0.0 and `BlueDotBrigade.Weevil.Common` (`Src/BlueDotBrigade.Weevil.Windows/BlueDotBrigade.Weevil.Windows.csproj:34`, `:62`). So a `using BlueDotBrigade.Weevil.IO;` or a MaterialDesign style key in `Dialogs` compiles and passes review unnoticed.
  - **The proof of concept enforced it.** It built the control as its own project with no references, with a comment stating the rule (`Doc/Research/Weevil3/OpenAsDialogPoC/Dialogs/BlueDotBrigade.Windows.Dialogs.csproj:2`).
  - **The instructions point the other way.** No instruction file applies to `Src/BlueDotBrigade.Weevil.Windows/**`; the five `applyTo` patterns cover Common, Core, Gui, Tst and everything (review). The repo-wide rule tells agents not to add manual `OnPropertyChanged` calls or backing fields, and to use Metalama instead (`.github/copilot-instructions.md:36`; `.github/instructions/gui.instructions.md:14-15`). Its scope is Gui ViewModels, but nothing tells an agent that this model is the exception. Only the class remark says so (`Dialogs/FileBrowserModel.cs:24`), and the plan is a dated record that agents are told not to treat as instructions.
  - **Weevil-specific work is next to it.** Step 4 adds Weevil's options panel and a plugin interface, and step 5 wires the dialog into Weevil (`1157-OpenAsDialog.md:62-63`). The move to a shared library comes after 3.0 (`:65`).
  - **It already ships as API.** The types are public in a NuGet package described as "types that can be consumed by Weevil plugins" (`.csproj:11-12`). The namespace `BlueDotBrigade.Weevil.Windows.Dialogs` will change on the move; the proof of concept used `BlueDotBrigade.Windows.Dialogs`.
- **Impact:** A leak is found only at step 7, when the move fails to compile, or never, if the move is put off. The repository's own instructions also invite a well-meant "fix" of the hand-written notification. The cost falls on whoever does the move, and on the other Blue Dot Brigade applications that are the reason the control is generic.
- **Recommendation:** One of these:
  - (a) An instruction file for `Src/BlueDotBrigade.Weevil.Windows/Dialogs/**`, with the `CLAUDE.md` that imports it as the repository requires. It would state the three rules: no Weevil, no MaterialDesign, hand-written notification. **Complexity:** S.
  - (b) Better: bring the project split of step 7 forward. Put `Dialogs` in a project with no references, as the proof of concept did, and reference it from `BlueDotBrigade.Weevil.Windows` or the GUI. The compiler then enforces decision 11, and step 7 becomes a move of files. This costs one more project, against the preference for a simple configuration. **Complexity:** S to M.
- **Sceptic:** Not challenged (Medium)

### U7-3. Containers are zips in all but name

- **Priority:** Low
- **Kind:** Intent
- **New in 3.0?** New design
- **Evidence:**
  - **The extension point is not real.** `ContainerReader` takes "the extensions of the files the browser enters like folders" (`Dialogs/Storage/ContainerReader.cs:57-63`). Yet every read is a `ZipArchive` (`:384-388`), and the error for any other format is "This is not a zip, or it is damaged." (`:461`).
  - **No host can reach it.** `FileBrowser` builds `new ContainerReader()` itself, so the parameter cannot be set (`Dialogs/FileBrowser.xaml.cs:30`).
  - **Three words for one idea.** `ItemKind.Container` shows as "Compressed folder" with a zip icon (`Storage/BrowserItem.cs:14`, `:63`; `ItemGlyphConverter.cs:18`). `Location` calls the same path `ArchivePath`, `IsInArchive` and `ForArchive` (`Storage/Location.cs:12-36`, `:72`), but `EnterContainer` and `entersContainer` (`:81-110`). The plan settles it: "A zip is a container, like a folder" (`Doc/Plans/1157-OpenAsDialog.md:24`).
- **Impact:** A reader can't tell whether other formats are planned (they aren't). A future host that passes `.7z` would list containers that fail to open. The cost is small, paid by whoever does step 5 or the move.
- **Recommendation:** Keep "container" for the role and "zip" for the format. Remove the extension parameter, and rename `Location`'s `Archive*` members to `Container*`. If another format is ever wanted, add it then, behind a real seam. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U7-4. Closing the dialog during an extraction leaves a copy in the temporary folder

- **Priority:** Low
- **Kind:** Correctness
- **New in 3.0?** New design
- **Evidence (seen in passing; not run on Windows):**
  - **Cancel stays live.** While extracting, only Open is disabled (`Dialogs/OpenAsDialog.xaml.cs:138-139`, `:165-168`). Cancel is `IsCancel="True"`, so Escape or a click closes the window (`Dialogs/OpenAsDialog.xaml:28`).
  - **Closing does not stop the copy.** `OnClosed` disposes the reader, which deletes nested-zip copies (`OpenAsDialog.xaml.cs:121-125`; `Storage/ContainerReader.cs:234-254`). The extraction keeps writing to its own folder, which only `Result.ExtractedFolder` would have handed to the host (`OpenAsDialog.xaml.cs:32-35`, `:144-147`).
  - **Then the result is set on a closed window.** When the copy ends, `this.DialogResult = true` runs on that closed window (`:148`). WPF allows `DialogResult` only while a window is shown as a dialog, so this should throw `InvalidOperationException`. The `catch` filter (`:150`) doesn't take it, and it escapes an `async void` handler (`:65`, `:127`).
  - **Two smaller leaks.** A failed extraction leaves its partial folder (`:150-153`). A nested-zip copy that started before `Dispose` recreates the copy folder after it (`ContainerReader.cs:411`), and nothing deletes it.
- **Impact:** None today, since the dialog is not wired in. After step 5, an analyst who cancels a slow extraction of a large log from a zip could see an error, and a full copy of the log would stay in `%TEMP%`.
- **Recommendation:** Disable Cancel while extracting, or make it cancel: pass a `CancellationToken` to the copy, delete the destination on failure or cancel, and skip `DialogResult` once the window has closed. Mark the reader disposed so a late copy creates nothing. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

### U7-5. The most intricate logic is the least specified

- **Priority:** Low
- **Kind:** Maintainability
- **New in 3.0?** New design
- **Evidence:**
  - **Parsing a path into a zip.** This is the 40-line branch of `Location.Parse` that finds the zip on disk and splits the rest (`Dialogs/Storage/Location.cs:168-206`). No production test covers it. `LocationTests` parses only a quoted folder and a missing path (`Tst/BlueDotBrigade.Weevil.Windows-UnitTests/Dialogs/Storage/LocationTests.cs:39-51`). The proof of concept did check that "a typed path into a zip in a zip finds the file" (`Doc/Research/Weevil3/OpenAsDialogPoC/SelfTest/Program.cs:159`). That check was not carried over.
  - **The round trip that recent folders rely on.** `Parse(location.ToString())` should equal `location` (`FileBrowser.xaml.cs:71`, `:78`). It is untested for any place inside a zip.
  - **Sorting.** Only the default sort is pinned (`FileBrowserModelTests.cs:34-40`). The other columns, descending order, and the boxed `Comparer<object>` that mixes strings, dates and sizes are not (`Dialogs/FileBrowserModel.cs:305-317`).
  - **Clean-up.** No test checks that `Dispose` deletes the nested-zip copies (`ContainerReader.cs:234-254`), or that items both hidden and system are skipped (`:306`).
  - **Elsewhere, coverage fits the risk.** Listing, zips in zips, damaged zips, companions and late results are specified. The window itself is left to the `manual_test` issue of step 6.
- **Impact:** These are the parts most likely to change in steps 4 and 5, which add typed paths into zips, recent folders kept between runs, and more columns. A regression there would show up only in the manual check.
- **Recommendation:** Add a data-driven `Parse` test over the nested-zip example in `Location`'s own remark, plus the `ToString` round trip, and one test per sort column in both directions. **Complexity:** S.
- **Sceptic:** Not challenged (Low)

## Minor

- **Two of each name.** There are two `OpenAsDialog` types (`Gui/IO/OpenAsDialog.xaml.cs:13`, `Windows/Dialogs/OpenAsDialog.xaml.cs:44`). There are also two `OpenAsResult` types: one in `Common/IO/OpenAsResult.cs:5`, returned by `IPlugin.ShowOpenAs` (`IPlugin.cs:16`), and one in `Windows/Dialogs/OpenAsDialog.xaml.cs:13`. Step 5 retires the first dialog, but the plan keeps existing plugins working, so both `OpenAsResult` types stay. "Open As" is also Weevil's term in a control meant to be generic.
- **`GetSibling` is in the wrong place.** It is a static on `ContainerReader`, not on `Location` or the interface (`ContainerReader.cs:171`). A host that finds a sidecar through `IContainerReader` must also name the concrete class.
- **`Extract` doesn't always extract.** For a file on disk it returns the path unchanged (`ContainerReader.cs:202-214`). A name such as `GetLocalPath` would say what it does.
- **`isBackOrForward` means "do not record".** Refresh passes `isBackOrForward: true` to stay out of history (`FileBrowserModel.cs:185`).
- **Notification dependencies are listed by hand.** `StatusText` depends on `IsBusy`, but `IsBusy`'s setter doesn't raise it (`FileBrowserModel.cs:160-164`, `:172`). As a result, `GoToPathTextAsync` never shows "Loading…" while it checks a typed path (`:213`). This is the price of decision 11, and worth a comment.
- **Back and Forward can go stale.** A Back or Forward that a newer navigation overtakes has already moved the history (`FileBrowserModel.cs:176-178`). `CanGoBack` and `CanGoForward` are raised only when a navigation completes (`:264-267`, `:289-290`).
- **Thread affinity is unstated.** The model relies on each `await` returning to the UI thread for `_navigation` and `VisibleItems` (`FileBrowserModel.cs:34`, `:223`, `:264`), but says only that listing runs in the background. In the tests, which have no synchronization context, the continuations run on pool threads.
- **A disposable control.** `FileBrowser` is `IDisposable` (`FileBrowser.xaml.cs:21`, `:97`), but WPF never disposes a control. A host that uses the browser without `OpenAsDialog`, which is the generic use, must remember, or nested-zip copies stay in `%TEMP%\BlueDotBrigade.OpenAs`.
- **The file size overstates the class.** `ContainerReader.cs` is 520 lines because it also holds `ListingResult` (`:9-35`) and `EntryStream` (`:465-518`). The reader itself is about 400 lines and cohesive, so no split is needed.
- **Two copies of the code.** `Src` is a near-verbatim copy of the proof of concept: `Location.cs` and `BrowserItem.cs` are identical, and `ContainerReader.cs` differs in one line. The proof of concept's README doesn't say that the code moved to `Src`, so a search for `ContainerReader` finds both.
- **"Not saved" depends on where the copy is.** Decision 4 says changes to a log from a zip are not saved. That holds only because the dialog extracts under `%TEMP%` (`OpenAsDialog.xaml.cs:144`) and Core skips saving there (`Common/IO/FileHelper.cs:16`, and see P-4 in [Performance.md](Performance.md)). Neither side names the other. Today's GUI path relies on the same rule (`Gui/Filter/FilterViewModel.cs:565-566`).
- **No natural sort yet.** Names sort ordinally, ignoring case (`FileBrowserModel.cs:313-315`), so `app.log.10` comes before `app.log.2`. The plan names `StrCmpLogicalW` for exactly this (`1157-OpenAsDialog.md:22`). See Open Questions.
- **SonarCloud.** `Progress.md` lists no lead for this unit.
- **Design notes.** `Doc/Design` has no note for the browser, which step 6 schedules. `Doc/Design/DiskMode.md:12` still correctly describes today's two steps. `tests.instructions.md` lists the new test project accurately.

## To Measure in Stage 2

All of these need Windows except the zip measurements, which `ContainerReader` alone can take on any system, for example through the proof of concept's self-test project.

- **A large folder.** `RefreshView` sorts with a boxed `Comparer<object>` and refills the `ObservableCollection` one item at a time on the UI thread. It runs on every navigation and every keystroke in the name filter (`FileBrowserModel.cs:294-328`; `FileBrowser.xaml:37`). Time a navigation and one keystroke in a folder of 10,000 and 100,000 files.
- **A zip with many entries.** Every `List`, `GetKind`, `Exists` and `OpenRead` opens the archive again and scans every entry (`ContainerReader.cs:111`, `:159`, `:188`, `:326`, `:439-444`). `Extract` with two companions opens it five times (`:219-228`). Time each call on a zip with 10,000 and 100,000 entries, locally and on a network share. Step 4 will call `Exists` and `OpenRead` on every selection.
- **The first entry into a nested zip.** The inner zip is copied whole to `%TEMP%` before its listing appears, and the copy cannot be cancelled (`ContainerReader.cs:390-427`). Measure the time and temporary space for an inner zip of 1 GB.
- **An unreachable recent folder (U7-1).** Measure how long the UI thread blocks when `RecentFolders` holds a path on a server that does not answer.

## Open Questions

- **Is natural sort intended?** The plan names `StrCmpLogicalW` for sorting `app.log.2` before `app.log.10`, and rolled logs are what "Log files" shows (decision 9). Is that a later piece, or was it dropped?
- **How will step 4 read a log in a zip without extracting it?**
  - Plugins are detected from a path (`Common/IPlugin.cs:12`, `:31`).
  - Core's sidecar reading has no `Stream` input. Step 3 will add "read a log's sections and regions without opening it".
  - `OpenRead` gives a stream that cannot seek (`ContainerReader.cs:479-481`).

  Will step 3 take a stream, as the proof of concept's `Sidecar.Read(stream)` did (`OpenAsDialogPoC/SelfTest/Program.cs:181`)? If not, every selection of a log in a zip would extract it first.
- **Can a log extracted from a zip still reach the files beside it?** Today Weevil extracts the whole zip and keeps it while the log is open, so another file from the same zip can be opened next (`Gui/Filter/FilterViewModel.cs:565-627`, `Gui/IO/ExtractedLog.cs:9-15`, #1162). The dialog extracts one log and its companions. Is losing access to the other files intended? Will `ExtractedLog`'s lifetime rules apply to `OpenAsResult.ExtractedFolder`?
- **Is an `.idx` that arrives in a zip untrusted input?** Decision 3 extracts `Application.log.idx` with the log, so the open question in [U1](U1-LineIndex.md#open-questions) becomes a routine path: a `.idx` made by someone else is trusted once its block checksums match.
