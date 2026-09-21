using System.Diagnostics;
using System.Globalization;
using IisLogParserArcGIS.Configuration;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Replacement;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.FileDiscovery;
using IisLogParserArcGIS.Logging;
using IisLogParserArcGIS.Parsing;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Cli;

/// <summary>
/// Runs the <c>harvest</c> and <c>harvest-regenerate</c> verbs: validates the arguments, reads configuration and
/// builds logging, opens the aggregate database, then discovers, parses and aggregates one local date's logs,
/// replaces that date's rows, reports the run summary, and - for <c>harvest-regenerate</c> - regenerates the
/// Dashboard on the same open connection.
/// </summary>
public sealed class HarvestRunner
{
    private const string InitializationFailureMessagePrefix = "Failed to initialize configuration, logging, or the output database";
    private const string HarvestFailureMessagePrefix = "Failed while harvesting the log files into the output database";

    private readonly RunEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="HarvestRunner"/> class.
    /// </summary>
    /// <param name="environment">The process environment this run reads from and reports to.</param>
    public HarvestRunner(RunEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        _environment = environment;
    }

    private enum PostHarvestStep
    {
        Nothing,
        RegenerateDashboard,
    }

    /// <summary>
    /// Runs the <c>harvest</c> verb: one Harvest Run, leaving the Dashboard untouched.
    /// </summary>
    /// <param name="arguments">The raw arguments parsed from the command line.</param>
    /// <returns><c>0</c> on success; <c>1</c> after writing the failure message to the error writer.</returns>
    public int Harvest(IHarvestArguments arguments)
    {
        return Execute(arguments, PostHarvestStep.Nothing);
    }

    /// <summary>
    /// Runs the <c>harvest-regenerate</c> verb: one Harvest Run followed by a Regeneration Run on the same open
    /// connection.
    /// </summary>
    /// <param name="arguments">The raw arguments parsed from the command line.</param>
    /// <returns><c>0</c> on success; <c>1</c> after writing the failure message to the error writer.</returns>
    public int HarvestAndRegenerate(IHarvestArguments arguments)
    {
        return Execute(arguments, PostHarvestStep.RegenerateDashboard);
    }

#pragma warning disable CC0034 // One straight-line pipeline whose steps share too many locals to split without parameter objects.
    private int Execute(IHarvestArguments arguments, PostHarvestStep postHarvestStep)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        ParsedArguments parsedArguments;
        try
        {
            parsedArguments = CliArgumentValidator.Validate(arguments);
        }
        catch (CliArgumentValidationException ex)
        {
            _environment.Error.WriteLine(ex.Message);
            return 1;
        }

