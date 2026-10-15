@echo off
rem ---------------------------------------------------------------------------------------------------------
rem Hourly wrapper for the Windows Task Scheduler: harvests TODAY's logs so far and regenerates this year's
rem Dashboard. Schedule it every hour. Safe to repeat: re-harvesting a date is always a full replace of that
rem date's rows, never an append, so an hourly re-run just refreshes today with whatever has accumulated.
rem Pair it with IisLogNightly.bat, which closes out the previous day.
rem
rem Point the Scheduled Task at this .bat file, never at PowerShell directly.
rem
rem This is a TEMPLATE: the three paths below are placeholders. Edit them for your deployment.
rem   LOG_ROOT        Folder holding one sub-folder of IIS logs per year, e.g. \\server\share\IISLogs\2026.
rem                   If all your years share one folder, drop "\%TARGET_YEAR%" from -LogSourceDirectory below.
rem   DATABASE_ROOT   Folder for the per-year SQLite databases (Iis2026.sqlite, ...).
rem   DASHBOARD_ROOT  Folder for the per-year Dashboards (one sub-folder per year).
rem ---------------------------------------------------------------------------------------------------------
setlocal

set LOG_ROOT=\\server\share\IISLogs
set DATABASE_ROOT=D:\Aggregates
set DASHBOARD_ROOT=D:\Dashboards

rem --- Today's date (yyyy-MM-dd) and its year ---
for /f "usebackq" %%D in (`powershell -NoProfile -Command "(Get-Date).ToString('yyyy-MM-dd')"`) do set TARGET_DATE=%%D
if not defined TARGET_DATE (echo Could not work out the target date.& exit /b 1)
set TARGET_YEAR=%TARGET_DATE:~0,4%

powershell.exe -NoProfile -ExecutionPolicy Bypass ^
  -File "%~dp0Invoke-IisLogDailyRun.ps1" ^
  -LogSourceDirectory "%LOG_ROOT%\%TARGET_YEAR%" ^
  -OutputDatabasePath "%DATABASE_ROOT%\Iis%TARGET_YEAR%.sqlite" ^
  -OutputDirectory "%DASHBOARD_ROOT%\%TARGET_YEAR%" ^
  -TargetDate %TARGET_DATE%

exit /b %ERRORLEVEL%
