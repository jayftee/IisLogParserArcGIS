@echo off
rem ---------------------------------------------------------------------------------------------------------
rem Backfill wrapper: harvests every Local Date of ONE year, then regenerates that year's Dashboard once.
rem
rem Usage:  IisLogBackfill.bat            backfills the current year
rem         IisLogBackfill.bat 2025       backfills 2025
rem
rem Two backfills for different years can run side by side: each writes to its own year's database and
rem Dashboard folder, so neither can overwrite the other.
rem
rem This is a TEMPLATE: the three paths below are placeholders. Edit them for your deployment.
rem   LOG_ROOT        Folder holding one sub-folder of IIS logs per year, e.g. \\server\share\IISLogs\2026.
rem                   If all your years share one folder, drop "\%YEAR%" from -LogSourceDirectory below.
rem   DATABASE_ROOT   Folder for the per-year SQLite databases (Iis2026.sqlite, ...).
rem   DASHBOARD_ROOT  Folder for the per-year Dashboards (one sub-folder per year).
rem ---------------------------------------------------------------------------------------------------------
setlocal

set LOG_ROOT=\\server\share\IISLogs
set DATABASE_ROOT=D:\Aggregates
set DASHBOARD_ROOT=D:\Dashboards

rem --- The year: the first argument, or the current year when none is given ---
set YEAR=%~1
if "%YEAR%"=="" (
  for /f "usebackq" %%Y in (`powershell -NoProfile -Command "(Get-Date).Year"`) do set YEAR=%%Y
)
if not defined YEAR (echo Could not work out the year.& exit /b 1)

powershell.exe -NoProfile -ExecutionPolicy Bypass ^
  -File "%~dp0Invoke-IisLogBackfill.ps1" ^
  -LogSourceDirectory "%LOG_ROOT%\%YEAR%" ^
  -OutputDatabasePath "%DATABASE_ROOT%\Iis%YEAR%.sqlite" ^
  -OutputDirectory "%DASHBOARD_ROOT%\%YEAR%" ^
  -Year %YEAR%

exit /b %ERRORLEVEL%
