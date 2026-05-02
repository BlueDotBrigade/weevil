param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [double]$SizeInGigabytes,

    [ValidateSet('auto', 'singlebyte', 'utf16le', 'utf16be', 'utf32le', 'utf32be')]
    [string]$EncodingMode = 'auto',

    [ValidateRange(1, 1024)]
    [int]$ReadBufferMB = 8
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "File not found: $Path"
}

if ($SizeInGigabytes -le 0) {
    throw 'SizeInGigabytes must be greater than 0.'
}

$maxBytesPerPart = [int64]($SizeInGigabytes * 1GB)
if ($maxBytesPerPart -lt 1) {
    throw 'Computed part size is too small.'
}

$fullPath  = (Resolve-Path -LiteralPath $Path).Path
$directory = [System.IO.Path]::GetDirectoryName($fullPath)
$baseName  = [System.IO.Path]::GetFileNameWithoutExtension($fullPath)
$extension = [System.IO.Path]::GetExtension($fullPath)

function Get-NewlinePatterns {
    param(
        [byte[]]$BomBytes,
        [string]$Mode
    )

    switch ($Mode) {
        'utf16le' {
            return @{
                CRLF = [byte[]](0x0D,0x00,0x0A,0x00)
                LF   = [byte[]](0x0A,0x00)
                CR   = [byte[]](0x0D,0x00)
            }
        }
        'utf16be' {
            return @{
                CRLF = [byte[]](0x00,0x0D,0x00,0x0A)
                LF   = [byte[]](0x00,0x0A)
                CR   = [byte[]](0x00,0x0D)
            }
        }
        'utf32le' {
            return @{
                CRLF = [byte[]](0x0D,0x00,0x00,0x00,0x0A,0x00,0x00,0x00)
                LF   = [byte[]](0x0A,0x00,0x00,0x00)
                CR   = [byte[]](0x0D,0x00,0x00,0x00)
            }
        }
        'utf32be' {
            return @{
                CRLF = [byte[]](0x00,0x00,0x00,0x0D,0x00,0x00,0x00,0x0A)
                LF   = [byte[]](0x00,0x00,0x00,0x0A)
                CR   = [byte[]](0x00,0x00,0x00,0x0D)
            }
        }
        'singlebyte' {
            return @{
                CRLF = [byte[]](0x0D,0x0A)
                LF   = [byte[]](0x0A)
                CR   = [byte[]](0x0D)
            }
        }
        'auto' {
            if ($BomBytes.Length -ge 4) {
                if ($BomBytes[0] -eq 0xFF -and $BomBytes[1] -eq 0xFE -and $BomBytes[2] -eq 0x00 -and $BomBytes[3] -eq 0x00) {
                    return Get-NewlinePatterns -BomBytes $BomBytes -Mode 'utf32le'
                }
                if ($BomBytes[0] -eq 0x00 -and $BomBytes[1] -eq 0x00 -and $BomBytes[2] -eq 0xFE -and $BomBytes[3] -eq 0xFF) {
                    return Get-NewlinePatterns -BomBytes $BomBytes -Mode 'utf32be'
                }
            }
            if ($BomBytes.Length -ge 2) {
                if ($BomBytes[0] -eq 0xFF -and $BomBytes[1] -eq 0xFE) {
                    return Get-NewlinePatterns -BomBytes $BomBytes -Mode 'utf16le'
                }
                if ($BomBytes[0] -eq 0xFE -and $BomBytes[1] -eq 0xFF) {
                    return Get-NewlinePatterns -BomBytes $BomBytes -Mode 'utf16be'
                }
            }

            return Get-NewlinePatterns -BomBytes $BomBytes -Mode 'singlebyte'
        }
        default {
            throw "Unsupported encoding mode: $Mode"
        }
    }
}

function Test-MatchAt {
    param(
        [byte[]]$Buffer,
        [int]$Index,
        [int]$Length,
        [byte[]]$Pattern
    )

    if ($Index -lt 0) { return $false }
    if (($Index + $Pattern.Length) -gt $Length) { return $false }

    for ($i = 0; $i -lt $Pattern.Length; $i++) {
        if ($Buffer[$Index + $i] -ne $Pattern[$i]) {
            return $false
        }
    }

    return $true
}

function Find-LastNewlineEnd {
    param(
        [byte[]]$Buffer,
        [int]$Length,
        [int]$MaxEndExclusive,
        [hashtable]$Patterns
    )

    $maxCheck = [Math]::Min($Length, $MaxEndExclusive)

    for ($end = $maxCheck; $end -gt 0; $end--) {
        $start = $end - $Patterns.CRLF.Length
        if (Test-MatchAt -Buffer $Buffer -Index $start -Length $Length -Pattern $Patterns.CRLF) {
            return $end
        }

        $start = $end - $Patterns.LF.Length
        if (Test-MatchAt -Buffer $Buffer -Index $start -Length $Length -Pattern $Patterns.LF) {
            return $end
        }

        $start = $end - $Patterns.CR.Length
        if (Test-MatchAt -Buffer $Buffer -Index $start -Length $Length -Pattern $Patterns.CR) {
            return $end
        }
    }

    return -1
}

function Find-FirstNewlineEnd {
    param(
        [byte[]]$Buffer,
        [int]$Length,
        [hashtable]$Patterns
    )

    for ($i = 0; $i -lt $Length; $i++) {
        if (Test-MatchAt -Buffer $Buffer -Index $i -Length $Length -Pattern $Patterns.CRLF) {
            return ($i + $Patterns.CRLF.Length)
        }
        if (Test-MatchAt -Buffer $Buffer -Index $i -Length $Length -Pattern $Patterns.LF) {
            return ($i + $Patterns.LF.Length)
        }
        if (Test-MatchAt -Buffer $Buffer -Index $i -Length $Length -Pattern $Patterns.CR) {
            return ($i + $Patterns.CR.Length)
        }
    }

    return -1
}

