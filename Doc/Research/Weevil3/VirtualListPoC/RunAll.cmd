@echo off
rem Runs the #1014 proof of concept twice and saves a report from each run next to this script.
rem Each run opens a window, works through every step on its own, then closes it. Leave the mouse and keyboard alone meanwhile.
setlocal
cd /d "%~dp0"

if exist "Weevil.VirtualListPoC.exe" (
    set "POC=Weevil.VirtualListPoC.exe"
) else if exist "publish\Weevil.VirtualListPoC.exe" (
    set "POC=publish\Weevil.VirtualListPoC.exe"
) else (
    rem From the source code: needs the .NET 10 SDK.
    set "POC=dotnet run -c Release --"
)

echo 1 of 2: the range selection, 100 million rows. A few minutes.
start "" /wait %POC% --auto --report results-ranges.md

echo 2 of 2: WPF's own selection, as Weevil 2.12, 1 million rows. A few minutes.
start "" /wait %POC% --auto --wpf-selection --report results-wpf-selection.md

echo.
echo Done. Please send results-ranges.md and results-wpf-selection.md, from this folder:
echo %CD%
pause
