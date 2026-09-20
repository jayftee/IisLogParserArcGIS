using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Repositories;

/// <summary>
/// CRUD for the <see cref="AggregateTableNames.ByUri"/> table, per the pattern established by
/// <see cref="AggregateRepositoryBase{TRow}"/>.
/// </summary>
public sealed class ByUriRepository : AggregateRepositoryBase<ByUriAggregateRow>
{
    /// <inheritdoc/>
    protected override string InsertSql =>
        $"""
        INSERT INTO {AggregateTableNames.ByUri} (local_date, uri_stem, time_taken_second, hits)
        VALUES (@LocalDate, @UriStem, @TimeTakenSecond, @Hits)
        """;

    /// <inheritdoc/>
    protected override string DeleteByLocalDateSql =>
        $"DELETE FROM {AggregateTableNames.ByUri} WHERE local_date = @LocalDate";

    /// <inheritdoc/>
    protected override string SelectByLocalDateSql =>
        $"""
        SELECT local_date AS LocalDate, uri_stem AS UriStem, time_taken_second AS TimeTakenSecond, hits AS Hits
        FROM {AggregateTableNames.ByUri}
        WHERE local_date = @LocalDate
        """;
}
