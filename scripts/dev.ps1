# usage: .\scripts\dev.ps1 -mode key-value
# usage: .\scripts\dev.ps1 -mode value-key
# usage: .\scripts\dev.ps1 -mode key-value -reverse
# usage: .\scripts\dev.ps1 -mode value-key -reverse
param (
    [Parameter(HelpMessage = "Specify whether to replace keys with values or values with keys")]
    [ValidateSet("key-value", "value-key")]
    [string]$mode = "key-value",
    [switch]$reverse
)

# Read Key="Value" pairs from the .env file
$FILE = ".env"
$env = @{}

if (Test-Path $FILE) {
    Get-Content $FILE | ForEach-Object {
        if ($_ -match '^([^=]+)=(.*)$') {
            $env[$Matches[1]] = $Matches[2] -replace '^["'']|["'']$', ''
        }
    }
}

$env

$PROJECT_NAME = "Rhythm"
$PROJECT_PATH = ".\$PROJECT_NAME"

# Replace all the keys in *.cs files
if ($mode -eq "key-value") {
    $env.GetEnumerator() | ForEach-Object {
        $key = $_.Key
        $value = $_.Value

        Get-ChildItem -Path $PROJECT_PATH -Filter *.cs -Recurse | ForEach-Object {
            $file = $_.FullName
            $content = Get-Content $file -Raw

            if ($reverse) {
                $content = $content -replace "$value", $key
            } else {
                $content = $content -replace "$key", $value
            }

            $content | Set-Content $file -NoNewline
        }
    }
} else {
    $env.GetEnumerator() | ForEach-Object {
        $key = $_.Key
        $value = $_.Value

        Get-ChildItem -Path $PROJECT_PATH -Filter *.cs -Recurse | ForEach-Object {
            $file = $_.FullName
            $content = Get-Content $file -Raw

            if ($reverse) {
                $content = $content -replace "$key", $value
            } else {
                $content = $content -replace "$value", $key
            }

            $content | Set-Content $file -NoNewline
        }
    }
}
