param(
    [Parameter(Position = 0)]
    [string]$Path,

    [Parameter(Position = 1)]
    [ValidateRange(1, 1000)]
    [int]$LineCount = 1
)

# Prompt for path if not provided
if ([string]::IsNullOrWhiteSpace($Path)) {

    $Path = Read-Host "Enter log file path"

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "A log file path is required."
    }

    $lineCountInput = Read-Host "Enter line count (default: 1)"

    if (-not [string]::IsNullOrWhiteSpace($lineCountInput)) {

        if (-not [int]::TryParse($lineCountInput, [ref]$LineCount)) {
            throw "Line count must be a valid integer."
        }

        if ($LineCount -lt 1 -or $LineCount -gt 1000) {
            throw "Line count must be between 1 and 1000."
        }
    }
}

# Resolve and validate path
$resolvedPath = Resolve-Path $Path -ErrorAction Stop

if (-not (Test-Path $resolvedPath)) {
    throw "File not found: $Path"
}

# Open file with shared read access
$fs = [System.IO.File]::Open(
    $resolvedPath,
    [System.IO.FileMode]::Open,
    [System.IO.FileAccess]::Read,
    [System.IO.FileShare]::ReadWrite
)

try {

    # ----- FIRST LINES -----

    $sr = New-Object System.IO.StreamReader($fs, $true)

    $firstLines = New-Object System.Collections.Generic.List[string]

    for ($i = 0; $i -lt $LineCount; $i++) {

        $line = $sr.ReadLine()

        if ($null -eq $line) {
            break
        }

        $firstLines.Add($line)
    }

    # ----- LAST LINES -----

    $bufferSize = 4096
    $fileLength = $fs.Length
    $position = $fileLength

    $sb = New-Object System.Text.StringBuilder

    while ($position -gt 0) {

        $readSize = [Math]::Min($bufferSize, $position)

        $position -= $readSize

        $fs.Seek($position, [System.IO.SeekOrigin]::Begin) | Out-Null

        $buffer = New-Object byte[] $readSize

        $fs.Read($buffer, 0, $readSize) | Out-Null

        $text = [System.Text.Encoding]::UTF8.GetString($buffer)

        $sb.Insert(0, $text) | Out-Null

        $lines = ($sb.ToString() -split "`r?`n") |
                 Where-Object { $_ -ne "" }

        # Stop once enough lines have been collected
        if ($lines.Count -ge $LineCount) {
            break
        }
    }

    $lastLines = $lines | Select-Object -Last $LineCount

    # ----- OUTPUT -----

    Write-Host "First $LineCount Line(s):"

    foreach ($line in $firstLines) {
        Write-Host $line
    }

    Write-Host ""

    Write-Host "Last $LineCount Line(s):"

    foreach ($line in $lastLines) {
        Write-Host $line
    }
}
finally {

    if ($sr) {
        $sr.Dispose()
    }

    if ($fs) {
        $fs.Dispose()
    }
}