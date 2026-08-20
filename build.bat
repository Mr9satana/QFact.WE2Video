@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: .NET 9 SDK not found. Run install_prereqs.bat first.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0prepare_ui.ps1"
if errorlevel 1 goto :fail
if exist "dist" rmdir /s /q "dist"
dotnet build "src\QFact.WE2Video\QFact.WE2Video.csproj" -c Release -r win-x64 -o "dist"
if errorlevel 1 goto :fail
echo.
echo BUILD SUCCESS
pause
exit /b 0
:fail
echo.
echo BUILD FAILED
pause
exit /b 1
