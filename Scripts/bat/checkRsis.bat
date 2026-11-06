@echo off
rem SSU: Validate sprite metadata and PNGs with the workflow RSI validator.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0workflowChecks.ps1" -Check Rsis %*
exit /b %errorlevel%
