using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Repositories;

/// <summary>
/// CRUD for the <see cref="AggregateTableNames.ByArcGisService"/> table, per the pattern established by
/// <see cref="AggregateRepositoryBase{TRow}"/>.
/// </summary>
public sealed class ByArcGisServiceRepository : AggregateRepositoryBase<ByArcGisServiceAggregateRow>
{
    /// <inheritdoc/>
    protected override string InsertSql =>
        $"""
        INSERT INTO {AggregateTableNames.ByArcGisService}
            (local_date, site, folder, service_name, service_type, successful_time_taken_second, failed_time_taken_second, hits, successful_hits, failed_hits)
        VALUES
            (@LocalDate, @Site, @Folder, @ServiceName, @ServiceType, @SuccessfulTimeTakenSecond, @FailedTimeTakenSecond, @Hits, @SuccessfulHits, @FailedHits)
        """;

    /// <inheritdoc/>
    protected override string DeleteByLocalDateSql =>
        $"DELETE FROM {AggregateTableNames.ByArcGisService} WHERE local_date = @LocalDate";

    /// <inheritdoc/>
    protected override string SelectByLocalDateSql =>
        $"""
        SELECT local_date AS LocalDate, site AS Site, folder AS Folder, service_name AS ServiceName,
            service_type AS ServiceType, successful_time_taken_second AS SuccessfulTimeTakenSecond,
            failed_time_taken_second AS FailedTimeTakenSecond, hits AS Hits,
            successful_hits AS SuccessfulHits, failed_hits AS FailedHits
        FROM {AggregateTableNames.ByArcGisService}
        WHERE local_date = @LocalDate
        """;
}
