<#
.SYNOPSIS
    Runs IisLogParserArcGIS.exe's harvest-regenerate verb for one explicit date, for a Scheduled Task (via
    one of the .bat wrappers next to this script) that runs unattended.
.DESCRIPTION
    A thin wrapper around the harvest-regenerate verb, which already harvests one Local Date and
    regenerates that date's year of the Dashboard in one process. This script is given the date and
    passes it straight through: it does not know what "today" or "yesterday" means, so the caller -
    typically a .bat wrapper - decides, and the Scheduled Task's own arguments never need editing as
    the calendar moves forward.

    The Dashboard covers the year of -TargetDate. A caller that harvests across a year boundary (the
    early-morning run for "yesterday" on January 1 targets December 31 of the year just ended) must
    therefore name the database and output directory for that date's year, not for the clock's. The
    .bat wrappers do exactly that.

    Unlike Invoke-IisLogBackfill.ps1, this script never scans the log source directory for dates -
    it always targets exactly the one date it was given, so its own exit code is simply whatever
    harvest-regenerate itself returned.

    Intended to be scheduled twice (see IisLogHourly.bat and IisLogNightly.bat):
      - Hourly, with today's date, so each run re-harvests however much of the current day's logs
        exist so far. Safe to repeat: a Daily Batch replace is always a full replace of that date's
        rows, never incremental, so an hourly re-run just replaces today's batch with whatever has
        accumulated since the last run - no double-counting.
      - Once daily, shortly after midnight (e.g. 02:00), with yesterday's date, to catch the last log
        lines IIS wrote right at the end of the previous day, after that day's final hourly run
        already happened.
.PARAMETER LogSourceDirectory
    Directory containing the IIS W3C log files to process.
.PARAMETER OutputDatabasePath
    Path to the output SQLite database file.
.PARAMETER OutputDirectory
    Directory the Dashboard is generated into. It must be new, empty, or hold a Dashboard a previous run
    generated (it carries a .iis-dashboard marker); any other non-empty directory is refused.
.PARAMETER TargetDate
    The Local Date to harvest, in yyyy-MM-dd format. Its year is the year the Dashboard reports on.
.EXAMPLE
    .\Invoke-IisLogDailyRun.ps1 -LogSourceDirectory "D:\IISLogs\2026" -OutputDatabasePath "D:\Aggregates\Iis2026.sqlite" -OutputDirectory "D:\Dashboards\2026" -TargetDate 2026-10-14
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$LogSourceDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDatabasePath,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [Parameter(Mandatory = $true)]
    [ValidateScript({
            $parsed = [datetime]::MinValue
            if (-not [datetime]::TryParseExact($_, 'yyyy-MM-dd', [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::None, [ref]$parsed)) {
                throw "TargetDate '$_' is not a valid date. Expected format: yyyy-MM-dd."
            }
            $true
        })]
    [string]$TargetDate
)

$exePath = Join-Path (Split-Path -Parent $PSScriptRoot) 'IisLogParserArcGIS.exe'

if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) {
    Write-Error "Could not find IisLogParserArcGIS.exe at '$exePath'. This script must be run from its copied location next to the compiled executable."
    exit 1
}

if (-not (Test-Path -LiteralPath $LogSourceDirectory -PathType Container)) {
    Write-Error "Log source directory '$LogSourceDirectory' does not exist."
    exit 1
}

Write-Host "Running harvest-regenerate for $TargetDate..."

& $exePath harvest-regenerate $LogSourceDirectory $TargetDate $OutputDatabasePath $OutputDirectory
exit $LASTEXITCODE
