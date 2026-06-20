Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "Converting Markdown to HTML..."

# --------------------------------------------------------------------------------------
# Resolve paths
# --------------------------------------------------------------------------------------

$ProjectDirectory =
    Split-Path -Parent $MyInvocation.MyCommand.Path

$TemplateFile =
    Join-Path $ProjectDirectory "MarkdownTemplate.html"

# --------------------------------------------------------------------------------------
# Files to convert
# --------------------------------------------------------------------------------------

$ReleaseDirectory =
    Join-Path $ProjectDirectory "Release"

$MarkdownFiles =
    Get-ChildItem -Path $ReleaseDirectory -Filter "*.md" -File |
    ForEach-Object { Join-Path "Release" $_.Name }

# --------------------------------------------------------------------------------------
# Locate Markdig assembly from NuGet package cache
# --------------------------------------------------------------------------------------

$NuGetPackageRoot =
    Join-Path $env:USERPROFILE ".nuget\packages\markdig"

if (-not (Test-Path $NuGetPackageRoot))
{
    throw "Unable to locate NuGet package cache for Markdig."
}

$MarkdigAssembly = Get-ChildItem `
    -Path $NuGetPackageRoot `
    -Filter "Markdig.dll" `
    -Recurse |
    Where-Object { $_.FullName -match "netstandard2\.0" } |
    Select-Object -First 1

if (-not $MarkdigAssembly)
{
    throw "Unable to locate Markdig.dll."
}

Write-Host "Using:"
Write-Host $MarkdigAssembly.FullName

Add-Type -Path $MarkdigAssembly.FullName

# --------------------------------------------------------------------------------------
# Read HTML template
# --------------------------------------------------------------------------------------

if (-not (Test-Path $TemplateFile))
{
    throw "Template file not found: $TemplateFile"
}

$Template =
    Get-Content $TemplateFile -Raw

# --------------------------------------------------------------------------------------
# Build markdown pipeline
# --------------------------------------------------------------------------------------

$PipelineBuilder =
    New-Object Markdig.MarkdownPipelineBuilder

$null =
    [Markdig.MarkdownExtensions]::UseAdvancedExtensions($PipelineBuilder)

$Pipeline =
    $PipelineBuilder.Build()

# --------------------------------------------------------------------------------------
# Convert markdown files
# --------------------------------------------------------------------------------------

foreach ($RelativeMarkdownPath in $MarkdownFiles)
{
    $MarkdownFile =
        Join-Path $ProjectDirectory $RelativeMarkdownPath

    if (-not (Test-Path $MarkdownFile))
    {
        throw "Markdown file not found: $MarkdownFile"
    }

    $HtmlFile =
        [System.IO.Path]::ChangeExtension($MarkdownFile, ".html")

    Write-Host ""
    Write-Host "Converting:"
    Write-Host $MarkdownFile

    $Markdown =
        Get-Content $MarkdownFile -Raw

    $Body =
        [Markdig.Markdown]::ToHtml($Markdown, $Pipeline)

    $Title =
        [System.IO.Path]::GetFileNameWithoutExtension($MarkdownFile)

    $Html = $Template
    $Html = $Html.Replace("<!--TITLE-->", $Title)
    $Html = $Html.Replace("<!--BODY-->", $Body)

    Set-Content `
        -Path $HtmlFile `
        -Value $Html `
        -Encoding UTF8

    Write-Host "Generated:"
    Write-Host $HtmlFile
}

Write-Host ""
Write-Host "Markdown conversion complete."