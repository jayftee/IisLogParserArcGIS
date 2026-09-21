using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Domain.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IisLogParserArcGIS.Logging;

/// <summary>
/// Reports the end-of-run summary an operator uses to sanity-check a run at a glance: total lines read,
/// valid/invalid line counts, and elapsed processing time.
/// </summary>
public sealed class RunSummaryReporter
{
    private readonly ILogger<RunSummaryReporter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunSummaryReporter"/> class.
    /// </summary>
    /// <param name="loggerFactory">
    /// The factory to create the logger from. When omitted, logging is a no-op (see ADR 0003).
    /// </param>
    public RunSummaryReporter(ILoggerFactory? loggerFactory = null)
    {
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<RunSummaryReporter>();
    }

    /// <summary>
    /// Logs the summary for a completed run at <see cref="LogLevel.Warning"/> - the application's default
    /// minimum level - so it's always visible on both the console and the rolling log file.
    /// </summary>
    /// <param name="counts">The combined data-line counts across the whole run.</param>
    /// <param name="elapsed">The elapsed processing time for the run.</param>
    public void Report(LogLineCounts counts, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(counts);

        _logger.LogWarning(
            "Run summary: {TotalLines} line(s) read, {ValidLines} valid, {InvalidLines} invalid/skipped, elapsed {ElapsedSeconds:F2}s.",
            counts.Total,
            counts.Valid,
            counts.Invalid,
            elapsed.TotalSeconds);
    }

    /// <summary>
    /// Logs that the by-referer-and-URI aggregate was not computed because <c>ComputeByRefererAndUri</c> is off, at
    /// <see cref="LogLevel.Warning"/> like the rest of the run summary, so an empty table is explainable from the
    /// log file even at the default minimum level.
    /// </summary>
    public void ReportByRefererAndUriSkipped()
    {
        _logger.LogWarning("By-referer-and-URI aggregate not computed: ComputeByRefererAndUri is off, so that table was left untouched.");
    }

    /// <summary>
    /// Logs how the harvested date's Field Maps devices were attributed to a username at
    /// <see cref="LogLevel.Warning"/>, so the (often large) unattributed share is visible rather than silent.
    /// </summary>
    /// <param name="counts">The attribution counts for the harvested date.</param>
    public void ReportFieldMapsAttribution(DeviceAttributionCounts counts)
    {
        ArgumentNullException.ThrowIfNull(counts);

        _logger.LogWarning(
            "Field Maps attribution: {DevicesSeen} device(s) seen, {AttributedSameDay} same-day login, {AttributedByBackfill} back-filled from another date, {Unattributed} unattributed.",
            counts.DevicesSeen,
            counts.AttributedSameDay,
            counts.AttributedByBackfill,
            counts.Unattributed);
    }

    /// <summary>
    /// Logs how the harvested date's Survey123 devices were attributed to a username at
    /// <see cref="LogLevel.Warning"/>, in the same shape as <see cref="ReportFieldMapsAttribution"/>.
    /// </summary>
    /// <param name="counts">The attribution counts for the harvested date.</param>
    public void ReportSurvey123Attribution(DeviceAttributionCounts counts)
    {
        ArgumentNullException.ThrowIfNull(counts);

        _logger.LogWarning(
            "Survey123 attribution: {DevicesSeen} device(s) seen, {AttributedSameDay} same-day login, {AttributedByBackfill} back-filled from another date, {Unattributed} unattributed.",
            counts.DevicesSeen,
            counts.AttributedSameDay,
            counts.AttributedByBackfill,
            counts.Unattributed);
    }
}
