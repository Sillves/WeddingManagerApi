param(
    [string]$ConnectionString
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $scriptDir

if (-not $ConnectionString) {
    if ($env:DatabaseSettings__ConnectionString) {
        $ConnectionString = $env:DatabaseSettings__ConnectionString
    }
    else {
        $appSettings = Join-Path $root "WeddingManager.Web/appsettings.Development.json"
        if (-not (Test-Path $appSettings)) {
            throw "Could not find appsettings.Development.json at $appSettings"
        }

        $json = Get-Content $appSettings -Raw | ConvertFrom-Json
        $ConnectionString = $json.DatabaseSettings.ConnectionString
    }
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "No connection string found. Pass -ConnectionString or set DatabaseSettings__ConnectionString."
}

$env:DatabaseSettings__ConnectionString = $ConnectionString

Push-Location $root
try {
    dotnet ef database update --project WeddingManager.Infrastructure --startup-project WeddingManager.Web
}
finally {
    Pop-Location
}
