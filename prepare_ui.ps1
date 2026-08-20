$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $Root 'src\QFact.WE2Video'
$Ui = Join-Path $Project 'ui'
$Bundle = Join-Path $Project 'ui.bundle.zip'
if (-not (Test-Path (Join-Path $Ui 'index.html'))) { throw 'UI folder is missing.' }
if (Test-Path $Bundle) { Remove-Item $Bundle -Force }
Compress-Archive -Path (Join-Path $Ui '*') -DestinationPath $Bundle -CompressionLevel Optimal
if (-not (Test-Path $Bundle)) { throw 'Could not create ui.bundle.zip.' }
Write-Host "UI bundle: $Bundle" -ForegroundColor Green
