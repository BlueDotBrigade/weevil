﻿param (
    [string]$logFilePath   # Path to the log file
)

# Verify that the logFilePath parameter is provided
if (-not $logFilePath) {
    Write-Error "logFilePath is required."
    exit 1
}

# Ensure relative paths resolve to the current working directory (instead of the user's home directory).
$logFilePath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot $logFilePath))

# Define the assembly paths
$commonAssemblyPath = ".\BlueDotBrigade.Weevil.Common.dll"
$coreAssemblyPath = ".\BlueDotBrigade.Weevil.Core.dll"

# Verify that both assemblies exist
if (-not (Test-Path $commonAssemblyPath)) {
    Write-Error "Common assembly not found at path: $commonAssemblyPath"
    exit 1
}
if (-not (Test-Path $coreAssemblyPath)) {
    Write-Error "Core assembly not found at path: $coreAssemblyPath"
    exit 1
}

try {
    # Load the common assembly first
    Add-Type -Path $commonAssemblyPath

    # Load the core assembly second
    Add-Type -Path $coreAssemblyPath
} catch {
    Write-Error "Failed to load assemblies: $_"
    exit 1
}

try {
    # Create an instance of the IEngineBuilder using the public static method in BlueDotBrigade.Weevil.Engine
    $engineBuilder = [BlueDotBrigade.Weevil.Engine]::UsingPath($logFilePath)

    # Open the engine (assuming that IEngineBuilder has an Open() method)
    $engine = $engineBuilder.Open()

    # Correct reference to FilterType in the namespace BlueDotBrigade.Weevil.Filter
    $filterEnum = [BlueDotBrigade.Weevil.Filter.FilterType]::RegularExpression

    $include = @"
\bHTTP\/[0-9.]+\s(4[0-9]{2}|5[0-9]{2})\b
"@

    $exclude = ""

    # Correct reference to FilterCriteria in BlueDotBrigade.Weevil.Filter
    $criteria = New-Object BlueDotBrigade.Weevil.Filter.FilterCriteria($include, $exclude)

    # Apply the filters using the enum value
    $engine.Filter.Apply($filterEnum, $criteria)

    # Iterate over the filtered results and print each record
    foreach ($record in $engine.Filter.Results) {
        Write-Output $record.ToString()
    }

} catch {
    Write-Error "An error occurred during execution: $_"
    exit 1
}