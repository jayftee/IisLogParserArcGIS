<#
.SYNOPSIS
    Runs IisLogParserArcGIS.exe's harvest verb once per Local Date of one year, then regenerates that
    year's Dashboard once at the end.
.DESCRIPTION
    The harvest verb processes exactly one Local Date per invocation. This script walks every Local
    Date of -Year - January 1 through December 31, or through today when -Year is the current year,
    since a future date has no logs yet - and invokes `harvest` once per date, in sequence, against
    the same -OutputDatabasePath. Once every date has been attempted, it invokes `regenerate` exactly
    once for -Year into -OutputDirectory - unconditionally, regardless of how many dates succeeded or
    failed - so a whole year regenerates the Dashboard once instead of once per date.

    Dates come from the calendar, never from the log files' names: each date is a Local Date handed
    to `harvest`, which itself works out (from the configured LocalTimeZone) which UTC-dated files that
    Local Date needs, including the adjacent day's. So this works whatever LocalTimeZone is, and
    whether -LogSourceDirectory holds one year of logs or many - only -Year's dates are attempted.

    A date whose invocation fails is recorded and the run continues with the next date. A date with no
    log files at all is such a failure: in a layout that keeps one folder per year, December 31 needs
    the next year's first file and will be reported as failed. That is expected, not a bug.

    Every harvest invocation writes to the same shared SQLite output file, so runs are always
    sequential - never parallel - to avoid concurrent-writer lock contention. The script's own exit
    code is non-zero if any date, or the final regenerate call, failed.
.PARAMETER LogSourceDirectory
    Directory containing the IIS W3C log files to process.
.PARAMETER OutputDatabasePath
    Path to the output SQLite database file. Shared across every date processed by this run.
.PARAMETER OutputDirectory
    Directory the Dashboard is generated into. It must be new, empty, or hold a Dashboard a previous run
    generated (it carries a .iis-dashboard marker); any other non-empty directory is refused.
.PARAMETER Year
    The four-digit calendar year to harvest and report on, e.g. 2026.
.EXAMPLE
    .\Invoke-IisLogBackfill.ps1 -LogSourceDirectory "D:\IISLogs\2026" -OutputDatabasePath "D:\Aggregates\Iis2026.sqlite" -OutputDirectory "D:\Dashboards\2026" -Year 2026
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
    [ValidateRange(1000, 9999)]
    [int]$Year
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

$firstDate = [datetime]::new($Year, 1, 1)
$lastDate = [datetime]::new($Year, 12, 31)
$today = (Get-Date).Date

if ($firstDate -gt $today) {
    Write-Error "Year $Year has not started yet; there are no logs to harvest."
    exit 1
}

if ($lastDate -gt $today) {
    $lastDate = $today
}

$dates = New-Object 'System.Collections.Generic.List[string]'
for ($date = $firstDate; $date -le $lastDate; $date = $date.AddDays(1)) {
    $dates.Add($date.ToString('yyyy-MM-dd', [System.Globalization.CultureInfo]::InvariantCulture))
}

$total = $dates.Count
$index = 0
$succeeded = New-Object 'System.Collections.Generic.List[string]'
$failed = New-Object 'System.Collections.Generic.List[string]'

foreach ($date in $dates) {
    $index++
    Write-Host "[$index/$total] Harvesting $date..."

    & $exePath harvest $LogSourceDirectory $date $OutputDatabasePath
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        $succeeded.Add($date)
    }
    else {
        Write-Warning "Date $date failed (exit code $exitCode)."
        $failed.Add($date)
    }
}

Write-Host ''
Write-Host "Harvest summary: $($succeeded.Count) succeeded, $($failed.Count) failed."

if ($failed.Count -gt 0) {
    Write-Host "Failed dates: $($failed -join ', ')"
}

Write-Host ''
Write-Host "Regenerating the $Year Dashboard..."

& $exePath regenerate $OutputDatabasePath $OutputDirectory --year $Year
$regenerateExitCode = $LASTEXITCODE
$regenerateFailed = $regenerateExitCode -ne 0

if ($regenerateFailed) {
    Write-Warning "Dashboard regeneration failed (exit code $regenerateExitCode)."
}

if ($failed.Count -gt 0 -or $regenerateFailed) {
    exit 1
}

exit 0
