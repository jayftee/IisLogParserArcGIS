<#
.SYNOPSIS
    Computes code coverage for the whole solution, on demand, and reports the riskiest methods.
.DESCRIPTION
    Runs the full test suite once with the coverlet collector (settings in coverage.runsettings), merges every test
    project's Cobertura file with reportgenerator, and writes an HTML report, a text summary, a JSON summary and a
    merged Cobertura file into -OutputDirectory (default: coverage/ at the repo root, which is git-ignored). Unless
    -SkipCrap is given, it then runs crap4dotnet over the merged coverage (every method is scored and written to
    crap-report.json) and prints how many methods are over the CRAP threshold, the -TopCrap highest-scoring methods
    (CRAP = complexity^2 * (1 - coverage)^3 + complexity, so the ones that are both complex and poorly tested), and
    every method that reports 0% coverage, since that can be a real gap or a method the collector could not map.

    This is deliberately not wired into build or test: coverage is only ever computed when this script is run. The
    Reports tests harvest the whole 2026 corpus, so a full run takes on the order of 10-15 minutes. The two local
    tools it uses (reportgenerator, crap4dotnet) come from .config/dotnet-tools.json and are restored on demand.

    Coverage of the console host comes only from IisLogParserArcGIS.Tests; the RegressionTests project runs the
    compiled executable as a separate process, which the collector cannot instrument.
.PARAMETER OutputDirectory
    Where to write the raw results and the report. Wiped at the start of every run.
.PARAMETER FailUnder
    Optional gate: exit non-zero when overall line coverage is below this percentage. 0 (the default) never fails.
.PARAMETER TopCrap
    How many of the highest-CRAP methods to print.
.PARAMETER SkipCrap
    Skip the CRAP analysis.
.PARAMETER ReportOnly
    Do not run the tests: rebuild the report and the CRAP analysis from the raw coverage an earlier full run left in
    -OutputDirectory. Takes seconds instead of ten minutes, for iterating on the report itself.
.EXAMPLE
    .\tools\Invoke-CodeCoverage.ps1
.EXAMPLE
    .\tools\Invoke-CodeCoverage.ps1 -FailUnder 80 -TopCrap 25
#>
[CmdletBinding()]
param(
    [string]$OutputDirectory,

    [double]$FailUnder = 0,

    [int]$TopCrap = 15,

    [switch]$SkipCrap,

    [switch]$ReportOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot 'coverage'
}

$rawDirectory = Join-Path $OutputDirectory 'raw'
$reportDirectory = Join-Path $OutputDirectory 'report'
$solution = Join-Path $repoRoot 'IisLogParserArcGIS.slnx'
$runSettings = Join-Path $repoRoot 'coverage.runsettings'

