@echo off
cd /d "%~dp0"
if exist "release\win-x64\QFact.WE2Video.exe" (
  start "" "release\win-x64\QFact.WE2Video.exe"
  exit /b
)
if exist "dist\QFact.WE2Video.exe" (
  start "" "dist\QFact.WE2Video.exe"
  exit /b
)
echo QFact.WE2Video.exe not found. Run publish_release.bat or build.bat first.
pause
