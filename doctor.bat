@echo off
cd /d "%~dp0"
set "APP=release\win-x64\QFact.WE2Video.exe"
if not exist "%APP%" set "APP=dist\QFact.WE2Video.exe"
if not exist "%APP%" (
  echo QFact.WE2Video.exe not found. Run publish_release.bat or build.bat first.
  pause
  exit /b 1
)
"%APP%" --doctor
pause