function Invoke-Native {
    param([string]$Description, [scriptblock]$Command, [int[]]$AllowedExitCodes = @(0))

    & $Command
    if ($LASTEXITCODE -notin $AllowedExitCodes) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

Push-Location $repoRoot
try {
    if ($ReportOnly) {
        if (-not (Test-Path -LiteralPath $rawDirectory)) {
            throw "-ReportOnly needs the coverage from an earlier full run, but '$rawDirectory' does not exist."
        }

        if (Test-Path -LiteralPath $reportDirectory) {
            Remove-Item -LiteralPath $reportDirectory -Recurse -Force
        }
    }
    elseif (Test-Path -LiteralPath $OutputDirectory) {
        Remove-Item -LiteralPath $OutputDirectory -Recurse -Force
    }

    Invoke-Native 'dotnet tool restore' { dotnet tool restore | Out-Host }

    if (-not $ReportOnly) {
        Write-Host 'Running the full test suite with coverage (this takes a while)...'
        Invoke-Native 'dotnet test' {
            dotnet test $solution --settings $runSettings '--collect:XPlat Code Coverage' --results-directory $rawDirectory | Out-Host
        }
    }

    Write-Host 'Merging coverage and generating the report...'
    Invoke-Native 'reportgenerator' {
        dotnet tool run reportgenerator "-reports:$rawDirectory/**/coverage.cobertura.xml" "-targetdir:$reportDirectory" '-reporttypes:Html;TextSummary;Cobertura;JsonSummary' | Out-Host
    }

    $mergedCobertura = Join-Path $reportDirectory 'Cobertura.xml'
    $summary = Get-Content -LiteralPath (Join-Path $reportDirectory 'Summary.json') -Raw | ConvertFrom-Json

    Write-Host ''
    Write-Host ('Overall: {0}% line, {1}% branch ({2}/{3} lines)' -f $summary.summary.linecoverage, $summary.summary.branchcoverage, $summary.summary.coveredlines, $summary.summary.coverablelines)
    $summary.coverage.assemblies |
        Sort-Object name |
        Select-Object @{ n = 'Assembly'; e = { $_.name } }, @{ n = 'Line %'; e = { $_.coverage } }, @{ n = 'Branch %'; e = { $_.branchcoverage } }, @{ n = 'Covered'; e = { $_.coveredlines } }, @{ n = 'Coverable'; e = { $_.coverablelines } } |
        Format-Table -AutoSize |
        Out-Host

    if (-not $SkipCrap) {
        $crapReport = Join-Path $OutputDirectory 'crap-report.json'

        # crap4dotnet takes a directory or .csproj/.sln, not a .slnx, and exits 1 (not an error) when any method is over
        # its threshold; 2 is a real failure. Its own table is discarded: the JSON is printed below instead.
        Invoke-Native 'dotnet-crap' {
            dotnet dotnet-crap analyze (Join-Path $repoRoot 'src') --coverage $mergedCobertura --output $crapReport | Out-Null
        } -AllowedExitCodes 0, 1

        $crap = Get-Content -LiteralPath $crapReport -Raw | ConvertFrom-Json
        $crappy = @($crap.methods | Where-Object { $_.isCrappy } | Sort-Object crap -Descending)
        $unmeasured = @($crap.methods | Where-Object { $_.coverage -eq 0 })

        Write-Host ''
        Write-Host ('CRAP: {0} methods, median {1}, {2} over the threshold of {3}.' -f $crap.stats.methodCount, $crap.stats.medianCrap, $crappy.Count, $crap.threshold)

        $crap.methods |
            Sort-Object crap -Descending |
            Select-Object -First $TopCrap |
            Select-Object @{ n = 'Method'; e = { ($_.fullName -replace '^IisLogParserArcGIS\.', '') -replace '\(.*$', '' } }, @{ n = 'CC'; e = { $_.complexity } }, @{ n = 'Cov %'; e = { [math]::Round($_.coverage * 100, 1) } }, @{ n = 'CRAP'; e = { [math]::Round($_.crap, 1) } }, @{ n = 'File'; e = { '{0}:{1}' -f $_.filePath, $_.lineNumber } } |
            Format-Table -AutoSize |
            Out-String -Width 250 |
            Write-Host

        if ($unmeasured.Count -gt 0) {
            Write-Host ('{0} method(s) show 0% coverage; check they are real gaps and not methods the collector could not map (top-level statements, async or iterator state machines):' -f $unmeasured.Count)
            $unmeasured | ForEach-Object { Write-Host ('  {0}  ({1}:{2})' -f $_.fullName, $_.filePath, $_.lineNumber) }
        }

        Write-Host "CRAP report: $crapReport"
    }

    Write-Host "HTML report: $(Join-Path $reportDirectory 'index.html')"

    if ($FailUnder -gt 0 -and $summary.summary.linecoverage -lt $FailUnder) {
        Write-Error ('Line coverage {0}% is below the required {1}%.' -f $summary.summary.linecoverage, $FailUnder)
        exit 1
    }

    # Otherwise the script would inherit crap4dotnet's "over the threshold" exit code (1) from its last native call.
    exit 0
}
finally {
    Pop-Location
}
