# Open As Dialog Proof of Concept

Step 1 of the [#1157 plan](../../../Plans/1157-OpenAsDialog.md). **Question:** can one dialog, built in plain WPF, browse like the Windows file dialog, treat zips as folders, and show a host's options for the file selected?

## What It Is

| Folder | What it holds |
|---|---|
| `Dialogs` | The generic dialog, `BlueDotBrigade.Windows.Dialogs`: no reference to Weevil, MaterialDesign or any package. `Storage` lists and reads folders and zips, with no WPF; `Wpf` holds the browser and the window. |
| `App` | A sample host with Weevil-like options, which reads sidecars with plain XML. |
| `SelfTest` | Checks `Storage` and the sidecar reader on any operating system: `dotnet run -c Release` in that folder. |

None of them is part of `Weevil-v2.sln`.

## Run It on Windows

From the repository folder, with the .NET 10 SDK:

```powershell
dotnet run -c Release --project Doc\Research\Weevil3\OpenAsDialogPoC\App
```

Logs with sidecars to try. Only level-1 sections are listed, and each runs to the next level-1 section:
- Sections and regions: make a sample in `C:\Temp`. Its sidecar lists three level-1 sections, one level-2 section and two regions.
  ```powershell
  Copy-Item 'Tst\BlueDotBrigade.Weevil.Core-FeatureTests\.Daten\.Global\Droid.log' C:\Temp\Sample.log
  Copy-Item 'Doc\Research\Weevil3\OpenAsDialogPoC\Samples\Sample.log.xml' C:\Temp\
  ```
- A zip of the sample, with its sidecar:
  ```powershell
  Compress-Archive -Path C:\Temp\Sample.log, C:\Temp\Sample.log.xml -DestinationPath C:\Temp\Logs.zip -Force
  ```

## Check It (about 10 minutes)

Press **Open** (Ctrl+O). Note anything that looks or feels wrong.

- [ ] **Looks right:** icons show, not empty boxes; columns, buttons and the options panel line up.
- [ ] **Browse:** This PC, Desktop, Documents, Downloads; Back, Forward and Up; double-click a folder; paste a folder into the path box and press Enter; sort by each column.
- [ ] **File type:** in `C:\Temp`, "Log files" shows `Sample.log` but hides `Sample.log.xml`; "All files" shows both.
- [ ] **Find:** type part of a name; only matching items remain.
- [ ] **Sections:** select `C:\Temp\Sample.log`. The panel shows its size and 3 sections: Startup (lines 1–119), Patrol (120–299) and Shutdown (300 to the end), but not Contact. Pick one, and Open: the result names it.
- [ ] **Regions:** the same log lists two regions, Startup and Shutdown.
- [ ] **Zip:** open `C:\Temp\Logs.zip` like a folder, select `Sample.log`, and see its sections. Open: the result says it was extracted with its sidecar.
- [ ] **RAM or Disk:** pick Disk, then select another log. It stays Disk.
- [ ] **Keyboard only:** Tab moves through the dialog. In the list: arrows, Enter opens, Backspace goes up. Alt+Left goes back, Ctrl+L goes to the path box, Ctrl+F to Find, Escape cancels.
- [ ] **Narrator** (Win+Ctrl+Enter): it names the buttons, the boxes and each file.
- [ ] **Dark theme:** tick it, then Open again. The dialog takes the dark colours.
- [ ] **Problems:** a folder you cannot read, such as another user's, shows a message instead of looking empty. A slow or missing network path (`\\server\share`) does not freeze the window.
- [ ] **OneDrive**, if you use it: an online-only file shows a cloud, and is not downloaded until you open it.

## Not in the Proof of Concept

Weevil's own rules and code: the plugin interface, Weevil's RAM-or-Disk advice, recent folders kept between runs, and loading the part chosen. The plan's later steps build those.
