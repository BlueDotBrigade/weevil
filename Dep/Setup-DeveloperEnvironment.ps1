[CmdletBinding()]
param(
	 [switch] $RepairDesignTimeCache
)

$ErrorActionPreference = 'Stop'

function Test-MetalamaExtensionInstalled {
	 $visualStudioRoot = Join-Path $env:LOCALAPPDATA 'Microsoft\VisualStudio'

	 if (-not (Test-Path -LiteralPath $visualStudioRoot)) {
		  return $false
	 }

	 $instances = Get-ChildItem -LiteralPath $visualStudioRoot -Directory -Filter '18.0_*' -ErrorAction SilentlyContinue

	 foreach ($instance in $instances) {
		  $manifests = Get-ChildItem -LiteralPath $instance.FullName -Recurse -File -Filter 'extension.vsixmanifest' -ErrorAction SilentlyContinue

		  foreach ($manifest in $manifests) {
				if (Select-String -Path $manifest.FullName -Pattern 'Metalama' -SimpleMatch -Quiet) {
					 return $true
				}
		  }
	 }

	 return $false
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$vsConfigPath = Join-Path $repoRoot '.vsconfig'

Write-Host ''
Write-Host 'Weevil developer environment check'
Write-Host ('Repository root: {0}' -f $repoRoot)
Write-Host ''

if (Test-Path -LiteralPath $vsConfigPath) {
	 Write-Host '.vsconfig detected. Open this repository in Visual Studio to install missing workloads/components.' -ForegroundColor Green
}
else {
	 Write-Warning '.vsconfig is missing. Visual Studio component installation prompt may not appear.'
}

if (Test-MetalamaExtensionInstalled) {
	 Write-Host 'Metalama Visual Studio extension appears to be installed.' -ForegroundColor Green
}
else {
	 Write-Warning 'Metalama Visual Studio extension was not detected.'
	 Write-Host 'Install it from Visual Studio > Extensions > Manage Extensions, then search for "Metalama".'
	 Write-Host 'Marketplace URL: https://marketplace.visualstudio.com/items?itemName=PostSharpTechnologies.Metalama'
}

if ($RepairDesignTimeCache) {
	 Write-Host ''
	 Write-Host 'Repairing local design-time caches (close Visual Studio first)...' -ForegroundColor Yellow

	 $solutionCache = Join-Path $repoRoot '.vs'
	 if (Test-Path -LiteralPath $solutionCache) {
		  Remove-Item -LiteralPath $solutionCache -Recurse -Force -ErrorAction SilentlyContinue
		  Write-Host 'Deleted .vs cache.'
	 }

	 Get-ChildItem -LiteralPath $repoRoot -Recurse -Directory -Force -ErrorAction SilentlyContinue |
		  Where-Object { $_.Name -in @('bin', 'obj') } |
		  ForEach-Object {
				Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
		  }

	 Write-Host 'Deleted bin/obj folders.'
	 dotnet nuget locals all --clear | Out-Host
	 Write-Host 'NuGet local caches cleared.'
}

Write-Host ''
Write-Host 'Done.' -ForegroundColor Green
