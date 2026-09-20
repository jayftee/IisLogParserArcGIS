using System.Diagnostics;
using CommandLine;
using IisLogParserArcGIS.Cli;
using IisLogParserArcGIS.Configuration;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Replacement;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.FileDiscovery;
using IisLogParserArcGIS.Logging;
using IisLogParserArcGIS.Parsing;
using IisLogParserArcGIS.Reports;
using IisLogParserArcGIS.Reports.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

using var parser = new Parser(settings => settings.HelpWriter = Console.Error);
var parserResult = parser.ParseArguments<HarvestArguments, RegenerateArguments, HarvestRegenerateArguments>(args);

return parserResult.MapResult(
    (HarvestArguments arguments) => RunHarvest(arguments, alsoRegenerate: false),
    (RegenerateArguments arguments) => RunRegenerate(arguments),
    (HarvestRegenerateArguments arguments) => RunHarvest(arguments, alsoRegenerate: true),
    _ => 1);

static int RunHarvest(IHarvestArguments programArguments, bool alsoRegenerate)
{
    ParsedArguments parsedArguments;
    try
    {
        parsedArguments = CliArgumentValidator.Validate(programArguments);
    }
    catch (CliArgumentValidationException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }

    try
    {
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var configuration = AppConfigurationFactory.Build(AppContext.BaseDirectory, environmentName);
        var appSettings = AppConfigurationFactory.BindAppSettings(configuration);

        using var loggerFactory = SerilogLoggerFactoryBuilder.Create(appSettings, AppContext.BaseDirectory);

        var startupAnnouncer = new StartupAnnouncer(loggerFactory);
        startupAnnouncer.AnnounceStartup(parsedArguments);

        using var connection = SqliteConnectionFactory.Open(parsedArguments.OutputDatabasePath);
        AggregateDatabaseSchema.EnsureCreated(connection);

        TimeSpan localUtcOffset;
        try
        {
            localUtcOffset = LocalUtcOffsetResolver.Resolve(appSettings.LocalTimeZone, parsedArguments.TargetLocalDate);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            Console.Error.WriteLine($"Invalid configured local time zone '{appSettings.LocalTimeZone}': {ex.Message}");
            return 1;
        }

        var stopwatch = Stopwatch.StartNew();

        var logFileLocator = new LogFileLocator(loggerFactory);
        var discoveredLogFiles = logFileLocator.Discover(parsedArguments.LogSourceDirectory, parsedArguments.TargetLocalDate, localUtcOffset);

        var logCorpusParser = new LogCorpusParser(loggerFactory);
        var parseResult = logCorpusParser.Parse(discoveredLogFiles, localUtcOffset);

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

        return alsoRegenerate ? RegenerateDashboard(connection, configuration) : 0;
    }
    catch (Exception ex) when (ex is FileNotFoundException or FormatException or IOException or UnauthorizedAccessException or SqliteException)
    {
        Console.Error.WriteLine($"Failed to initialize configuration, logging, or the output database: {ex.Message}");
        return 1;
    }
    catch (NoLogFilesFoundException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

static int RunRegenerate(RegenerateArguments programArguments)
{
    ParsedRegenerateArguments parsedArguments;
    try
    {
        parsedArguments = CliArgumentValidator.Validate(programArguments);
    }
    catch (CliArgumentValidationException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }

    try
    {
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var configuration = AppConfigurationFactory.Build(AppContext.BaseDirectory, environmentName);

        using var connection = SqliteConnectionFactory.Open(parsedArguments.InputDatabasePath);
        AggregateDatabaseSchema.EnsureCreated(connection);

        return RegenerateDashboard(connection, configuration);
    }
    catch (Exception ex) when (ex is FileNotFoundException or FormatException or IOException or UnauthorizedAccessException or SqliteException)
    {
        Console.Error.WriteLine($"Failed to initialize configuration or the input database: {ex.Message}");
        return 1;
    }
}

static int RegenerateDashboard(SqliteConnection connection, IConfiguration configuration)
{
    try
    {
        var reportsSettings = ReportsConfigurationFactory.BindReportsSettings(configuration);
        var reportsOutputDirectory = ResolveReportsOutputDirectory(reportsSettings.OutputDirectory, AppContext.BaseDirectory);

        RegenerationRun.Run(connection, reportsSettings, TimeProvider.System, reportsOutputDirectory);
        return 0;
    }
    catch (Exception ex) when (ex is FileNotFoundException or FormatException or IOException or UnauthorizedAccessException
        or TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException or SqliteException)
    {
        Console.Error.WriteLine($"Failed to regenerate the Dashboard: {ex.Message}");
        return 1;
    }
}

static string ResolveReportsOutputDirectory(string outputDirectory, string baseDirectory)
{
    var resolved = RelativePathResolver.Resolve(outputDirectory, baseDirectory);
    var fullResolved = Path.GetFullPath(resolved);
    var fullBaseDirectory = Path.GetFullPath(baseDirectory);

    if (string.Equals(fullResolved, fullBaseDirectory, StringComparison.OrdinalIgnoreCase))
    {
        throw new ArgumentException(
            $"Refusing to use '{fullResolved}' as the Dashboard output directory: it resolves to the executable's own directory, which this run would delete recursively.",
            nameof(outputDirectory));
    }

    return resolved;
}
