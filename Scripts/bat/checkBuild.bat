@echo off
rem SSU: Run the local workflow build from Explorer or a terminal.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0workflowChecks.ps1" -Check Build %*
exit /b %errorlevel%
