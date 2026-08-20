$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Parts = Join-Path $Root '.github\source-parts\mainform'
$Target = Join-Path $Root 'src\QFact.WE2Video\MainForm.cs'
$ExpectedSha256 = '58f3c3c31e6d970e9ca7a19e603acd354dfec4898e9370b8927e2ec4da4bce24'

if (-not (Test-Path $Parts)) { throw 'MainForm source parts are missing.' }
$files = Get-ChildItem $Parts -Filter 'part*.b64' -File | Sort-Object Name
if ($files.Count -ne 6) { throw "Expected 6 MainForm source parts, found $($files.Count)." }

$base64 = ($files | ForEach-Object { [IO.File]::ReadAllText($_.FullName).Trim() }) -join ''
$bytes = [Convert]::FromBase64String($base64)
[IO.File]::WriteAllBytes($Target, $bytes)

$actual = (Get-FileHash -Algorithm SHA256 $Target).Hash.ToLowerInvariant()
if ($actual -ne $ExpectedSha256) {
    Remove-Item $Target -Force -ErrorAction SilentlyContinue
    throw "MainForm source checksum mismatch. Expected $ExpectedSha256, got $actual."
}

Write-Host "Restored source: $Target ($($bytes.Length) bytes)" -ForegroundColor Green
