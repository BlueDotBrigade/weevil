# Weevil: Installer


## Updating WiX

1. Compile the entire Weevil solution.
2. Open the `BlueDotBrigade.Weevil.Gui` project in the Windows file explorer.
3. Find the `Plugin` directory.
4. Delete the `Bin` and  `Obj` sub-directories
5. Copy the assemblies (and runtime dependencies) to `BlueDotBrigade.Weevil.Gui` for:
	- `BlueDotBrigade.Weevil.Cli`
	- `BlueDotBrigade.Weevil.PowerShell`
6. Use the following script to update `WeevilBinFiles.wxs`

```cmd
CD "%USERPROFILE%\.nuget\packages\wixtoolset.heat\5.0.2\tools\net6.0"
SET WIX_DIR=%CD%

CD "C:\Code\BlueDotBrigade\OpenSource\weevil\Src\BlueDotBrigade.Weevil.Gui\bin\x64\Debug\net9.0-windows"
SET SOURCE_BIN_DIR=%CD%

CD "C:\Code\BlueDotBrigade\OpenSource\weevil\Src\BlueDotBrigade.Weevil.Installer"
SET SETUP_DIR=%CD%

ECHO Generating new WeevilBinFiles.wxs
dotnet exec "%WIX_DIR%\heat.dll" dir %SOURCE_BIN_DIR% -dr InstallFolderBin -srd -var var.SourceDirPath -gg -sfrag -cg weevil_bin_files -sreg -scom -suid -out "%SETUP_DIR%\WeevilBinFiles.wxs"
```