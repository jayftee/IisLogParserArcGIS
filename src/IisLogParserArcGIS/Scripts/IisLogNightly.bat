@echo off
rem ---------------------------------------------------------------------------------------------------------
rem Nightly wrapper for the Windows Task Scheduler: harvests YESTERDAY's logs and regenerates that year's
rem Dashboard. Schedule it once a day shortly after midnight (e.g. 02:00), to catch the last log lines IIS
rem wrote at the very end of the previous day, after that day's final hourly run (IisLogHourly.bat).
rem
rem Point the Scheduled Task at this .bat file, never at PowerShell directly.
rem
rem This is a TEMPLATE: the three paths below are placeholders. Edit them for your deployment.
rem   LOG_ROOT        Folder holding one sub-folder of IIS logs per year, e.g. \\server\share\IISLogs\2026.
rem                   If all your years share one folder, drop "\%TARGET_YEAR%" from -LogSourceDirectory below.
rem   DATABASE_ROOT   Folder for the per-year SQLite databases (Iis2026.sqlite, ...).
rem   DASHBOARD_ROOT  Folder for the per-year Dashboards (one sub-folder per year).
rem
rem The year comes from the date being harvested, NOT from the clock: at 02:00 on January 1 this harvests
rem December 31 of the year that just ended, and must write to that year's database and Dashboard.
rem (With one log folder per year, that one day also needs the new year's first log file, which lives in the
rem next folder, so December 31 can only be completed by a backfill run for the old year.)
rem ---------------------------------------------------------------------------------------------------------
setlocal

set LOG_ROOT=\\server\share\IISLogs
set DATABASE_ROOT=D:\Aggregates
set DASHBOARD_ROOT=D:\Dashboards

rem --- Yesterday's date (yyyy-MM-dd) and its year ---
for /f "usebackq" %%D in (`powershell -NoProfile -Command "(Get-Date).AddDays(-1).ToString('yyyy-MM-dd')"`) do set TARGET_DATE=%%D
if not defined TARGET_DATE (echo Could not work out the target date.& exit /b 1)
set TARGET_YEAR=%TARGET_DATE:~0,4%

powershell.exe -NoProfile -ExecutionPolicy Bypass ^
  -File "%~dp0Invoke-IisLogDailyRun.ps1" ^
  -LogSourceDirectory "%LOG_ROOT%\%TARGET_YEAR%" ^
  -OutputDatabasePath "%DATABASE_ROOT%\Iis%TARGET_YEAR%.sqlite" ^
  -OutputDirectory "%DASHBOARD_ROOT%\%TARGET_YEAR%" ^
  -TargetDate %TARGET_DATE%

exit /b %ERRORLEVEL%
