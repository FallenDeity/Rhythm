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
