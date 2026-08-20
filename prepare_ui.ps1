$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $Root 'src\QFact.WE2Video'
$Ui = Join-Path $Project 'ui'
$Parts = Join-Path $Ui 'css.parts'
$Css = Join-Path $Ui 'app.css'
$Bundle = Join-Path $Project 'ui.bundle.zip'

if (-not (Test-Path (Join-Path $Ui 'index.html'))) { throw 'UI folder is missing.' }

if (Test-Path $Parts) {
    $chunks = Get-ChildItem $Parts -File | Sort-Object Name | ForEach-Object { [IO.File]::ReadAllText($_.FullName) }
    [IO.File]::WriteAllText($Css, ($chunks -join ''), [Text.UTF8Encoding]::new($false))
}

if (-not (Test-Path $Css)) { throw 'app.css is missing and could not be reconstructed.' }
if (Test-Path $Bundle) { Remove-Item $Bundle -Force }
Compress-Archive -Path (Join-Path $Ui '*') -DestinationPath $Bundle -CompressionLevel Optimal
if (-not (Test-Path $Bundle)) { throw 'Could not create ui.bundle.zip.' }
Write-Host "UI bundle: $Bundle" -ForegroundColor Green
