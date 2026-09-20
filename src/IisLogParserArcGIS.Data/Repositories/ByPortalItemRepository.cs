using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Repositories;

/// <summary>
/// CRUD for the <see cref="AggregateTableNames.ByPortalItem"/> table, per the pattern established by
/// <see cref="AggregateRepositoryBase{TRow}"/>.
/// </summary>
public sealed class ByPortalItemRepository : AggregateRepositoryBase<ByPortalItemAggregateRow>
{
    /// <inheritdoc/>
    protected override string InsertSql =>
        $"""
        INSERT INTO {AggregateTableNames.ByPortalItem}
            (local_date, portal_item_id, time_taken_second, hits, successful_hits, failed_hits)
        VALUES
            (@LocalDate, @PortalItemId, @TimeTakenSecond, @Hits, @SuccessfulHits, @FailedHits)
        """;

    /// <inheritdoc/>
    protected override string DeleteByLocalDateSql =>
        $"DELETE FROM {AggregateTableNames.ByPortalItem} WHERE local_date = @LocalDate";

    /// <inheritdoc/>
    protected override string SelectByLocalDateSql =>
        $"""
        SELECT local_date AS LocalDate, portal_item_id AS PortalItemId, time_taken_second AS TimeTakenSecond,
            hits AS Hits, successful_hits AS SuccessfulHits, failed_hits AS FailedHits
        FROM {AggregateTableNames.ByPortalItem}
        WHERE local_date = @LocalDate
        """;
}
