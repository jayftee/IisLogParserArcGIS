using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Repositories;

/// <summary>
/// CRUD for the <see cref="AggregateTableNames.ByRoot"/> table, per the pattern established by
/// <see cref="AggregateRepositoryBase{TRow}"/>.
/// </summary>
public sealed class ByRootRepository : AggregateRepositoryBase<ByRootAggregateRow>
{
    /// <inheritdoc/>
    protected override string InsertSql =>
        $"""
        INSERT INTO {AggregateTableNames.ByRoot} (local_date, root, time_taken_second, hits)
        VALUES (@LocalDate, @Root, @TimeTakenSecond, @Hits)
        """;

    /// <inheritdoc/>
    protected override string DeleteByLocalDateSql =>
        $"DELETE FROM {AggregateTableNames.ByRoot} WHERE local_date = @LocalDate";

    /// <inheritdoc/>
    protected override string SelectByLocalDateSql =>
        $"""
        SELECT local_date AS LocalDate, root AS Root, time_taken_second AS TimeTakenSecond, hits AS Hits
        FROM {AggregateTableNames.ByRoot}
        WHERE local_date = @LocalDate
        """;
}
