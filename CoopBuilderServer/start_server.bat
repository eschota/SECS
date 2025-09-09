@echo off
setlocal

REM Navigate to the script directory
cd /d "%~dp0"

REM Environment
set ASPNETCORE_ENVIRONMENT=Production

echo Building CoopBuilderServer (Release)...
dotnet build -c Release
if errorlevel 1 (
  echo Build failed. Exiting.
  exit /b 1
)

echo Starting CoopBuilderServer on http://localhost:3329 (PathBase=/api-game)...
dotnet run -c Release --no-launch-profile --urls http://localhost:3329

endlocal

