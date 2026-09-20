<#
.SYNOPSIS
    Runs IisLogParserArcGIS.exe's harvest verb once per UTC calendar date found in a log source
    directory, then regenerates the Dashboard once at the end.
.DESCRIPTION
    The harvest verb processes exactly one local date per invocation. This script scans
    -LogSourceDirectory for files named u_ex<YYMMDD>*.log (the same opaque-wildcard glob the CLI
    itself uses - it never inspects or branches on anything after the date), derives the distinct
    set of UTC dates from those file names, and invokes `harvest` once per date, in sequence,
    against the same -OutputDatabasePath. Once every date has been attempted, it invokes
    `regenerate` exactly once against -OutputDatabasePath - unconditionally, regardless of how many
    dates succeeded or failed - so a full backlog regenerates the Dashboard once instead of once per
    date.

    A file's own embedded UTC date is passed straight through as harvest's target local date, with
    no time-zone adjustment. This is only correct when the configured LocalTimeZone is UTC, and even
    then, lines that cross midnight in the very first or last physical file found are not attributed
    to any run. This is an accepted characteristic of this script, not a bug.

    Every harvest invocation writes to the same shared SQLite output file, so runs are always
    sequential - never parallel - to avoid concurrent-writer lock contention. A date whose invocation
    fails is recorded and the run continues with the next date; the script's own exit code is
    non-zero if any date, or the final regenerate call, failed.
.PARAMETER LogSourceDirectory
    Directory containing the IIS W3C log files to process.
.PARAMETER OutputDatabasePath
    Path to the output SQLite database file. Shared across every date processed by this run.
.EXAMPLE
    .\Invoke-IisLogBackfill.ps1 -LogSourceDirectory "D:\IISLogs" -OutputDatabasePath "D:\Aggregates\usage.sqlite"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$LogSourceDirectory,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$OutputDatabasePath
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

$logFiles = Get-ChildItem -LiteralPath $LogSourceDirectory -File
$datePattern = '^u_ex(\d{6}).*\.log$'
$dates = New-Object 'System.Collections.Generic.SortedSet[string]'

foreach ($file in $logFiles) {
    $match = [regex]::Match($file.Name, $datePattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) {
        continue
    }

    $rawDate = $match.Groups[1].Value
    $parsedDate = New-Object DateTime
    $isValidDate = [datetime]::TryParseExact(
        "20$rawDate",
        'yyyyMMdd',
        [System.Globalization.CultureInfo]::InvariantCulture,
        [System.Globalization.DateTimeStyles]::None,
        [ref]$parsedDate)

    if (-not $isValidDate) {
        Write-Warning "Skipping '$($file.Name)': '$rawDate' is not a valid date."
        continue
    }

    [void]$dates.Add($parsedDate.ToString('yyyy-MM-dd'))
}

if ($dates.Count -eq 0) {
    Write-Error "No files matching 'u_ex<YYMMDD>*.log' were found in '$LogSourceDirectory'."
    exit 1
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
Write-Host 'Regenerating the Dashboard...'

& $exePath regenerate $OutputDatabasePath
$regenerateExitCode = $LASTEXITCODE
$regenerateFailed = $regenerateExitCode -ne 0

if ($regenerateFailed) {
    Write-Warning "Dashboard regeneration failed (exit code $regenerateExitCode)."
}

if ($failed.Count -gt 0 -or $regenerateFailed) {
    exit 1
}

exit 0