        var failureMessagePrefix = InitializationFailureMessagePrefix;
        try
        {
            var configuration = AppConfigurationFactory.Build(_environment.BaseDirectory, _environment.EnvironmentName);
            var appSettings = AppConfigurationFactory.BindAppSettings(configuration);

            using var loggerFactory = SerilogLoggerFactoryBuilder.Create(appSettings, _environment.BaseDirectory);

            var startupAnnouncer = new StartupAnnouncer(loggerFactory);
            startupAnnouncer.AnnounceStartup(parsedArguments);

            TimeSpan localUtcOffset;
            try
            {
                localUtcOffset = LocalUtcOffsetResolver.Resolve(appSettings.LocalTimeZone, parsedArguments.TargetLocalDate);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                _environment.Error.WriteLine($"Invalid configured local time zone '{appSettings.LocalTimeZone}': {ex.Message}");
                return 1;
            }

            using var connection = SqliteConnectionFactory.Open(parsedArguments.OutputDatabasePath);
            AggregateDatabaseSchema.EnsureCreated(connection);

            failureMessagePrefix = HarvestFailureMessagePrefix;
            var stopwatch = Stopwatch.StartNew();

            var logFileLocator = new LogFileLocator(loggerFactory);
            var discoveredLogFiles = logFileLocator.Discover(parsedArguments.LogSourceDirectory, parsedArguments.TargetLocalDate, localUtcOffset);

            var logCorpusParser = new LogCorpusParser(loggerFactory);
            var parseResult = logCorpusParser.Parse(discoveredLogFiles, localUtcOffset);

            if (parseResult.SkippedFileCount > 0)
            {
                var targetDate = parsedArguments.TargetLocalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                _environment.Error.WriteLine(
                    $"Refusing to replace {targetDate}: {parseResult.SkippedFileCount} of {discoveredLogFiles.Count} log file(s) were skipped " +
                    "(unreadable, or missing a required field), so the data would be incomplete. " +
                    "The existing data for that date was left untouched; see the log for the reason each file was skipped.");
                return 1;
            }

            var byFieldMapsDevice = ByFieldMapsDeviceAggregator.Aggregate(parseResult.Requests, parsedArguments.TargetLocalDate, appSettings.PortalWebAdaptorName);
            var bySurvey123Device = BySurvey123DeviceAggregator.Aggregate(parseResult.Requests, parsedArguments.TargetLocalDate, appSettings.PortalWebAdaptorName);
            var dailyAggregateBatch = new DailyAggregateBatch
            {
                LocalDate = parsedArguments.TargetLocalDate,
                ByUri = ByUriAggregator.Aggregate(parseResult.Requests, parsedArguments.TargetLocalDate),
                ByRoot = ByRootAggregator.Aggregate(parseResult.Requests, parsedArguments.TargetLocalDate),
                ByUserAgent = ByUserAgentAggregator.Aggregate(parseResult.Requests, parsedArguments.TargetLocalDate),
                ByReferer = ByRefererAggregator.Aggregate(parseResult.Requests, parsedArguments.TargetLocalDate),
                ByForwardedForIp = ByForwardedForIpAggregator.Aggregate(parseResult.Requests, parsedArguments.TargetLocalDate),
                ByRefererAndUri = ByRefererAndUriAggregator.Aggregate(parseResult.Requests, parsedArguments.TargetLocalDate),
                ByArcGisService = ByArcGisServiceAggregator.Aggregate(parseResult.Requests, parsedArguments.TargetLocalDate),
                ByPortalItem = ByPortalItemAggregator.Aggregate(parseResult.Requests, parsedArguments.TargetLocalDate, appSettings.PortalWebAdaptorName),
                ByFieldMapsDevice = byFieldMapsDevice,
                BySurvey123Device = bySurvey123Device,
            };

            DailyAggregateReplacer.Replace(connection, dailyAggregateBatch);

            stopwatch.Stop();

            var runSummaryReporter = new RunSummaryReporter(loggerFactory);
            runSummaryReporter.Report(parseResult.Counts, stopwatch.Elapsed);
            var persistedFieldMapsRows = new ByFieldMapsDeviceRepository().GetByLocalDate(connection, parsedArguments.TargetLocalDate).ToArray();
            runSummaryReporter.ReportFieldMapsAttribution(DeviceAttributionCounts.Create(byFieldMapsDevice, persistedFieldMapsRows));
            var persistedSurvey123Rows = new BySurvey123DeviceRepository().GetByLocalDate(connection, parsedArguments.TargetLocalDate).ToArray();
            runSummaryReporter.ReportSurvey123Attribution(DeviceAttributionCounts.Create(bySurvey123Device, persistedSurvey123Rows));

            return postHarvestStep == PostHarvestStep.RegenerateDashboard
                ? new DashboardRegenerator(_environment).Regenerate(connection, configuration, parsedArguments.LogSourceDirectory)
                : 0;
        }
        catch (Exception ex) when (ex is FileNotFoundException or FormatException or IOException or UnauthorizedAccessException or SqliteException)
        {
            _environment.Error.WriteLine($"{failureMessagePrefix}: {ex.Message}");
            return 1;
        }
        catch (NoLogFilesFoundException ex)
        {
            _environment.Error.WriteLine(ex.Message);
            return 1;
        }
    }
#pragma warning restore CC0034
}
