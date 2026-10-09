<#
.SYNOPSIS
    Runs IisLogParserArcGIS.exe's harvest-regenerate verb for one day - today by default, or
    yesterday when -Yesterday is given - for a Scheduled Task that runs this unattended.
.DESCRIPTION
    A thin wrapper around the harvest-regenerate verb, which already harvests one local date and
    regenerates the Dashboard in one process. This script only computes which date "today" or
    "yesterday" means on the machine it runs on and passes that straight through, so the Scheduled
    Task's own arguments never need editing as the calendar moves forward.

    Unlike Invoke-IisLogBackfill.ps1, this script never scans the log source directory for dates -
    it always targets exactly the one date it computed, so its own exit code is simply whatever
    harvest-regenerate itself returned.

    Intended to be scheduled twice:
      - Hourly, with no -Yesterday switch, so each run re-harvests however much of the current
        day's logs exist so far. Safe to repeat: a Daily Batch replace is always a full replace of
        that date's rows, never incremental, so an hourly re-run just replaces today's batch with
        whatever has accumulated since the last run - no double-counting.
      - Once daily, shortly after midnight (e.g. 02:00), with -Yesterday, to catch the last log
        lines IIS wrote right at the end of the previous day, after that day's final hourly run
        already happened.
.PARAMETER LogSourceDirectory
    Directory containing the IIS W3C log files to process.
.PARAMETER OutputDatabasePath
    Path to the output SQLite database file.
.PARAMETER Yesterday
    Target yesterday's local date instead of today's. Omit for the normal hourly run; pass it for
    the once-daily early-morning run that catches the previous day's final log lines.
.EXAMPLE
    .\Invoke-IisLogDailyRun.ps1 -LogSourceDirectory "D:\IISLogs" -OutputDatabasePath "D:\Aggregates\usage.sqlite"
.EXAMPLE
    .\Invoke-IisLogDailyRun.ps1 -LogSourceDirectory "D:\IISLogs" -OutputDatabasePath "D:\Aggregates\usage.sqlite" -Yesterday
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$LogSourceDirectory,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$OutputDatabasePath,

    [switch]$Yesterday
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

$targetDate = if ($Yesterday) { (Get-Date).AddDays(-1) } else { Get-Date }
$targetDateText = $targetDate.ToString('yyyy-MM-dd')

Write-Host "Running harvest-regenerate for $targetDateText..."

& $exePath harvest-regenerate $LogSourceDirectory $targetDateText $OutputDatabasePath
exit $LASTEXITCODE