function New-PartStream {
    param(
        [string]$Directory,
        [string]$BaseName,
        [string]$Extension,
        [int]$PartNumber,
        [int]$BufferSize
    )

    $partFileName = '{0}.part{1:D4}{2}' -f $BaseName, $PartNumber, $Extension
    $partPath = Join-Path $Directory $partFileName

    return New-Object System.IO.FileStream (
        $partPath,
        [System.IO.FileMode]::Create,
        [System.IO.FileAccess]::Write,
        [System.IO.FileShare]::None,
        $BufferSize,
        [System.IO.FileOptions]::SequentialScan
    )
}

$input = $null
$output = $null

try {
    $readBufferSize = $ReadBufferMB * 1MB
    $input = New-Object System.IO.FileStream (
        $fullPath,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read,
        $readBufferSize,
        [System.IO.FileOptions]::SequentialScan
    )

    $bomProbe = New-Object byte[] 4
    $bomRead = $input.Read($bomProbe, 0, $bomProbe.Length)
    if ($bomRead -gt 0) {
        $actualBom = New-Object byte[] $bomRead
        [Array]::Copy($bomProbe, 0, $actualBom, 0, $bomRead)
    }
    else {
        $actualBom = [byte[]]@()
    }
    $input.Position = 0

    $patterns = Get-NewlinePatterns -BomBytes $actualBom -Mode $EncodingMode

    $readBuffer = New-Object byte[] $readBufferSize
    $workBuffer = New-Object byte[] ($readBufferSize * 2)

    $carryCount = 0
    $partNumber = 1
    $bytesInPart = 0L

    while ($true) {
        $bytesRead = $input.Read($readBuffer, 0, $readBuffer.Length)
        if ($bytesRead -eq 0) {
            break
        }

        if (($carryCount + $bytesRead) -gt $workBuffer.Length) {
            $newSize = [Math]::Max($workBuffer.Length * 2, $carryCount + $bytesRead)
            $newBuffer = New-Object byte[] $newSize
            if ($carryCount -gt 0) {
                [Array]::Copy($workBuffer, 0, $newBuffer, 0, $carryCount)
            }
            $workBuffer = $newBuffer
        }

        [Array]::Copy($readBuffer, 0, $workBuffer, $carryCount, $bytesRead)
        $workCount = $carryCount + $bytesRead
        $offset = 0

        while ($offset -lt $workCount) {
            if ($null -eq $output) {
                $output = New-PartStream -Directory $directory -BaseName $baseName -Extension $extension -PartNumber $partNumber -BufferSize $readBufferSize
            }

            $remainingInPart = $maxBytesPerPart - $bytesInPart
            if ($remainingInPart -le 0) {
                $output.Dispose()
                $output = $null
                $partNumber++
                $bytesInPart = 0L
                continue
            }

            $available = $workCount - $offset

            if ($available -le $remainingInPart) {
                $output.Write($workBuffer, $offset, $available)
                $bytesInPart += $available
                $offset = $workCount
                break
            }

            $splitSearchLength = [int][Math]::Min($available, $remainingInPart)
            $splitEnd = Find-LastNewlineEnd -Buffer $workBuffer -Length $workCount -MaxEndExclusive ($offset + $splitSearchLength) -Patterns $patterns

            if ($splitEnd -gt $offset) {
                $countToWrite = $splitEnd - $offset
                $output.Write($workBuffer, $offset, $countToWrite)
                $bytesInPart += $countToWrite
                $offset = $splitEnd

                $output.Dispose()
                $output = $null
                $partNumber++
                $bytesInPart = 0L
                continue
            }

            if ($bytesInPart -gt 0) {
                $output.Dispose()
                $output = $null
                $partNumber++
                $bytesInPart = 0L
                continue
            }

            $newlineEnd = Find-FirstNewlineEnd -Buffer $workBuffer -Length $workCount -Patterns $patterns
            if ($newlineEnd -gt 0) {
                if ($newlineEnd -gt $maxBytesPerPart) {
                    throw 'A single line exceeds the requested part size. Refusing to split mid-line.'
                }

                $output.Write($workBuffer, $offset, $newlineEnd - $offset)
                $bytesInPart += ($newlineEnd - $offset)
                $offset = $newlineEnd

                $output.Dispose()
                $output = $null
                $partNumber++
                $bytesInPart = 0L
                continue
            }

            if ($workCount -gt $maxBytesPerPart) {
                throw 'A single line exceeds the requested part size. Refusing to split mid-line.'
            }

            break
        }

        $carryCount = $workCount - $offset
        if ($carryCount -gt 0) {
            [Array]::Copy($workBuffer, $offset, $workBuffer, 0, $carryCount)
        }
    }

    if ($carryCount -gt 0) {
        if (($bytesInPart + $carryCount) -gt $maxBytesPerPart -and $bytesInPart -gt 0) {
            if ($null -ne $output) {
                $output.Dispose()
                $output = $null
            }
            $partNumber++
            $bytesInPart = 0L
        }

        if ($carryCount -gt $maxBytesPerPart) {
            throw 'A single line exceeds the requested part size. Refusing to split mid-line.'
        }

        if ($null -eq $output) {
            $output = New-PartStream -Directory $directory -BaseName $baseName -Extension $extension -PartNumber $partNumber -BufferSize $readBufferSize
        }

        $output.Write($workBuffer, 0, $carryCount)
        $bytesInPart += $carryCount
    }
}
finally {
    if ($null -ne $output) { $output.Dispose() }
    if ($null -ne $input)  { $input.Dispose() }
}