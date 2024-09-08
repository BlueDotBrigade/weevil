

param (
    [string]$logFilePath,   # Path to the log file
    [string]$include = "",  # Include filter (default to empty string)
    [string]$exclude = ""   # Exclude filter (default to empty string)
)

if (-not $logFilePath) {
    Write-Error "logFilePath is required."
    exit 1
}

# Ensure relative paths resolve to the current working directory (instead of the user's home directory).
$logFilePath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot $logFilePath))

if ([string]::IsNullOrWhiteSpace($include) -and [string]::IsNullOrWhiteSpace($exclude)) {
    $include = Read-Host "Include filter"
    $exclude = Read-Host "Exclude filter"
}

# Define the assembly paths
$commonAssemblyPath = ".\BlueDotBrigade.Weevil.Common.dll"
$coreAssemblyPath = ".\BlueDotBrigade.Weevil.Core.dll"

if (-not (Test-Path $commonAssemblyPath)) {
    Write-Error "Common assembly not found at path: $commonAssemblyPath"
    exit 1
}
if (-not (Test-Path $coreAssemblyPath)) {
    Write-Error "Core assembly not found at path: $coreAssemblyPath"
    exit 1
}

try {
    Add-Type -Path $commonAssemblyPath
    Add-Type -Path $coreAssemblyPath
} catch {
    Write-Error "Failed to load assemblies: $_"
    exit 1
}

try {
    # Create an instance of the IEngineBuilder using the public static method in BlueDotBrigade.Weevil.Engine
    $engineBuilder = [BlueDotBrigade.Weevil.Engine]::UsingPath($logFilePath)

    $engine = $engineBuilder.Open()
    $filterEnum = [BlueDotBrigade.Weevil.Filter.FilterType]::RegularExpression
    $criteria = New-Object BlueDotBrigade.Weevil.Filter.FilterCriteria($include, $exclude)
    $engine.Filter.Apply($filterEnum, $criteria)

    foreach ($record in $engine.Filter.Results) {
        Write-Output $record.ToString()
    }

} catch {
    Write-Error "An error occurred during execution: $_"
    exit 1
}