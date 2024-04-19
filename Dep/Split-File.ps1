param (
    [string]$inputFilePath,
    [string]$delimiter = "------"
)

# Resolve the full path of the input file
$inputFilePath = Resolve-Path -Path $inputFilePath

# Ensure the input file exists
if (-Not (Test-Path -Path $inputFilePath)) {
    Write-Host "Input file does not exist."
    return
}

$fileDirectory = [System.IO.Path]::GetDirectoryName($inputFilePath)
$filePrefix = [System.IO.Path]::GetFileNameWithoutExtension($inputFilePath)
$fileExtension = [System.IO.Path]::GetExtension($inputFilePath)


# Display initial operation details
Write-Host "Processing input file: $inputFilePath"
Write-Host "Output will be written to: $fileDirectory"

$counter = 0
$writer = $null

# Get file length for progress tracking
$fileLength = (Get-Item $inputFilePath).Length
$currentPosition = 0
$updateInterval = $fileLength / 100 # Update every 1% to make it easier to control and round to nearest 10%
$nextUpdateThreshold = $updateInterval

try {
    # Process each line of the input file, reading as a stream
    $reader = [System.IO.StreamReader]::new($inputFilePath)
    while ($null -ne ($line = $reader.ReadLine())) {
        $currentPosition = $reader.BaseStream.Position

        # Progress tracking adjustment
        if ($currentPosition -ge $nextUpdateThreshold) {
            $percentageComplete = [math]::Round(($currentPosition / $fileLength) * 100)
            $roundedPercentage = [math]::Round($percentageComplete / 10) * 10 # Correctly round to nearest 10%
            Write-Host "$roundedPercentage% complete"
            $nextUpdateThreshold += $updateInterval
        }


        if ($line -match "^$delimiter") {
            if ($null -ne $writer) {
                # Close the current writer if it's open
                $writer.Close()
                $writer = $null
            }
            $counter++

            $newFilePath = Join-Path -Path $fileDirectory -ChildPath "${filePrefix}_$(($counter).ToString('0000'))$fileExtension"

            $writer = New-Object System.IO.StreamWriter $newFilePath
        } elseif ($null -ne $writer) {
            # Write the line to the current file if a writer exists
            $writer.WriteLine($line)
        }
    }
} finally {
    if ($null -ne $reader) {
        $reader.Close()
    }
    if ($null -ne $writer) {
        $writer.Close()
    }
}

if ($counter -gt 0) {
    Write-Host "Splitting completed. Created $counter files."
} else {
    Write-Host "No delimiters found. No files were created."
}
