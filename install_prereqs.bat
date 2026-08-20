@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install_prereqs.ps1"
if errorlevel 1 (
  echo.
  echo Prerequisite installation failed.
  pause
  exit /b 1
)
echo.
pause
