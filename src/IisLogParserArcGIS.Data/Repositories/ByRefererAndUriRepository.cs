using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Repositories;

/// <summary>
/// CRUD for the <see cref="AggregateTableNames.ByRefererAndUri"/> table, per the pattern established by
/// <see cref="AggregateRepositoryBase{TRow}"/>.
/// </summary>
public sealed class ByRefererAndUriRepository : AggregateRepositoryBase<ByRefererAndUriAggregateRow>
{
    /// <inheritdoc/>
    protected override string InsertSql =>
        $"""
        INSERT INTO {AggregateTableNames.ByRefererAndUri} (local_date, referer, uri_stem, time_taken_second, hits)
        VALUES (@LocalDate, @Referer, @UriStem, @TimeTakenSecond, @Hits)
        """;

    /// <inheritdoc/>
    protected override string DeleteByLocalDateSql =>
        $"DELETE FROM {AggregateTableNames.ByRefererAndUri} WHERE local_date = @LocalDate";

    /// <inheritdoc/>
    protected override string SelectByLocalDateSql =>
        $"""
        SELECT local_date AS LocalDate, referer AS Referer, uri_stem AS UriStem, time_taken_second AS TimeTakenSecond, hits AS Hits
        FROM {AggregateTableNames.ByRefererAndUri}
        WHERE local_date = @LocalDate
        """;
}
