@echo off
rem SSU: Run the local build, tests, prototype linter, and sprite checks.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0workflowChecks.ps1" -Check All %*
exit /b %errorlevel%
