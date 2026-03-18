<#
.SYNOPSIS
Recursively compresses .log files (and optional .log.xml sidecar metadata) into .zip archives.

.DESCRIPTION
This script scans a directory recursively for .log files. If a corresponding sidecar file
with the name "<logname>.log.xml" exists, it is treated as metadata associated with the log.

Behavior depends on the -CompressAll parameter:

- CompressAll = $true (default)
    All .log files are compressed. If a sidecar exists, it is included in the archive.

- CompressAll = $false
    Only log files that have a matching .log.xml sidecar are compressed.

After a successful compression, the original files are sent to the Windows Recycle Bin.

The script provides runtime progress feedback and a final summary.

.PARAMETER Path
The root directory to search for .log files. May be relative or fully qualified.

.PARAMETER CompressAll
If $true (default), all logs are compressed whether or not they have a sidecar.
If $false, only logs with a matching .log.xml sidecar are compressed.

.EXAMPLE
.\Compress-Logs.ps1 -Path "C:\Logs"

Compress all log files under C:\Logs. Sidecars will be included if present.

.EXAMPLE
.\Compress-Logs.ps1 -Path ".\Logs" -CompressAll $false

Compress only logs that have matching .log.xml metadata files.

.NOTES
Requires Windows 11 or later and PowerShell 5.1+.
Uses the .NET Microsoft.VisualBasic assembly to send files to the Recycle Bin.
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter()]
    [bool]$CompressAll = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Load assembly required for Recycle Bin operations
Add-Type -AssemblyName Microsoft.VisualBasic

function Write-Info {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Cyan
}

function Write-Warn {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Yellow
}

function Write-Err {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Red
}

function Send-ToRecycleBin {
    param(
        [string[]]$Files
    )

    foreach ($file in $Files) {
        try {
            if (Test-Path $file) {
                [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile(
                    $file,
                    'OnlyErrorDialogs',
                    'SendToRecycleBin'
                )
            }
        }
        catch {
            Write-Err "Failed to send file to Recycle Bin: $file"
            Write-Err $_
        }
    }
}

try {

    if (-not (Test-Path $Path)) {
        throw "The specified path does not exist: $Path"
    }

    Write-Info "Scanning for log files..."

    $logFiles = Get-ChildItem -Path $Path -Recurse -File -Filter "*.log"

    $logsFound = $logFiles.Count
    $compressionList = @()

    foreach ($log in $logFiles) {

        $sidecar = "$($log.FullName).xml"
        $hasSidecar = Test-Path $sidecar

        if ($CompressAll -or $hasSidecar) {

            $compressionList += [PSCustomObject]@{
                Log      = $log.FullName
                Sidecar  = if ($hasSidecar) { $sidecar } else { $null }
            }
        }
    }

    $totalToCompress = $compressionList.Count
    $compressedCount = 0
    $currentIndex = 0

    Write-Info "Logs discovered: $logsFound"
    Write-Info "Logs scheduled for compression: $totalToCompress"
    Write-Host ""

    foreach ($item in $compressionList) {

        $currentIndex++

        $log = $item.Log
        $sidecar = $item.Sidecar

        $zipPath = [System.IO.Path]::ChangeExtension($log, ".zip")

        Write-Host -NoNewline "`rProcessing $currentIndex of $totalToCompress logs..."

        try {

            $paths = @($log)

            if ($sidecar) {
                $paths += $sidecar
            }

            Compress-Archive -Path $paths -DestinationPath $zipPath -Force

            Send-ToRecycleBin -Files $paths

            $compressedCount++
        }
        catch {
            Write-Host ""
            Write-Err "Compression failed for: $log"
            Write-Err $_
        }
    }

    Write-Host ""
    Write-Host ""
    Write-Info "Operation Complete"
    Write-Host "------------------"
    Write-Host ("Logs Found      : {0}" -f $logsFound)
    Write-Host ("Logs Compressed : {0}" -f $compressedCount)

}
catch {
    Write-Err "Fatal error occurred."
    Write-Err $_
}