@echo off
setlocal EnableExtensions
cd /d "%~dp0"
set "PROJECT=src\QFact.WE2Video\QFact.WE2Video.csproj"
set "OUT=release\win-x64"

echo.
echo ==========================================================
echo  QFact.WE2Video 1.0.3 - Release Publisher
echo ==========================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: .NET 9 SDK is not installed.
  echo Run install_prereqs.bat first.
  pause
  exit /b 1
)

for /f "tokens=1" %%v in ('dotnet --version') do set "DOTNET_VERSION=%%v"
echo .NET SDK: %DOTNET_VERSION%

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0prepare_ui.ps1"
if errorlevel 1 goto :fail

if exist "%OUT%" rmdir /s /q "%OUT%"
mkdir "%OUT%" >nul 2>nul

echo.
echo Publishing self-contained single-file win-x64 build...
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=embedded -o "%OUT%"
if errorlevel 1 goto :fail

if not exist "%OUT%\QFact.WE2Video.exe" (
  echo ERROR: QFact.WE2Video.exe was not produced.
  goto :fail
)

echo.
echo Checking for unexpected publish sidecars...
for %%F in ("%OUT%\*") do (
  if /I not "%%~nxF"=="QFact.WE2Video.exe" echo NOTE: publish sidecar: %%~nxF
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "$p='%OUT%\QFact.WE2Video.exe'; $h=(Get-FileHash -Algorithm SHA256 $p).Hash.ToLower(); Set-Content -Encoding ASCII 'release\SHA256SUMS.txt' ($h + '  QFact.WE2Video.exe'); Write-Host ('SHA-256: ' + $h) -ForegroundColor Cyan"

copy /Y "README.md" "release\README.md" >nul
copy /Y "THIRD_PARTY.md" "release\THIRD_PARTY.md" >nul
copy /Y "CHANGELOG.md" "release\CHANGELOG.md" >nul

echo.
echo ==========================================================
echo  RELEASE BUILD SUCCESS
echo  %CD%\%OUT%\QFact.WE2Video.exe
echo ==========================================================
echo.
start "" "%OUT%"
pause
exit /b 0

:fail
echo.
echo RELEASE BUILD FAILED.
pause
exit /b 1
