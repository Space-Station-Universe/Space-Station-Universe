@echo off
rem SSU: Build and run the same prototype linter used by GitHub Actions.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0workflowChecks.ps1" -Check Yaml %*
exit /b %errorlevel%
