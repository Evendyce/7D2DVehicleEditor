$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishRoot = Join-Path $projectRoot "release"
$publishDir = Join-Path $publishRoot "publish"

if (Test-Path $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

Push-Location $projectRoot
try {
    dotnet publish .\SevenDaysVehicleEditor.csproj -c Release -o $publishDir
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Publish complete:" -ForegroundColor Green
Write-Host $publishDir
