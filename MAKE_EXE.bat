@echo off
setlocal
cd /d "%~dp0"
title QFact.WE2Video 1.0.3 - Build EXE
where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET 9 SDK is not available. Running prerequisite installer first...
  call "%~dp0install_prereqs.bat"
  if errorlevel 1 exit /b 1
)
call "%~dp0publish_release.bat"
exit /b %ERRORLEVEL%
