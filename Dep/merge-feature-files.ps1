<#
.SYNOPSIS
Merges all `.feature` files in a specified directory and its subdirectories into a single output.

.DESCRIPTION
This script searches for all `.feature` files in the specified directory and its subdirectories, excluding `.feature.cs` files. 
The content of these files is displayed in the console and can be piped to a text file for further use.

.PARAMETER SourceDir
Specifies the root directory where the search for `.feature` files should begin.

.EXAMPLES

# Example 1: Display the merged content in the console
.\merge-feature-files.ps1 -SourceDir "..\Tst"

# Example 2: Save the merged content to a file
.\merge-feature-files.ps1 -SourceDir "..\Tst" > All.feature

# Example 3: Display the merged content and save it simultaneously
.\merge-feature-files.ps1 -SourceDir "..\Tst" | Tee-Object -FilePath All.feature
#>

param (
    [Parameter(Mandatory = $true, HelpMessage = "Specify the root directory to search for .feature files.")]
    [string]$SourceDir
)

# Ensure provided directory exists
if (-not (Test-Path -Path $SourceDir)) {
    Write-Error "The directory '$SourceDir' does not exist. Please provide a valid directory."
    exit 1
}

# Find all .feature files (excluding .feature.cs)
$FeatureFiles = Get-ChildItem -Path $SourceDir -Recurse -Filter "*.feature" | Where-Object {
    $_.Name -notlike "*.feature.cs"
}

if (-not $FeatureFiles) {
    Write-Output "No .feature files found in '$SourceDir'."
    exit 0
}

# Genreate output
foreach ($File in $FeatureFiles) {
    Get-Content -Path $File.FullName
}
