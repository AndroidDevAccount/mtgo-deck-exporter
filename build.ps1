param([switch]$Portable)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'MtgoDeckExporter\MtgoDeckExporter.csproj'
$output = Join-Path $PSScriptRoot 'dist'

if ($Portable) {
    dotnet publish $project -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $output
} else {
    dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $output
}

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

Write-Host "Built: $output\MTGO Deck Exporter.exe"
