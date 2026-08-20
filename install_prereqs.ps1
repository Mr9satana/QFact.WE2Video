$ErrorActionPreference = 'Continue'
Write-Host 'QFact.WE2Video 1.0.3 - prerequisites / dependencies' -ForegroundColor Cyan
Write-Host ''
if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
  Write-Host 'ERROR: winget was not found. Install/update App Installer from Microsoft Store.' -ForegroundColor Red
  exit 1
}
Write-Host '[1/3] .NET 9 SDK (needed only to build the EXE)...' -ForegroundColor Yellow
winget install --id Microsoft.DotNet.SDK.9 -e --accept-package-agreements --accept-source-agreements
Write-Host ''
Write-Host '[2/3] Full FFmpeg build...' -ForegroundColor Yellow
winget install --id Gyan.FFmpeg -e --accept-package-agreements --accept-source-agreements
winget upgrade --id Gyan.FFmpeg -e --accept-package-agreements --accept-source-agreements
Write-Host ''
Write-Host '[3/3] Microsoft Edge WebView2 Runtime...' -ForegroundColor Yellow
winget install --id Microsoft.EdgeWebView2Runtime -e --accept-package-agreements --accept-source-agreements
winget upgrade --id Microsoft.EdgeWebView2Runtime -e --accept-package-agreements --accept-source-agreements
Write-Host ''
Write-Host 'Done. Run publish_release.bat to create the self-contained QFact.WE2Video.exe.' -ForegroundColor Green
