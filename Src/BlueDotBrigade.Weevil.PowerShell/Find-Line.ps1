param(
    [Parameter(Mandatory=$true)]
    [string]$pattern,

    [string]$fileExtension = ".log"
)

Write-Output " "

Get-ChildItem -Recurse -Filter "*$fileExtension" | 
    Select-String -Pattern $pattern -List | 
    ForEach-Object {
        $filename = [System.IO.Path]::GetFileName($_.Path)
        Write-Output "# $($filename)"
        
        # Find all matching lines in the file and output LineNumber and Line
        Select-String -Path $_.Path -Pattern $pattern | 
        ForEach-Object {
            Write-Output "$($_.LineNumber)`t$($_.Line)"
        }
    }
