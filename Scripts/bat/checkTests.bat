@echo off
rem SSU: Build and run the workflow unit and integration tests.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0workflowChecks.ps1" -Check Tests %*
exit /b %errorlevel%
