$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Parts = Join-Path $Root '.github\source-parts\mainform'
$Target = Join-Path $Root 'src\QFact.WE2Video\MainForm.cs'

# Modern repository revisions keep MainForm.cs directly in source control. Do not overwrite it
# with the v1.0.3 bootstrap chunks; those are only a compatibility fallback for old checkouts.
if (Test-Path $Target) {
    Write-Host "Source already present: $Target" -ForegroundColor Green
    exit 0
}

if (-not (Test-Path $Parts)) { throw 'MainForm source parts are missing.' }
$files = Get-ChildItem $Parts -Filter 'part*.b64' -File | Sort-Object Name
if ($files.Count -ne 6) { throw "Expected 6 MainForm source parts, found $($files.Count)." }
$base64 = ($files | ForEach-Object { [IO.File]::ReadAllText($_.FullName).Trim() }) -join ''
$bytes = [Convert]::FromBase64String($base64)
[IO.File]::WriteAllBytes($Target, $bytes)
Write-Host "Restored legacy source: $Target ($($bytes.Length) bytes)" -ForegroundColor Yellow
